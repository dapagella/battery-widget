using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

// Battery Widget installer / uninstaller (single file, no dependencies, per-user: no admin rights needed).
//   Setup.exe            install (or update) and start the widget
//   Setup.exe /S         silent install
//   Setup.exe /uninstall remove it   (also registered in Windows "Installed apps")
[assembly: AssemblyTitle("Battery Widget Setup")]
[assembly: AssemblyProduct("Battery Widget")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

static class Setup
{
    const string AppName = "Battery Widget";
    const string Version = "1.1.0";
    const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\BatteryWidget";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    static readonly string InstallDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\BatteryWidget");
    static readonly string ExePath = Path.Combine(InstallDir, "BatteryWidget.exe");
    static readonly string UninstallerPath = Path.Combine(InstallDir, "Uninstall.exe");
    static readonly string StartMenuLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName + ".lnk");
    static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatteryWidget");

    static bool Silent;
    static bool Spanish { get { return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es"; } }
    static string T(string es, string en) { return Spanish ? es : en; }

    [STAThread]
    static int Main(string[] args)
    {
        foreach (var a in args) if (a.Equals("/S", StringComparison.OrdinalIgnoreCase) || a.Equals("--silent", StringComparison.OrdinalIgnoreCase)) Silent = true;
        bool uninstall = false;
        foreach (var a in args) if (a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase) || a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)) uninstall = true;
        try { return uninstall ? Uninstall() : Install(); }
        catch (Exception e) { Info(T("No se pudo completar: ", "Could not finish: ") + e.Message, true); return 1; }
    }

    static void Info(string text, bool error)
    {
        if (Silent) return;
        MessageBox.Show(text, AppName, MessageBoxButtons.OK, error ? MessageBoxIcon.Error : MessageBoxIcon.Information);
    }

    static void StopRunning()
    {
        foreach (var p in Process.GetProcessesByName("BatteryWidget"))
        {
            try { p.Kill(); p.WaitForExit(4000); } catch { }
        }
    }

    static int Install()
    {
        StopRunning();
        Directory.CreateDirectory(InstallDir);

        // the widget itself is embedded in this installer
        using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("BatteryWidget.exe"))
        using (var f = File.Create(ExePath)) s.CopyTo(f);
        File.Copy(Assembly.GetExecutingAssembly().Location, UninstallerPath, true);

        // start with Windows (current user)
        using (var k = Registry.CurrentUser.CreateSubKey(RunKey)) k.SetValue("BatteryWidget", "\"" + ExePath + "\"");
        // plus a Task Scheduler logon task: this is what makes it start dependably. If Windows needs permission to create it,
        // it shows its standard prompt (UAC); the Run key above stays as a second line of defence either way.
        bool task = BatteryWidget.AutoStartTask.Create(ExePath, !Silent);

        // "Installed apps" entry
        using (var k = Registry.CurrentUser.CreateSubKey(UninstallKey))
        {
            k.SetValue("DisplayName", AppName);
            k.SetValue("DisplayVersion", Version);
            k.SetValue("Publisher", "Battery Widget contributors");
            k.SetValue("DisplayIcon", ExePath);
            k.SetValue("InstallLocation", InstallDir);
            k.SetValue("UninstallString", "\"" + UninstallerPath + "\" /uninstall");
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            k.SetValue("EstimatedSize", (int)(new FileInfo(ExePath).Length / 1024) + 200, RegistryValueKind.DWord);
        }

        MakeShortcut();
        var proc = Process.Start(new ProcessStartInfo(ExePath) { WorkingDirectory = InstallDir, UseShellExecute = true });

        // don't claim success unless the widget is really alive a few seconds later
        System.Threading.Thread.Sleep(4000);
        bool alive = false;
        try { alive = !proc.HasExited || Process.GetProcessesByName("BatteryWidget").Length > 0; } catch { }
        if (!alive)
        {
            string log = Path.Combine(DataDir, "error.log");
            Info(T("Se instaló, pero el widget no pudo iniciar.\n\nSi existe este archivo, mándalo a los desarrolladores:\n" + log,
                   "Installed, but the widget could not start.\n\nIf this file exists, please send it to the developers:\n" + log), true);
            return 2;
        }
        Info(T("Listo. Battery Widget ya está funcionando y se iniciará solo con Windows.\n\nBúscalo a la izquierda del reloj, en la barra de tareas (si tienes dos pantallas, puede estar en la segunda). Solo aparecen los dispositivos inalámbricos que estén conectados; si no ves ninguno, enciende uno.",
               "All set. Battery Widget is running and will start with Windows.\n\nLook for it just left of the clock on the taskbar (with two monitors it may be on the second one). Only connected wireless devices are shown; if you see none, switch one on."), false);
        return 0;
    }

    static void MakeShortcut()
    {
        try
        {
            var shell = Type.GetTypeFromProgID("WScript.Shell");
            dynamic sh = Activator.CreateInstance(shell);
            dynamic lnk = sh.CreateShortcut(StartMenuLink);
            lnk.TargetPath = ExePath; lnk.WorkingDirectory = InstallDir; lnk.Description = AppName; lnk.IconLocation = ExePath;
            lnk.Save();
        }
        catch { /* the shortcut is a nicety, never fail the install over it */ }
    }

    static int Uninstall()
    {
        if (!Silent)
        {
            var r = MessageBox.Show(T("¿Quitar Battery Widget de este equipo?", "Remove Battery Widget from this computer?"), AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return 0;
        }
        StopRunning();
        try { using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true)) { if (k != null) k.DeleteValue("BatteryWidget", false); } } catch { }
        try { BatteryWidget.AutoStartTask.Delete(!Silent); } catch { }
        try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch { }
        try { if (File.Exists(StartMenuLink)) File.Delete(StartMenuLink); } catch { }

        bool wipeData = false;   // a silent uninstall keeps settings (safe for upgrades); the interactive one asks
        if (!Silent && Directory.Exists(DataDir))
            wipeData = MessageBox.Show(T("¿Borrar también tus ajustes?", "Also delete your settings?"), AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        if (wipeData) { try { Directory.Delete(DataDir, true); } catch { } }

        try { if (File.Exists(ExePath)) File.Delete(ExePath); } catch { }
        // this very file is running from the install folder: let cmd remove it (and the folder) a moment after we exit
        var self = Assembly.GetExecutingAssembly().Location;
        if (string.Equals(Path.GetDirectoryName(self), InstallDir, StringComparison.OrdinalIgnoreCase))
        {
            var cmd = "/c ping 127.0.0.1 -n 3 > nul & del /f /q \"" + self + "\" & rmdir \"" + InstallDir + "\"";
            Process.Start(new ProcessStartInfo("cmd.exe", cmd) { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden });
        }
        else { try { Directory.Delete(InstallDir, true); } catch { } }
        Info(T("Battery Widget se quitó correctamente.", "Battery Widget was removed."), false);
        return 0;
    }
}

