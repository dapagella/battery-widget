using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace BatteryWidget
{
    /// <summary>
    /// Looks at the project's GitHub releases about once a day (a single small request: nothing about you or your devices is sent)
    /// and, when there is a newer version, offers a one-click update. The installer comes from this project's own release page
    /// over HTTPS and its SHA-256 is checked against the value GitHub publishes for the file.
    /// </summary>
    public sealed class Updater
    {
        const string Repo = "dapagella/battery-widget";
        const string AllowedPrefix = "https://github.com/" + Repo + "/releases/download/";

        public sealed class Release { public Version Version; public string Url, Sha256; }

        public static Version Current { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        /// <summary>Returns the newest release if it is newer than this one; null if up to date; throws on network errors.</summary>
        public static Release CheckNow(out Version latest)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var req = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/" + Repo + "/releases/latest");
            req.UserAgent = "BatteryWidget-update-check"; req.Accept = "application/vnd.github+json"; req.Timeout = 15000; req.ReadWriteTimeout = 15000;
            string json;
            using (var resp = req.GetResponse()) using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) json = sr.ReadToEnd();
            var d = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            latest = null;
            if (d == null || !d.ContainsKey("tag_name")) return null;
            Version v; if (!Version.TryParse(((string)d["tag_name"]).TrimStart('v', 'V'), out v)) return null;
            latest = v;
            if (v <= NormalizedCurrent()) return null;
            var rel = new Release { Version = v };
            var assets = d.ContainsKey("assets") ? d["assets"] as object[] : null;
            if (assets != null)
                foreach (var a in assets)
                {
                    var ad = a as Dictionary<string, object>; if (ad == null) continue;
                    if ((string)ad["name"] != "BatteryWidget-Setup.exe") continue;
                    rel.Url = (string)ad["browser_download_url"];
                    if (ad.ContainsKey("digest") && ad["digest"] != null) rel.Sha256 = ((string)ad["digest"]).Replace("sha256:", "").Trim().ToLowerInvariant();
                }
            return rel.Url != null && rel.Url.StartsWith(AllowedPrefix, StringComparison.Ordinal) ? rel : null;
        }

        static Version NormalizedCurrent() { var c = Current; return new Version(c.Major, c.Minor, Math.Max(0, c.Build)); }

        /// <summary>Downloads the installer, verifies it and runs it silently (it restarts the widget by itself).</summary>
        public static void Apply(Release r)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            string file = Path.Combine(Path.GetTempPath(), "BatteryWidget-Setup-" + r.Version + ".exe");
            using (var wc = new WebClient()) { wc.Headers[HttpRequestHeader.UserAgent] = "BatteryWidget-update"; wc.DownloadFile(r.Url, file); }
            if (!string.IsNullOrEmpty(r.Sha256))
            {
                string got;
                using (var sha = SHA256.Create()) using (var fs = File.OpenRead(file)) got = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
                if (got != r.Sha256) { try { File.Delete(file); } catch { } throw new InvalidDataException("download does not match the published checksum"); }
            }
            Process.Start(new ProcessStartInfo(file, "/S") { UseShellExecute = true });
        }
    }
}
