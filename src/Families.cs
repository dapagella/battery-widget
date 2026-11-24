using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BatteryWidget
{
    /// <summary>Guesses what kind of peripheral a product name is.</summary>
    public static class KindGuess
    {
        public static Kind FromName(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            string[] kb = { "keyboard", "teclado", "blackwidow", "huntsman", "deathstalker", "ornata", "cynosa", "alloy", "k70", "k100", "k65", "k63", "k57", "k55", "mk.", "strafe", "tkl", "g915", "g913", "g815", "g715", "g613", "g pro keyboard", "k552", "k580", "k585", "mx keys", "pop keys", "wireless keyboard", "falchion", "claymore", "azoth", "scope" };
            string[] ms = { "mouse", "deathadder", "viper", "basilisk", "naga", "orochi", "cobra", "pro click", "lancehead", "mamba", "harpe", "gladius", "keris", "pugio", "chakram", "rog spatha", "dark core", "darkstar", "scimitar", "ironclaw", "harpoon", "nightsabre", "katar", "sabre", "m65", "m75", "slipstream", "g305", "g403", "g502", "g603", "g604", "g703", "g900", "g903", "g pro wireless", "g pro x superlight", "superlight", "mx master", "mx anywhere", "m590", "m720", "m585", "m705", "cestus", "griffin", "storm", "trackball" };
            string[] hs = { "headset", "headphone", "auricular", "kraken", "blackshark", "barracuda", "nari", "hammerhead", "void", "virtuoso", "hs80", "hs70", "hs60", "hs55", "hs65", "cloud", "arctis", "g733", "g535", "g435", "g533", "g933", "g935", "g pro x", "zone", "earbud", "buds", "airpods", "pulse", "delta", "strix", "theta", "zeus", "pandora", "lamia", "ares" };
            string[] gp = { "controller", "gamepad", "joystick", "nova", "kishi", "wolverine", "raiju", "gamesir", "cyclone", "tarantula", "g7", "t4", "g4", "x2" };
            string[] sp = { "speaker", "soundbar", "parlante", "boom", "groove", "bar " };
            foreach (var k in gp) if (n.Contains(k)) return Kind.Gamepad;
            foreach (var k in hs) if (n.Contains(k)) return Kind.Headset;
            foreach (var k in sp) if (n.Contains(k)) return Kind.Speaker;
            foreach (var k in kb) if (n.Contains(k)) return Kind.Keyboard;
            foreach (var k in ms) if (n.Contains(k)) return Kind.Mouse;
            return Kind.Other;
        }
    }

    // ========================================================================================
    //  Headsets with their own USB receiver: Corsair, HyperX (other models).
    //  Packet formats follow the open-source HeadsetControl project (GPL-3.0, github.com/Sapd/HeadsetControl).
    //  Add a model = add one entry to Specs.
    // ========================================================================================
    public class HeadsetFamiliesSource : ISource
    {
        public string Name { get { return "headsets"; } }
        public int IntervalSec { get { return 30; } }
        public void Kick() { }

        class Spec
        {
            public string Key, DefaultName;
            public ushort Vid; public ushort[] Pids;
            public Func<Native.HidInfo, bool> Match;
            public Func<Native.HidInfo, DeviceInfo> Probe;
        }

        static byte[] First(List<byte[]> reps, Func<byte[], bool> ok) { foreach (var r in reps) if (ok(r)) return r; return null; }

        static readonly List<Spec> Specs = new List<Spec>
        {
            // ---- Corsair VOID / HS family with the USB dongle or cable (vendor collection 0xFFC5) ----
            new Spec { Key = "corsair-void", DefaultName = "Corsair headset", Vid = 0x1B1C,
                Pids = new ushort[] { 0x1B27, 0x0A14, 0x0A16, 0x0A17, 0x0A1D, 0x0A1A, 0x1B2A, 0x1B23, 0x1B29, 0x0A55, 0x0A51, 0x0A52, 0x0A38, 0x0A4F, 0x0A2B, 0x0A75, 0x0A56 },
                Match = h => h.UsagePage == 0xFFC5,
                Probe = h =>
                {
                    string err; var rep = Native.HidExchange(h, new byte[] { 0xC9, 0x64 }, 900, out err, false, r => r.Length >= 5);
                    var r5 = First(rep, r => r.Length >= 5); if (r5 == null) return null;
                    int status = r5[4]; if (status == 0) return null;            // 0 = headset not connected
                    int pct = r5[2] & 0x7F; if (pct <= 0 || pct > 100) return null; // bit 7 = mic flipped up
                    return new DeviceInfo { Percent = pct, Charging = status == 4 || status == 5 };
                } },

            // ---- HyperX Cloud Alpha Wireless ----
            new Spec { Key = "hyperx-alpha", DefaultName = "HyperX Cloud Alpha Wireless", Vid = 0x03F0, Pids = new ushort[] { 0x098D },
                Match = h => h.OutLen >= 31 && h.UsagePage >= 0xFF00,
                Probe = h =>
                {
                    string err;
                    var c = First(Native.HidExchange(h, new byte[] { 0x21, 0xBB, 0x03 }, 800, out err, false, r => r.Length >= 4), r => r.Length >= 4);
                    if (c == null || c[3] != 0x01) return null;                    // not linked
                    var b = First(Native.HidExchange(h, new byte[] { 0x21, 0xBB, 0x0B }, 800, out err, false, r => r.Length >= 4), r => r.Length >= 4);
                    if (b == null || b[3] <= 0 || b[3] > 100) return null;
                    var ch = First(Native.HidExchange(h, new byte[] { 0x21, 0xBB, 0x0C }, 800, out err, false, r => r.Length >= 4), r => r.Length >= 4);
                    return new DeviceInfo { Percent = b[3], Charging = ch != null && ch[3] == 0x01 };
                } },

            // ---- HyperX Cloud Flight (battery reported as voltage) ----
            new Spec { Key = "hyperx-flight", DefaultName = "HyperX Cloud Flight", Vid = 0x0951, Pids = new ushort[] { 0x16C4, 0x1723 },
                Match = h => h.OutLen >= 20 && h.UsagePage >= 0xFF00,
                Probe = h =>
                {
                    string err; var rep = Native.HidExchange(h, new byte[] { 0x21, 0xFF, 0x05 }, 900, out err, false, r => r.Length >= 15 && r[0] == 0x21);
                    var r1 = First(rep, r => r.Length >= 15 && r[0] == 0x21); if (r1 == null) return null;
                    int mv = (r1[3] << 8) | r1[4];
                    if (mv > 0x100B) return new DeviceInfo { Percent = -1, Charging = true };
                    return new DeviceInfo { Percent = VoltToPct(mv) };
                } },

            // ---- HyperX Cloud II Wireless (HP-branded dongle) ----
            new Spec { Key = "hyperx-cloud2-hp", DefaultName = "HyperX Cloud II Wireless", Vid = 0x03F0, Pids = new ushort[] { 0x0696, 0x018B },
                Match = h => h.UsagePage == 0xFF90 && h.Usage == 0x0303,
                Probe = h =>
                {
                    string err;
                    var b = First(Native.HidExchange(h, new byte[] { 0x06, 0xFF, 0xBB, 0x02 }, 900, out err, false, r => r.Length >= 8 && r[0] == 0x06 && r[3] == 0x02), r => r.Length >= 8 && r[0] == 0x06 && r[2] == 0xBB && r[3] == 0x02);
                    if (b == null || b[7] <= 0 || b[7] > 100) return null;
                    var ch = First(Native.HidExchange(h, new byte[] { 0x06, 0xFF, 0xBB, 0x03 }, 900, out err, false, r => r.Length >= 5 && r[0] == 0x06 && r[3] == 0x03), r => r.Length >= 5 && r[0] == 0x06 && r[3] == 0x03);
                    return new DeviceInfo { Percent = b[7], Charging = ch != null && ch[4] == 1 };
                } },
        };

        static int VoltToPct(int mv)
        {
            double p;
            if (mv <= 3648) p = 0.00125 * mv;
            else if (mv > 3975) p = 100;
            else { double v = mv; p = 0.00000002547505 * Math.Pow(v, 4) - 0.0003900299 * Math.Pow(v, 3) + 2.238321 * v * v - 5706.256 * v + 5452299.0; }
            return (int)Math.Max(1, Math.Min(100, Math.Round(p)));
        }

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            foreach (var spec in Specs)
            {
                try
                {
                    foreach (var h in Native.FindHid(spec.Vid, spec.Pids).Where(spec.Match))
                    {
                        var d = spec.Probe(h);
                        Log.W("headsets " + spec.Key + " page=" + h.UsagePage.ToString("X") + " -> " + (d == null ? "no answer" : d.Percent + "%"));
                        if (d == null) continue;
                        d.Key = "hs:" + spec.Key; d.Name = spec.DefaultName;
                        d.Kind = Kind.Headset; d.Connection = "2.4 GHz"; d.Source = "headsets";
                        res.Add(d); break;
                    }
                }
                catch (Exception e) { Log.W("headsets " + spec.Key + ": " + e.Message); }
            }
            return res;
        }
    }

    // ========================================================================================
    //  Razer (mice, keyboards, headsets with a USB dongle) - the "Razer Chroma" feature-report protocol
    //  documented by the OpenRazer project (GPL-2.0). Read-only: asks the device for its battery level.
    //  Every Razer HID device exposing the 91-byte feature report is probed, so no model list is needed.
    // ========================================================================================
    public class RazerSource : ISource
    {
        public string Name { get { return "razer"; } }
        public int IntervalSec { get { return 30; } }
        public void Kick() { }
        static readonly byte[] Tids = { 0x1F, 0x3F, 0xFF, 0x9F, 0x2F };
        readonly Dictionary<string, byte> goodTid = new Dictionary<string, byte>();

        static byte[] Build(byte tid, byte cmd)
        {
            var b = new byte[91];                 // [0] report id, then the 90-byte Razer report
            b[2] = tid; b[6] = 0x02; b[7] = 0x07; b[8] = cmd;
            byte crc = 0; for (int i = 3; i <= 88; i++) crc ^= b[i];
            b[89] = crc; return b;
        }

        static byte[] Ask(Native.HidInfo h, byte tid, byte cmd)
        {
            var r = Native.HidFeature(h, Build(tid, cmd), 12);
            return (r != null && r[1] == 0x02) ? r : null;     // 0x02 = command successful
        }

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            try
            {
                foreach (var h in Native.FindHid(0x1532, null).Where(x => x.FeatLen == 91))
                {
                    byte tid; byte[] r = null;
                    if (goodTid.TryGetValue(h.Path, out tid)) r = Ask(h, tid, 0x80);
                    if (r == null) foreach (var t in Tids) { r = Ask(h, t, 0x80); if (r != null) { tid = t; goodTid[h.Path] = t; break; } }
                    if (r == null) continue;
                    int raw = r[10]; if (raw <= 0) continue;                // 0 = wired / asleep / not connected
                    int pct = (int)Math.Round(raw * 100.0 / 255.0);
                    var c = Ask(h, tid, 0x84);
                    string name = string.IsNullOrEmpty(h.Product) ? "Razer device" : h.Product;
                    if (!name.ToLowerInvariant().Contains("razer")) name = "Razer " + name;
                    Log.W("razer " + name + " tid=" + tid.ToString("X2") + " raw=" + raw);
                    if (res.Any(x => x.Name == name && x.Percent == pct)) continue;      // same device on several interfaces
                    res.Add(new DeviceInfo { Key = "razer:" + h.Pid.ToString("x4"), Name = name, Kind = KindGuess.FromName(name), Percent = Math.Min(100, pct), Charging = c != null && c[10] == 1, Connection = "2.4 GHz", Source = "razer" });
                }
            }
            catch (Exception e) { Log.W("razer: " + e.Message); return null; }
            return res;
        }
    }

    // ========================================================================================
    //  Logitech HID++ 2.0 straight over USB (Unifying / Bolt / LIGHTSPEED receivers): keyboards, mice, headsets.
    //  Only used when G HUB is not running (G HUB already reports these and we don't want to get in its way).
    // ========================================================================================
    public class LogitechHidppSource : ISource
    {
        public string Name { get { return "hidpp"; } }
        public int IntervalSec { get { return 30; } }
        public void Kick() { }
        public static bool AlwaysOn;   // --hidpp: also run while G HUB is open (for testing)
        const byte Sw = 0x07;

        static byte[] Req(Native.HidInfo h, byte idx, byte feat, int fn, params byte[] p)
        {
            var o = new byte[20]; o[0] = 0x11; o[1] = idx; o[2] = feat; o[3] = (byte)((fn << 4) | Sw);
            for (int i = 0; i < p.Length && i < 16; i++) o[4 + i] = p[i];
            string err;
            var reps = Native.HidExchange(h, o, 350, out err, false, r => r.Length >= 7 && r[0] == 0x11 && r[1] == idx && (r[2] == 0xFF || r[2] == 0x8F || (r[2] == feat && (r[3] & 0x0F) == Sw)));
            foreach (var r in reps)
                if (r.Length >= 7 && r[0] == 0x11 && r[1] == idx && r[2] == feat && (r[3] & 0x0F) == Sw) return r;
            return null;
        }

        static int FeatureIndex(Native.HidInfo h, byte idx, ushort feature)
        {
            var r = Req(h, idx, 0x00, 0, (byte)(feature >> 8), (byte)(feature & 0xFF));
            return r == null ? 0 : r[4];
        }

        static string DeviceName(Native.HidInfo h, byte idx, int nameFeat)
        {
            var c = Req(h, idx, (byte)nameFeat, 0); if (c == null) return null;
            int count = c[4], got = 0; var sb = new StringBuilder();
            while (got < count && got < 64)
            {
                var r = Req(h, idx, (byte)nameFeat, 1, (byte)got); if (r == null) break;
                int n = 0; for (int i = 4; i < r.Length && r[i] != 0 && got + n < count; i++) { sb.Append((char)r[i]); n++; }
                if (n == 0) break; got += n;
            }
            return sb.ToString();
        }

        static string[] ReceiverPids = { "c52b", "c52f", "c532", "c534", "c539", "c53a", "c53d", "c53f", "c541", "c545", "c547", "c548", "c54d", "c551", "c552", "c553", "c554", "c555", "c556" };

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            // G HUB already handles Logitech devices; don't compete with it (or flash a duplicate before its first reading)
            if (!AlwaysOn && (GHubSource.HasDevices || System.Diagnostics.Process.GetProcessesByName("lghub_agent").Length > 0)) return res;
            try
            {
                foreach (var h in Native.FindHid(0x046D, null).Where(x => x.UsagePage == 0xFF00 && x.Usage == 0x0002 && x.OutLen >= 20))
                {
                    bool receiver = ReceiverPids.Contains(h.Pid.ToString("x4"));
                    var indices = receiver ? new byte[] { 1, 2, 3, 4, 5, 6 } : new byte[] { 0xFF, 0x00 };
                    foreach (byte idx in indices)
                    {
                        var ping = Req(h, idx, 0x00, 1, 0, 0, 0x5A);
                        Log.W("hidpp probe " + h.Pid.ToString("x4") + " idx=" + idx + " ping=" + (ping == null ? "none" : "ok"));
                        if (ping == null || ping[6] != 0x5A) continue;     // nobody home on this slot
                        ushort feat = 0x1004; int fi = FeatureIndex(h, idx, feat);
                        if (fi == 0) { feat = 0x1000; fi = FeatureIndex(h, idx, feat); }
                        if (fi == 0) continue;
                        int pct, chargeFlag;
                        if (feat == 0x1004)
                        {
                            var s = Req(h, idx, (byte)fi, 1); if (s == null) continue;
                            pct = s[4]; chargeFlag = s[6];
                            if (pct == 0) { int lvl = s[5]; pct = (lvl & 8) != 0 ? 90 : (lvl & 4) != 0 ? 50 : (lvl & 2) != 0 ? 20 : 5; } // coarse level only
                        }
                        else
                        {
                            var s = Req(h, idx, (byte)fi, 0); if (s == null) continue;
                            pct = s[4]; chargeFlag = s[6];
                        }
                        if (pct <= 0 || pct > 100) continue;
                        int nf = FeatureIndex(h, idx, 0x0005);
                        string name = nf != 0 ? DeviceName(h, idx, nf) : null;
                        int type = -1; if (nf != 0) { var t = Req(h, idx, (byte)nf, 2); if (t != null) type = t[4]; }
                        if (string.IsNullOrEmpty(name)) name = "Logitech device";
                        else if (!name.ToLowerInvariant().Contains("logi") && !name.StartsWith("G") && !name.StartsWith("MX")) name = "Logitech " + name;
                        Kind k = type == 0 ? Kind.Keyboard : (type == 3 || type == 4 || type == 5) ? Kind.Mouse : type == 8 ? Kind.Headset : KindGuess.FromName(name);
                        Log.W("hidpp " + h.Pid.ToString("x4") + " idx=" + idx + " " + name + " " + pct + "% feat=" + feat.ToString("X4"));
                        res.Add(new DeviceInfo { Key = "hidpp:" + h.Pid.ToString("x4") + ":" + idx, Name = name, Kind = k, Percent = pct, Charging = chargeFlag == 1 || chargeFlag == 2 || chargeFlag == 4, Connection = receiver ? "LIGHTSPEED" : "USB", Source = "hidpp" });
                    }
                }
            }
            catch (Exception e) { Log.W("hidpp: " + e.Message); return null; }
            return res;
        }
    }
}

