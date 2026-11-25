using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace BatteryWidget
{
    /// <summary>
    /// Remembers how each device's battery moved over the last two weeks and turns that into "about N hours left".
    /// Stored locally in %APPDATA%\BatteryWidget\history.json (a few KB). Only real readings are recorded, never estimates.
    /// </summary>
    public sealed class History
    {
        public static readonly History Instance = new History();
        static string FilePath { get { return Path.Combine(Config.Dir, "history.json"); } }

        const double KeepDays = 14, UseDays = 7;
        readonly object lk = new object();
        // key -> samples [unix seconds, percent, charging 0/1]
        readonly Dictionary<string, List<double[]>> data = new Dictionary<string, List<double[]>>();
        DateTime lastSave = DateTime.UtcNow; bool dirty;

        History() { Load(); }

        static double Now() { return (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds; }

        void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                var d = new JavaScriptSerializer().Deserialize<Dictionary<string, List<List<double>>>>(File.ReadAllText(FilePath));
                if (d == null) return;
                foreach (var kv in d) data[kv.Key] = kv.Value.Where(s => s.Count == 3).Select(s => s.ToArray()).ToList();
            }
            catch { /* a damaged history only costs the estimate, never the widget */ }
        }

        public void Flush()
        {
            lock (lk)
            {
                if (!dirty) return;
                try
                {
                    Directory.CreateDirectory(Config.Dir);
                    var tmp = FilePath + ".tmp";
                    File.WriteAllText(tmp, new JavaScriptSerializer().Serialize(data));
                    if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null); else File.Move(tmp, FilePath);
                    dirty = false; lastSave = DateTime.UtcNow;
                }
                catch { }
            }
        }

        public void Add(string key, int pct, bool charging)
        {
            if (key == null || pct < 0 || pct > 100) return;
            lock (lk)
            {
                List<double[]> list; if (!data.TryGetValue(key, out list)) data[key] = list = new List<double[]>();
                double now = Now(), c = charging ? 1 : 0;
                var last = list.Count > 0 ? list[list.Count - 1] : null;
                if (last != null && (int)last[1] == pct && last[2] == c && now - last[0] < 1800) return;   // nothing new
                list.Add(new[] { now, pct, c });
                list.RemoveAll(s => now - s[0] > KeepDays * 86400);
                dirty = true;
                if ((DateTime.UtcNow - lastSave).TotalMinutes > 10) Flush();
            }
        }

        /// <summary>Hours of use left at the recent discharge rate, or null when there is not enough history yet.</summary>
        public double? HoursLeft(string key, int pct, bool charging)
        {
            if (charging || pct <= 0) return null;
            lock (lk)
            {
                List<double[]> list; if (!data.TryGetValue(key, out list) || list.Count < 2) return null;
                double now = Now(), drop = 0, hours = 0;
                for (int i = 1; i < list.Count; i++)
                {
                    var a = list[i - 1]; var b = list[i];
                    if (now - b[0] > UseDays * 86400) continue;
                    double dt = (b[0] - a[0]) / 3600.0;
                    if (dt <= 0 || dt > 3) continue;                    // gaps: the device was off or out of range
                    if (a[2] != 0 || b[2] != 0) continue;               // charging
                    double d = a[1] - b[1]; if (d < 0) continue;        // it went up: recharged without telling us
                    drop += d; hours += dt;
                }
                if (hours < 1.5 || drop < 3) return null;
                double rate = drop / hours;                             // percent per hour
                double left = pct / rate;
                return left > 999 ? (double?)null : left;
            }
        }

        /// <summary>"≈ 14 h" style text (already translated), or null.</summary>
        public string RemainingText(string key, int pct, bool charging)
        {
            var h = HoursLeft(key, pct, charging); if (h == null) return null;
            string t;
            if (h >= 48) t = "≈ " + Math.Round(h.Value / 24).ToString(CultureInfo.InvariantCulture) + " " + Loc.T("días");
            else if (h >= 1) t = "≈ " + Math.Round(h.Value).ToString(CultureInfo.InvariantCulture) + " h";
            else t = "≈ " + Math.Max(5, (int)Math.Round(h.Value * 60 / 5) * 5) + " min";
            return t + " " + Loc.T("restantes");
        }
    }
}
