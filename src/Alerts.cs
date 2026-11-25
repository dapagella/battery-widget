using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;

namespace BatteryWidget
{
    /// <summary>
    /// Two notifications, nothing else: "low battery" (at 20 % and again at 10 %) and "fully charged" (once, when a device that was
    /// charging reaches 100 %). Also feeds the battery history used for the time-left estimate.
    /// </summary>
    public sealed class AlertEngine
    {
        readonly Config cfg;
        readonly Toaster toaster;
        readonly History history = History.Instance;

        sealed class St { public int Last = -1; public int Level; public bool WasCharging, FullSent; }
        readonly Dictionary<string, St> state = new Dictionary<string, St>();

        public AlertEngine(Config cfg, Toaster toaster) { this.cfg = cfg; this.toaster = toaster; }

        public void Process(IEnumerable<DeviceInfo> devices)
        {
            foreach (var d in devices)
            {
                if (d.Key == null || d.Percent < 0 || d.Estimated) continue;       // only real readings
                history.Add(d.Key, d.Percent, d.Charging);

                St s; if (!state.TryGetValue(d.Key, out s)) state[d.Key] = s = new St();

                // ---- low battery ----
                if (d.Charging) s.Level = 0;
                else
                {
                    int level = d.Percent <= 10 ? 2 : d.Percent <= 20 ? 1 : 0;
                    if (level > s.Level)
                    {
                        s.Level = level;
                        if (cfg.AlertLow) Low(d, level == 2);
                    }
                    else if (d.Percent > 25) s.Level = 0;                          // recharged enough: re-arm
                }

                // ---- fully charged ----
                bool full = d.Percent >= 100 && (d.Charging || s.WasCharging);
                if (full && !s.FullSent && s.Last >= 0 && s.Last < 100)
                {
                    s.FullSent = true;
                    if (cfg.AlertFull) Full(d);
                }
                if (d.Percent <= 95) s.FullSent = false;

                s.WasCharging = d.Charging;
                s.Last = d.Percent;
            }
        }

        void Low(DeviceInfo d, bool critical)
        {
            toaster.Show(new ToastItem
            {
                Title = d.Name,
                Body = Loc.T(critical ? "Batería muy baja" : "Batería baja") + ": " + d.Percent.ToString(CultureInfo.InvariantCulture) + " %. " + Loc.T("Conéctalo a cargar."),
                Accent = critical ? Theme.Bad : Theme.Warn,
                Kind = d.Kind,
                Seconds = critical ? 12 : 9,
                Sound = cfg.AlertSound ? "warn" : null
            });
        }

        void Full(DeviceInfo d)
        {
            toaster.Show(new ToastItem
            {
                Title = d.Name,
                Body = Loc.T("Cargado al 100 %.") + " " + Loc.T("Ya puedes desenchufarlo."),
                Accent = Theme.Good,
                Kind = d.Kind,
                Seconds = 8,
                Sound = cfg.AlertSound ? "ok" : null
            });
        }
    }
}
