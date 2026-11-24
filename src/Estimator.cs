using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;

namespace BatteryWidget
{
    /// <summary>
    /// Estimates the battery of a Bluetooth speaker that does not report it (opt-in: list its name under "PinnedBluetooth" in config.json).
    /// Drain model: only while connected, scaled by whether audio is actually playing (Core Audio peak meter)
    /// and by the Windows volume of that endpoint. The user marks "charged" and can calibrate the real autonomy.
    /// </summary>
    public class Estimator
    {
        public static readonly Estimator Instance = new Estimator();
        static readonly string StateFile = Path.Combine(Config.Dir, "estimate.json");

        // reference: 13 h at 50% volume (manufacturer figure) => 100/13 %/h
        const double IdleFactor = 0.30;   // connected, nothing playing (BT link + amp idle)
        const double MaxGapSec = 300;     // ignore long gaps (sleep / app paused)

        readonly object lk = new object();
        double level = 100;               // %
        double lifeHours = 13;            // autonomy at 50% volume, playing
        double consumedSinceFull = 0;     // modelled % consumed since last "100%" mark
        bool fromFull = true;             // cycle started from a real full charge
        DateTime last = DateTime.MinValue;
        public string LastDebug = "";

        Estimator() { Load(); }

        public double Level { get { lock (lk) return level; } }
        public double LifeHours { get { lock (lk) return lifeHours; } }

        /// <summary>Called periodically while the speaker is connected. Returns the estimated percentage.</summary>
        public int Tick(bool connected, bool playing, double volume)
        {
            lock (lk)
            {
                var now = DateTime.UtcNow;
                double dt = last == DateTime.MinValue ? 0 : (now - last).TotalSeconds;
                last = now;
                if (connected && dt > 0 && dt <= MaxGapSec)
                {
                    double basePerHour = 100.0 / lifeHours;                       // at 50% volume, playing
                    double load = playing ? (0.4 + 1.2 * Math.Max(0, Math.Min(1, volume))) : IdleFactor;
                    double drain = basePerHour * load * dt / 3600.0;
                    level = Math.Max(0, level - drain);
                    consumedSinceFull += drain;
                    LastDebug = "playing=" + playing + " vol=" + volume.ToString("0.00") + " drain=" + drain.ToString("0.000");
                }
                Save();
                return (int)Math.Round(Math.Max(1, level));
            }
        }

        public void MarkFull() { lock (lk) { level = 100; consumedSinceFull = 0; fromFull = true; Save(); } }

        public void SetLevel(int pct) { lock (lk) { level = pct; consumedSinceFull = 0; fromFull = false; Save(); } }

        /// <summary>User says the speaker just ran out of battery: learn the real autonomy.</summary>
        public void MarkEmpty()
        {
            lock (lk)
            {
                if (fromFull && consumedSinceFull > 20)
                {
                    // the model consumed X% while the real battery went 100% -> 0%
                    double newLife = lifeHours * (consumedSinceFull / 100.0);
                    lifeHours = Math.Max(5, Math.Min(30, 0.5 * lifeHours + 0.5 * newLife));
                }
                level = 0; consumedSinceFull = 0; fromFull = false; Save();
            }
        }

        void Load()
        {
            try
            {
                if (!File.Exists(StateFile)) return;
                var d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(StateFile)) as Dictionary<string, object>;
                if (d == null) return;
                if (d.ContainsKey("level")) level = Convert.ToDouble(d["level"]);
                if (d.ContainsKey("life")) lifeHours = Convert.ToDouble(d["life"]);
                if (d.ContainsKey("consumed")) consumedSinceFull = Convert.ToDouble(d["consumed"]);
                if (d.ContainsKey("fromFull")) fromFull = Convert.ToBoolean(d["fromFull"]);
            }
            catch { }
        }

        int saveCount;
        void Save()
        {
            if (++saveCount % 4 != 1 && saveCount > 1) return; // not every tick: keep disk writes rare
            try
            {
                Directory.CreateDirectory(Config.Dir);
                File.WriteAllText(StateFile, new JavaScriptSerializer().Serialize(new Dictionary<string, object> {
                    { "level", level }, { "life", lifeHours }, { "consumed", consumedSinceFull }, { "fromFull", fromFull } }));
            }
            catch { }
        }

