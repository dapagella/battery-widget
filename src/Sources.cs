using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace BatteryWidget
{
    public enum Kind { Keyboard, Mouse, Headset, Gamepad, Speaker, Other }

    public class DeviceInfo
    {
        public string Key;          // stable id
        public string Name;
        public Kind Kind;
        public int Percent = -1;    // -1 unknown
        public bool Charging;
        public string Connection;   // "LIGHTSPEED", "Bluetooth", "USB"...
        public string Source;
        public bool Estimated;      // percentage is a model, not a reading
        public bool SameAs(DeviceInfo o) { return o != null && Estimated == o.Estimated && Key == o.Key && Name == o.Name && Percent == o.Percent && Charging == o.Charging && Connection == o.Connection && Kind == o.Kind; }
    }

    public interface ISource { string Name { get; } int IntervalSec { get; } List<DeviceInfo> Poll(); void Kick(); }

    public static class Log
    {
        public static bool Enabled;
        static readonly object L = new object();
        public static void W(string s)
        {
            if (!Enabled) return;
            try { lock (L) { Directory.CreateDirectory(Config.Dir); var lf = Path.Combine(Config.Dir, "debug.log"); if (File.Exists(lf) && new FileInfo(lf).Length > 1500000) File.Delete(lf); File.AppendAllText(Path.Combine(Config.Dir, "debug.log"), DateTime.Now.ToString("HH:mm:ss ") + s + "\r\n"); } } catch { }
        }
    }

    // ===================== Logitech G HUB (local websocket) =====================
    public class GHubSource : ISource
    {
        public string Name { get { return "ghub"; } }
        public int IntervalSec { get { return 20; } }
        public static volatile bool HasDevices;   // lets the direct HID++ reader step aside while G HUB is doing the job
        ClientWebSocket ws;
        readonly JavaScriptSerializer js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        readonly byte[] buf = new byte[1 << 20];
        int seq;

        public void Kick() { }

        bool Connect()
        {
            try
            {
                if (ws != null && ws.State == WebSocketState.Open) return true;
                if (ws != null) { try { ws.Dispose(); } catch { } }
                ws = new ClientWebSocket();
                ws.Options.AddSubProtocol("json");
                ws.Options.SetRequestHeader("Origin", "file://");
                if (!ws.ConnectAsync(new Uri("ws://127.0.0.1:9010"), CancellationToken.None).Wait(3000)) return false;
                Recv(1500); // hello
                return ws.State == WebSocketState.Open;
            }
            catch (Exception e) { Log.W("ghub connect: " + e.Message); ws = null; return false; }
        }

        string Recv(int ms)
        {
            var cts = new CancellationTokenSource(ms);
            var sb = new StringBuilder();
            try
            {
                WebSocketReceiveResult r;
                do { r = ws.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token).Result; sb.Append(Encoding.UTF8.GetString(buf, 0, r.Count)); } while (!r.EndOfMessage);
                return sb.ToString();
            }
            catch { return null; }
        }

        Dictionary<string, object> Request(string path)
        {
            string id = "m" + (++seq);
            var b = Encoding.UTF8.GetBytes("{\"msgId\":\"" + id + "\",\"verb\":\"GET\",\"path\":\"" + path + "\"}");
            ws.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Text, true, CancellationToken.None).Wait(2000);
            for (int i = 0; i < 6; i++) // skip unrelated pushed messages
            {
                var t = Recv(2500); if (t == null) return null;
                var d = js.DeserializeObject(t) as Dictionary<string, object>;
                if (d != null && d.ContainsKey("msgId") && (string)d["msgId"] == id) return d;
            }
            return null;
        }

        static object G(object o, params string[] path)
        {
            foreach (var p in path) { var d = o as Dictionary<string, object>; if (d == null || !d.ContainsKey(p)) return null; o = d[p]; }
            return o;
        }

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            if (!Connect()) return null; // G HUB not reachable right now
            try
            {
                var list = Request("/devices/list");
                var infos = G(list, "payload", "deviceInfos") as object[];
                if (infos == null) { try { ws.Abort(); } catch { } ws = null; return null; }
                foreach (var o in infos)
                {
                    if ((string)G(o, "state") != "ACTIVE") continue;
                    string id = (string)G(o, "id");
                    string type = (string)G(o, "deviceType") ?? "";
                    string name = (string)G(o, "displayName") ?? id;
                    string ext = (string)G(o, "extendedDisplayName") ?? "";
                    string ctype = (string)G(o, "connectionType") ?? "";
                    string dtype = (string)G(o, "displayConnectionType") ?? "";
                    var bat = Request("/battery/" + id + "/state");
                    var pl = G(bat, "payload");
                    if (pl == null) continue;
                    var pct = G(pl, "percentage");
                    if (pct == null) continue;
                    var di = new DeviceInfo { Key = "ghub:" + id, Name = name, Source = "ghub", Percent = Convert.ToInt32(pct) };
                    di.Charging = Convert.ToBoolean(G(pl, "charging") ?? false);
                    di.Kind = type == "KEYBOARD" ? Kind.Keyboard : type == "MOUSE" ? Kind.Mouse : type == "HEADSET" ? Kind.Headset : type == "GAMEPAD" ? Kind.Gamepad : Kind.Other;
                    string up = (dtype + " " + ext + " " + ctype).ToUpperInvariant();
                    di.Connection = up.Contains("LIGHTSPEED") ? "LIGHTSPEED" : up.Contains("BLUETOOTH") || up.Contains("BLE") ? "Bluetooth" : up.Contains("WIRELESS") ? "Inalámbrico" : "USB";
                    res.Add(di);
                }
            }
            catch (Exception e) { Log.W("ghub poll: " + e.Message); try { ws.Abort(); } catch { } ws = null; return null; }
            HasDevices = res.Count > 0;
            return res;
        }
    }

    // ===================== HyperX Cloud II Wireless (HID via USB receiver) =====================
    public class HyperXSource : ISource
    {
        public string Name { get { return "hyperx"; } }
        public int IntervalSec { get { return 30; } }
        static readonly ushort[] Pids = { 0x1718, 0x0b92, 0x16ea, 0x16eb };
        Native.HidInfo cached;
        bool had;
        public void Kick() { }

        static byte[] Packet(byte cmd)
        {
            return new byte[] { 0x06, 0x00, 0x02, 0x00, 0x9A, 0x00, 0x00, 0x68, 0x4A, 0x8E, 0x0A, 0x00, 0x00, 0x00, 0xBB, cmd };
        }

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            try
            {
                var cands = Native.FindHid(0x0951, Pids).Where(h => h.OutLen >= 32 && h.InLen >= 32)
                    .OrderByDescending(h => h.UsagePage >= 0xFF00).ToList();
                if (cands.Count == 0) { cached = null; return res; }
                foreach (var h in cands)
                {
                    string err;
                    var rb = Native.HidExchange(h, Packet(2), 1200, out err);
                    Log.W("hyperx " + h.UsagePage.ToString("X") + " battery err=" + err + " reports=" + string.Join(" | ", rb.Select(r => BitConverter.ToString(r, 0, Math.Min(r.Length, 12)))));
                    int pct = -1;
                    foreach (var r in rb) if (r.Length > 7 && r[0] == 0x0B && r[2] == 0xBB && r[3] == 2) pct = r[7];
                    if (pct <= 0 || pct > 100) continue; // 0 shows up while the headset is still waking up / linking
                    bool charging = false;
                    var rc = Native.HidExchange(h, Packet(3), 800, out err);
                    foreach (var r in rc) if (r.Length > 4 && r[0] == 0x0B && r[2] == 0xBB && r[3] == 3) charging = r[4] == 1;
                    res.Add(new DeviceInfo { Key = "hyperx:cloud2", Name = "HyperX Cloud II Wireless", Kind = Kind.Headset, Percent = pct, Charging = charging, Connection = "2.4 GHz", Source = "hyperx" });
                    cached = h; break;
                }
            }
            catch (Exception e) { Log.W("hyperx: " + e.Message); return null; }
            if (res.Count == 0 && had) { had = false; return null; } // one-poll grace if a read just failed
            had = res.Count > 0;
            return res;
        }
    }

    // ===================== Bluetooth devices (Windows device properties) =====================
    public class BluetoothSource : ISource
    {
        public string Name { get { return "bt"; } }
        public int IntervalSec { get { return 30; } }
        readonly Func<string[]> pinned;
        public BluetoothSource(Func<string[]> pinnedNames) { pinned = pinnedNames; }
        public void Kick() { }

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            try
            {
                var names = pinned();
                foreach (var id in Native.BluetoothDeviceIds())
                {
                    var nb = Native.GetDevProp(id, Native.KeyName); if (nb == null) continue;
                    string name = Encoding.Unicode.GetString(nb).TrimEnd('\0');
                    var cb = Native.GetDevProp(id, Native.KeyConnected);
                    bool connected = cb != null && cb.Length > 0 && cb[0] != 0;
                    var bb = Native.GetDevProp(id, Native.KeyBtBattery);
                    int pct = bb != null && bb.Length > 0 ? bb[0] : -1;
                    Log.W("bt " + name + " connected=" + connected + " battery=" + pct);
                    bool isPinned = names.Any(n => name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
                    bool estimate = pct < 0 && isPinned;
                    if (estimate && !connected) Estimator.Instance.Tick(false, false, 0); // keep the clock moving, no drain
                    if (!connected) continue;
                    if (estimate)
                    {
                        double vol; bool playing;
                        if (!AudioProbe.Read(new[] { name }, out vol, out playing)) { vol = 0.5; playing = false; }
                        pct = Estimator.Instance.Tick(true, playing, vol);
                        Log.W("estimate " + pct + "% " + Estimator.Instance.LastDebug);
                    }
                    if (pct < 0 && !isPinned) continue; // only show unpinned BT devices that actually report a battery
                    string ln = name.ToLowerInvariant();
                    Kind k = ln.Contains("speaker") || ln.Contains("soundbar") || ln.Contains("bar") ? Kind.Speaker
                           : ln.Contains("mouse") ? Kind.Mouse : ln.Contains("key") ? Kind.Keyboard
                           : ln.Contains("head") || ln.Contains("buds") || ln.Contains("pods") ? Kind.Headset
                           : ln.Contains("pad") || ln.Contains("controller") ? Kind.Gamepad : Kind.Other;
                    if (estimate && k == Kind.Other) k = Kind.Speaker;   // a pinned device without battery data is almost always a speaker
                    res.Add(new DeviceInfo { Key = "bt:" + id, Name = name, Kind = k, Percent = pct, Connection = "Bluetooth", Source = "bt", Estimated = estimate });
                }
            }
            catch (Exception e) { Log.W("bt: " + e.Message); }
            return res;
        }
    }

    // ===================== GameSir 2.4G dongle (vendor HID channel, same one GameSir Connect uses) =====================
    public class GameSirSource : ISource
    {
        public string Name { get { return "gamesir"; } }
        public int IntervalSec { get { return 30; } }
        public static volatile bool Found; // lets the XInput fallback stay quiet
        bool had;
        readonly Func<string> gamepadName;
        public GameSirSource(Func<string> name) { gamepadName = name; }
        public void Kick() { }

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            Found = false;
            try
            {
                var h = Native.FindHid(0x3537, new ushort[] { 0x1098 }).FirstOrDefault(x => x.UsagePage == 0xFF7A || (x.OutLen == 65 && x.InLen == 65));
                if (h == null) return res;
                string err;
                var rep = Native.HidExchange(h, new byte[] { 0x00, 0x01, 0x01, 0x00 }, 1200, out err, false);
                Log.W("gamesir err=" + err + " reports=" + string.Join(" | ", rep.Select(r => BitConverter.ToString(r, 0, Math.Min(r.Length, 24)))));
                foreach (var r in rep)
                {
                    if (r.Length <= 19) continue;
                    int b = r[19];
                    bool charging = false; int pct = b;
                    if (b == 255) { charging = true; pct = -1; }
                    else if (b > 100) { charging = (b & 0x80) != 0; pct = b & 0x7F; }
                    if (pct == 0 && !charging) continue; // dongle answers with 0 when the controller is off
                    Found = true;
                    res.Add(new DeviceInfo { Key = "gamesir:1098", Name = gamepadName(), Kind = Kind.Gamepad, Percent = pct, Charging = charging, Connection = "2.4 GHz", Source = "gamesir" });
                    break;
                }
            }
            catch (Exception e) { Log.W("gamesir: " + e.Message); return null; }
            if (res.Count == 0 && had) { had = false; Found = true; return null; } // one-poll grace if a read just failed
            had = res.Count > 0;
            return res;
        }
    }

    // ===================== Gamepad (XInput: GameSir etc.) =====================
    public class XInputSource : ISource
    {
        public string Name { get { return "xinput"; } }
        public int IntervalSec { get { return 30; } }
        readonly Func<string> gamepadName;
        bool warmed;
        public XInputSource(Func<string> name) { gamepadName = name; }
        public void Kick() { }

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            try
            {
                if (!warmed) { warmed = true; Thread.Sleep(4000); } // let the GameSir source report first, avoids a duplicate at startup
                if (GameSirSource.Found) return res; // GameSir battery already read through its own channel
                for (int i = 0; i < 4; i++)
                {
                    Native.XINPUT_BATTERY_INFORMATION bi;
                    int r = Native.XInputGetBatteryInformation(i, 0, out bi);
                    Log.W("xinput " + i + " r=" + r + " type=" + bi.BatteryType + " level=" + bi.BatteryLevel);
                    if (r != 0 || bi.BatteryType == 0 || bi.BatteryType == 1) continue; // 0 = disconnected, 1 = wired (no battery to show)
                    var d = new DeviceInfo { Key = "xinput:" + i, Name = gamepadName(), Kind = Kind.Gamepad, Source = "xinput" };
                    if (bi.BatteryType == 1) { d.Connection = "USB / cable"; d.Percent = -1; }
                    else
                    {
                        d.Connection = "Inalámbrico";
                        switch (bi.BatteryLevel) { case 0: d.Percent = 10; break; case 1: d.Percent = 40; break; case 2: d.Percent = 70; break; default: d.Percent = 100; break; }
                    }
                    res.Add(d);
                }
            }
            catch (Exception e) { Log.W("xinput: " + e.Message); }
            return res;
        }
    }

    // ===================== Aggregator =====================
    public class DeviceHub
    {
        readonly List<ISource> sources = new List<ISource>();
        readonly Dictionary<string, List<DeviceInfo>> latest = new Dictionary<string, List<DeviceInfo>>();
        readonly List<Timer> timers = new List<Timer>();
        readonly Dictionary<string, int> misses = new Dictionary<string, int>();
        readonly object lk = new object();
        public event Action Changed;

        public void Add(ISource s) { sources.Add(s); }

        public void Start()
        {
            foreach (var s in sources)
            {
                var src = s; var running = 0;
                var t = new Timer(_ =>
                {
                    if (Interlocked.Exchange(ref running, 1) == 1) return;
                    try { Update(src); } finally { running = 0; }
                }, null, 0, src.IntervalSec * 1000);
                timers.Add(t);
            }
        }

        public void RefreshNow()
        {
            foreach (var s in sources) { var src = s; ThreadPool.QueueUserWorkItem(_ => Update(src)); }
        }

        void Update(ISource s)
        {
            List<DeviceInfo> r;
            try { r = s.Poll(); } catch (Exception e) { Log.W(s.Name + " poll crashed: " + e.Message); r = null; }
            bool changed;
            lock (lk)
            {
                // null = "could not read this time" (G HUB restarting, receiver busy...): keep the last known
                // devices for a couple of polls instead of making them blink out; clear after 3 misses in a row.
                int m; misses.TryGetValue(s.Name, out m);
                if (r == null) { misses[s.Name] = ++m; if (m < 3) return; r = new List<DeviceInfo>(); }
                else misses[s.Name] = 0;
                List<DeviceInfo> old; latest.TryGetValue(s.Name, out old);
                changed = old == null || old.Count != r.Count;
                if (!changed) foreach (var x in r) if (!x.SameAs(old.FirstOrDefault(y => y.Key == x.Key))) { changed = true; break; }
                latest[s.Name] = r;
            }
            if (changed)
            {
                if (Log.Enabled) Log.W("devices: " + string.Join("; ", Snapshot().Select(d => d.Source + ":" + d.Name + " " + d.Percent + "%")));
                if (Changed != null) Changed();
            }
        }

        // When two sources see the same physical device (e.g. G HUB and direct HID++, or a Bluetooth node), the more
        // specific source wins and the other copy is dropped.
        static readonly string[] Priority = { "ghub", "gamesir", "hyperx", "headsets", "razer", "hidpp", "bt", "xinput" };
        static string Norm(string s) { var sb = new System.Text.StringBuilder(); foreach (char c in (s ?? "").ToLowerInvariant()) if (char.IsLetterOrDigit(c)) sb.Append(c); return sb.ToString(); }
        static int Rank(string src) { int i = Array.IndexOf(Priority, src); return i < 0 ? 99 : i; }

        public List<DeviceInfo> Snapshot()
        {
            lock (lk)
            {
                var all = latest.Values.SelectMany(x => x).OrderBy(d => Rank(d.Source)).ThenBy(d => d.Name).ToList();
                var kept = new List<DeviceInfo>();
                foreach (var d in all)
                {
                    string n = Norm(d.Name);
                    bool dup = kept.Any(k => k.Kind == d.Kind && (k.Source != d.Source || k.Name == d.Name) && n.Length >= 3
                                             && (Norm(k.Name).Contains(n) || n.Contains(Norm(k.Name))));
                    if (!dup) kept.Add(d);
                }
                return kept.OrderBy(d => (int)d.Kind).ThenBy(d => d.Name).ToList();
            }
        }
    }
}
