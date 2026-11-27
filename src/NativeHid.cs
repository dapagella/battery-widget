using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace BatteryWidget
{
    // HID conversations (write a report, read the answer) done with overlapped I/O.
    //
    // Buffers and OVERLAPPED structures given to a *pending* overlapped call must not move: the kernel keeps writing to the address
    // it was handed. Managed arrays can be relocated by the garbage collector, which corrupted the heap and crashed the .NET runtime
    // (clr.dll, 0xC0000005) when several devices were probed while Windows was still signing in. So every buffer here lives in
    // unmanaged memory, and a timed-out call is cancelled AND waited for before its memory is released.
    public static partial class Native
    {
        // One HID conversation at a time: stops the sources from fighting over a device (or each other) during the busy first seconds.
        static readonly object HidGate = new object();
        static readonly int OvSize = IntPtr.Size == 8 ? 32 : 20;
        static readonly int OvEventOffset = IntPtr.Size == 8 ? 24 : 16;

        [DllImport("kernel32.dll", EntryPoint = "ReadFile", SetLastError = true)] static extern bool ReadFileN(SafeFileHandle h, IntPtr buf, int n, IntPtr read, IntPtr ov);
        [DllImport("kernel32.dll", EntryPoint = "WriteFile", SetLastError = true)] static extern bool WriteFileN(SafeFileHandle h, IntPtr buf, int n, IntPtr written, IntPtr ov);
        [DllImport("kernel32.dll", EntryPoint = "GetOverlappedResult", SetLastError = true)] static extern bool GetOverlappedResultN(SafeFileHandle h, IntPtr ov, out int n, bool wait);

        static IntPtr NewOverlapped(IntPtr ev)
        {
            var p = Marshal.AllocHGlobal(OvSize);
            for (int i = 0; i < OvSize; i++) Marshal.WriteByte(p, i, 0);
            Marshal.WriteIntPtr(p, OvEventOffset, ev);
            return p;
        }

        /// <summary>orphaned = the cancelled request still has not finished after 2 s (a driver that never answers): its memory is then
        /// deliberately leaked instead of freed, because freeing memory the kernel may still write to is what corrupts the heap.</summary>
        static bool Finish(SafeFileHandle h, IntPtr ev, IntPtr ov, int timeoutMs, out int count, out bool orphaned)
        {
            count = 0; orphaned = false;
            if (WaitForSingleObject(ev, (uint)Math.Max(0, timeoutMs)) != 0)
            {
                CancelIo(h);
                if (WaitForSingleObject(ev, 2000) != 0) { orphaned = true; return false; }     // the cancel itself completes the request
                int dummy; GetOverlappedResultN(h, ov, out dummy, false);
                return false;
            }
            return GetOverlappedResultN(h, ov, out count, false);
        }

        static string WriteReport(SafeFileHandle h, IntPtr ev, byte[] data)
        {
            IntPtr buf = Marshal.AllocHGlobal(data.Length), ov = NewOverlapped(ev); bool orphaned = false;
            try
            {
                Marshal.Copy(data, 0, buf, data.Length); ResetEvent(ev);
                if (!WriteFileN(h, buf, data.Length, IntPtr.Zero, ov))
                {
                    int e = Marshal.GetLastWin32Error();
                    if (e != 997) return "write " + e;
                    int n; if (!Finish(h, ev, ov, 1000, out n, out orphaned)) return "write timeout";
                }
                return null;
            }
            finally { if (!orphaned) { Marshal.FreeHGlobal(buf); Marshal.FreeHGlobal(ov); } }
        }

        /// <summary>One input report, or null on timeout/error (error = Win32 code, 0 for a plain timeout).</summary>
        static byte[] ReadReport(SafeFileHandle h, IntPtr ev, int len, int timeoutMs, out int error)
        {
            error = 0;
            IntPtr buf = Marshal.AllocHGlobal(len), ov = NewOverlapped(ev); bool orphaned = false;
            try
            {
                ResetEvent(ev);
                int cnt = 0;
                if (!ReadFileN(h, buf, len, IntPtr.Zero, ov))
                {
                    int e = Marshal.GetLastWin32Error();
                    if (e != 997) { error = e; return null; }
                    if (!Finish(h, ev, ov, timeoutMs, out cnt, out orphaned)) return null;
                }
                else if (!GetOverlappedResultN(h, ov, out cnt, false)) return null;
                if (cnt <= 0) return null;
                var r = new byte[cnt]; Marshal.Copy(buf, r, 0, cnt); return r;
            }
            finally { if (!orphaned) { Marshal.FreeHGlobal(buf); Marshal.FreeHGlobal(ov); } }
        }

        // read/write first; read-only if Windows refuses (some collections are owned by the system)
        static SafeFileHandle OpenHid(string path)
        {
            var h = CreateFile(path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
            if (h.IsInvalid) h = CreateFile(path, 0x80000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
            return h;
        }

        static string TryFeature(SafeFileHandle h, HidInfo info, byte id)
        {
            if (info.FeatLen == 0) return "n/a";
            var f = new byte[info.FeatLen]; f[0] = id;
            return HidD_GetFeature(h, f, f.Length) ? "ok" : "failed (" + Marshal.GetLastWin32Error() + ")";
        }

        /// <summary>Diagnostics: result of the feature reads and the first few reports a collection sends (id, length, first bytes).</summary>
        public static string HidSample(HidInfo info, int timeoutMs, params byte[] featureIds)
        {
            lock (HidGate)
            {
                var sb = new StringBuilder();
                var h = OpenHid(info.Path);
                if (h.IsInvalid) return "open failed (error " + Marshal.GetLastWin32Error() + ")";
                IntPtr ev = CreateEvent(IntPtr.Zero, true, false, null);
                try
                {
                    foreach (var id in featureIds) sb.Append("feature 0x" + id.ToString("X2") + " " + TryFeature(h, info, id) + "; ");
                    var deadline = Environment.TickCount + timeoutMs; int n = 0;
                    while (n < 4)
                    {
                        int left = deadline - Environment.TickCount; if (left <= 0) break;
                        int err; var r = ReadReport(h, ev, info.InLen, left, out err);
                        if (r == null) { if (err != 0) sb.Append("read error " + err); break; }
                        sb.Append("[id " + r[0].ToString("X2") + " len " + r.Length + ": " + BitConverter.ToString(r, 0, Math.Min(r.Length, 14)) + "] "); n++;
                    }
                    if (n == 0) sb.Append("no input reports");
                }
                finally { CloseHandle(ev); h.Close(); }
                return sb.ToString();
            }
        }

        /// <summary>Optionally reads feature reports (some pads only start streaming full reports after that), then listens for
        /// input reports until accept() is satisfied or the time is up. Sends nothing.</summary>
        public static byte[] HidListen(HidInfo info, int timeoutMs, Func<byte[], bool> accept, params byte[] featureIds)
        {
            lock (HidGate)
            {
                var h = OpenHid(info.Path);
                if (h.IsInvalid) return null;
                IntPtr ev = CreateEvent(IntPtr.Zero, true, false, null);
                try
                {
                    foreach (var id in featureIds) TryFeature(h, info, id);
                    var deadline = Environment.TickCount + timeoutMs;
                    while (true)
                    {
                        int left = deadline - Environment.TickCount; if (left <= 0) break;
                        int err; var r = ReadReport(h, ev, info.InLen, left, out err);
                        if (r == null) { if (err != 0) break; continue; }
                        if (accept(r)) return r;
                    }
                }
                finally { CloseHandle(ev); h.Close(); }
                return null;
            }
        }

        /// <summary>Sends one output report and collects reports for up to timeoutMs. Returns the list of input reports.</summary>
        public static List<byte[]> HidExchange(HidInfo info, byte[] output, int timeoutMs, out string error, bool preRead = true, Func<byte[], bool> accept = null)
        {
            error = null;
            var got = new List<byte[]>();
            lock (HidGate)
            {
                var h = CreateFile(info.Path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
                if (h.IsInvalid) { error = "open " + Marshal.GetLastWin32Error(); return got; }
                IntPtr ev = CreateEvent(IntPtr.Zero, true, false, null);
                try
                {
                    // some firmwares (HyperX Kingston) expect an input-report read before the first write
                    if (preRead) try { var pre = new byte[info.InLen]; pre[0] = 0x06; HidD_GetInputReport(h, pre, pre.Length); } catch { }
                    var pkt = new byte[info.OutLen]; Array.Copy(output, pkt, Math.Min(output.Length, pkt.Length));
                    var werr = WriteReport(h, ev, pkt);
                    if (werr != null) { error = werr; return got; }
                    var deadline = Environment.TickCount + timeoutMs;
                    while (true)
                    {
                        int left = deadline - Environment.TickCount; if (left <= 0) break;
                        int err; var r = ReadReport(h, ev, info.InLen, left, out err);
                        if (r == null) { if (err != 0) error = "read " + err; break; }
                        got.Add(r);
                        if (accept != null && accept(r)) break;
                    }
                }
                finally { CloseHandle(ev); h.Close(); }
                return got;
            }
        }
    }
}
