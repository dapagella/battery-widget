using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BatteryWidget
{
    /// <summary>"Save diagnostics": a plain-text report of what Windows and the supported protocols see.
    /// Contains device names and USB ids only (no personal data). Send it with a bug report so a missing device can be added.</summary>
    public static class Diagnostics
    {
        public static string Write()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Battery Widget diagnostics  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine("Version " + typeof(Diagnostics).Assembly.GetName().Version + " | " + Environment.OSVersion.VersionString + " | " + (Environment.Is64BitOperatingSystem ? "x64" : "x86"));
            sb.AppendLine();

            // --- Bluetooth ---
            sb.AppendLine("== Bluetooth devices (classic + LE) ==");
            try
            {
                foreach (var id in Native.BluetoothDeviceIds())
                {
                    var nb = Native.GetDevProp(id, Native.KeyName);
                    var cb = Native.GetDevProp(id, Native.KeyConnected);
                    var bb = Native.GetDevProp(id, Native.KeyBtBattery);
                    sb.AppendLine("  " + (nb == null ? "?" : Encoding.Unicode.GetString(nb).TrimEnd('\0')) + " | connected=" + (cb != null && cb.Length > 0 && cb[0] != 0) + " | battery=" + (bb != null && bb.Length > 0 ? bb[0].ToString() : "none"));
                }
            }
            catch (Exception e) { sb.AppendLine("  error: " + e.Message); }
            sb.AppendLine();

            // --- every HID collection of the brands we support (plus anything with a battery-looking vendor page) ---
            sb.AppendLine("== HID collections (vendor:product 'name' usagePage/usage in/out/feature) ==");
            var interesting = new ushort[] { 0x046D, 0x1B1C, 0x0951, 0x03F0, 0x1532, 0x054C, 0x3537, 0x0B05, 0x1A81, 0x258A, 0x0C45, 0x25A7, 0x04D9, 0x1038, 0x0D8C, 0x3151, 0x248A };
            var seen = new List<Native.HidInfo>();
            try
            {
                foreach (var vid in interesting) seen.AddRange(Native.FindHid(vid, null));
                foreach (var h in seen)
                    sb.AppendLine("  " + h.Vid.ToString("X4") + ":" + h.Pid.ToString("X4") + " '" + h.Product + "' page=" + h.UsagePage.ToString("X") + "/" + h.Usage.ToString("X") + " in=" + h.InLen + " out=" + h.OutLen + " feat=" + h.FeatLen);
            }
            catch (Exception e) { sb.AppendLine("  error: " + e.Message); }
            sb.AppendLine();

            // --- Corsair: what each vendor collection answers ---
            sb.AppendLine("== Corsair probe ==");
            try
            {
                foreach (var h in seen.Where(x => x.Vid == 0x1B1C && x.UsagePage >= 0xFF00 && x.OutLen >= 64))
                    foreach (byte id in new byte[] { 0x02, 0x00 })
                        foreach (byte target in new byte[] { 0x09, 0x08 })
                        {
                            var req = new byte[Math.Max(65, (int)h.OutLen)]; req[0] = id; req[1] = target; req[2] = 0x02; req[3] = 0x0F;
                            string err; var reps = Native.HidExchange(h, req, 300, out err, false);
                            sb.AppendLine("  " + h.Pid.ToString("X4") + " page=" + h.UsagePage.ToString("X") + " id=" + id + " target=" + target + " -> " + (err != null ? "error " + err : reps.Count == 0 ? "no reply" : string.Join(" | ", reps.Take(2).Select(r => BitConverter.ToString(r, 0, Math.Min(r.Length, 12))))));
                        }
            }
            catch (Exception e) { sb.AppendLine("  error: " + e.Message); }
            sb.AppendLine();

            // --- PlayStation: what the pad sends ---
            sb.AppendLine("== PlayStation probe ==");
            try
            {
                foreach (var h in seen.Where(x => x.Vid == 0x054C))
                    sb.AppendLine("  " + h.Pid.ToString("X4") + " page=" + h.UsagePage.ToString("X") + "/" + h.Usage.ToString("X") + " -> " + (h.UsagePage == 1 && h.Usage == 5 ? Native.HidSample(h, 2500, 0x02, 0x05) : "(skipped)"));
            }
            catch (Exception e) { sb.AppendLine("  error: " + e.Message); }
            sb.AppendLine();

            // --- what the widget currently shows ---
            sb.AppendLine("== log tail ==");
            try
            {
                var f = Path.Combine(Config.Dir, "error.log");
                if (File.Exists(f)) foreach (var line in File.ReadAllLines(f).Reverse().Take(15).Reverse()) sb.AppendLine("  " + line);
                else sb.AppendLine("  (no errors logged)");
            }
            catch { }

            Directory.CreateDirectory(Config.Dir);
            var path = Path.Combine(Config.Dir, "diagnostic.txt");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }
    }
}
