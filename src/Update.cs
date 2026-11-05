using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DiskMate
{
    public static class AppInfo
    {
        /// <summary>程序版本。发版时改这里（tag 用 v + 同样的数字）。</summary>
        public const string Version = "1.0.0";

        /// <summary>GitHub 仓库。</summary>
        public const string Repo = "ankeji/DiskMate";
        public const string RepoUrl = "https://github.com/ankeji/DiskMate";

        /// <summary>
        /// 主更新源：仓库里的 version.txt（raw，无频率限制）。
        /// 想换成自己的服务器，只改这一行即可。
        /// </summary>
        public const string ManifestUrl = "https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt";

        /// <summary>备用更新源：GitHub Releases API（没发 version.txt 时兜底）。</summary>
        public const string ReleaseApi = "https://api.github.com/repos/ankeji/DiskMate/releases/latest";

        public static Version Current { get { return UpdateService.ParseVersion(Version); } }
    }

    public class UpdateInfo
    {
        public string Version = "";
        public string Url = "";
        public string Sha256 = "";
        public long Size;
        public string Notes = "";
        public string Date = "";
        public string Source = "";
    }

    /// <summary>
    /// 自更新：读清单（version.txt 或 GitHub Releases API）→ 比对版本 → 下载新 exe →
    /// SHA-256 校验 → 交给延迟替换的批处理（等旧进程退出后覆盖 exe 并重启）。
    /// 零第三方依赖，纯 .NET Framework。
    /// </summary>
    public static class UpdateService
    {
        /// <summary>--selftest 时置 true，避免自检触碰网络。</summary>
        public static bool Disabled;

        public static string ManifestUrl = AppInfo.ManifestUrl;
        public static string ReleaseApi = AppInfo.ReleaseApi;

        static UpdateService()
        {
            try
            {
                // 老 .NET 默认只开 SSL3 / TLS1.0，这里补上 TLS 1.2
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            }
            catch { }
        }

        public static Version ParseVersion(string s)
        {
            if (string.IsNullOrEmpty(s)) return new Version(0, 0);
            var parts = new List<int>();
            foreach (string seg in s.Trim().TrimStart('v', 'V').Split('.'))
            {
                int v;
                if (!int.TryParse(seg, out v)) break;
                parts.Add(v);
            }
            while (parts.Count < 2) parts.Add(0);
            if (parts.Count == 2) parts.Add(0);
            return new Version(parts[0], parts[1], parts[2]);
        }

        // ------------------------------------------------------------ check
        /// <summary>
        /// 检查更新：先读 version.txt，再问 GitHub Releases API，取版本号更大的那个。
        /// 返回 null 表示已是最新；error 非空表示所有源都失败。
        /// </summary>
        public static UpdateInfo Check(out string error)
        {
            error = null;
            var errors = new List<string>();

            bool ok1;
            UpdateInfo a = TrySource(ManifestUrl, errors, out ok1);
            if (ok1)
            {
                // version.txt 由 CI 每次发版生成，是权威源：可达就以它为准，
                // 避免历史上更高版本号的旧 Release 被误判成「有更新」
                return a;
            }

            // 清单不可达时才退回 GitHub Releases API
            bool ok2;
            UpdateInfo b = TrySource(ReleaseApi, errors, out ok2);
            if (b != null) return b;
            if (!ok2 && errors.Count > 0) error = string.Join("; ", errors.ToArray());
            return null;
        }

        public static UpdateInfo Check(string url, out string error)
        {
            var errors = new List<string>();
            bool ok;
            UpdateInfo info = TrySource(url, errors, out ok);
            error = (!ok || (info == null && errors.Count > 0)) && errors.Count > 0 ? errors[0] : null;
            return info;
        }

        private static UpdateInfo TrySource(string url, List<string> errors, out bool reachable)
        {
            reachable = false;
            if (Disabled || string.IsNullOrEmpty(url)) return null;
            if (url.IndexOf("example.com", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                errors.Add("not configured");
                return null;
            }
            try
            {
                string text = FetchText(url, url == ReleaseApi);
                reachable = true;
                UpdateInfo info = text.TrimStart().StartsWith("{")
                    ? FromGitHubRelease(text, url)
                    : FromManifest(text, url);
                if (info == null) return null;
                if (ParseVersion(info.Version) <= AppInfo.Current) return null;
                return info;
            }
            catch (Exception ex)
            {
                errors.Add(Short(url) + ": " + ex.Message);
                return null;
            }
        }

        private static string Short(string url)
        {
            int i = url.LastIndexOf('/');
            return i >= 0 ? url.Substring(i + 1) : url;
        }

        // ------------------------------------------------------------ version.txt
        private static UpdateInfo FromManifest(string text, string baseUrl)
        {
            Dictionary<string, string> m = ParseManifest(text);
            string ver = Get(m, "version");
            if (ver.Length == 0) return null;

            string lang = I18n.Lang;
            string notes = Get(m, "notes." + lang);
            if (notes.Length == 0) notes = Get(m, "notes." + lang.Substring(0, 2));
            if (notes.Length == 0) notes = Get(m, "notes.en");
            if (notes.Length == 0) notes = Get(m, "notes");

            var info = new UpdateInfo();
            info.Source = "version.txt";
            info.Version = ver;
            info.Url = ResolveUrl(baseUrl, Get(m, "url"));
            info.Sha256 = Get(m, "sha256").Replace(" ", "").ToUpperInvariant();
            info.Notes = Decode(notes);
            info.Date = Get(m, "date");
            long size;
            if (long.TryParse(Get(m, "size"), out size)) info.Size = size;
            if (info.Url.Length == 0) return null;
            return info;
        }

        // ------------------------------------------------------------ GitHub release
        private static UpdateInfo FromGitHubRelease(string json, string apiUrl)
        {
            Dictionary<string, object> root = MiniJson.ParseObject(json);
            if (root == null) return null;

            string tag = MiniJson.Str(root, "tag_name");
            if (tag.Length == 0) tag = MiniJson.Str(root, "name");
            if (tag.Length == 0) return null;

            var info = new UpdateInfo();
            info.Source = "releases/latest";
            info.Version = tag.TrimStart('v', 'V');
            info.Date = MiniJson.Str(root, "published_at");
            if (info.Date.Length > 10) info.Date = info.Date.Substring(0, 10);

            string body = MiniJson.Str(root, "body");
            info.Notes = Decode(ExtractNotes(body));

            List<object> assets = MiniJson.Arr(root, "assets");
            if (assets != null)
            {
                Dictionary<string, object> pick = null;
                foreach (object o in assets)
                {
                    Dictionary<string, object> a = MiniJson.Obj(o);
                    if (a == null) continue;
                    string name = MiniJson.Str(a, "name");
                    if (string.Equals(name, "DiskMate.exe", StringComparison.OrdinalIgnoreCase)) { pick = a; break; }
                    if (pick == null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) pick = a;
                }
                if (pick != null)
                {
                    info.Url = MiniJson.Str(pick, "browser_download_url");
                    info.Size = MiniJson.Num(pick, "size");
                    string digest = MiniJson.Str(pick, "digest");       // 新 API 才有，形如 sha256:xxx
                    if (digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                        info.Sha256 = digest.Substring(7).ToUpperInvariant();
                }
            }
            if (info.Url.Length == 0 && body.Length > 0)
            {
                string u = ExtractNote(body, "exe:");
                if (u != null) info.Url = u;
            }
            if (info.Sha256.Length == 0)
            {
                string h = ExtractNote(body, "sha256:");
                if (h != null) info.Sha256 = h.Replace(" ", "").ToUpperInvariant();
            }
            if (info.Url.Length == 0) return null;
            return info;
        }

        /// <summary>取发布说明里 "key: value" 形式的一行。</summary>
        private static string ExtractNote(string body, string key)
        {
            if (string.IsNullOrEmpty(body)) return null;
            foreach (string raw in body.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(key.Length).Trim();
            }
            return null;
        }

        /// <summary>发布说明：有 &lt;!--notes--&gt; 标记就取标记之间，否则取全文。</summary>
        private static string ExtractNotes(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";
            const string open = "<!--notes-->", close = "<!--/notes-->";
            int a = body.IndexOf(open, StringComparison.OrdinalIgnoreCase);
            int b = body.IndexOf(close, StringComparison.OrdinalIgnoreCase);
            if (a >= 0 && b > a) return body.Substring(a + open.Length, b - a - open.Length).Trim();
            return body.Trim();
        }

        // ------------------------------------------------------------ helpers
        private static string Decode(string s)
        {
            return (s ?? "").Replace("\\n", Environment.NewLine);
        }

        private static string FetchText(string url, bool isApi)
        {
            WebRequest req = WebRequest.Create(url);
            req.Timeout = 10000;
            var http = req as HttpWebRequest;
            if (http != null)
            {
                http.UserAgent = "DiskMate/" + AppInfo.Version + " (+" + AppInfo.RepoUrl + ")";
                http.AllowAutoRedirect = true;
                http.Timeout = 10000;
                http.Headers["Cache-Control"] = "no-cache";
                if (isApi) http.Accept = "application/vnd.github+json";
            }
            using (WebResponse resp = req.GetResponse())
            using (Stream s = resp.GetResponseStream())
            using (var r = new StreamReader(s, Encoding.UTF8))
                return r.ReadToEnd();
        }

        private static Dictionary<string, string> ParseManifest(string text)
        {
            var m = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;
                int i = line.IndexOf('=');
                if (i <= 0) continue;
                m[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }
            return m;
        }

        private static string Get(Dictionary<string, string> m, string key)
        {
            string v;
            return m.TryGetValue(key, out v) ? v : "";
        }

        /// <summary>相对路径按清单地址解析，方便把 exe 和 version.txt 放同一目录。</summary>
        private static string ResolveUrl(string baseUrl, string file)
        {
            if (file.Length == 0) return "";
            if (file.IndexOf("://", StringComparison.Ordinal) > 0) return file;
            int slash = baseUrl.LastIndexOf('/');
            if (slash < 0) return file;
            return baseUrl.Substring(0, slash + 1) + file;
        }

        // ------------------------------------------------------------ download
        /// <summary>下载新版本到缓存目录，返回文件路径；失败返回 null。</summary>
        public static string Download(UpdateInfo info, Action<long, long> progress, Action cancelled, out string error)
        {
            error = null;
            try
            {
                string dir = CacheDir();
                string dst = Path.Combine(dir, "DiskMate-" + info.Version + ".exe");

                WebRequest req = WebRequest.Create(info.Url);
                req.Timeout = 30000;
                var http = req as HttpWebRequest;
                if (http != null)
                {
                    http.UserAgent = "DiskMate/" + AppInfo.Version;
                    http.AllowAutoRedirect = true;
                    http.Timeout = 30000;
                }

                using (WebResponse resp = req.GetResponse())
                using (Stream ins = resp.GetResponseStream())
                using (var outs = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    long total = resp.ContentLength > 0 ? resp.ContentLength : info.Size;
                    long done = 0;
                    var buf = new byte[81920];
                    int n;
                    while ((n = ins.Read(buf, 0, buf.Length)) > 0)
                    {
                        outs.Write(buf, 0, n);
                        done += n;
                        if (progress != null) progress(done, total);
                    }
                }
                return dst;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        /// <summary>下载目录：先 TEMP，不可写（受限权限 / 绿色运行）就退回 exe 同目录。</summary>
        private static string CacheDir()
        {
            try
            {
                string d1 = Path.Combine(Path.GetTempPath(), "DiskMate-update");
                Directory.CreateDirectory(d1);
                return d1;
            }
            catch { }
            string d2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update-cache");
            Directory.CreateDirectory(d2);
            return d2;
        }

        public static string Sha256Of(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(fs);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }

        public static bool Verify(string path, UpdateInfo info, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(info.Sha256)) return true;   // 清单没给哈希就不校验
            try
            {
                string actual = Sha256Of(path);
                if (!string.Equals(actual, info.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    error = "sha256 mismatch: " + actual;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ------------------------------------------------------------ release helper
        /// <summary>
        /// 生成 version.txt（发版用）：版本号取当前 exe，url 指向 GitHub Release 资产，
        /// sha256 / size 直接算当前 exe。
        /// DiskMate.exe --writeversion version.txt https://github.com/ankeji/DiskMate/releases/download/v2.1.1/DiskMate.exe
        /// </summary>
        public static string WriteManifest(string path, string exeUrl, string notesZh, string notesEn)
        {
            // 环境变量兜底：CI 里避免 | 被 cmd 当管道
            if (string.IsNullOrEmpty(exeUrl) || exeUrl == "-")
            {
                string envUrl = Environment.GetEnvironmentVariable("DISKMATE_EXE_URL");
                if (!string.IsNullOrEmpty(envUrl)) exeUrl = envUrl;
            }
            if (string.IsNullOrEmpty(notesZh))
            {
                string envNotes = Environment.GetEnvironmentVariable("DISKMATE_NOTES_FILE");
                if (!string.IsNullOrEmpty(envNotes) && File.Exists(envNotes)) notesZh = "@" + envNotes;
            }            // 说明支持 @文件：CI 里直接把 tag 注释文件丢进来，避免多行参数转义
            if (!string.IsNullOrEmpty(notesZh) && notesZh[0] == '@')
            {
                notesZh = File.ReadAllText(notesZh.Substring(1), Encoding.UTF8).Trim();
                if (string.IsNullOrEmpty(notesEn)) notesEn = notesZh;
            }
            string exe = Application.ExecutablePath;
            var fi = new FileInfo(exe);
            var sb = new StringBuilder();
            sb.AppendLine("# DiskMate update manifest (generated by DiskMate.exe --writeversion)");
            sb.AppendLine("version=" + AppInfo.Version);
            sb.AppendLine("url=" + (exeUrl == null ? "" : exeUrl));
            sb.AppendLine("sha256=" + Sha256Of(exe));
            sb.AppendLine("size=" + fi.Length);
            sb.AppendLine("date=" + DateTime.Now.ToString("yyyy-MM-dd"));
            if (!string.IsNullOrEmpty(notesZh)) sb.AppendLine("notes.zh=" + notesZh.Replace("\r", "").Replace("\n", "\\n"));
            if (!string.IsNullOrEmpty(notesEn)) sb.AppendLine("notes.en=" + notesEn.Replace("\r", "").Replace("\n", "\\n"));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }

        // ------------------------------------------------------------ swap
        /// <summary>
        /// 启动替换脚本：等当前进程退出 → 覆盖 exe（最多重试 60 秒）→ 重新拉起。
        /// 脚本自身在结束后自删。返回 false 表示脚本没起来。
        /// </summary>
        public static bool RunInstaller(string newExe, out string error)
        {
            error = null;
            try
            {
                string target = Application.ExecutablePath;
                string dir = Path.GetDirectoryName(newExe);
                string bat = WriteInstallerScript(newExe);

                string args = "/c \"\"" + bat + "\" \"" + newExe + "\" \"" + target + "\" 1\"";
                var psi = new ProcessStartInfo("cmd.exe", args);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.WorkingDirectory = dir;
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>把替换脚本写到新 exe 同目录，返回脚本路径（内容纯 ASCII，避免编码问题）。</summary>
        public static string WriteInstallerScript(string newExe)
        {
            string dir = Path.GetDirectoryName(newExe);
            string bat = Path.Combine(dir, "diskmate-update.bat");
            File.WriteAllText(bat, InstallerBat, Encoding.ASCII);
            return bat;
        }

        private const string InstallerBat =
@"@echo off
setlocal
set ""NEW=%~1""
set ""TARGET=%~2""
set ""LAUNCH=%~3""
set /a tries=0
:retry
copy /y ""%NEW%"" ""%TARGET%"" >nul 2>nul
if not errorlevel 1 goto done
set /a tries+=1
if %tries% geq 60 goto failed
ping -n 2 127.0.0.1 >nul
goto retry
:done
if /i ""%LAUNCH%""==""1"" start """" ""%TARGET%""
del ""%NEW%"" >nul 2>nul
(goto) 2>nul & del ""%~f0""
exit /b 0
:failed
> ""%NEW%.failed"" echo copy to %TARGET% failed, retried %tries% times
(goto) 2>nul & del ""%~f0""
exit /b 1
";
    }
}
