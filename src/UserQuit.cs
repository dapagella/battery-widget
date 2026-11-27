using System;
using System.Diagnostics;
using System.IO;

namespace BatteryWidget
{
    /// <summary>Remembers "the user quit the widget on purpose in this Windows session" so the safety-net relaunch respects it.
    /// A session is identified by when Explorer started, so signing out and in (or rebooting) starts the widget again.</summary>
    static class UserQuit
    {
        static string FlagFile { get { return Path.Combine(Config.Dir, "quit.flag"); } }

        static long Session()
        {
            try
            {
                long best = long.MaxValue; int mine = Process.GetCurrentProcess().SessionId;
                foreach (var p in Process.GetProcessesByName("explorer"))
                    try { if (p.SessionId == mine && p.StartTime.Ticks < best) best = p.StartTime.Ticks; } catch { }
                return best == long.MaxValue ? 0 : best;
            }
            catch { return 0; }
        }

        public static void Mark() { try { Directory.CreateDirectory(Config.Dir); File.WriteAllText(FlagFile, Session().ToString()); } catch { } }
        public static void Clear() { try { if (File.Exists(FlagFile)) File.Delete(FlagFile); } catch { } }
        public static bool Active()
        {
            try { long s = Session(); return s != 0 && File.Exists(FlagFile) && File.ReadAllText(FlagFile).Trim() == s.ToString(); }
            catch { return false; }
        }
    }
}