namespace BatteryWidget
{
    // ========================================================================================
    //  Corsair "Bragi" protocol: wireless mice (Katar, Dark Core, Slipstream dongle...), keyboards, headsets (Virtuoso, HS80...).
    //  Every Corsair HID collection on usage page 0xFF42 is asked for property 0x0F (battery, tenths of a percent).
    //  Based on the open-source HeadsetControl documentation of the protocol.
    // ========================================================================================
    public class CorsairBragiSource : ISource
    {
        public string Name { get { return "corsair"; } }
        public int IntervalSec { get { return 30; } }
        public void Kick() { }

        static byte[] Get(Native.HidInfo h, byte target, byte prop)
        {
            // two framings exist: headsets use numbered report 0x02; mice/keyboards behind a Slipstream dongle use an unnumbered
            // 64-byte report (report id 0). The reply has the same shape either way: [id, source, 0x02, status, value lo, value hi...]
            foreach (byte id in new byte[] { 0x02, 0x00 })
            {
                var req = new byte[Math.Max(65, (int)h.OutLen)]; req[0] = id; req[1] = target; req[2] = 0x02; req[3] = prop;
                string err;
                Func<byte[], bool> ok = r => r.Length >= 8 && (r[0] == 0x01 || r[0] == 0x00) && r[2] == 0x02 && (r[3] == 0x00 || r[3] == 0x05 || r[3] == 0x09);
                var reps = Native.HidExchange(h, req, 300, out err, false, ok);
                if (err != null) { Log.W("corsair write(id " + id + ") " + h.Pid.ToString("x4") + " page=" + h.UsagePage.ToString("X") + " err=" + err); continue; }
                foreach (var r in reps) if (ok(r)) return r;
            }
            return null;
        }

