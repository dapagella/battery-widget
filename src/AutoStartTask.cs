using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace BatteryWidget
{
    /// <summary>
    /// "Start with Windows" through a Task Scheduler logon task. Much more dependable than the Run registry key:
    /// it starts a few seconds after sign-in, survives Fast Startup, and restarts the widget if it ever dies.
    /// Created without elevation when Windows allows it, otherwise with the normal Windows permission (UAC) prompt.
    /// Self-contained: compiled into both the widget and the installer.
    /// </summary>
    public static class AutoStartTask
    {
        public const string TaskName = "BatteryWidget";

        static string Xml(string exe, string user)
        {
            Func<string, string> esc = SecurityElement.Escape;
            return
"<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
"<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
"  <RegistrationInfo><Description>Battery Widget: starts the battery widget when you sign in.</Description></RegistrationInfo>\r\n" +
"  <Triggers><LogonTrigger><Repetition><Interval>PT5M</Interval><StopAtDurationEnd>false</StopAtDurationEnd></Repetition><Enabled>true</Enabled><UserId>" + esc(user) + "</UserId><Delay>PT5S</Delay></LogonTrigger></Triggers>\r\n" +
"  <Principals><Principal id=\"Author\"><UserId>" + esc(user) + "</UserId><LogonType>InteractiveToken</LogonType><RunLevel>LeastPrivilege</RunLevel></Principal></Principals>\r\n" +
"  <Settings>\r\n" +
"    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n" +
"    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\r\n" +
"    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\r\n" +
"    <AllowHardTerminate>true</AllowHardTerminate>\r\n" +
"    <StartWhenAvailable>true</StartWhenAvailable>\r\n" +
"    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>\r\n" +
"    <IdleSettings><StopOnIdleEnd>false</StopOnIdleEnd><RestartOnIdle>false</RestartOnIdle></IdleSettings>\r\n" +
"    <AllowStartOnDemand>true</AllowStartOnDemand>\r\n" +
"    <Enabled>true</Enabled>\r\n" +
"    <Hidden>false</Hidden>\r\n" +
"    <RunOnlyIfIdle>false</RunOnlyIfIdle>\r\n" +
"    <WakeToRun>false</WakeToRun>\r\n" +
"    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\r\n" +
"    <Priority>7</Priority>\r\n" +
"    <RestartOnFailure><Interval>PT1M</Interval><Count>5</Count></RestartOnFailure>\r\n" +
"  </Settings>\r\n" +
"  <Actions Context=\"Author\"><Exec><Command>" + esc(exe) + "</Command><Arguments>--from-task</Arguments><WorkingDirectory>" + esc(Path.GetDirectoryName(exe)) + "</WorkingDirectory></Exec></Actions>\r\n" +
"</Task>";
        }

        /// <summary>Runs schtasks.exe; elevated shows the standard Windows permission prompt. Returns the exit code (-1 on failure/cancel).</summary>
        static int Schtasks(string args, bool elevate)
        {
            try
            {
                var psi = new ProcessStartInfo("schtasks.exe", args) { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true };
                if (elevate) { psi.UseShellExecute = true; psi.Verb = "runas"; }
                else { psi.UseShellExecute = false; psi.RedirectStandardOutput = true; psi.RedirectStandardError = true; }
                using (var p = Process.Start(psi))
                {
                    if (!elevate) { p.StandardOutput.ReadToEnd(); p.StandardError.ReadToEnd(); }
                    if (!p.WaitForExit(30000)) return -1;
                    return p.ExitCode;
                }
            }
            catch { return -1; }   // e.g. the user pressed "No" on the permission prompt
        }

        public static bool Exists() { return Schtasks("/Query /TN \"" + TaskName + "\"", false) == 0; }

        /// <summary>Creates (or replaces) the logon task for the current user. Tries without elevation first, then asks Windows for permission.</summary>
        public static bool Create(string exe, bool allowPrompt)
        {
            string xmlFile = Path.Combine(Path.GetTempPath(), "BatteryWidget-task-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                File.WriteAllText(xmlFile, Xml(exe, WindowsIdentity.GetCurrent().Name), Encoding.Unicode);
                string args = "/Create /TN \"" + TaskName + "\" /XML \"" + xmlFile + "\" /F";
                if (Schtasks(args, false) == 0) return true;
                return allowPrompt && Schtasks(args, true) == 0;
            }
            catch { return false; }
            finally { try { File.Delete(xmlFile); } catch { } }
        }

        public static bool Delete(bool allowPrompt)
        {
            if (!Exists()) return true;
            string args = "/Delete /TN \"" + TaskName + "\" /F";
            if (Schtasks(args, false) == 0) return true;
            return allowPrompt && Schtasks(args, true) == 0;
        }
    }
}
