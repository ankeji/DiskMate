using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DiskMate
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            bool selfTest = false, makeIcon = false, langTest = false, checkUpdate = false, updateTest = false, cleanProbe = false, maskProbe = false;
            string installTest = null;
            string dialogTest = null;
            string writeVersion = null;
            string donateTest = null;
            string contactTest = null;
            string wechatTest = null;
            string authTest = null;
            string accountTest = null;
            string announceTest = null;
            string outPath = null;
            bool featureProbe = false;
            string buildInfo = null;   // "--buildinfo" 打印到控制台；"--buildinfo=file" 同时写文件（CI 校验用）
            foreach (string a in args)
            {
                if (a.StartsWith("--update-url=", StringComparison.OrdinalIgnoreCase))
                    UpdateService.ManifestUrl = a.Substring("--update-url=".Length);
                else if (a.StartsWith("--buildinfo=", StringComparison.OrdinalIgnoreCase)) buildInfo = a.Substring("--buildinfo=".Length);
                else if (string.Equals(a, "--buildinfo", StringComparison.OrdinalIgnoreCase)) buildInfo = "";
                else if (string.Equals(a, "--selftest", StringComparison.OrdinalIgnoreCase)) selfTest = true;
                else if (string.Equals(a, "--maskprobe", StringComparison.OrdinalIgnoreCase)) maskProbe = true;
                else if (string.Equals(a, "--cleanprobe", StringComparison.OrdinalIgnoreCase)) cleanProbe = true;
                else if (string.Equals(a, "--featureprobe", StringComparison.OrdinalIgnoreCase)) featureProbe = true;
                else if (string.Equals(a, "--makeicon", StringComparison.OrdinalIgnoreCase)) makeIcon = true;
                else if (string.Equals(a, "--langtest", StringComparison.OrdinalIgnoreCase)) langTest = true;
                else if (string.Equals(a, "--checkupdate", StringComparison.OrdinalIgnoreCase)) checkUpdate = true;
                else if (string.Equals(a, "--updatetest", StringComparison.OrdinalIgnoreCase)) updateTest = true;
                else if (a.StartsWith("--installtest=", StringComparison.OrdinalIgnoreCase)) installTest = a.Substring("--installtest=".Length);
                else if (a.StartsWith("--updatedialog=", StringComparison.OrdinalIgnoreCase)) dialogTest = a.Substring("--updatedialog=".Length);
                else if (a.StartsWith("--writeversion=", StringComparison.OrdinalIgnoreCase)) writeVersion = a.Substring("--writeversion=".Length);
                else if (a.StartsWith("--donatedialog=", StringComparison.OrdinalIgnoreCase)) donateTest = a.Substring("--donatedialog=".Length);
                else if (a.StartsWith("--contactdialog=", StringComparison.OrdinalIgnoreCase)) contactTest = a.Substring("--contactdialog=".Length);
                else if (a.StartsWith("--wechatdialog=", StringComparison.OrdinalIgnoreCase)) wechatTest = a.Substring("--wechatdialog=".Length);
                else if (a.StartsWith("--authtest=", StringComparison.OrdinalIgnoreCase)) authTest = a.Substring("--authtest=".Length);
                else if (a.StartsWith("--accountdialog=", StringComparison.OrdinalIgnoreCase)) accountTest = a.Substring("--accountdialog=".Length);
                else if (a.StartsWith("--announcedialog=", StringComparison.OrdinalIgnoreCase)) announceTest = a.Substring("--announcedialog=".Length);
                else if (!a.StartsWith("--")) outPath = a;
            }

            if (buildInfo != null)
            {
                string text =
                    "version=" + AppInfo.Version + Environment.NewLine +
                    "channel=" + (Api.IsDev ? "dev" : "prod") + Environment.NewLine +
                    "api=" + Api.BaseUrl + Environment.NewLine;
                if (buildInfo.Length > 0)
                {
                    try { File.WriteAllText(buildInfo, text, new UTF8Encoding(false)); } catch { }
                }
                try { Native.AttachConsole(-1); } catch { }
                Console.Write(text);
                return;
            }

            if (selfTest)
            {
                UpdateService.Disabled = true;
                RunSelfTest();
                return;
            }

            if (maskProbe)
            {
                UpdateService.Disabled = true;
                try { Native.AttachConsole(-1); } catch { }
                RunMaskProbe();
                return;
            }

            if (cleanProbe)
            {
                UpdateService.Disabled = true;
                RunCleanProbe();
                return;
            }

            if (featureProbe)
            {
                UpdateService.Disabled = true;
                RunFeatureProbe();
                return;
            }

            if (makeIcon)
            {
                try { Native.AttachConsole(-1); } catch { }
                string target = outPath != null ? outPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                Logo.SaveIco(target);
                Console.WriteLine("icon written: " + target + "  " + new FileInfo(target).Length + " bytes");
                return;
            }

            if (langTest)
            {
                try { Native.AttachConsole(-1); } catch { }
                foreach (string code in I18n.Codes)
                {
                    I18n.Lang = code;
                    Console.WriteLine(code + "  " + I18n.NativeName(code));
                    Console.WriteLine("   " + I18n.T("nav.clean") + " | " + I18n.T("welcome.title") + " | " + I18n.F("sel.some", 8, "8.23 GB"));
                    Console.WriteLine("   " + I18n.T("item.npm.name") + " | " + I18n.T("risk.high") + " | " + I18n.F("update.btn", AppInfo.Version));
                }
                return;
            }

            if (updateTest)
            {
                try { Native.AttachConsole(-1); } catch { }
                Console.WriteLine("DiskMate " + AppInfo.Version + "  manifest=" + UpdateService.ManifestUrl);
                string err;
                UpdateInfo info = UpdateService.Check(out err);
                if (err != null) { Console.WriteLine("check failed: " + err); return; }
                if (info == null) { Console.WriteLine("up to date"); return; }
                Console.WriteLine("available: " + info.Version + "  url=" + info.Url);
                string path = UpdateService.Download(info, delegate(long done, long total)
                {
                    if (total > 0 && done == total) Console.WriteLine("downloaded " + done + " / " + total);
                }, null, out err);
                if (path == null) { Console.WriteLine("download failed: " + err); return; }
                Console.WriteLine("file: " + path + "  (" + new FileInfo(path).Length + " bytes)");
                string verr;
                bool ok = UpdateService.Verify(path, info, out verr);
                Console.WriteLine("sha256 verify: " + (ok ? "OK" : "FAILED " + verr));
                Console.WriteLine("actual sha256: " + UpdateService.Sha256Of(path));
                return;
            }

            if (installTest != null)
            {
                try { Native.AttachConsole(-1); } catch { }
                string[] parts = installTest.Split('|');
                if (parts.Length != 2) { Console.WriteLine("usage: --installtest=NEW.exe|TARGET.exe"); return; }
                string bat = UpdateService.WriteInstallerScript(parts[0]);
                Console.WriteLine("script: " + bat);
                Console.WriteLine("sha256 new   : " + UpdateService.Sha256Of(parts[0]));
                Console.WriteLine("sha256 target before: " + UpdateService.Sha256Of(parts[1]));
                var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe",
                    "/c \"\"" + bat + "\" \"" + parts[0] + "\" \"" + parts[1] + "\" 0\"");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                using (var proc = System.Diagnostics.Process.Start(psi)) proc.WaitForExit(70000);
                Console.WriteLine("target size after swap: " + new FileInfo(parts[1]).Length);
                Console.WriteLine("sha256 target after : " + UpdateService.Sha256Of(parts[1]));
                Console.WriteLine("script removed: " + (!File.Exists(bat)) + "  payload removed: " + (!File.Exists(parts[0])));
                return;
            }
            if (donateTest != null)
            {
                try { Native.AttachConsole(-1); } catch { }
                string[] dt = donateTest.Split('|');
                if (dt.Length > 1) I18n.Lang = dt[1];
                DonateDialog.RenderTo(dt[0]);
                Console.WriteLine("donate dialog rendered: " + dt[0] + "  lang=" + I18n.Lang);
                return;
            }
            if (contactTest != null)
            {
                try { Native.AttachConsole(-1); } catch { }
                string[] ct = contactTest.Split('|');
                if (ct.Length > 1) I18n.Lang = ct[1];
                ContactDialog.RenderTo(ct[0]);
                Console.WriteLine("contact dialog rendered: " + ct[0] + "  lang=" + I18n.Lang);
                return;
            }
            if (wechatTest != null)
            {
                try { Native.AttachConsole(-1); } catch { }
                string[] wt = wechatTest.Split('|');
                if (wt.Length > 1) I18n.Lang = wt[1];
                WeChatDialog.RenderTo(wt[0]);
                Console.WriteLine("wechat dialog rendered: " + wt[0] + "  lang=" + I18n.Lang);
                return;
            }
            if (authTest != null)
            {
                try { Native.AttachConsole(-1); } catch { }
                RunAuthProbe(authTest);
                return;
            }
            if (accountTest != null)
            {
                try { Native.AttachConsole(-1); } catch { }
                string[] at = accountTest.Split('|');
                AccountDialog.RenderTo(at[0], at.Length > 1 ? at[1] : "login");
                Console.WriteLine("account dialog rendered: " + at[0]);
                return;
            }
            if (announceTest != null)
            {
                try { Native.AttachConsole(-1); } catch { }
                string[] an = announceTest.Split('|');
                string apath = an[0];
                string atitle = an.Length > 1 ? an[1] : "关于清理功能的重要说明";
                string abody = an.Length > 2 ? an[2].Replace("\\n", "\n") : "这是一条测试公告。";
                string alevel = an.Length > 3 ? an[3] : "info";
                AnnouncementDialog.RenderTo(apath, atitle, abody, alevel);
                Console.WriteLine("announce dialog rendered: " + apath);
                return;
            }
            if (writeVersion != null)
            {
                try { Native.AttachConsole(-1); } catch { }
                string[] w = writeVersion.Split('|');
                string path = w[0];
                string exeUrl = w.Length > 1 ? w[1] : "";
                string nz = w.Length > 2 ? w[2] : null;
                string ne = w.Length > 3 ? w[3] : null;
                UpdateService.WriteManifest(path, exeUrl, nz, ne);
                Console.WriteLine("manifest written: " + Path.GetFullPath(path));
                Console.WriteLine(File.ReadAllText(path));
                return;
            }
            if (dialogTest != null)
            {
                try { Native.AttachConsole(-1); } catch { }
                var fake = new UpdateInfo();
                fake.Version = "2.1.1";
                fake.Url = "https://example.com/diskmate/DiskMate-2.1.1.exe";
                fake.Sha256 = "699D41EAE90B068BAB4A655F54264376C64F574164BFE8353B54798778FC944D";
                fake.Size = 311296;
                fake.Date = "2026-11-10";
                fake.Notes = I18n.T("update.notesSample");
                UpdateDialog.RenderTo(dialogTest, fake);
                Console.WriteLine("dialog rendered: " + dialogTest);
                return;
            }
            if (checkUpdate)
            {
                try { Native.AttachConsole(-1); } catch { }
                Console.WriteLine("DiskMate " + AppInfo.Version + "  manifest=" + UpdateService.ManifestUrl);
                string err;
                UpdateInfo info = UpdateService.Check(out err);
                if (err != null) Console.WriteLine("check failed: " + err);
                else if (info == null) Console.WriteLine("up to date (no newer version)");
                else
                {
                    Console.WriteLine("update available: " + info.Version);
                    Console.WriteLine("  url   = " + info.Url);
                    Console.WriteLine("  source= " + info.Source);
                    Console.WriteLine("  sha256= " + (info.Sha256.Length == 0 ? "(none)" : info.Sha256));
                    Console.WriteLine("  size  = " + info.Size);
                    Console.WriteLine("  notes = " + info.Notes.Replace(Environment.NewLine, " / "));
                }
                return;
            }

            try { Native.SetProcessDPIAware(); }
            catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e) { LogCrash(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { LogCrash(e.ExceptionObject as Exception); };
            Application.Run(new MainForm());
        }

        /// <summary>
        /// 功能开关自检：真拉一次 /api/features，把三个功能的「是否需登录」打出来。
        /// 用法：DiskMate.exe --featureprobe（结果同时写到 exe 目录 diskmate-featureprobe.txt）
        /// </summary>
        private static void RunFeatureProbe()
        {
            try { Native.AttachConsole(-1); } catch { }
            var sb = new StringBuilder();
            try
            {
                Api.LoadConfig();
                sb.AppendLine("api.base = " + Api.BaseUrl);

                FeatureGate.Load();                       // 和正常启动走同一条路径
                System.Threading.Thread.Sleep(2200);      // 等后台那次刷新回来
                sb.AppendLine("gates    = " + FeatureGate.Describe());
                sb.AppendLine("clean    requireLogin = " + FeatureGate.LoginRequired(FeatureGate.Clean));
                sb.AppendLine("wechat   requireLogin = " + FeatureGate.LoginRequired(FeatureGate.WeChat));
                sb.AppendLine("big      requireLogin = " + FeatureGate.LoginRequired(FeatureGate.Big));

                string cache = FeatureGate.CacheFile;
                sb.AppendLine("cache    = " + (string.IsNullOrEmpty(cache) ? "未生成" : "已写入 " + cache));
                if (!string.IsNullOrEmpty(FeatureGate.LastError)) sb.AppendLine("lastErr  = " + FeatureGate.LastError);
            }
            catch (Exception ex)
            {
                sb.AppendLine("probe failed: " + ex.Message);
            }

            try
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diskmate-featureprobe.txt"),
                    sb.ToString(), Encoding.UTF8);
            }
            catch { }
            Console.WriteLine(sb.ToString());
        }

        private static void LogCrash(Exception ex)
        {
            if (ex == null) return;
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate", "logs");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "crash-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
                File.WriteAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + ex, Encoding.UTF8);
            }
            catch { }
            try { MessageBox.Show(ex.Message, "DiskMate 出错了", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            catch { }
        }

        private static bool IsAdmin()
        {
            try
            {
                return new System.Security.Principal.WindowsPrincipal(
                    System.Security.Principal.WindowsIdentity.GetCurrent())
                    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        /// <summary>
        /// 账号链路自检：真的走 HTTP 打通「图形验证码 → 登录 → me → 刷新」。
        /// 用法：DiskMate.exe --authtest=邮箱|密码
        /// </summary>
        private static void RunAuthProbe(string spec)
        {
            Api.LoadConfig();
            Console.WriteLine("api.base = " + Api.BaseUrl);
            string[] parts = spec.Split('|');
            string email = parts.Length > 0 ? parts[0] : "";
            string password = parts.Length > 1 ? parts[1] : "";

            int ok = 0, bad = 0;
            Action<string, bool, string> check = delegate(string name, bool pass, string detail)
            {
                if (pass) { ok++; Console.WriteLine("  [OK]   " + name + (detail.Length > 0 ? "  " + detail : "")); }
                else { bad++; Console.WriteLine("  [FAIL] " + name + "  " + detail); }
            };

            ApiResult mode = Api.Get("/api/captcha/mode", null);
            check("验证码模式", mode.Ok, mode.Ok ? mode.DataStr("mode") : mode.Message);

            ApiResult cap = Api.Get("/api/captcha/numeric", null);
            Image img = cap.Ok ? Api.DecodeDataUrl(cap.DataStr("image")) : null;
            check("拉取图形验证码", cap.Ok && img != null,
                img == null ? cap.Message : img.Width + "x" + img.Height);

            ApiResult login = Api.Post("/api/auth/login",
                "{\"email\":\"" + Session.Escape(email) + "\",\"password\":\"" + Session.Escape(password) + "\"}", null);
            check("登录", login.Ok, login.Ok ? "" : login.Message);

            if (login.Ok)
            {
                Session.Clear();
                Session.Apply(login.Data);
                check("保存登录态", Session.IsLoggedIn, Session.Email + " #" + Session.UserId);

                ApiResult me = Session.AuthedGet("/api/auth/me");
                check("带 token 取 /me", me.Ok, me.Ok ? MiniJson.Str(MiniJson.Obj(me.Data == null ? null : Get(me.Data, "user")), "email") : me.Message);

                string oldRefresh = Session.RefreshToken;
                bool refreshed = Session.TryRefresh();
                check("refreshToken 续期", refreshed && Session.RefreshToken != oldRefresh, refreshed ? "" : "续期失败");

                ApiResult after = Session.AuthedGet("/api/auth/me");
                check("续期后仍可取 /me", after.Ok, after.Ok ? "" : after.Message);
                Session.Clear();
            }

            ApiResult wrong = Api.Post("/api/auth/login",
                "{\"email\":\"" + Session.Escape(email) + "\",\"password\":\"definitely-wrong-1\"}", null);
            check("错误密码提示可读", !wrong.Ok && wrong.Message.Length > 0 && wrong.Message != "请求失败（401）", wrong.Message);

            ApiResult offline = Api.Send("GET", "/api/nope-not-exist", null, null);
            check("404 也能拿到结构化错误", offline.Status == 404 && offline.Code == "NOT_FOUND", offline.Code);

            Console.WriteLine();
            Console.WriteLine("结果：通过 " + ok + " · 失败 " + bad);
            Environment.ExitCode = bad == 0 ? 0 : 1;
        }

        private static object Get(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v : null;
        }

        /// <summary>
        /// 清理引擎诊断：只在临时目录造数据做真实删除，不碰用户的任何文件。
        /// 用法：DiskMate.exe --cleanprobe
        /// </summary>
        private static void RunCleanProbe()
        {
            try { Native.AttachConsole(-1); } catch { }
            Console.WriteLine("DiskMate cleanprobe  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine("admin: " + (IsAdmin() ? "yes" : "no"));

            string root = Path.Combine(Path.GetTempPath(), "diskmate-probe");
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            Directory.CreateDirectory(Path.Combine(root, "sub", "deep"));
            byte[] buf = new byte[1024 * 1024];
            new Random(1).NextBytes(buf);
            File.WriteAllBytes(Path.Combine(root, "a.bin"), buf);
            File.WriteAllBytes(Path.Combine(root, "sub", "b.bin"), buf);
            File.WriteAllBytes(Path.Combine(root, "sub", "deep", "c.bin"), buf);
            string locked = Path.Combine(root, "locked.bin");
            File.WriteAllBytes(locked, buf);
            File.SetAttributes(Path.Combine(root, "sub", "b.bin"), FileAttributes.ReadOnly);

            long before = Engine.DirSize(root);
            using (new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Engine.FreeStat st = Engine.Measurable(root, null);
                Console.WriteLine("Measurable: 可删=" + Theme.Fmt(st.Free) + "  占用=" + Theme.Fmt(st.BlockedBytes) + "(" + st.Blocked + " 个)");
                var log = new StringBuilder();
                long freed = Engine.ClearDir(root, log);
                long after = Engine.DirSize(root);
                Console.WriteLine("ClearDir : before=" + Theme.Fmt(before) + "  freed=" + Theme.Fmt(freed) + "  after=" + Theme.Fmt(after));
                Console.WriteLine("log      : " + (log.Length == 0 ? "(空)" : log.ToString().Trim()));
                Console.WriteLine("期望     : 可删 == freed，after == 占用大小");
            }
            try { Directory.Delete(root, true); } catch { }

            Console.WriteLine();
            Console.WriteLine("--- 现有扫描项的可删性（只探测，不删除）---");
            using (var form = new MainForm())
            {
                List<CleanItem> items = form.SelfTestScan();
                foreach (CleanItem it in items)
                {
                    if (it.Kind != "dir") continue;
                    int files = 0, lockedCount = 0;
                    long bytes = 0;
                    foreach (string t in it.Targets) ProbeTree(t, ref files, ref lockedCount, ref bytes);
                    Console.WriteLine(string.Format("  {0,-24} {1,10}  文件 {2,6}  占用 {3,5}  可删 {4}",
                        it.Name, Theme.Fmt(it.Size), files, lockedCount, Theme.Fmt(bytes)));
                }
            }
            Console.WriteLine("done");
        }

        private static void ProbeTree(string dir, ref int files, ref int locked, ref long bytes)
        {
            string[] fs;
            try { fs = Directory.GetFiles(dir); } catch { fs = new string[0]; }
            foreach (string f in fs)
            {
                files++;
                long len = 0;
                try { len = new FileInfo(f).Length; } catch { }
                try
                {
                    using (new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                    bytes += len;
                }
                catch { locked++; }
            }
            string[] ds;
            try { ds = Directory.GetDirectories(dir); } catch { ds = new string[0]; }
            foreach (string d in ds)
            {
                try { if ((new DirectoryInfo(d).Attributes & FileAttributes.ReparsePoint) != 0) continue; }
                catch { }
                ProbeTree(d, ref files, ref locked, ref bytes);
            }
        }

        /// <summary>
        /// 无界面自检：跑一遍完整扫描，再把「欢迎页」和「结果页」各渲染成 PNG。
        /// 不删除任何文件。用法：DiskMate.exe --selftest
        /// </summary>
        private static void RunSelfTest()
        {
            try { Native.AttachConsole(-1); } catch { }
            Api.Offline = true;   // 自检不联网：CI 上连不到国内服务器会一直等超时
            var sb = new StringBuilder();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                using (var form = new MainForm())
                {
                    List<CleanItem> items = form.SelfTestScan();
                    sb.AppendLine("DiskMate selftest  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.AppendLine("version: " + AppInfo.Version);
                    sb.AppendLine("channel: " + Api.ChannelName);
                    sb.AppendLine("api    : " + Api.BaseUrl);
                    sb.AppendLine("admin: " + (IsAdmin() ? "yes" : "no"));
                    sb.AppendLine("items: " + items.Count);
                    long total = 0;
                    foreach (CleanItem it in items)
                    {
                        total += it.Size;
                        sb.AppendLine(string.Format("  [{0}] {1,-26} {2,10}  {3}",
                            it.Risk == 0 ? "低" : (it.Risk == 1 ? "中" : "高"), it.Name, Theme.Fmt(it.Size), it.Category));
                    }
                    sb.AppendLine("total: " + Theme.Fmt(total));

                    sb.AppendLine("--- ui render ---");
                    try
                    {
                        form.Show();
                        Application.DoEvents();
                        sb.AppendLine("form: " + form.Width + "x" + form.Height + "  scale=" + Theme.S.ToString("0.00"));
                        SaveShot(form, Path.Combine(baseDir, "screenshot.png"), sb, "welcome");
                        form.TestShowResults(items);
                        sb.AppendLine(form.TestLayoutDump());
                        sb.AppendLine(form.TestScrollProbe());
                        sb.AppendLine("--- text fit (default size) ---");
                        sb.AppendLine(form.TestTextFit());
                        sb.AppendLine("--- overlap check (default size) ---");
                        sb.AppendLine(form.TestOverlaps());
                        Application.DoEvents();
                        form.Refresh();
                        Application.DoEvents();
                        SaveShot(form, Path.Combine(baseDir, "_render-results.png"), sb, "results");

                        form.TestResize(Theme.WindowSize(1040, 700));
                        Application.DoEvents();
                        sb.AppendLine("--- text fit (minimum window) ---");
                        sb.AppendLine(form.TestTextFit());
                        sb.AppendLine("--- overlap check (minimum window 1040x700) ---");
                        sb.AppendLine(form.TestOverlaps());
                        form.Refresh();
                        Application.DoEvents();
                        SaveShot(form, Path.Combine(baseDir, "_render-small.png"), sb, "results-small");

                        sb.AppendLine("--- big files page ---");
                        form.TestShowBig();
                        sb.AppendLine(form.TestTextFit());
                        sb.AppendLine(form.TestOverlaps());
                        form.Refresh();
                        Application.DoEvents();
                        SaveShot(form, Path.Combine(baseDir, "_render-big.png"), sb, "big");

                        sb.AppendLine("--- wechat page ---");
                        form.TestShowWeChat();
                        Application.DoEvents();
                        sb.AppendLine(form.TestTextFit());
                        sb.AppendLine(form.TestOverlaps());
                        form.Refresh();
                        Application.DoEvents();
                        SaveShot(form, Path.Combine(baseDir, "_render-wechat-page.png"), sb, "wechat");

                        form.Hide();
                        sb.AppendLine("render: ok");

                        form.Show();
                        Application.DoEvents();
                        string originalLang = I18n.Lang;
                        sb.AppendLine("--- i18n: en-US ---");
                        sb.AppendLine(form.TestLanguage("en-US"));
                        form.Refresh();
                        Application.DoEvents();
                        SaveShot(form, Path.Combine(baseDir, "_render-en.png"), sb, "results-en");

                        sb.AppendLine("--- i18n: ja-JP ---");
                        sb.AppendLine(form.TestLanguage("ja-JP"));
                        form.Refresh();
                        Application.DoEvents();
                        SaveShot(form, Path.Combine(baseDir, "_render-ja.png"), sb, "results-ja");

                        sb.AppendLine("--- i18n: back to " + originalLang + " ---");
                        sb.AppendLine(form.TestLanguage(originalLang));
                        form.Hide();
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("render: FAILED " + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("selftest failed: " + ex);
            }

            string path = Path.Combine(Path.GetTempPath(), "diskmate-selftest.txt");
            try { File.WriteAllText(path, sb.ToString(), Encoding.UTF8); }
            catch
            {
                try { path = Path.Combine(baseDir, "diskmate-selftest.txt"); File.WriteAllText(path, sb.ToString(), Encoding.UTF8); } catch { }
            }
            Console.WriteLine(sb.ToString());
            Console.WriteLine("written: " + path);
        }

        /// <summary>
        /// 遮罩 + 模态弹窗链路自检：打开→自动关闭→再打开，验证 ModalMask 不再抛
        /// “未启用的窗体不能显示为模式对话框”，且主窗口关闭后恢复可用。
        /// </summary>
        private static void RunMaskProbe()
        {
            try
            {
                using (var main = new MainForm())
                {
                    main.Show();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(200);
                    Application.DoEvents();

                    for (int round = 1; round <= 2; round++)
                    {
                        using (var dlg = new DialogShell())
                        {
                            dlg.ClientSize = new Size(320, 200);
                            dlg.Text = "mask probe";
                            var t = new System.Windows.Forms.Timer();
                            t.Interval = 700;
                            t.Tick += delegate { t.Stop(); dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
                            t.Start();
                            DialogResult r = ModalMask.Wrap(main, delegate { return dlg.ShowDialog(main); });
                            Console.WriteLine("round " + round + ": result=" + r + "  main.Enabled=" + main.Enabled);
                        }
                        Application.DoEvents();
                    }
                    main.Close();
                }
                Console.WriteLine("maskprobe PASS");
            }
            catch (Exception ex)
            {
                Console.WriteLine("maskprobe FAIL: " + ex.Message);
                Environment.ExitCode = 1;
            }
            Environment.Exit(0);
        }

        private static void SaveShot(Form form, string path, StringBuilder sb, string tag)
        {
            using (var bmp = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    File.WriteAllBytes(path, ms.ToArray());
                    var colors = new Dictionary<int, int>();
                    long bright = 0; long n = 0;
                    for (int y = 0; y < bmp.Height; y += 3)
                        for (int x = 0; x < bmp.Width; x += 3)
                        {
                            Color c = bmp.GetPixel(x, y);
                            int key = c.ToArgb();
                            if (!colors.ContainsKey(key)) colors[key] = 0;
                            colors[key]++;
                            bright += (c.R + c.G + c.B) / 3;
                            n++;
                        }
                    sb.AppendLine(string.Format("{0}: {1} {2}x{3} {4} bytes, colors={5}, brightness={6:N1}",
                        tag, Path.GetFileName(path), bmp.Width, bmp.Height, ms.Length, colors.Count, (double)bright / Math.Max(1, n)));
                }
            }
        }
    }
}
