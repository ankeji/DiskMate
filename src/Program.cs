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
            bool selfTest = false, makeIcon = false, langTest = false, checkUpdate = false, updateTest = false;
            string installTest = null;
            string dialogTest = null;
            string writeVersion = null;
            string outPath = null;
            foreach (string a in args)
            {
                if (a.StartsWith("--update-url=", StringComparison.OrdinalIgnoreCase))
                    UpdateService.ManifestUrl = a.Substring("--update-url=".Length);
                else if (string.Equals(a, "--selftest", StringComparison.OrdinalIgnoreCase)) selfTest = true;
                else if (string.Equals(a, "--makeicon", StringComparison.OrdinalIgnoreCase)) makeIcon = true;
                else if (string.Equals(a, "--langtest", StringComparison.OrdinalIgnoreCase)) langTest = true;
                else if (string.Equals(a, "--checkupdate", StringComparison.OrdinalIgnoreCase)) checkUpdate = true;
                else if (string.Equals(a, "--updatetest", StringComparison.OrdinalIgnoreCase)) updateTest = true;
                else if (a.StartsWith("--installtest=", StringComparison.OrdinalIgnoreCase)) installTest = a.Substring("--installtest=".Length);
                else if (a.StartsWith("--updatedialog=", StringComparison.OrdinalIgnoreCase)) dialogTest = a.Substring("--updatedialog=".Length);
                else if (a.StartsWith("--writeversion=", StringComparison.OrdinalIgnoreCase)) writeVersion = a.Substring("--writeversion=".Length);
                else if (!a.StartsWith("--")) outPath = a;
            }

            if (selfTest)
            {
                UpdateService.Disabled = true;
                RunSelfTest();
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
        /// 无界面自检：跑一遍完整扫描，再把「欢迎页」和「结果页」各渲染成 PNG。
        /// 不删除任何文件。用法：DiskMate.exe --selftest
        /// </summary>
        private static void RunSelfTest()
        {
            try { Native.AttachConsole(-1); } catch { }
            var sb = new StringBuilder();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                using (var form = new MainForm())
                {
                    List<CleanItem> items = form.SelfTestScan();
                    sb.AppendLine("DiskMate selftest  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
                        sb.AppendLine("form: " + form.Width + "x" + form.Height);
                        SaveShot(form, Path.Combine(baseDir, "screenshot.png"), sb, "welcome");
                        form.TestShowResults(items);
                        sb.AppendLine(form.TestLayoutDump());
                        sb.AppendLine("--- overlap check (default size) ---");
                        sb.AppendLine(form.TestOverlaps());
                        Application.DoEvents();
                        form.Refresh();
                        Application.DoEvents();
                        SaveShot(form, Path.Combine(baseDir, "_render-results.png"), sb, "results");

                        form.TestResize(new Size(1040, 700));
                        Application.DoEvents();
                        sb.AppendLine("--- overlap check (minimum window 1040x700) ---");
                        sb.AppendLine(form.TestOverlaps());
                        form.Refresh();
                        Application.DoEvents();
                        SaveShot(form, Path.Combine(baseDir, "_render-small.png"), sb, "results-small");

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
