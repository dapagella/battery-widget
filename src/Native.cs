using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace BatteryWidget
{
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVPROPKEY { public Guid fmtid; public uint pid; public DEVPROPKEY(string g, uint p) { fmtid = new Guid(g); pid = p; } }

    public static class Native
    {
        // ---- window / taskbar ----
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int idx);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr h, int idx, int val);
        [DllImport("shell32.dll")] public static extern int SHQueryUserNotificationState(out int state);
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 1, SWP_NOMOVE = 2, SWP_NOACTIVATE = 0x10, SWP_SHOWWINDOW = 0x40;
        public const int GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
        public static string ClassOf(IntPtr h) { var sb = new StringBuilder(64); return GetClassName(h, sb, 64) > 0 ? sb.ToString() : ""; }
        public delegate void WinEventProc(IntPtr hook, uint evt, IntPtr hwnd, int idObj, int idChild, uint thread, uint time);
        [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr mod, WinEventProc proc, uint pid, uint tid, uint flags);
        [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hook);
        /// <summary>Calls back the moment the foreground window changes or a menu/flyout opens or closes (out-of-context, no polling).</summary>
        public static IntPtr HookZOrderEvents(WinEventProc proc)
        {
            IntPtr a = SetWinEventHook(0x0003, 0x0003, IntPtr.Zero, proc, 0, 0, 0);   // EVENT_SYSTEM_FOREGROUND
            SetWinEventHook(0x0006, 0x0007, IntPtr.Zero, proc, 0, 0, 0);               // menu popup start/end
            SetWinEventHook(0x000A, 0x000B, IntPtr.Zero, proc, 0, 0, 0);               // move/size end (flyouts, Start)
            return a;
        }
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);
        /// <summary>Per-monitor-v2 DPI awareness: real pixel coordinates on every screen, no bitmap stretching.</summary>
        public static void MakeDpiAware() { try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { } }

        // ---- monitors ----
        [StructLayout(LayoutKind.Sequential)] struct MONITORINFO { public int cbSize; public RECT rcMonitor, rcWork; public int dwFlags; }
        delegate bool MonitorEnumProc(IntPtr h, IntPtr dc, ref RECT r, IntPtr data);
        [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorEnumProc cb, IntPtr data);
        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr h, ref MONITORINFO mi);
        [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr h, int type, out uint x, out uint y);
        public class Mon { public IntPtr H; public RECT Rect, Work; public bool Primary; public double Scale = 1; }

        public static List<Mon> Monitors()
        {
            var list = new List<Mon>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr h, IntPtr dc, ref RECT r, IntPtr d) =>
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (GetMonitorInfo(h, ref mi))
                {
                    var m = new Mon { H = h, Rect = mi.rcMonitor, Work = mi.rcWork, Primary = (mi.dwFlags & 1) != 0 };
                    uint dx, dy; try { if (GetDpiForMonitor(h, 0, out dx, out dy) == 0 && dx > 0) m.Scale = dx / 96.0; } catch { }
                    list.Add(m);
                }
                return true;
            }, IntPtr.Zero);
            return list;
        }

        // ---- process power ----
        [StructLayout(LayoutKind.Sequential)] struct PROCESS_POWER_THROTTLING_STATE { public uint Version, ControlMask, StateMask; }
        [DllImport("kernel32.dll")] static extern bool SetProcessInformation(IntPtr h, int cls, ref PROCESS_POWER_THROTTLING_STATE s, int size);
        [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] static extern bool SetProcessWorkingSetSize(IntPtr h, IntPtr min, IntPtr max);
        public static void TrimMemory() { try { SetProcessWorkingSetSize(GetCurrentProcess(), new IntPtr(-1), new IntPtr(-1)); } catch { } }

        public static void EnableEcoMode()
        {
            try
            {
                var s = new PROCESS_POWER_THROTTLING_STATE { Version = 1, ControlMask = 1, StateMask = 1 };
                SetProcessInformation(GetCurrentProcess(), 4, ref s, Marshal.SizeOf(s));
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            }
            catch { }
        }

        // ---- XInput ----
        [StructLayout(LayoutKind.Sequential)] public struct XINPUT_BATTERY_INFORMATION { public byte BatteryType, BatteryLevel; }
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation")] public static extern int XInputGetBatteryInformation(int user, byte devType, out XINPUT_BATTERY_INFORMATION info);

        // ---- Cfgmgr32 (Bluetooth device properties) ----
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)] static extern int CM_Get_Device_ID_List_SizeW(out int len, string filter, uint flags);
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)] static extern int CM_Get_Device_ID_ListW(string filter, byte[] buf, int len, uint flags);
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)] static extern int CM_Locate_DevNodeW(out uint devInst, string id, uint flags);
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)] static extern int CM_Get_DevNode_PropertyW(uint devInst, ref DEVPROPKEY key, out uint type, byte[] buf, ref int size, uint flags);

        public static readonly DEVPROPKEY KeyName = new DEVPROPKEY("b725f130-47ef-101a-a5f1-02608c9eebac", 10);
        public static readonly DEVPROPKEY KeyConnected = new DEVPROPKEY("83da6326-97a6-4088-9453-a1923f573b29", 15);
        public static readonly DEVPROPKEY KeyBtBattery = new DEVPROPKEY("104EA319-6EE2-4701-BD47-8DDBF425BBE5", 2);

        public static List<string> BluetoothDeviceIds()
        {
            var list = new List<string>();
            foreach (var en in new[] { "BTHENUM", "BTHLE" })    // classic Bluetooth and Bluetooth LE (most modern mice/keyboards)
            {
                int len;
                if (CM_Get_Device_ID_List_SizeW(out len, en, 1) != 0 || len <= 1) continue;
                var buf = new byte[len * 2];
                if (CM_Get_Device_ID_ListW(en, buf, len, 1) != 0) continue;
                foreach (var s in Encoding.Unicode.GetString(buf).Split('\0'))
                    if (s.StartsWith(en + "\\DEV_", StringComparison.OrdinalIgnoreCase)) list.Add(s);
            }
            return list;
        }

        public static byte[] GetDevProp(string instanceId, DEVPROPKEY key)
        {
            uint inst; if (CM_Locate_DevNodeW(out inst, instanceId, 0) != 0) return null;
            uint type; int size = 0;
            CM_Get_DevNode_PropertyW(inst, ref key, out type, null, ref size, 0);
            if (size <= 0) return null;
            var buf = new byte[size];
            if (CM_Get_DevNode_PropertyW(inst, ref key, out type, buf, ref size, 0) != 0) return null;
            return buf;
        }

        // ---- HID ----
        [StructLayout(LayoutKind.Sequential)] struct SP_DEVICE_INTERFACE_DATA { public int cbSize; public Guid g; public int flags; public IntPtr r; }
        [StructLayout(LayoutKind.Sequential)] struct HIDD_ATTRIBUTES { public int Size; public ushort VendorID, ProductID, VersionNumber; }
        [StructLayout(LayoutKind.Sequential)]
        struct HIDP_CAPS
        {
            public ushort Usage, UsagePage, InputReportByteLength, OutputReportByteLength, FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved;
            public ushort a, b, c, d, e, f, g, h, i, j;
        }
        [StructLayout(LayoutKind.Sequential)] struct OVERLAPPED { public IntPtr Internal, InternalHigh; public int Offset, OffsetHigh; public IntPtr hEvent; }
        [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid g);
        [DllImport("hid.dll")] static extern bool HidD_GetAttributes(SafeFileHandle h, ref HIDD_ATTRIBUTES a);
        [DllImport("hid.dll")] static extern bool HidD_GetPreparsedData(SafeFileHandle h, out IntPtr p);
        [DllImport("hid.dll")] static extern bool HidD_FreePreparsedData(IntPtr p);
        [DllImport("hid.dll")] static extern bool HidD_GetInputReport(SafeFileHandle h, byte[] b, int len);
        [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr p, out HIDP_CAPS c);
        [DllImport("setupapi.dll")] static extern IntPtr SetupDiGetClassDevs(ref Guid g, string e, IntPtr p, int f);
        [DllImport("setupapi.dll")] static extern bool SetupDiEnumDeviceInterfaces(IntPtr s, IntPtr d, ref Guid g, int i, ref SP_DEVICE_INTERFACE_DATA o);
        [DllImport("setupapi.dll", CharSet = CharSet.Auto)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr s, ref SP_DEVICE_INTERFACE_DATA d, IntPtr det, int sz, out int req, IntPtr di);
        [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr s);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern SafeFileHandle CreateFile(string n, uint acc, uint share, IntPtr sa, uint disp, uint fl, IntPtr t);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteFile(SafeFileHandle h, byte[] b, int n, IntPtr written, ref OVERLAPPED o);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadFile(SafeFileHandle h, byte[] b, int n, IntPtr read, ref OVERLAPPED o);
        [DllImport("kernel32.dll")] static extern IntPtr CreateEvent(IntPtr a, bool manual, bool initial, string name);
        [DllImport("kernel32.dll")] static extern bool ResetEvent(IntPtr e);
        [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr h, uint ms);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll")] static extern bool CancelIo(SafeFileHandle h);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetOverlappedResult(SafeFileHandle h, ref OVERLAPPED o, out int n, bool wait);

        [DllImport("hid.dll", CharSet = CharSet.Unicode)] static extern bool HidD_GetProductString(SafeFileHandle h, StringBuilder s, int len);
        [DllImport("hid.dll")] static extern bool HidD_SetFeature(SafeFileHandle h, byte[] b, int len);
        [DllImport("hid.dll")] static extern bool HidD_GetFeature(SafeFileHandle h, byte[] b, int len);

        public class HidInfo { public string Path, Product; public ushort Vid, Pid, UsagePage, Usage, InLen, OutLen, FeatLen; }

        /// <summary>Feature-report round trip (set, wait, get) used by Razer devices. buf[0] is the report id.</summary>
        public static byte[] HidFeature(HidInfo info, byte[] request, int delayMs)
        {
            var h = CreateFile(info.Path, 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (h.IsInvalid) return null;
            try
            {
                if (!HidD_SetFeature(h, request, request.Length)) return null;
                System.Threading.Thread.Sleep(delayMs);
                var resp = new byte[request.Length]; resp[0] = request[0];
                return HidD_GetFeature(h, resp, resp.Length) ? resp : null;
            }
            finally { h.Close(); }
        }

        public static List<HidInfo> FindHid(ushort vid, ushort[] pids)
        {
            var res = new List<HidInfo>();
            Guid g; HidD_GetHidGuid(out g);
            var s = SetupDiGetClassDevs(ref g, null, IntPtr.Zero, 0x12);
            try
            {
                for (int i = 0; ; i++)
                {
                    var d = new SP_DEVICE_INTERFACE_DATA(); d.cbSize = Marshal.SizeOf(d);
                    if (!SetupDiEnumDeviceInterfaces(s, IntPtr.Zero, ref g, i, ref d)) break;
                    int req; SetupDiGetDeviceInterfaceDetail(s, ref d, IntPtr.Zero, 0, out req, IntPtr.Zero);
                    var p = Marshal.AllocHGlobal(req);
                    string path = null;
                    try
                    {
                        Marshal.WriteInt32(p, IntPtr.Size == 8 ? 8 : 6);
                        if (SetupDiGetDeviceInterfaceDetail(s, ref d, p, req, out req, IntPtr.Zero))
                            path = Marshal.PtrToStringAuto(new IntPtr(p.ToInt64() + 4));
                    }
                    finally { Marshal.FreeHGlobal(p); }
                    if (path == null) continue;
                    // cheap pre-filter on the path text
                    var lp = path.ToLowerInvariant();
                    if (!lp.Contains("vid_" + vid.ToString("x4"))) continue;
                    bool pidOk = pids == null; if (pids != null) foreach (var pid in pids) if (lp.Contains("pid_" + pid.ToString("x4"))) pidOk = true;
                    if (!pidOk) continue;
                    var h = CreateFile(path, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
                    if (h.IsInvalid) continue;
                    var at = new HIDD_ATTRIBUTES(); at.Size = Marshal.SizeOf(at);
                    IntPtr pp;
                    if (HidD_GetAttributes(h, ref at) && HidD_GetPreparsedData(h, out pp))
                    {
                        HIDP_CAPS c; HidP_GetCaps(pp, out c); HidD_FreePreparsedData(pp);
                        var ps = new StringBuilder(128); HidD_GetProductString(h, ps, 256);
                        res.Add(new HidInfo { Path = path, Vid = at.VendorID, Pid = at.ProductID, Product = ps.ToString().Trim(), UsagePage = c.UsagePage, Usage = c.Usage, InLen = c.InputReportByteLength, OutLen = c.OutputReportByteLength, FeatLen = c.FeatureReportByteLength });
                    }
                    h.Close();
                }
            }
            finally { SetupDiDestroyDeviceInfoList(s); }
            return res;
        }

        /// <summary>Sends one output report and collects reports for up to timeoutMs. Returns list of input reports.</summary>
        public static List<byte[]> HidExchange(HidInfo info, byte[] output, int timeoutMs, out string error, bool preRead = true, Func<byte[], bool> accept = null)
        {
            error = null;
            var got = new List<byte[]>();
            var h = CreateFile(info.Path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000 /*OVERLAPPED*/, IntPtr.Zero);
            if (h.IsInvalid) { error = "open " + Marshal.GetLastWin32Error(); return got; }
            IntPtr ev = CreateEvent(IntPtr.Zero, true, false, null);
            try
            {
                // some firmwares (HyperX Kingston) expect an input-report read before the first write
                if (preRead) try { var pre = new byte[info.InLen]; pre[0] = 0x06; HidD_GetInputReport(h, pre, pre.Length); } catch { }
                var wo = new OVERLAPPED { hEvent = ev };
                var pkt = new byte[info.OutLen]; Array.Copy(output, pkt, Math.Min(output.Length, pkt.Length));
                bool ok = WriteFile(h, pkt, pkt.Length, IntPtr.Zero, ref wo);
                if (!ok)
                {
                    int e = Marshal.GetLastWin32Error();
                    if (e != 997) { error = "write " + e; return got; }
                    if (WaitForSingleObject(ev, 1000) != 0) { CancelIo(h); error = "write timeout"; return got; }
                    int n; if (!GetOverlappedResult(h, ref wo, out n, false)) { error = "write " + Marshal.GetLastWin32Error(); return got; }
                }
                var deadline = Environment.TickCount + timeoutMs;
                while (true)
                {
                    int left = deadline - Environment.TickCount; if (left <= 0) break;
                    var buf = new byte[info.InLen];
                    ResetEvent(ev);
                    var ro = new OVERLAPPED { hEvent = ev };
                    bool rok = ReadFile(h, buf, buf.Length, IntPtr.Zero, ref ro);
                    if (!rok && Marshal.GetLastWin32Error() != 997) { error = "read " + Marshal.GetLastWin32Error(); break; }
                    if (!rok)
                    {
                        if (WaitForSingleObject(ev, (uint)left) != 0) { CancelIo(h); WaitForSingleObject(ev, 50); break; }
                    }
                    int cnt; if (GetOverlappedResult(h, ref ro, out cnt, false) && cnt > 0) { var r = new byte[cnt]; Array.Copy(buf, r, cnt); got.Add(r); if (accept != null && accept(r)) break; }
                }
            }
            finally { CloseHandle(ev); h.Close(); }
            return got;
        }
    }
}
