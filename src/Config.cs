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

        public static Config Load()
        {
            FileExisted = File.Exists(FilePath) || File.Exists(BackupPath);
            foreach (var path in new[] { FilePath, BackupPath })     // main file first, then the last good copy
            {
                try
                {
                    var c = TryParse(path);
                    if (c != null) { if (path == BackupPath) Log.Err("config.json unreadable, restored from backup"); return c; }
                }
                catch (Exception e) { Log.Err("could not read " + Path.GetFileName(path) + ": " + e.Message); }
            }
            if (FileExisted)
            {
                LoadFailed = true;
                try { File.Copy(FilePath, Path.Combine(Dir, "config.unreadable.json"), true); } catch { }
            }
            return new Config();
        }

        int saveLock;
        public void Save()
        {
            if (LoadFailed) { LoadFailed = false; }   // the user changed something on purpose: from now on this is the real config
            try
            {
                Directory.CreateDirectory(Dir);
                string json = new JavaScriptSerializer().Serialize(this);
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, json);                       // write to a temp file, then swap: a crash can't leave half a file
                if (File.Exists(FilePath)) File.Replace(tmp, FilePath, BackupPath);
                else File.Move(tmp, FilePath);
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