        public void Flush() { lock (lk) { saveCount = 0; Save(); } }
    }

    /// <summary>Minimal Core Audio access: find a render endpoint by name, read volume and a peak sample.</summary>
    public static class AudioProbe
    {
        [StructLayout(LayoutKind.Sequential)] struct PROPERTYKEY { public Guid fmtid; public uint pid; }
        [StructLayout(LayoutKind.Explicit, Size = 24)] struct PROPVARIANT { [FieldOffset(0)] public ushort vt; [FieldOffset(8)] public IntPtr p; }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceEnumerator
        {
            [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
        }
        [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceCollection
        {
            [PreserveSig] int GetCount(out uint n);
            [PreserveSig] int Item(uint i, out IMMDevice dev);
        }
        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDevice
        {
            [PreserveSig] int Activate(ref Guid iid, int ctx, IntPtr p, [MarshalAs(UnmanagedType.IUnknown)] out object o);
            [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore ps);
        }
        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IPropertyStore
        {
            [PreserveSig] int GetCount(out uint c);
            [PreserveSig] int GetAt(uint i, out PROPERTYKEY k);
            [PreserveSig] int GetValue(ref PROPERTYKEY k, out PROPVARIANT v);
        }
        [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAudioEndpointVolume
        {
            [PreserveSig] int RegisterControlChangeNotify(IntPtr p);
            [PreserveSig] int UnregisterControlChangeNotify(IntPtr p);
            [PreserveSig] int GetChannelCount(out uint c);
            [PreserveSig] int SetMasterVolumeLevel(float l, ref Guid ctx);
            [PreserveSig] int SetMasterVolumeLevelScalar(float l, ref Guid ctx);
            [PreserveSig] int GetMasterVolumeLevel(out float l);
            [PreserveSig] int GetMasterVolumeLevelScalar(out float l);
        }
        [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAudioMeterInformation
        {
            [PreserveSig] int GetPeakValue(out float p);
        }
        [DllImport("ole32.dll")] static extern int PropVariantClear(ref PROPVARIANT pv);

        static readonly Guid CLSID_Enum = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
        static readonly Guid IID_Vol = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
        static readonly Guid IID_Meter = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");

        /// <summary>Returns false if no matching active render endpoint exists.</summary>
        public static bool Read(string[] nameParts, out double volume, out bool playing)
        {
            volume = 0.5; playing = false;
            IMMDeviceEnumerator en = null; IMMDeviceCollection col = null;
            try
            {
                en = (IMMDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_Enum));
                if (en.EnumAudioEndpoints(0, 1, out col) != 0 || col == null) return false;
                uint n; col.GetCount(out n);
                for (uint i = 0; i < n; i++)
                {
                    IMMDevice dev; if (col.Item(i, out dev) != 0) continue;
                    try
                    {
                        string name = FriendlyName(dev);
                        if (name == null) continue;
                        string ln = name.ToLowerInvariant();
                        bool match = false; foreach (var p in nameParts) if (ln.Contains(p.ToLowerInvariant())) match = true;
                        if (!match || ln.Contains("micr") || ln.Contains("hands-free") || ln.Contains("manos libres")) continue;
                        object o; var iv = IID_Vol;
                        if (dev.Activate(ref iv, 23, IntPtr.Zero, out o) == 0) { float v; ((IAudioEndpointVolume)o).GetMasterVolumeLevelScalar(out v); volume = v; Marshal.ReleaseComObject(o); }
                        var im = IID_Meter;
                        if (dev.Activate(ref im, 23, IntPtr.Zero, out o) == 0)
                        {
                            var m = (IAudioMeterInformation)o; float peak = 0;
                            for (int k = 0; k < 6; k++) { float p; if (m.GetPeakValue(out p) == 0 && p > peak) peak = p; Thread.Sleep(40); }
                            playing = peak > 0.002f; Marshal.ReleaseComObject(o);
                        }
                        return true;
                    }
                    finally { Marshal.ReleaseComObject(dev); }
                }
            }
            catch (Exception e) { Log.W("audio: " + e.Message); }
            finally { if (col != null) Marshal.ReleaseComObject(col); if (en != null) Marshal.ReleaseComObject(en); }
            return false;
        }

        static string FriendlyName(IMMDevice dev)
        {
            IPropertyStore ps;
            if (dev.OpenPropertyStore(0, out ps) != 0 || ps == null) return null;
            try
            {
                var key = new PROPERTYKEY { fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 14 };
                PROPVARIANT pv;
                if (ps.GetValue(ref key, out pv) != 0) return null;
                string s = pv.vt == 31 ? Marshal.PtrToStringUni(pv.p) : null;
                PropVariantClear(ref pv);
                return s;
            }
            finally { Marshal.ReleaseComObject(ps); }
        }
    }
}
