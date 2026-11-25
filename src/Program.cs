using System;
using System.Threading;
using System.Windows;

namespace BatteryWidget
{
    public static class Program
    {
        public static bool ForceVisible;

        [STAThread]
        public static void Main(string[] args)
        {
            // set up crash logging before anything else, so a failure on someone else's PC leaves a trace in error.log
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Err("fatal: " + e.ExceptionObject);
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) => { Log.Err("task: " + e.Exception); e.SetObserved(); };
            try { Run(args); }
            catch (Exception e) { Log.Err("startup failed: " + e); }
        }

        static void Run(string[] args)
        {
            if (Array.IndexOf(args, "--diag") >= 0) { Console.Out.Flush(); try { Diagnostics.Write(); } catch (Exception e) { Log.Err("diag: " + e); } return; }
            bool created;
            using (var mutex = new Mutex(true, "Local\\BatteryWidget.TaskbarWidget", out created))
            {
                if (!created) return; // already running
                Native.MakeDpiAware();
                var cfg = Config.Load();
                bool debugFlag = Array.IndexOf(args, "--debug") >= 0;   // not written back to config.json
                ForceVisible = Array.IndexOf(args, "--always-visible") >= 0;
                Log.Enabled = cfg.Debug || debugFlag;
                Log.W("start " + Environment.OSVersion.VersionString + " " + (Environment.Is64BitProcess ? "x64" : "x86"));
                Native.EnableEcoMode();
                if (!cfg.AutoStartInitDone && !Config.LoadFailed) { cfg.AutoStartInitDone = true; cfg.Save(); System.Threading.ThreadPool.QueueUserWorkItem(_ => Config.SetAutoStart(true)); }   // first run: turn "start with Windows" on
                else System.Threading.ThreadPool.QueueUserWorkItem(_ => { try { if (Config.GetAutoStart()) Config.SetAutoStart(true); } catch { } });   // self-heal: keeps the task and path current

                var hub = new DeviceHub();
                hub.Add(new GHubSource());
                hub.Add(new HyperXSource());
                LogitechHidppSource.AlwaysOn = Array.IndexOf(args, "--hidpp") >= 0;
                hub.Add(new LogitechHidppSource());
                hub.Add(new HeadsetFamiliesSource());
                hub.Add(new RazerSource());
                hub.Add(new CorsairBragiSource());
                hub.Add(new DualShock4Source());
                hub.Add(new BluetoothSource(() => cfg.PinnedBluetooth.ToArray()));
                hub.Add(new GameSirSource(() => cfg.GamepadName));
                hub.Add(new XInputSource(() => Loc.T("Control inalámbrico")));

                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                // a bug must never take the widget down (or show a crash dialog): log it and keep going
                app.DispatcherUnhandledException += (s, e) => { Log.Err("UI error: " + e.Exception); e.Handled = true; };
                var win = new WidgetWindow(cfg, hub);
                win.Show();
                hub.Start();
                foreach (var a in args) if (a.StartsWith("--shots=")) win.RunSelfTest(a.Substring(8));
                if (Array.IndexOf(args, "--demo-alerts") >= 0) win.DemoAlerts();
                app.Run();
                if (!Config.LoadFailed) cfg.Save();
                Estimator.Instance.Flush();
                History.Instance.Flush();
            }
        }
    }
}