        readonly Dictionary<string, DateTime> quiet = new Dictionary<string, DateTime>();   // collections that never answer: don't ask again for a while

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            try
            {
                var all = Native.FindHid(0x1B1C, null);
                if (Log.Enabled) foreach (var x in all) Log.W("corsair hid " + x.Pid.ToString("x4") + " '" + x.Product + "' page=" + x.UsagePage.ToString("X") + " usage=" + x.Usage + " in=" + x.InLen + " out=" + x.OutLen);
                foreach (var h in all.Where(x => x.UsagePage >= 0xFF00 && x.UsagePage != 0xFFC5 && x.OutLen >= 64 && x.InLen >= 64))
                {
                    DateTime until; if (quiet.TryGetValue(h.Path, out until) && until > DateTime.UtcNow) continue;
                    bool answered = false;
                    foreach (byte target in new byte[] { 0x09, 0x08 })     // 0x09: device behind a receiver, 0x08: the device itself
                    {
                        var r = Get(h, target, 0x0F); if (r != null) answered = true;
                        if (r == null || r[3] != 0x00) continue;
                        int level = r[4] | (r[5] << 8);
                        if (level <= 0 || level > 1000) continue;
                        var c = Get(h, target, 0x10);
                        string prod = h.Product ?? "";
                        string up = prod.ToUpperInvariant();
                        bool dongle = up.Contains("RECEIVER") || up.Contains("SLIPSTREAM") || up.Contains("DONGLE");
                        string name = dongle || prod.Length == 0 ? "Corsair wireless device" : prod;
                        Kind k = KindGuess.FromName(prod); if (k == Kind.Other) k = Kind.Mouse;
                        if (dongle && k == Kind.Mouse) name = "Corsair wireless mouse";
                        Log.W("corsair " + h.Pid.ToString("x4") + " target=" + target + " " + name + " " + level / 10 + "%");
                        res.Add(new DeviceInfo { Key = "corsair:" + h.Pid.ToString("x4") + ":" + target, Name = name, Kind = k, Percent = level / 10, Charging = c != null && c[3] == 0 && c[4] == 1, Connection = dongle ? "2.4 GHz" : "USB", Source = "corsair" });
                        break;
                    }
                    if (!answered) quiet[h.Path] = DateTime.UtcNow.AddSeconds(60);
                }
            }
            catch (Exception e) { Log.W("corsair: " + e.Message); return null; }
            return res;
        }
    }

    // ========================================================================================
    //  PlayStation 4 controller (DualShock 4) over Bluetooth or USB.
    //  Over Bluetooth Windows only delivers the short input report; reading feature report 0x02 makes the pad
    //  send the full report (0x11), which carries the battery in byte 32 (byte 30 of the USB report 0x01).
    // ========================================================================================
    public class DualShock4Source : ISource
    {
        public string Name { get { return "ds4"; } }
        public int IntervalSec { get { return 30; } }
        public void Kick() { }

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            try
            {
                var pads = Native.FindHid(0x054C, new ushort[] { 0x05C4, 0x09CC, 0x0BA0 }).Where(x => x.UsagePage == 0x01 && x.Usage == 0x05 && x.InLen >= 10).ToList();
                int n = 0;
                foreach (var h in pads)
                {
                    int idx = -1;
                    var r = Native.HidListen(h, 1500, x => (x[0] == 0x11 && x.Length >= 34) || (x[0] == 0x01 && x.Length >= 32 && h.InLen < 70), 0x02);
                    if (r == null) continue;
                    idx = r[0] == 0x11 ? 32 : 30;
                    int b = r[idx], lvl = b & 0x0F; bool cable = (b & 0x10) != 0;
                    int pct = Math.Min(100, lvl * 100 / (cable ? 11 : 8));
                    Log.W("ds4 " + h.Pid.ToString("x4") + " report=" + r[0].ToString("X2") + " byte=" + b.ToString("X2") + " -> " + pct + "%");
                    if (pct <= 0 && !cable) continue;
                    res.Add(new DeviceInfo { Key = "ds4:" + (n++), Name = "DualShock 4", Kind = Kind.Gamepad, Percent = pct, Charging = cable && lvl < 11, Connection = r[0] == 0x11 ? "Bluetooth" : "USB", Source = "ds4" });
                }
            }
            catch (Exception e) { Log.W("ds4: " + e.Message); return null; }
            return res;
        }
    }
}
