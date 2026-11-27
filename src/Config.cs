using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace BatteryWidget
{
    public class Config
    {
        public static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatteryWidget");
        static readonly string FilePath = Path.Combine(Dir, "config.json");
        static readonly string BackupPath = Path.Combine(Dir, "config.json.bak");

        /// <summary>True when a settings file existed but could not be read (never overwrite it with defaults behind the user's back).</summary>
        public static bool LoadFailed;
        public static bool FileExisted;

        public string Design = "unified";       // unified | separate | minimal | ring
        public string Background = "dark";      // dark | black | gray | navy | violet | glass | none
        public string Shape = "rounded";        // rounded | pill | square
        public string Scale = "m";              // (legacy, unused)
        public double SizeFactor = 1.0;         // overall size, 0.6 .. 2.0
        public string Monitor = "auto";         // auto (second screen if there is one) | primary | secondary
        public bool ShowPercent = true;
        public double OffsetX = 0;              // horizontal drag offset from the default spot (DIPs, negative = left)
        public List<string> Hidden = new List<string>();
        public List<string> PinnedBluetooth = new List<string>();
        public string GamepadName = "GameSir controller";
        public bool Debug = false;
        public bool AutoStartInitDone = false;  // first run turns "start with Windows" on once; the menu can switch it off afterwards
        public bool AlertLow = true;            // notify at 20 % and 10 %
        public bool AlertFull = true;           // notify once when a charging device reaches 100 %
        public bool AlertSound = true;
        public bool CheckUpdates = true;        // one small request a day to this project's GitHub releases

        static string ReadWithRetry(string path)
        {
            // the file can be held for a moment by another process (antivirus, a second copy saving): retry instead of giving up
            for (int i = 0; i < 6; i++)
            {
                try { return File.ReadAllText(path); }
                catch (IOException) { System.Threading.Thread.Sleep(150); }
            }
            return File.ReadAllText(path);
        }

        static Config TryParse(string path)
        {
            if (!File.Exists(path)) return null;
            var c = new JavaScriptSerializer().Deserialize<Config>(ReadWithRetry(path).TrimStart('﻿'));
            if (c == null) return null;
            if (c.Hidden == null) c.Hidden = new List<string>();
            if (c.PinnedBluetooth == null) c.PinnedBluetooth = new List<string>();
            if (!(c.SizeFactor >= 0.5 && c.SizeFactor <= 2.2)) c.SizeFactor = 1.0;      // also catches NaN
            if (double.IsNaN(c.OffsetX) || Math.Abs(c.OffsetX) > 20000) c.OffsetX = 0;
            if (string.IsNullOrEmpty(c.GamepadName)) c.GamepadName = "GameSir controller";
            return c;
        }

        public long SavedUtc;                   // when this copy was written; the newest readable copy always wins at start-up
        public static string LoadedFrom = "defaults";

        public static Config Load()
        {
            FileExisted = File.Exists(FilePath) || File.Exists(BackupPath);
            // Both the main file and its backup are read and the NEWER one wins (by the time stamp stored inside, or the file's date
            // for copies written by older versions): a stale or damaged main file can never hide what you chose last.
            Config best = null; string bestFrom = null; long bestScore = long.MinValue;
            foreach (var path in new[] { FilePath, BackupPath })
            {
                try
                {
                    var c = TryParse(path); if (c == null) continue;
                    long score = Math.Max(c.SavedUtc, File.GetLastWriteTimeUtc(path).Ticks);
                    if (score > bestScore) { best = c; bestFrom = Path.GetFileName(path); bestScore = score; }
                }
                catch (Exception e) { Log.Err("could not read " + Path.GetFileName(path) + ": " + e.Message); }
            }
            if (best != null)
            {
                LoadedFrom = bestFrom;
                Log.Note("settings loaded from " + bestFrom + ": " + best.Design + "/" + best.Background + "/" + best.Shape + " size " + best.SizeFactor + " screen " + best.Monitor);
                return best;
            }
            if (FileExisted)
            {
                LoadFailed = true;
                try { File.Copy(FilePath, Path.Combine(Dir, "config.unreadable.json"), true); } catch { }
            }
            Log.Note(FileExisted ? "settings UNREADABLE: using defaults, nothing will be overwritten until you change something" : "no settings yet: defaults");
            return new Config();
        }

        public void Save()
        {
            if (LoadFailed) { LoadFailed = false; }   // the user changed something on purpose: from now on this is the real config
            try
            {
                Directory.CreateDirectory(Dir);
                SavedUtc = DateTime.UtcNow.Ticks;
                string json = new JavaScriptSerializer().Serialize(this);
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, json);                       // write to a temp file, then swap: a crash can't leave half a file
                try { if (File.Exists(FilePath)) File.Replace(tmp, FilePath, BackupPath); else File.Move(tmp, FilePath); }
                catch (Exception e) { Log.Err("swap failed (" + e.Message + "), writing directly"); File.WriteAllText(FilePath, json); }
                // trust, but verify: read it back; if what is on disk is not what we wrote, write it again
                string back; try { back = File.ReadAllText(FilePath); } catch { back = null; }
                if (back != json) { Log.Err("settings read-back mismatch, rewriting"); File.WriteAllText(FilePath, json); }
                Log.Note("settings saved: " + Design + "/" + Background + "/" + Shape + " size " + SizeFactor + " screen " + Monitor);
            }
            catch (Exception e) { Log.Err("could not save settings: " + e.Message); }
        }

        // ---- start with Windows: Task Scheduler logon task (reliable) + the classic Run key as a second line ----
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        static string ExePath { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

        public static bool GetAutoStart()
        {
            try { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) if (k != null && k.GetValue("BatteryWidget") != null) return true; } catch { }
            return AutoStartTask.Exists();
        }

        /// <summary>allowPrompt: may show the Windows permission (UAC) prompt if creating the task needs it.</summary>
        public static void SetAutoStart(bool on, bool allowPrompt = false)
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (on) k.SetValue("BatteryWidget", "\"" + ExePath + "\"");
                    else k.DeleteValue("BatteryWidget", false);
                }
            }
            catch { }
            try { if (on) AutoStartTask.Create(ExePath, allowPrompt); else AutoStartTask.Delete(allowPrompt); }
            catch (Exception e) { Log.Err("autostart task: " + e.Message); }
        }
    }
}
