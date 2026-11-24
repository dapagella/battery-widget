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

        public static Config Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var c = new JavaScriptSerializer().Deserialize<Config>(File.ReadAllText(FilePath));
                    if (c != null)
                    {
                        if (c.Hidden == null) c.Hidden = new List<string>();
                        if (c.PinnedBluetooth == null) c.PinnedBluetooth = new List<string>();
                        if (!(c.SizeFactor >= 0.5 && c.SizeFactor <= 2.2)) c.SizeFactor = 1.0;      // also catches NaN
                        if (double.IsNaN(c.OffsetX) || Math.Abs(c.OffsetX) > 20000) c.OffsetX = 0;
                        if (string.IsNullOrEmpty(c.GamepadName)) c.GamepadName = "GameSir controller";
                        return c;
                    }
                }
            }
            catch { }
            return new Config();
        }

        public void Save()
        {
            try { Directory.CreateDirectory(Dir); File.WriteAllText(FilePath, new JavaScriptSerializer().Serialize(this)); } catch { }
        }

        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static bool GetAutoStart()
        {
            try { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue("BatteryWidget") != null; } catch { return false; }
        }
        public static void SetAutoStart(bool on)
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (on) k.SetValue("BatteryWidget", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
                    else k.DeleteValue("BatteryWidget", false);
                }
            }
            catch { }
        }
    }
}
