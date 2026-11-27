using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BatteryWidget
{
    public static class Icons
    {
        public const string Keyboard = "M20 5H4a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2zM11 8h2v2h-2zM11 11h2v2h-2zM8 8h2v2H8zM8 11h2v2H8zM5 8h2v2H5zM5 11h2v2H5zM8 15h8v2H8zM14 8h2v2h-2zM14 11h2v2h-2zM17 8h2v2h-2zM17 11h2v2h-2z";
        public const string Mouse = "M13 1.07V9h7c0-4.08-3.05-7.44-7-7.93zM4 15c0 4.42 3.58 8 8 8s8-3.58 8-8v-4H4v4zm7-13.93C7.05 1.56 4 4.92 4 9h7V1.07z";
        public const string Headset = "M12 1c-4.97 0-9 4.03-9 9v7c0 1.66 1.34 3 3 3h3v-8H5v-2c0-3.87 3.13-7 7-7s7 3.13 7 7v2h-4v8h3c1.66 0 3-1.34 3-3v-7c0-4.97-4.03-9-9-9z";
        // Xbox-style controller: rounded body with two grips; holes (even-odd) = left stick, face buttons, d-pad, right stick
        public const string Gamepad = "F0 M21.58 16.09l-1.09-7.66C20.21 6.46 18.52 5 16.53 5H7.47C5.48 5 3.79 6.46 3.51 8.43l-1.09 7.66C2.2 17.63 3.39 19 4.94 19c.68 0 1.32-.27 1.8-.75L9 16h6l2.25 2.25c.48.48 1.13.75 1.8.75 1.56 0 2.75-1.37 2.53-2.91z"
            + " M7.4 7.9a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3z"
            + " M16.5 7.4a.6.6 0 1 0 0 1.2.6.6 0 0 0 0-1.2zM16.5 10.4a.6.6 0 1 0 0 1.2.6.6 0 0 0 0-1.2zM15 8.9a.6.6 0 1 0 0 1.2.6.6 0 0 0 0-1.2zM18 8.9a.6.6 0 1 0 0 1.2.6.6 0 0 0 0-1.2z"
            + " M9.2 12.2h1v.8h.8v1h-.8v.8h-1v-.8h-.8v-1h.8z"
            + " M14.2 12.3a1.1 1.1 0 1 0 0 2.2 1.1 1.1 0 0 0 0-2.2z";
        public const string Speaker = "M4 7h16a2 2 0 0 1 2 2v6a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2zM6.5 10.5a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zM12 10.5a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zM17.5 10.5a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3z";
        public const string Battery = "M16 4h-1V2H9v2H8a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V6a2 2 0 0 0-2-2z";
        public const string Bolt = "M11 21h-1l1-7H7.5c-.58 0-.57-.32-.38-.66.19-.34.05-.08.07-.12C8.48 10.94 10.42 7.54 13 3h1l-1 7h3.5c.49 0 .56.33.47.51l-.07.15C12.96 17.55 11 21 11 21z";
        // Line icons on a 24x24 grid (stroke 1.75, round caps/joins), drawn in the style of the Lucide set (ISC license).
        const string LKeyboard = "M4 5h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2z M6 9h.01 M10 9h.01 M14 9h.01 M18 9h.01 M6 13h.01 M10 13h.01 M14 13h.01 M18 13h.01 M8 16.2h8";
        const string LMouse = "M12 2.5c-3.6 0-6.5 2.9-6.5 6.5v6c0 3.6 2.9 6.5 6.5 6.5s6.5-2.9 6.5-6.5V9c0-3.6-2.9-6.5-6.5-6.5z M12 2.5v6 M12 6v2.4";
        const string LHeadset = "M3.5 14v-2a8.5 8.5 0 0 1 17 0v2 M3.5 14h2.2a1.3 1.3 0 0 1 1.3 1.3v3.4a1.3 1.3 0 0 1-1.3 1.3H5.5a2 2 0 0 1-2-2V14z M20.5 14h-2.2a1.3 1.3 0 0 0-1.3 1.3v3.4a1.3 1.3 0 0 0 1.3 1.3h.2a2 2 0 0 0 2-2V14z M20.5 18.5v.5a3 3 0 0 1-3 3H13";
        const string LGamepad = "M7.2 6h9.6a4.2 4.2 0 0 1 4.15 3.6l.9 6.4a2.8 2.8 0 0 1-4.7 2.4L15.4 16.4a2 2 0 0 0-1.4-.6H10a2 2 0 0 0-1.4.6l-1.75 1.9a2.8 2.8 0 0 1-4.7-2.4l.9-6.4A4.2 4.2 0 0 1 7.2 6z M7 10v3.2 M5.4 11.6h3.2 M15.6 10.2h.01 M18 12.6h.01";
        const string LSpeaker = "M7 2.5h10a2 2 0 0 1 2 2v15a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2v-15a2 2 0 0 1 2-2z M12 6.5h.01 M12 10.6a3.7 3.7 0 1 0 0 7.4 3.7 3.7 0 0 0 0-7.4z M12 13.3a1 1 0 1 0 0 2 1 1 0 0 0 0-2z";
        const string LBattery = "M3.5 7.5h13a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-13a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2z M21.5 11v2 M5 10.5v3";
        public static string For(Kind k)
        {
            switch (k) { case Kind.Keyboard: return LKeyboard; case Kind.Mouse: return LMouse; case Kind.Headset: return LHeadset; case Kind.Gamepad: return LGamepad; case Kind.Speaker: return LSpeaker; default: return LBattery; }
        }
    }

    public class WidgetWindow : Window
    {
        readonly Config cfg;
        readonly Toaster toaster;
        readonly AlertEngine alerts;
        Updater.Release pendingUpdate;
        DispatcherTimer updateTimer;

        // used by the notification cards
        public IntPtr Handle { get { return hwnd; } }
        public double CurrentScale { get { return curScale; } }
        public bool IsSuppressed { get { return Suppressed; } }

        // =====================================================================
        //  Updates: one quiet check a day; a notification offers a one-click update
        // =====================================================================
        void StartUpdateChecks()
        {
            updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };   // first look shortly after start, then daily
            updateTimer.Tick += (s, e) => { updateTimer.Interval = TimeSpan.FromHours(24); if (cfg.CheckUpdates) CheckForUpdates(false); };
            updateTimer.Start();
        }

        /// <summary>--demo-alerts: shows one of each notification (used to review their look).</summary>
        public void DemoAlerts()
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            t.Tick += (s, e) =>
            {
                t.Stop();
                toaster.Show(new ToastItem { Title = "G915 X LIGHTSPEED TKL", Body = Loc.T("Batería baja") + ": 18 %. " + Loc.T("Conéctalo a cargar."), Accent = Theme.Warn, Kind = Kind.Keyboard, Seconds = 4 });
                toaster.Show(new ToastItem { Title = "HyperX Cloud II Wireless", Body = Loc.T("Batería muy baja") + ": 8 %. " + Loc.T("Conéctalo a cargar."), Accent = Theme.Bad, Kind = Kind.Headset, Seconds = 4 });
                toaster.Show(new ToastItem { Title = "G305", Body = Loc.T("Cargado al 100 %.") + " " + Loc.T("Ya puedes desenchufarlo."), Accent = Theme.Good, Kind = Kind.Mouse, Seconds = 4 });
                toaster.Show(new ToastItem { Title = Loc.T("Hay una versión nueva") + " (1.2.0)", Body = Loc.T("Se instala en unos segundos y el widget se reinicia solo."), Button = Loc.T("Actualizar"), OnButton = () => { }, Accent = Theme.Good, Kind = Kind.Other, Seconds = 5 });
            };
            t.Start();
        }

        void CheckForUpdates(bool manual)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    Version latest; var r = Updater.CheckNow(out latest);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (r != null) OfferUpdate(r, manual);
                        else if (manual) toaster.Show(new ToastItem { Title = "Battery Widget", Body = Loc.T("Estás al día") + " (" + Updater.Current.ToString(3) + ")", Accent = Theme.Good, Kind = Kind.Other, Seconds = 6 });
                    }));
                }
                catch (Exception e)
                {
                    Log.W("update check: " + e.Message);
                    if (manual) Dispatcher.BeginInvoke(new Action(() => toaster.Show(new ToastItem { Title = "Battery Widget", Body = Loc.T("No se pudo buscar actualizaciones"), Accent = Theme.Warn, Kind = Kind.Other, Seconds = 7 })));
                }
            });
        }

        void OfferUpdate(Updater.Release r, bool force)
        {
            if (!force && pendingUpdate != null && pendingUpdate.Version == r.Version) return;   // already offered this session
            pendingUpdate = r;
            toaster.Show(new ToastItem
            {
                Title = Loc.T("Hay una versión nueva") + " (" + r.Version.ToString(3) + ")",
                Body = Loc.T("Se instala en unos segundos y el widget se reinicia solo."),
                Button = Loc.T("Actualizar"), OnButton = () => ApplyUpdate(r), Accent = Theme.Good, Kind = Kind.Other, Seconds = 25
            });
        }

        void ApplyUpdate(Updater.Release r)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { Updater.Apply(r); }
                catch (Exception e)
                {
                    Log.Err("update failed: " + e.Message);
                    Dispatcher.BeginInvoke(new Action(() => toaster.Show(new ToastItem { Title = "Battery Widget", Body = Loc.T("No se pudo actualizar"), Accent = Theme.Bad, Kind = Kind.Other, Seconds = 8 })));
                }
            });
        }
        readonly DeviceHub hub;
        readonly StackPanel host = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        readonly DispatcherTimer fast = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        uint taskbarCreatedMsg;
        bool autoHideOn, autohideHidden;
        IntPtr hwnd; int lastState = -1, ticks;
        bool fullscreenHidden;
        List<DeviceInfo> shown = new List<DeviceInfo>();
        readonly FontFamily font = new FontFamily("Segoe UI Variable Text, Segoe UI");
        readonly List<ItemView> views = new List<ItemView>();
        string lastSig = "";
        bool introPlayed;

        public WidgetWindow(Config c, DeviceHub h)
        {
            cfg = c; hub = h;
            WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
            ShowInTaskbar = false; Topmost = true; ResizeMode = ResizeMode.NoResize; ShowActivated = false;
            UseLayoutRounding = true; SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            Content = host;
            host.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // hit-testable even with "no background"
            host.HorizontalAlignment = HorizontalAlignment.Right;
            host.Margin = new Thickness(8, 0, 10, 0);          // room for rounding, so nothing is cut
            SizeToContent = SizeToContent.Width;                // the window fits its content by itself
            SizeChanged += (s, e) => { if (e.WidthChanged && !inReposition) { lastKey = ""; Reposition(true); } };
            StartAppsWatcher();
            MouseLeftButtonDown += OnDragStart; MouseMove += OnDragMove; MouseLeftButtonUp += OnDragEnd;
            ContextMenuOpening += (s, e) => { ContextMenu = BuildMenu(); };
            Loaded += (s, e) => { Rebuild(); };
            SourceInitialized += (s, e) =>
            {
                hwnd = new WindowInteropHelper(this).Handle;
                int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
                Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, ex | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE);
                // The taskbar jumps above topmost windows whenever you touch it (Start, tray, flyouts). Put the widget
                // back on top the instant that happens, instead of waiting for the next timer tick.
                zHook = new Native.WinEventProc((hk, evt, wh, o, ch, th, tm) => Dispatcher.BeginInvoke(new Action(Reassert), DispatcherPriority.Send));
                Native.HookZOrderEvents(zHook);
                // device plugged/unplugged, display or taskbar changes: react at once and again a moment later (the taskbar settles after ~1 s)
                taskbarCreatedMsg = Native.RegisterWindowMessage("TaskbarCreated");
                var hs = HwndSource.FromHwnd(hwnd); if (hs != null) hs.AddHook(WndProc);
            };
            timer.Tick += (s, e) => { try { Tick(); } catch (Exception ex) { Log.Err("tick: " + ex); } };
            timer.Start();
            fast.Tick += (s, e) => { try { FastTick(); } catch (Exception ex) { Log.Err("fast: " + ex); } };
            fast.Start();
            toaster = new Toaster(this);
            alerts = new AlertEngine(cfg, t => toaster.Show(t));
            Action checkAlerts = () => { try { alerts.Process(hub.Snapshot().Where(d => !cfg.Hidden.Contains(d.Key))); } catch (Exception ex) { Log.Err("alerts: " + ex); } };
            // also look every 15 s: an alert needs the same reading to hold across two looks, and a steady value produces no "changed" event
            var alertTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            alertTimer.Tick += (s, e) => checkAlerts();
            alertTimer.Start();
            hub.Changed += () => Dispatcher.BeginInvoke(new Action(() =>
            {
                checkAlerts();
                try { Rebuild(); } catch (Exception ex) { Log.Err("rebuild: " + ex); }
            }));
            StartUpdateChecks();
        }

        // =====================================================================
        //  Visual language: one quiet surface, color only for battery level
        // =====================================================================
        Color BgColor()
        {
            switch (cfg.Background)
            {
                case "black": return Color.FromArgb(0xF2, 0x00, 0x00, 0x00);
                case "gray": return Color.FromArgb(0xEC, 0x2D, 0x30, 0x38);
                case "navy": return Color.FromArgb(0xEC, 0x0E, 0x1A, 0x33);
                case "violet": return Color.FromArgb(0xEC, 0x21, 0x15, 0x3A);
                case "glass": return Color.FromArgb(0x66, 0x70, 0x78, 0x8C);
                case "none": return Colors.Transparent;
                default: return Color.FromArgb(0xEA, 0x1B, 0x1E, 0x26);
            }
        }
        double Sc() { return Math.Max(0.5, Math.Min(Math.Min(2.2, cfg.SizeFactor), fitCap)); }
        string DesignKey() { var d = cfg.Design; return d == "separate" || d == "minimal" || d == "ring" ? d : "unified"; }

        CornerRadius Corner(double h) { return new CornerRadius(cfg.Shape == "pill" ? h / 2 : cfg.Shape == "square" ? 3 : Math.Min(9 * Sc(), h / 2)); }

        static SolidColorBrush B(Color c) { return Theme.Brush(c); }

        sealed class ItemView
        {
            public string Key;
            public Border Hit;
            public SolidColorBrush Hover;
            public System.Windows.Shapes.Path Icon;
            public TextBlock Pct;
            public System.Windows.Shapes.Path Bolt;
            public Grid BarGrid; public ColumnDefinition ColFill, ColRest; public Border BarFill;
            public Grid RingHost; public UIElement RingArc;
            public ToolTip Tip;
        }

        Native.WinEventProc zHook; // kept in a field so the delegate is never garbage-collected
        void Reassert()
        {
            if (hwnd == IntPtr.Zero || autohideHidden || UpdateFullscreen()) return;
            Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
        }

        // ---------- build / update ----------
        string Signature()
        {
            return string.Join("|", DesignKey(), cfg.Background, cfg.Shape, Sc().ToString("F2", CultureInfo.InvariantCulture), cfg.ShowPercent.ToString(),
                string.Join(",", shown.Select(x => x.Key + (x.Percent < 0 ? "n" : "p") + (x.Charging ? "c" : ""))));
        }

        void Rebuild()
        {
            shown = hub.Snapshot().Where(d => !cfg.Hidden.Contains(d.Key)).ToList();
            string sig = Signature();
            if (sig == lastSig && shown.Count > 0 && views.Count == shown.Count)
            {
                // same structure: update numbers/colors in place (no flicker, hover and tooltip keep working)
                for (int i = 0; i < views.Count; i++) UpdateItem(views[i], shown[i]);
                Reposition(false);
                return;
            }
            lastSig = sig;
            FullBuild();
        }

        void FullBuild()
        {
            host.Children.Clear(); views.Clear();
            double s = Sc();
            if (shown.Count == 0) host.Children.Add(EmptyChip());
            else
            {
                string design = DesignKey();
                var bg = BgColor();
                Brush surface = bg.A == 0 ? B(Color.FromArgb(1, 0, 0, 0)) : B(bg);
                var items = shown.Select(d => MakeItem(d)).ToList();
                views.AddRange(items);
                double h = 26 * s;
                if (design == "separate")
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var cap = new Border { Background = surface, BorderBrush = B(Theme.Hairline), BorderThickness = new Thickness(bg.A == 0 ? 0 : 1), CornerRadius = Corner(h + 6 * s), Padding = new Thickness(3 * s), Child = items[i].Hit, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, i < items.Count - 1 ? 6 * s : 0, 0) };
                        host.Children.Add(cap);
                    }
                }
                else
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (i > 0 && design != "minimal")
                            panel.Children.Add(new Border { Width = 1, Height = 14 * s, Background = B(Theme.Hairline), Margin = new Thickness(2 * s, 0, 2 * s, 0), VerticalAlignment = VerticalAlignment.Center });
                        panel.Children.Add(items[i].Hit);
                    }
                    if (design == "minimal") host.Children.Add(panel);
                    else host.Children.Add(new Border { Background = surface, BorderBrush = B(Theme.Hairline), BorderThickness = new Thickness(bg.A == 0 ? 0 : 1), CornerRadius = Corner(h + 6 * s), Padding = new Thickness(3 * s), Child = panel, VerticalAlignment = VerticalAlignment.Center });
                }
                for (int i = 0; i < items.Count; i++) UpdateItem(items[i], shown[i]);
            }
            UpdateLayout();
            Reposition(true);
            if (!introPlayed && shown.Count > 0) { introPlayed = true; host.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }); }
        }

        UIElement EmptyChip()
        {
            double s = Sc(), h = 26 * s;
            var icon = new System.Windows.Shapes.Path { Data = Geometry.Parse(Icons.For(Kind.Other)), Stroke = B(Theme.WithAlpha(Theme.Text, 0xE6)), StrokeThickness = Math.Max(1.0, 16 * s * 1.75 / 24.0), StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Fill = Brushes.Transparent, Width = 16 * s, Height = 16 * s, Stretch = Stretch.Uniform };
            var b = new Border { Height = h + 6 * s, Padding = new Thickness(11 * s, 0, 11 * s, 0), CornerRadius = Corner(h + 6 * s), Background = B(Color.FromArgb(0xCC, 0x2A, 0x2E, 0x38)), BorderBrush = B(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)), BorderThickness = new Thickness(1), Child = icon, VerticalAlignment = VerticalAlignment.Center };
            var tip = MakeTip();
            SetTipContent(tip, Loc.T("Sin dispositivos conectados"), null, Theme.Neutral, Loc.T("Enciende un teclado, mouse o auricular inalámbrico."), null);
            b.ToolTip = tip; SetupTipService(b);
            return b;
        }

        double PctWidth(double fs)
        {
            var ft = new FormattedText("100%", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(font, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal), fs, Brushes.Black);
            return Math.Ceiling(ft.WidthIncludingTrailingWhitespace) + 1;
        }

        ItemView MakeItem(DeviceInfo d)
        {
            double s = Sc(), h = 26 * s;
            string design = DesignKey();
            var v = new ItemView { Key = d.Key };
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            bool bar = design != "ring" && d.Percent >= 0;

            if (design == "ring")
            {
                double rs = 22 * s;
                v.RingHost = new Grid { Width = rs, Height = rs, VerticalAlignment = VerticalAlignment.Center };
                v.RingHost.Children.Add(new Ellipse { Stroke = B(Color.FromArgb(0x2E, 0xFF, 0xFF, 0xFF)), StrokeThickness = 2.2 * s });
                v.Icon = Glyph(d.Kind, 11 * s); v.Icon.HorizontalAlignment = HorizontalAlignment.Center; v.Icon.VerticalAlignment = VerticalAlignment.Center;
                v.RingHost.Children.Add(v.Icon);
                row.Children.Add(v.RingHost);
            }
            else { v.Icon = Glyph(d.Kind, 16 * s); row.Children.Add(v.Icon); }

            if (cfg.ShowPercent)
            {
                double fs = 12.5 * s;
                v.Pct = new TextBlock { FontFamily = font, FontSize = fs, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, MinWidth = PctWidth(fs), Margin = new Thickness(6 * s, 0, 0, 0), TextAlignment = TextAlignment.Left };
                Typography.SetNumeralAlignment(v.Pct, FontNumeralAlignment.Tabular);
                row.Children.Add(v.Pct);
            }
            if (d.Charging)
            {
                v.Bolt = new System.Windows.Shapes.Path { Data = Geometry.Parse(Icons.Bolt), Fill = B(Theme.Warn), Width = 11 * s, Height = 11 * s, Stretch = Stretch.Uniform, Margin = new Thickness(cfg.ShowPercent ? 1 * s : 4 * s, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(v.Bolt);
            }

            var grid = new Grid();
            row.Margin = new Thickness(0, 0, 0, bar ? 3 * s : 0);
            grid.Children.Add(row);
            if (bar)
            {
                v.BarGrid = new Grid { Height = 2 * s, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 2 * s) };
                v.ColFill = new ColumnDefinition(); v.ColRest = new ColumnDefinition();
                v.BarGrid.ColumnDefinitions.Add(v.ColFill); v.BarGrid.ColumnDefinitions.Add(v.ColRest);
                var track = new Border { CornerRadius = new CornerRadius(1), Background = B(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF)) };
                Grid.SetColumnSpan(track, 2);
                v.BarFill = new Border { CornerRadius = new CornerRadius(1) };
                v.BarGrid.Children.Add(track); v.BarGrid.Children.Add(v.BarFill);
                grid.Children.Add(v.BarGrid);
            }

            v.Hover = new SolidColorBrush(Color.FromArgb(0, 0xFF, 0xFF, 0xFF));
            v.Hit = new Border { Height = h, Padding = new Thickness(8 * s, 0, 8 * s, 0), CornerRadius = new CornerRadius(Math.Min(6 * s, h / 2)), Background = v.Hover, Child = grid };
            var hover = v.Hover;
            v.Hit.MouseEnter += (a, b) => { if (!dragging) hover.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Color.FromArgb(0x1C, 0xFF, 0xFF, 0xFF), Theme.Fast)); };
            v.Hit.MouseLeave += (a, b) => hover.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Color.FromArgb(0, 0xFF, 0xFF, 0xFF), Theme.Fast));
            v.Tip = MakeTip(); v.Hit.ToolTip = v.Tip; SetupTipService(v.Hit);
            return v;
        }

        System.Windows.Shapes.Path Glyph(Kind k, double size)
        {
            return new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(Icons.For(k)), Width = size, Height = size, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center,
                StrokeThickness = Math.Max(1.0, size * 1.75 / 24.0), StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Fill = Brushes.Transparent
            };
        }

        static string PctText(DeviceInfo d)
        {
            if (d.Percent >= 0) return d.Percent + "%";
            if (d.Connection != null && d.Connection.StartsWith("USB")) return "USB";
            return d.Connection == "Bluetooth" ? "BT" : "--";
        }

        void UpdateItem(ItemView v, DeviceInfo d)
        {
            var lc = Theme.Level(d.Percent);
            bool ring = v.RingHost != null;
            v.Icon.Stroke = B(ring ? lc : (d.Percent < 0 ? Theme.TextDim : Theme.WithAlpha(Theme.Text, 0xE6)));
            if (v.Pct != null) { v.Pct.Text = PctText(d); v.Pct.Foreground = B(d.Percent < 0 ? Theme.TextDim : lc); }
            if (v.BarFill != null)
            {
                double p = Math.Max(0, Math.Min(100, d.Percent));
                v.ColFill.Width = new GridLength(Math.Max(p, 0.0001), GridUnitType.Star);
                v.ColRest.Width = new GridLength(Math.Max(100 - p, 0.0001), GridUnitType.Star);
                v.BarFill.Background = B(lc);
            }
            if (ring)
            {
                if (v.RingArc != null) v.RingHost.Children.Remove(v.RingArc);
                if (d.Percent >= 0) { v.RingArc = Arc(v.RingHost.Width, d.Percent, lc, 2.2 * Sc()); v.RingHost.Children.Insert(1, v.RingArc); }
            }
            string status = Loc.T(d.Charging ? "Cargando" : "Conectado"); string conn = Loc.T(d.Connection);
            SetTipContent(v.Tip, d.Name, d.Percent >= 0 ? d.Percent + "%" : "--", lc, status, d.Estimated ? (string.IsNullOrEmpty(conn) ? Loc.T("Estimado") : conn + ", " + Loc.T("estimado")) : conn,
                d.Estimated ? null : History.Instance.RemainingText(d.Key, d.Percent, d.Charging));
        }

        UIElement Arc(double size, int pct, Color c, double thick)
        {
            double r = size / 2 - thick / 2, cx = size / 2, cy = size / 2;
            if (pct >= 100) return new Ellipse { Stroke = B(c), StrokeThickness = thick };
            double ang = Math.Max(1, pct) * 3.6 * Math.PI / 180.0;
            var start = new Point(cx, cy - r);
            var end = new Point(cx + r * Math.Sin(ang), cy - r * Math.Cos(ang));
            var fig = new PathFigure { StartPoint = start };
            fig.Segments.Add(new ArcSegment(end, new Size(r, r), 0, ang > Math.PI, SweepDirection.Clockwise, true));
            var pg = new PathGeometry(); pg.Figures.Add(fig);
            return new System.Windows.Shapes.Path { Data = pg, Stroke = B(c), StrokeThickness = thick, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        }

        // ---------- tooltip ----------
        ToolTip MakeTip()
        {
            var tip = new ToolTip { Placement = PlacementMode.Top, HasDropShadow = false, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), VerticalOffset = -8, Template = Theme.TipTemplate };
            tip.Opened += (s, e) => tip.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Theme.Fast));
            return tip;
        }

        static void SetupTipService(DependencyObject o)
        {
            ToolTipService.SetInitialShowDelay(o, 250);
            ToolTipService.SetBetweenShowDelay(o, 0);
            ToolTipService.SetShowDuration(o, 30000);
            ToolTipService.SetPlacement(o, PlacementMode.Top);
        }

        void SetTipContent(ToolTip tip, string title, string pct, Color pctColor, string line1, string line2, string line3 = null)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var t = new TextBlock { Text = title, Foreground = B(Theme.Text), FontFamily = font, FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) };
            Grid.SetColumnSpan(t, 2); g.Children.Add(t);
            int col = 0;
            if (pct != null)
            {
                var p = new TextBlock { Text = pct, Foreground = B(pctColor), FontFamily = font, FontSize = 20, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
                Typography.SetNumeralAlignment(p, FontNumeralAlignment.Tabular);
                Grid.SetRow(p, 1); Grid.SetColumn(p, 0); g.Children.Add(p); col = 1;
            }
            var st = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            if (!string.IsNullOrEmpty(line1)) st.Children.Add(new TextBlock { Text = line1, Foreground = B(Theme.Text), FontFamily = font, FontSize = 12 });
            if (!string.IsNullOrEmpty(line2)) st.Children.Add(new TextBlock { Text = line2, Foreground = B(Theme.TextDim), FontFamily = font, FontSize = 11.5, Margin = new Thickness(0, 1, 0, 0) });
            Grid.SetRow(st, 1); Grid.SetColumn(st, col); Grid.SetColumnSpan(st, 2 - col); g.Children.Add(st);
            if (!string.IsNullOrEmpty(line3))      // e.g. "≈ 14 h left": its own row so it never crowds the status lines
            {
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var r = new TextBlock { Text = line3, Foreground = B(Theme.Text), FontFamily = font, FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) };
                Grid.SetRow(r, 2); Grid.SetColumnSpan(r, 2); g.Children.Add(r);
            }
            tip.Content = g;
        }

        // =====================================================================
        //  Drag to move (4 px threshold so a plain click never nudges the widget)
        // =====================================================================
        bool pressed, dragging; int dragStartX; double dragStartOffset;
        void OnDragStart(object s, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2) { e.Handled = true; pressed = false; ShowSizeBox(); return; }
            Native.POINT p; Native.GetCursorPos(out p);
            pressed = true; dragging = false; dragStartX = p.X; dragStartOffset = cfg.OffsetX;
            CaptureMouse(); e.Handled = true;
        }
        void OnDragMove(object s, MouseEventArgs e)
        {
            if (!pressed) return;
            Native.POINT p; Native.GetCursorPos(out p);
            if (!dragging)
            {
                if (Math.Abs(p.X - dragStartX) < 4 * curScale) return;
                dragging = true; Cursor = Cursors.SizeAll;
            }
            cfg.OffsetX = dragStartOffset + (p.X - dragStartX) / curScale;
            Reposition(true);
        }
        void OnDragEnd(object s, MouseButtonEventArgs e)
        {
            if (!pressed) return;
            pressed = false; ReleaseMouseCapture();
            if (dragging) { dragging = false; Cursor = null; cfg.OffsetX = Math.Round(cfg.OffsetX, 1); cfg.Save(); }
        }

        // =====================================================================
        //  Placement
        // =====================================================================
        void Tick()
        {
            if (hwnd == IntPtr.Zero) return;
            if (++ticks % 30 == 0) Native.TrimMemory();
            if (UpdateFullscreen()) return;
            Reposition(false);
        }

        /// <summary>Hides the widget while any window covers the whole screen it lives on (games, browser/video full screen, presentations).
        /// Decided by the foreground window's geometry, so it works for every app, not only the ones Windows flags as "busy".</summary>
        bool UpdateFullscreen()
        {
            bool full = IsFullscreenOnMyScreen();
            if (full != fullscreenHidden) { fullscreenHidden = full; ApplyVisibility(); }
            return full;
        }

        void ApplyVisibility() { Visibility = (fullscreenHidden || autohideHidden) ? Visibility.Hidden : Visibility.Visible; }
        bool Suppressed { get { return fullscreenHidden || autohideHidden; } }

        // ---- quick reactions (cheap): keeps the widget on top and follows an auto-hiding taskbar without a visible delay ----
        void FastTick()
        {
            if (hwnd == IntPtr.Zero) return;
            bool ah = Native.TaskbarAutoHide();
            if (ah != autoHideOn)
            {
                autoHideOn = ah;
                fast.Interval = TimeSpan.FromMilliseconds(ah ? 100 : 400);   // an auto-hiding taskbar slides in ~200 ms: watch it closely
                if (!ah && autohideHidden) { autohideHidden = false; ApplyVisibility(); }
                lastKey = "";
            }
            if (UpdateFullscreen()) return;
            if (autoHideOn) { Reposition(false); return; }          // also shows/hides with the taskbar
            Reassert();
        }

        IntPtr WndProc(IntPtr h, int msg, IntPtr wp, IntPtr lp, ref bool handled)
        {
            bool devices = msg == 0x0219;                                              // WM_DEVICECHANGE
            if (devices || msg == 0x007E || msg == 0x001A || (taskbarCreatedMsg != 0 && (uint)msg == taskbarCreatedMsg)) // display / settings / taskbar restarted
            {
                Reassert();
                foreach (int ms in new[] { 150, 700, 1800 })
                {
                    var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
                    t.Tick += (s, e) => { t.Stop(); try { lastKey = ""; Reposition(true); Reassert(); } catch { } };
                    t.Start();
                }
                if (devices) hub.RefreshSoon(1200);                                    // new reading right after Windows settles
            }
            return IntPtr.Zero;
        }

        bool IsFullscreenOnMyScreen()
        {
            if (Program.ForceVisible || hwnd == IntPtr.Zero) return false;
            var mons = Native.Monitors(); if (mons.Count == 0) return false;
            var target = PickMonitor(mons);
            IntPtr fg = Native.GetForegroundWindow(); RECT fr;
            if (fg == IntPtr.Zero || fg == hwnd || !Native.GetWindowRect(fg, out fr)) return false;
            uint pid; Native.GetWindowThreadProcessId(fg, out pid);
            if (pid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id) return false;
            string cls = Native.ClassOf(fg);
            if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd") return false; // the desktop itself
            // the foreground window must cover the target screen completely (taskbar area included)
            return fr.Left <= target.Rect.Left + 2 && fr.Top <= target.Rect.Top + 2 && fr.Right >= target.Rect.Right - 2 && fr.Bottom >= target.Rect.Bottom - 2;
        }

        double curScale = 1, fitCap = 2.2;
        IntPtr curTray; string lastKey = ""; bool inReposition, rebuildingForCap; DateTime lastDpiFix = DateTime.MinValue;
        Timer appsTimer;

        Native.Mon PickMonitor(List<Native.Mon> mons)
        {
            var prim = mons.FirstOrDefault(m => m.Primary) ?? mons[0];
            var sec = mons.FirstOrDefault(m => !m.Primary);
            return cfg.Monitor == "primary" ? prim : (sec ?? prim); // "auto"/"secondary": second screen when there is one
        }

        static IntPtr FindTray(Native.Mon t)
        {
            if (t.Primary) return Native.FindWindow("Shell_TrayWnd", null);
            IntPtr h = IntPtr.Zero;
            while ((h = Native.FindWindowEx(IntPtr.Zero, h, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
            {
                RECT r; if (!Native.GetWindowRect(h, out r)) continue;
                // match by horizontal position + any vertical overlap: an auto-hidden taskbar sits with only a sliver on the screen
                int cx = (r.Left + r.Right) / 2;
                if (cx >= t.Rect.Left && cx < t.Rect.Right && r.Top < t.Rect.Bottom && r.Bottom > t.Rect.Top) return h;
            }
            return IntPtr.Zero;
        }

        void Reposition(bool force)
        {
            if (hwnd == IntPtr.Zero) return;
            var mons = Native.Monitors(); if (mons.Count == 0) return;
            var t = PickMonitor(mons);
            // put the window on the target monitor first, so it (and WPF's layout) takes that monitor's DPI
            RECT cur;
            if (Native.GetWindowRect(hwnd, out cur))
            {
                int ccx = (cur.Left + cur.Right) / 2, ccy = (cur.Top + cur.Bottom) / 2;
                if (ccx < t.Rect.Left || ccx >= t.Rect.Right || ccy < t.Rect.Top || ccy >= t.Rect.Bottom)
                    Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, t.Rect.Left + 40, t.Rect.Bottom - 80, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | (Suppressed ? 0u : Native.SWP_SHOWWINDOW));
            }
            uint wdpi = Native.GetDpiForWindow(hwnd);
            double scale = wdpi > 0 ? wdpi / 96.0 : t.Scale; curScale = scale;
            IntPtr tray = FindTray(t); curTray = tray;
            RECT tb;
            if (tray == IntPtr.Zero || !Native.GetWindowRect(tray, out tb))
                tb = new RECT { Left = t.Rect.Left, Right = t.Rect.Right, Bottom = t.Rect.Bottom, Top = t.Rect.Bottom - (int)(48 * scale) };
            int tbH = tb.Bottom - tb.Top;
            bool horizontal = (tb.Right - tb.Left) >= tbH;
            // auto-hide taskbar: the widget belongs to the taskbar, so it shows and hides together with it (no polling delay)
            bool tbHidden = autoHideOn && tray != IntPtr.Zero && (Math.Min(tb.Bottom, t.Rect.Bottom) - Math.Max(tb.Top, t.Rect.Top)) < tbH * 0.5;
            if (tbHidden != autohideHidden) { autohideHidden = tbHidden; ApplyVisibility(); }
            if (tbHidden) return;
            if (!autoHideOn && horizontal && tbH < (int)(30 * scale)) // taskbar reported as a sliver: use a normal-height strip at the screen edge
            {
                if (tb.Top > t.Rect.Top + (t.Rect.Bottom - t.Rect.Top) / 2) { tb.Bottom = t.Rect.Bottom; tb.Top = tb.Bottom - (int)(48 * scale); }
                else { tb.Top = t.Rect.Top; tb.Bottom = tb.Top + (int)(48 * scale); }
                tbH = tb.Bottom - tb.Top;
            }

            // never taller than the taskbar: cap the size so nothing sticks out over app windows
            double cap = Math.Max(0.6, ((horizontal ? tbH : 40 * scale) / scale - 6) / 32.0);
            if (Math.Abs(cap - fitCap) > 0.02 && !rebuildingForCap)
            {
                fitCap = cap; rebuildingForCap = true;
                try { lastSig = ""; Rebuild(); } finally { rebuildingForCap = false; }
                return;
            }

            // width = whatever WPF measured for the content (SizeToContent.Width): can never clip, whatever the DPI
            RECT wr0; Native.GetWindowRect(hwnd, out wr0);
            int w = Math.Max(10, wr0.Right - wr0.Left);
            int appsRight = AppsRight(tray);
            string key = string.Join(",", tb.Left, tb.Top, tb.Right, tb.Bottom, t.Rect.Left, t.Rect.Top, scale, w, appsRight, cfg.OffsetX);
            if (!force && key == lastKey)
            {
                // still re-assert topmost (explorer steals z-order when interacted with)
                Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
                return;
            }
            lastKey = key;
            Log.W("reposition mon=" + t.Rect.Left + "," + t.Rect.Top + " scale=" + scale + " w=" + w + " tb=" + tb.Left + "," + tb.Top + "," + tb.Right + "," + tb.Bottom + " apps=" + appsRight);

            int hgt = horizontal ? tbH : (int)(44 * scale), x, y;
            if (horizontal)
            {
                int right = t.Primary ? tb.Right - (int)(250 * scale) : tb.Right - (int)(170 * scale);
                RECT nr; IntPtr notify = tray == IntPtr.Zero ? IntPtr.Zero : Native.FindWindowEx(tray, IntPtr.Zero, "TrayNotifyWnd", null);
                if (notify != IntPtr.Zero && Native.GetWindowRect(notify, out nr) && nr.Right > nr.Left && nr.Left > tb.Left + (tb.Right - tb.Left) / 2) right = nr.Left;
                int baseX = right - w - (int)(4 * scale);
                x = baseX + (int)(cfg.OffsetX * scale);
                int minVisible = Math.Max(tb.Left + (int)(4 * scale), appsRight > 0 ? appsRight + (int)(6 * scale) : 0);
                int minX = minVisible, maxX = tb.Right - w - (int)(2 * scale);
                if (x > maxX) x = maxX;
                if (x < minX) x = minX;           // never over the taskbar's app buttons
                if (dragging) cfg.OffsetX = (x - baseX) / scale; // while dragging, don't keep "winding up" past the limits
                y = tb.Top;
            }
            else { x = tb.Left + ((tb.Right - tb.Left) - w) / 2; y = tb.Bottom - hgt - (int)(80 * scale); }
            inReposition = true;
            try { Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, x, y, w, hgt, Native.SWP_NOACTIVATE | (Suppressed ? 0u : Native.SWP_SHOWWINDOW)); }
            finally { inReposition = false; }
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            lastKey = "";
            if ((DateTime.UtcNow - lastDpiFix).TotalMilliseconds > 700)
            {
                lastDpiFix = DateTime.UtcNow;
                Dispatcher.BeginInvoke(new Action(() => Reposition(true)));
            }
        }

        // ---- right edge of the taskbar's app buttons (UI Automation, polled rarely, off the UI thread) ----
        readonly Dictionary<IntPtr, int> appsEdge = new Dictionary<IntPtr, int>();
        int AppsRight(IntPtr tray)
        {
            if (tray == IntPtr.Zero) return 0;
            lock (appsEdge) { int v; return appsEdge.TryGetValue(tray, out v) ? v : 0; }
        }
        void StartAppsWatcher()
        {
            appsTimer = new Timer(_ =>
            {
                var tray = curTray; if (tray == IntPtr.Zero || fullscreenHidden) return;
                int edge = 0;
                try
                {
                    var root = System.Windows.Automation.AutomationElement.FromHandle(tray);
                    var all = root.FindAll(System.Windows.Automation.TreeScope.Descendants,
                        new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ClassNameProperty, "Taskbar.TaskListButtonAutomationPeer"));
                    foreach (System.Windows.Automation.AutomationElement el in all)
                    {
                        var r = el.Current.BoundingRectangle;
                        if (!r.IsEmpty && r.Right > edge && r.Width < 400) edge = (int)Math.Ceiling(r.Right);
                    }
                }
                catch { }
                bool changed;
                lock (appsEdge) { int old; appsEdge.TryGetValue(tray, out old); changed = Math.Abs(old - edge) > 2; appsEdge[tray] = edge; }
                if (changed) Dispatcher.BeginInvoke(new Action(() => Reposition(true)));
            }, null, 1500, 15000);
        }

        // =====================================================================
        //  Double click: size popover
        // =====================================================================
        Window sizeBox;
        Window BuildSizeBox()
        {
            var box = new Window { WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent, ShowInTaskbar = false, Topmost = true, ResizeMode = ResizeMode.NoResize, SizeToContent = SizeToContent.WidthAndHeight };
            double max = Math.Max(0.8, fitCap);
            var title = new TextBlock { Text = Loc.T("Tamaño"), Foreground = B(Theme.Text), FontFamily = font, FontSize = 13, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            var value = new TextBlock { Foreground = B(Theme.TextDim), FontFamily = font, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Typography.SetNumeralAlignment(value, FontNumeralAlignment.Tabular);
            var slider = new Slider { Style = Theme.SliderStyle, Minimum = 0.6, Maximum = max, Value = Math.Min(Math.Max(0.6, cfg.SizeFactor), max), SmallChange = 0.05, LargeChange = 0.1, TickFrequency = 0.05, IsSnapToTickEnabled = true, Margin = new Thickness(0, 10, 0, 0) };
            Action upd = () => { value.Text = (int)Math.Round(Sc() * 100) + "%"; };
            slider.ValueChanged += (s, e) => { cfg.SizeFactor = Math.Round(slider.Value, 2); upd(); Rebuild(); };
            var reset = new Button { Style = Theme.GhostButtonStyle, Content = Loc.T("Restablecer"), FontFamily = font, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 6, -6, -4) };
            reset.Click += (s, e) => slider.Value = Math.Min(1.0, slider.Maximum);
            var head = new Grid(); head.Children.Add(title); head.Children.Add(value);
            var stack = new StackPanel { Width = 232 };
            stack.Children.Add(head); stack.Children.Add(slider); stack.Children.Add(reset);
            box.Content = new Border { Background = B(Color.FromArgb(0xF5, 0x1B, 0x1E, 0x26)), BorderBrush = B(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(16, 13, 16, 10), Child = stack };
            upd();
            return box;
        }

        void ShowSizeBox()
        {
            if (sizeBox != null) { try { sizeBox.Close(); } catch { } return; }
            var box = BuildSizeBox(); sizeBox = box;
            box.Deactivated += (s, e) => { try { box.Close(); } catch { } };
            box.KeyDown += (s, e) => { if (e.Key == Key.Escape) box.Close(); };
            box.Closed += (s, e) => { sizeBox = null; cfg.Save(); };
            box.Loaded += (s, e) =>
            {
                var bh = new WindowInteropHelper(box).Handle;
                RECT wr; Native.GetWindowRect(hwnd, out wr);
                box.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                int bw = (int)Math.Ceiling(box.DesiredSize.Width * curScale), bhh = (int)Math.Ceiling(box.DesiredSize.Height * curScale);
                int x = (wr.Left + wr.Right) / 2 - bw / 2, y = wr.Top - bhh - (int)(8 * curScale);
                var mons = Native.Monitors(); var t = PickMonitor(mons);
                if (x + bw > t.Rect.Right) x = t.Rect.Right - bw - 4; if (x < t.Rect.Left) x = t.Rect.Left + 4;
                if (y < t.Rect.Top) y = wr.Bottom + 8;
                Native.SetWindowPos(bh, Native.HWND_TOPMOST, x, y, bw, bhh, 0);
                box.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Theme.Medium) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            };
            box.Show(); box.Activate();
        }

        // =====================================================================
        //  Context menu
        // =====================================================================
        MenuItem Mi(string header)
        {
            return new MenuItem { Header = Loc.T(header), Style = Theme.MenuItemStyle };
        }
        Separator Sep() { return new Separator { Style = Theme.SeparatorStyle }; }

        MenuItem Radio(string header, bool on, Action act)
        {
            var mi = Mi(header); mi.IsCheckable = true; mi.IsChecked = on;
            mi.Click += (s, e) => { act(); cfg.Save(); lastSig = ""; Rebuild(); };
            return mi;
        }

        ContextMenu BuildMenu()
        {
            var m = new ContextMenu { Style = Theme.ContextMenuStyle };
            var design = Mi("Diseño");
            foreach (var p in new[] { new[] { "unified", "Unificado" }, new[] { "separate", "Separado" }, new[] { "minimal", "Minimalista" }, new[] { "ring", "Anillo" } })
            { var v = p[0]; design.Items.Add(Radio(p[1], DesignKey() == v, () => cfg.Design = v)); }
            m.Items.Add(design);

            var bg = Mi("Fondo");
            foreach (var p in new[] { new[] { "dark", "Oscuro" }, new[] { "black", "Negro" }, new[] { "gray", "Grafito" }, new[] { "navy", "Azul noche" }, new[] { "violet", "Violeta" }, new[] { "glass", "Cristal" }, new[] { "none", "Sin fondo" } })
            { var v = p[0]; bg.Items.Add(Radio(p[1], cfg.Background == v, () => cfg.Background = v)); }
            m.Items.Add(bg);

            var shape = Mi("Forma");
            foreach (var p in new[] { new[] { "rounded", "Redondeada" }, new[] { "pill", "Cápsula" }, new[] { "square", "Cuadrada" } })
            { var v = p[0]; shape.Items.Add(Radio(p[1], cfg.Shape == v, () => cfg.Shape = v)); }
            m.Items.Add(shape);

            var size = Mi("Tamaño");
            foreach (var p in new[] { new object[] { 0.7, "Muy chico" }, new object[] { 0.85, "Chico" }, new object[] { 1.0, "Mediano" }, new object[] { 1.2, "Grande" }, new object[] { 1.5, "Muy grande" } })
            { double v = (double)p[0]; size.Items.Add(Radio((string)p[1], Math.Abs(cfg.SizeFactor - v) < 0.01, () => cfg.SizeFactor = v)); }
            size.Items.Add(Sep());
            size.Items.Add(new MenuItem { Header = Loc.T("Más ajustes: doble clic en el widget"), Style = Theme.MenuItemStyle, IsEnabled = false });
            m.Items.Add(size);

            m.Items.Add(Radio("Mostrar porcentaje", cfg.ShowPercent, () => cfg.ShowPercent = !cfg.ShowPercent));
            m.Items.Add(Sep());

            var devs = Mi("Dispositivos");
            var all = hub.Snapshot();
            if (all.Count == 0) devs.Items.Add(new MenuItem { Header = Loc.T("Ninguno conectado"), Style = Theme.MenuItemStyle, IsEnabled = false });
            foreach (var d in all)
            {
                var k = d.Key;
                devs.Items.Add(Radio(d.Name, !cfg.Hidden.Contains(k), () => { if (cfg.Hidden.Contains(k)) cfg.Hidden.Remove(k); else cfg.Hidden.Add(k); }));
            }
            if (cfg.Hidden.Count > 0)
            {
                devs.Items.Add(Sep());
                var showAll = Mi("Mostrar todos"); showAll.Header = Loc.T("Mostrar todos") + " (" + cfg.Hidden.Count + " " + Loc.T("ocultos") + ")";
                showAll.Click += (s, e) => { cfg.Hidden.Clear(); cfg.Save(); lastSig = ""; Rebuild(); };
                devs.Items.Add(showAll);
            }
            m.Items.Add(devs);

            if (hub.Snapshot().Any(d => d.Estimated))
            {
                var est = Mi("Batería del parlante");
                Action<string, Action> add = (h, a) => { var mi = Mi(h); mi.Click += (s, e) => { a(); hub.RefreshNow(); }; est.Items.Add(mi); };
                add("Recién cargado (100%)", () => Estimator.Instance.MarkFull());
                add("Ajustar a 75%", () => Estimator.Instance.SetLevel(75));
                add("Ajustar a 50%", () => Estimator.Instance.SetLevel(50));
                add("Ajustar a 25%", () => Estimator.Instance.SetLevel(25));
                add("Ajustar a 10%", () => Estimator.Instance.SetLevel(10));
                est.Items.Add(Sep());
                add("Se apagó por batería (calibrar)", () => Estimator.Instance.MarkEmpty());
                est.Items.Add(new MenuItem { Header = Loc.T("Autonomía aprendida") + ": " + Estimator.Instance.LifeHours.ToString("0.0", CultureInfo.InvariantCulture) + " h", Style = Theme.MenuItemStyle, IsEnabled = false });
                m.Items.Add(est);
            }

            var al = Mi("Avisos");
            al.Items.Add(Radio("Batería baja (20 % y 10 %)", cfg.AlertLow, () => cfg.AlertLow = !cfg.AlertLow));
            al.Items.Add(Radio("Cargada al 100 %", cfg.AlertFull, () => cfg.AlertFull = !cfg.AlertFull));
            al.Items.Add(Sep());
            al.Items.Add(Radio("Sonido", cfg.AlertSound, () => cfg.AlertSound = !cfg.AlertSound));
            m.Items.Add(al);

            var up = Mi("Actualizaciones");
            up.Items.Add(Radio("Buscar automáticamente", cfg.CheckUpdates, () => cfg.CheckUpdates = !cfg.CheckUpdates));
            var chk = Mi("Buscar ahora"); chk.Click += (s, e) => CheckForUpdates(true); up.Items.Add(chk);
            up.Items.Add(Sep());
            up.Items.Add(new MenuItem { Header = "v" + Updater.Current.ToString(3), Style = Theme.MenuItemStyle, IsEnabled = false });
            m.Items.Add(up);

            var mon = Mi("Pantalla");
            foreach (var p in new[] { new[] { "auto", "Automática" }, new[] { "primary", "Principal" }, new[] { "secondary", "Secundaria" } })
            { var v = p[0]; mon.Items.Add(Radio(p[1], cfg.Monitor == v, () => { cfg.Monitor = v; cfg.OffsetX = 0; })); }
            m.Items.Add(mon);

            var pos = Mi("Restablecer posición");
            pos.Click += (s, e) => { cfg.OffsetX = 0; cfg.Save(); Reposition(true); };
            m.Items.Add(pos);
            m.Items.Add(Sep());

            var rf = Mi("Actualizar ahora"); rf.Click += (s, e) => hub.RefreshNow(); m.Items.Add(rf);
            var dg = Mi("Guardar diagnóstico");
            dg.Click += (s, e) => System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try { var p = Diagnostics.Write(); System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + p + "\""); }
                catch (Exception dex) { Log.Err("diagnostics: " + dex); }
            });
            m.Items.Add(dg);
            var auto = Mi("Iniciar con Windows"); auto.IsCheckable = true; auto.IsChecked = Config.GetAutoStart();
            auto.Click += (s, e) => { bool want = auto.IsChecked; System.Threading.ThreadPool.QueueUserWorkItem(_ => Config.SetAutoStart(want, true)); }; m.Items.Add(auto);
            m.Items.Add(Sep());
            var ex = Mi("Salir"); ex.Click += (s, e) => { UserQuit.Mark(); Application.Current.Shutdown(); }; m.Items.Add(ex);
            return m;
        }

        // =====================================================================
        //  Self-test: render the real UI to PNGs (used to review the design without a screen grab)
        // =====================================================================
        public void RunSelfTest(string dir)
        {
            Directory.CreateDirectory(dir);
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            t.Tick += (s, e) =>
            {
                t.Stop();
                try
                {
                    Shot(host, System.IO.Path.Combine(dir,"widget.png"), 3, Color.FromRgb(0x2A, 0x2D, 0x36));
                    var menu = BuildMenu(); menu.PlacementTarget = host; menu.Placement = PlacementMode.Top; menu.IsOpen = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Shot(menu, System.IO.Path.Combine(dir,"menu.png"), 2, Color.FromRgb(0x2A, 0x2D, 0x36));
                        var first = menu.Items[0] as MenuItem; first.IsSubmenuOpen = true;
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var pop = first.Template.FindName("PART_Popup", first) as Popup;
                            if (pop != null && pop.Child != null) Shot((FrameworkElement)pop.Child, System.IO.Path.Combine(dir,"submenu.png"), 2, Color.FromRgb(0x2A, 0x2D, 0x36));
                            menu.IsOpen = false;
                            if (views.Count > 0) { views[0].Tip.PlacementTarget = views[0].Hit; views[0].Tip.IsOpen = true; }
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (views.Count > 0) { Shot(views[0].Tip, System.IO.Path.Combine(dir,"tooltip.png"), 3, Color.FromRgb(0x2A, 0x2D, 0x36)); views[0].Tip.IsOpen = false; }
                                var box = BuildSizeBox(); box.Show();
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    Shot((FrameworkElement)box.Content, System.IO.Path.Combine(dir,"sizebox.png"), 3, Color.FromRgb(0x2A, 0x2D, 0x36));
                                    box.Close(); Application.Current.Shutdown();
                                }), DispatcherPriority.ContextIdle);
                            }), DispatcherPriority.ContextIdle);
                        }), DispatcherPriority.ContextIdle);
                    }), DispatcherPriority.ContextIdle);
                }
                catch (Exception ex) { Log.W("selftest: " + ex); Application.Current.Shutdown(); }
            };
            t.Start();
        }

        static void Shot(FrameworkElement el, string path, double zoom, Color backdrop)
        {
            double w = Math.Max(1, el.ActualWidth), h = Math.Max(1, el.ActualHeight);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(backdrop), null, new Rect(0, 0, w + 24, h + 24));
                dc.DrawRectangle(new VisualBrush(el), null, new Rect(12, 12, w, h));
            }
            var rtb = new RenderTargetBitmap((int)Math.Ceiling((w + 24) * zoom), (int)Math.Ceiling((h + 24) * zoom), 96 * zoom, 96 * zoom, PixelFormats.Pbgra32);
            rtb.Render(dv);
            var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(rtb));
            using (var fs = File.Create(path)) enc.Save(fs);
        }
    }
}
