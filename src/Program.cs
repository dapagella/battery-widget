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
            bool created;
            using (var mutex = new Mutex(true, "Local\\BatteryWidget.TaskbarWidget", out created))
            {
                if (!created) return; // already running
                Native.MakeDpiAware();
                var cfg = Config.Load();
                bool debugFlag = Array.IndexOf(args, "--debug") >= 0;   // not written back to config.json
                ForceVisible = Array.IndexOf(args, "--always-visible") >= 0;
                Log.Enabled = cfg.Debug || debugFlag;
                Native.EnableEcoMode();
                if (!cfg.AutoStartInitDone) { Config.SetAutoStart(true); cfg.AutoStartInitDone = true; cfg.Save(); }
                else if (Config.GetAutoStart()) Config.SetAutoStart(true); // keeps the registered path current if the exe moved

                var hub = new DeviceHub();
                hub.Add(new GHubSource());
                hub.Add(new HyperXSource());
                LogitechHidppSource.AlwaysOn = Array.IndexOf(args, "--hidpp") >= 0;
                hub.Add(new LogitechHidppSource());
                hub.Add(new HeadsetFamiliesSource());
                hub.Add(new RazerSource());
                hub.Add(new BluetoothSource(() => cfg.PinnedBluetooth.ToArray()));
                hub.Add(new GameSirSource(() => cfg.GamepadName));
                hub.Add(new XInputSource(() => Loc.T("Control inalámbrico")));

                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                // a bug must never take the widget down (or show a crash dialog): log it and keep going
                app.DispatcherUnhandledException += (s, e) => { Log.W("UI error: " + e.Exception); e.Handled = true; };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.W("fatal: " + e.ExceptionObject);
                System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) => { Log.W("task: " + e.Exception); e.SetObserved(); };
                var win = new WidgetWindow(cfg, hub);
                win.Show();
                hub.Start();
                foreach (var a in args) if (a.StartsWith("--shots=")) win.RunSelfTest(a.Substring(8));
                app.Run();
                Estimator.Instance.Flush();
            }
        }
    }
}
