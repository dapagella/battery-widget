using System;
using System.Collections.Generic;
using BatteryWidget;

// Feeds the real AlertEngine with scripted readings and a fake clock; prints what it would announce.
static class T
{
    static int fails;
    static DateTime t0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), now;
    static List<ToastItem> got = new List<ToastItem>();
    static AlertEngine e;

    static DeviceInfo D(string key, int pct, bool charging = false, bool est = false, string src = "ghub")
    { return new DeviceInfo { Key = key, Name = key, Kind = Kind.Mouse, Percent = pct, Charging = charging, Estimated = est, Source = src }; }
    static void Step(int sec, params DeviceInfo[] devs) { now = t0.AddSeconds(sec); e.Process(devs); }
    static void Expect(string name, int count) { bool ok = got.Count == count; if (!ok) fails++; Console.WriteLine((ok ? "PASS " : "FAIL ") + name + " -> alerts: " + got.Count + " (expected " + count + ")" + (got.Count > 0 ? "  [" + got[got.Count - 1].Title + ": " + got[got.Count - 1].Body + "]" : "")); got.Clear(); }
    static void Fresh() { var cfg = new Config(); got.Clear(); e = new AlertEngine(cfg, t => got.Add(t), () => now); }

    static void Main()
    {
        Fresh(); Step(0, D("mouse", 90)); Step(15, D("mouse", 90)); Step(60, D("mouse", 90)); Expect("mouse stays at 90 %: nothing", 0);
        Fresh(); Step(0, D("headset", 61)); Step(15, D("headset", 61)); Step(120, D("headset", 61)); Expect("headset steady at 61 %: nothing", 0);
        Fresh(); Step(0, D("hs", 61)); Step(15, D("hs", 8)); Step(30, D("hs", 61)); Step(60, D("hs", 61)); Expect("one glitchy 8 % reading between two 61 %: nothing", 0);
        Fresh(); Step(0, D("kb", 25)); Step(15, D("kb", 18)); Expect("first low reading alone: not yet", 0);
        Step(30, D("kb", 18)); Expect("still 18 % 15 s later: not yet (needs 30 s)", 0);
        Step(46, D("kb", 17)); Expect("held 30 s below 20 %: ONE low-battery alert", 1);
        Step(70, D("kb", 17)); Step(100, D("kb", 16)); Expect("keeps draining 17->16: no repeat", 0);
        Step(130, D("kb", 9)); Step(165, D("kb", 9)); Expect("drops under 10 % and holds: ONE critical alert", 1);
        Step(200, D("kb", 8)); Step(240, D("kb", 8)); Expect("8 % afterwards: no repeat", 0);
        Fresh(); Step(0, D("a", 15, true)); Step(40, D("a", 15, true)); Step(80, D("a", 16, true)); Expect("low but CHARGING: nothing", 0);
        Fresh(); Step(0, D("pad", 10, false, false, "xinput")); Step(60, D("pad", 10, false, false, "xinput")); Expect("XInput pad bucket value 10: nothing (coarse)", 0);
        Fresh(); Step(0, D("spk", 5, false, true)); Step(60, D("spk", 5, false, true)); Expect("estimated speaker 5 %: nothing", 0);
        Fresh(); Step(0, D("m", 98, true)); Step(20, D("m", 99, true)); Step(40, D("m", 100, true)); Expect("reaches 100 % charging: not yet", 0);
        Step(75, D("m", 100, true)); Expect("100 % held 30+ s: ONE fully-charged alert", 1);
        Step(120, D("m", 100, true)); Step(300, D("m", 100, true)); Expect("stays at 100 %: no repeat", 0);
        Fresh(); Step(0, D("n", 100, true)); Step(40, D("n", 100, true)); Step(80, D("n", 100, true)); Expect("plugged in already at 100 % (never saw it charge up): nothing", 0);
        Fresh(); Step(0, D("u", 60, true)); Step(30, D("u", 61, true)); Step(60, D("u", 100, false)); Step(100, D("u", 100, false)); Expect("unplugged then reads 100 % without charging: nothing", 0);
        Fresh(); Step(0, D("g", 98, true)); Step(20, D("g", 99, true)); Step(40, D("g", 100, false)); Step(80, D("g", 100, false)); Expect("climbs 98 -> 99 -> 100 and the device stops flagging charging: ONE fully-charged alert", 1);
        Fresh(); Step(0, D("c", 18)); Step(40, D("c", 18)); got.Clear(); Step(60, D("c", 40, true)); Step(3600, D("c", 90)); Step(7200, D("c", 19)); Step(7240, D("c", 19)); Expect("after a full recharge it can warn again", 1);
        Console.WriteLine(fails == 0 ? "\nALL PASSED" : "\nFAILURES: " + fails);
        Environment.Exit(fails == 0 ? 0 : 1);
    }
}
