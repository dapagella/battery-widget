using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BatteryWidget
{
    public sealed class ToastItem
    {
        public string Title, Body, Button;
        public Color Accent = Theme.Good;
        public Kind Kind = Kind.Other;
        public Action OnButton;
        public int Seconds = 9;
        public string Sound;          // "warn" | "ok" | null
    }

    /// <summary>
    /// Small notification card that rises from the widget. Never takes focus, never covers a full-screen app
    /// (it waits until the screen is free), one at a time, fades in and out.
    /// </summary>
    public sealed class Toaster
    {
        readonly WidgetWindow owner;
        readonly Queue<ToastItem> queue = new Queue<ToastItem>();
        readonly DispatcherTimer retry = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        Window current;

        public Toaster(WidgetWindow owner) { this.owner = owner; retry.Tick += (s, e) => { retry.Stop(); Pump(); }; }

        public void Show(ToastItem t)
        {
            // an identical message already waiting (same device, same level) is not worth showing twice
            if (queue.Any(q => q.Title == t.Title && q.Body == t.Body)) return;
            queue.Enqueue(t);
            while (queue.Count > 6) queue.Dequeue();
            Pump();
        }

        public bool Active { get { return current != null; } }
        public void DismissCurrent() { if (current != null) Close(current); }

        void Pump()
        {
            if (current != null || queue.Count == 0) return;
            if (owner.IsSuppressed) { retry.Start(); return; }       // full-screen app or hidden taskbar: try again in a moment
            Display(queue.Dequeue());
        }

        void Display(ToastItem t)
        {
            var font = new FontFamily("Segoe UI Variable Text, Segoe UI");
            var win = new Window { WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent, ShowInTaskbar = false, Topmost = true, ResizeMode = ResizeMode.NoResize, ShowActivated = false, SizeToContent = SizeToContent.WidthAndHeight };
            current = win;

            var icon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(Icons.For(t.Kind)), Stroke = Theme.Brush(t.Accent), StrokeThickness = 1.75, Fill = Brushes.Transparent, Width = 22, Height = 22, Stretch = Stretch.Uniform,
                StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 1, 12, 0)
            };
            var text = new StackPanel { Width = 236 };
            text.Children.Add(new TextBlock { Text = t.Title, Foreground = Theme.Brush(Theme.Text), FontFamily = font, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            text.Children.Add(new TextBlock { Text = t.Body, Foreground = Theme.Brush(Theme.TextDim), FontFamily = font, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) });
            if (!string.IsNullOrEmpty(t.Button))
            {
                var b = new Button { Style = Theme.GhostButtonStyle, Content = t.Button, FontFamily = font, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-8, 8, 0, -4) };
                b.Click += (s, e) => { e.Handled = true; try { if (t.OnButton != null) t.OnButton(); } finally { Close(win); } };
                text.Children.Add(b);
            }
            var row = new StackPanel { Orientation = Orientation.Horizontal }; row.Children.Add(icon); row.Children.Add(text);
            var card = new Border
            {
                Background = Theme.Brush(Color.FromArgb(0xF5, 0x1B, 0x1E, 0x26)), BorderBrush = Theme.Brush(Theme.WithAlpha(t.Accent, 0x99)), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12), Padding = new Thickness(16, 13, 16, 13), Child = row, Cursor = Cursors.Hand
            };
            win.Content = card;
            win.MouseLeftButtonUp += (s, e) => Close(win);                // click anywhere on the card to dismiss
            win.SourceInitialized += (s, e) =>
            {
                var h = new WindowInteropHelper(win).Handle;
                Native.SetWindowLong(h, Native.GWL_EXSTYLE, Native.GetWindowLong(h, Native.GWL_EXSTYLE) | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE);
            };
            win.Loaded += (s, e) =>
            {
                var h = new WindowInteropHelper(win).Handle;
                RECT wr; Native.GetWindowRect(owner.Handle, out wr);
                double sc = owner.CurrentScale;
                win.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                int bw = (int)Math.Ceiling(win.DesiredSize.Width * sc), bh = (int)Math.Ceiling(win.DesiredSize.Height * sc);
                int x = wr.Right - bw, y = wr.Top - bh - (int)(10 * sc);
                foreach (var m in Native.Monitors())
                    if ((wr.Left + wr.Right) / 2 >= m.Rect.Left && (wr.Left + wr.Right) / 2 < m.Rect.Right)
                    {
                        if (x + bw > m.Rect.Right - 8) x = m.Rect.Right - bw - 8; if (x < m.Rect.Left + 8) x = m.Rect.Left + 8;
                        if (y < m.Rect.Top + 8) y = wr.Bottom + (int)(10 * sc);      // taskbar at the top: appear below it
                        if (y + bh > m.Rect.Bottom - 8) y = m.Rect.Bottom - bh - 8;  // never past the bottom edge either
                        break;
                    }
                Native.SetWindowPos(h, Native.HWND_TOPMOST, x, y, bw, bh, Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
                win.BeginAnimation(Window.OpacityProperty, new DoubleAnimation(0, 1, Theme.Medium) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            };
            win.Closed += (s, e) => { if (current == win) current = null; var gap = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) }; gap.Tick += (a, b) => { gap.Stop(); Pump(); }; gap.Start(); };

            if (t.Sound == "warn") System.Media.SystemSounds.Exclamation.Play();
            else if (t.Sound == "ok") System.Media.SystemSounds.Asterisk.Play();

            win.Opacity = 0; win.Show();
            var life = new DispatcherTimer { Interval = TimeSpan.FromSeconds(t.Seconds) };
            life.Tick += (s, e) => { life.Stop(); Close(win); };
            life.Start();
        }

        static void Close(Window w)
        {
            try
            {
                var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(160));
                fade.Completed += (s, e) => { try { w.Close(); } catch { } };
                w.BeginAnimation(Window.OpacityProperty, fade);
            }
            catch { }
        }
    }
}
