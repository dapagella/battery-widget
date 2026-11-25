using System;
using System.Collections.Generic;
using System.Globalization;

namespace BatteryWidget
{
    /// <summary>
    /// Two notifications, nothing else: "low battery" (at 20 % and again at 10 %) and "fully charged" (once, when a device that was
    /// charging reaches 100 %). Also feeds the battery history used for the time-left estimate.
    ///
    /// Rules that keep it honest:
    ///  - only real readings: estimates (speaker) and coarse readings (XInput pads report 4 steps) never trigger anything;
    ///  - a condition must hold for HoldSeconds across consecutive readings before it is announced, so one glitchy reading can't fire it;
    ///  - each alert fires once per discharge/charge cycle and re-arms only after the battery has really recovered.
    /// </summary>
    public sealed class AlertEngine
    {
        public const int HoldSeconds = 30;

        readonly Config cfg;
        readonly Action<ToastItem> sink;
        readonly Func<DateTime> clock;
        readonly History history = History.Instance;

        sealed class St
        {
            public int Level, PendingLevel;                 // 0 none, 1 <=20 %, 2 <=10 %
            public DateTime LowSince = DateTime.MinValue;
            public bool ChargingSeen, BelowSeen, FullSent;
            public int LastBelow;                           // last reading under 100 %: a real charge climbs through 90+, it does not jump
            public DateTime FullSince = DateTime.MinValue;
        }
        readonly Dictionary<string, St> state = new Dictionary<string, St>();

        public AlertEngine(Config cfg, Action<ToastItem> sink, Func<DateTime> clock = null)
        {
            this.cfg = cfg; this.sink = sink; this.clock = clock ?? (() => DateTime.UtcNow);
        }

        static bool Trustworthy(DeviceInfo d)
        {
            return d.Key != null && d.Percent >= 0 && d.Percent <= 100 && !d.Estimated && d.Source != "xinput";
        }

        public void Process(IEnumerable<DeviceInfo> devices)
        {
            var now = clock();
            foreach (var d in devices)
            {
                if (!Trustworthy(d)) continue;
                history.Add(d.Key, d.Percent, d.Charging);

                St s; if (!state.TryGetValue(d.Key, out s)) state[d.Key] = s = new St();

                // ---- low battery ----
                if (d.Charging) { s.Level = 0; s.PendingLevel = 0; }
                else
                {
                    int level = d.Percent <= 10 ? 2 : d.Percent <= 20 ? 1 : 0;
                    if (level > s.Level)
                    {
                        if (s.PendingLevel != level) { s.PendingLevel = level; s.LowSince = now; }          // first sighting: start the clock
                        else if ((now - s.LowSince).TotalSeconds >= HoldSeconds)                           // still low a while later: it is real
                        {
                            s.Level = level; s.PendingLevel = 0;
                            if (cfg.AlertLow) Low(d, level == 2);
                        }
                    }
                    else
                    {
                        s.PendingLevel = 0;                                                                  // it was a blip
                        if (d.Percent > 25) s.Level = 0;                                                      // recharged enough: re-arm
                    }
                }

                // ---- fully charged ----
                if (d.Charging) s.ChargingSeen = true;
                else if (d.Percent < 100) s.ChargingSeen = false;                                            // unplugged before it was full
                if (d.Percent < 100) { s.BelowSeen = true; s.LastBelow = d.Percent; }
                if (d.Percent <= 95) s.FullSent = false;

                bool full = d.Percent >= 100 && s.ChargingSeen && s.BelowSeen && s.LastBelow >= 90 && !s.FullSent;
                if (!full) s.FullSince = DateTime.MinValue;
                else if (s.FullSince == DateTime.MinValue) s.FullSince = now;
                else if ((now - s.FullSince).TotalSeconds >= HoldSeconds)
                {
                    s.FullSent = true; s.BelowSeen = false; s.ChargingSeen = false; s.FullSince = DateTime.MinValue;
                    if (cfg.AlertFull) Full(d);
                }
            }
        }

        void Low(DeviceInfo d, bool critical)
        {
            sink(new ToastItem
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
            sink(new ToastItem
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
