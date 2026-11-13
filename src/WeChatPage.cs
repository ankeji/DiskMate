using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskMate
{
    public partial class MainForm
    {
        // ---- 微信专清页
        private Label lblWcPath, lblWcHint, lblWcEmpty;
        private FlatButton btnWcPick, btnWcScan, btnWcClean;
        private CheckBox chkWcForce;
        private ListView lvWc;
        private string wcRoot;
        private bool wcBusy;
        private volatile bool wcCancel;
        private int wcScanned;
        private System.Windows.Forms.Timer wcTimer;

        /// <summary>当前扫描结果（key = image / video / audio / file）</summary>
        private readonly Dictionary<string, WeChatCategory> wcCats =
            new Dictionary<string, WeChatCategory>(StringComparer.OrdinalIgnoreCase);

        // ============================================================ wechat page
        private void BuildWeChatPage()
        {
            pageWechat.BackColor = Theme.Bg;

            var bar = new Panel();
            bar.BackColor = Theme.Bg;
            pageWechat.Controls.Add(bar);

            btnWcPick = new FlatButton();
            btnWcPick.Kind = BtnKind.Secondary;
            Txt(btnWcPick, "wechat.browse");
            btnWcPick.Size = new Size(Theme.P(132), Theme.P(36));
            btnWcPick.Location = new Point(Theme.P(0), Theme.P(11));
            btnWcPick.Click += delegate { PickWeChatFolder(); };
            bar.Controls.Add(btnWcPick);

            btnWcScan = new FlatButton();
            btnWcScan.Kind = BtnKind.Primary;
            Txt(btnWcScan, "wechat.scan");
            btnWcScan.Size = new Size(Theme.P(132), Theme.P(36));
            btnWcScan.Location = new Point(Theme.P(144), Theme.P(11));
            btnWcScan.Click += delegate { StartWeChatScan(); };
            bar.Controls.Add(btnWcScan);

            btnWcClean = new FlatButton();
            btnWcClean.Kind = BtnKind.Danger;
            Txt(btnWcClean, "wechat.cleanSel");
            btnWcClean.Icon = "trash";
            btnWcClean.Enabled = false;
            btnWcClean.Size = new Size(Theme.P(132), Theme.P(36));
            btnWcClean.Location = new Point(Theme.P(660), Theme.P(11));
            btnWcClean.Click += delegate { CleanWeChatSelected(); };
            bar.Controls.Add(btnWcClean);

            // 回收站被禁用 / 文件被占用时，勾上这个就直接用管理员权限删（不进回收站）
            chkWcForce = new CheckBox();
            chkWcForce.AutoSize = false;
            chkWcForce.FlatStyle = FlatStyle.Flat;
            chkWcForce.Font = Theme.Small;
            chkWcForce.ForeColor = Theme.TextSub;
            chkWcForce.BackColor = Theme.Bg;
            chkWcForce.Size = new Size(Theme.P(200), Theme.P(24));
            chkWcForce.Location = new Point(Theme.P(460), Theme.P(17));
            Txt(chkWcForce, "wechat.forceMode");
            bar.Controls.Add(chkWcForce);

            lblWcPath = new Label();
            lblWcPath.AutoSize = false;
            lblWcPath.AutoEllipsis = true;
            lblWcPath.Font = Theme.Small;
            lblWcPath.ForeColor = Theme.TextSub;
            lblWcPath.SetBounds(Theme.P(2), Theme.P(56), Theme.P(420), Theme.P(28));
            lblWcPath.Text = I18n.T("wechat.path") + "：";
            bar.Controls.Add(lblWcPath);

            lblWcHint = new Label();
            lblWcHint.AutoSize = false;
            lblWcHint.AutoEllipsis = true;
            lblWcHint.Font = Theme.Small;
            lblWcHint.ForeColor = Theme.TextSub;
            lblWcHint.SetBounds(Theme.P(432), Theme.P(56), Theme.P(400), Theme.P(28));
            Txt(lblWcHint, "wechat.hint.idle");
            bar.Controls.Add(lblWcHint);

            var card = new CardPanel();
            card.Padding = new Padding(Theme.P(12), Theme.P(2), Theme.P(8), Theme.P(10));
            pageWechat.Controls.Add(card);

            lvWc = new ListView();
            lvWc.View = View.Details;
            lvWc.FullRowSelect = true;
            lvWc.CheckBoxes = true;
            lvWc.BorderStyle = BorderStyle.None;
            lvWc.Font = Theme.Base;
            lvWc.BackColor = Theme.Surface;
            lvWc.HideSelection = false;
            var rows = new ImageList();
            rows.ImageSize = new Size(1, Theme.P(40));   // 行高放大一点，类别说明要两行
            rows.ColorDepth = ColorDepth.Depth32Bit;
            lvWc.SmallImageList = rows;
            lvWc.Columns.Add(I18n.T("wechat.col.cat"), Theme.P(150));
            lvWc.Columns.Add(I18n.T("wechat.col.count"), Theme.P(90), HorizontalAlignment.Right);
            lvWc.Columns.Add(I18n.T("wechat.col.size"), Theme.P(110), HorizontalAlignment.Right);
            lvWc.Columns.Add(I18n.T("wechat.col.note"), Theme.P(400));
            lvWc.ItemCheck += delegate (object s, ItemCheckEventArgs e)
            {
                // 聊天记录行不给勾选
                string key = lvWc.Items[e.Index].Tag as string;
                if (key == WeChatScanner.Db) e.NewValue = CheckState.Unchecked;
                BeginInvoke((MethodInvoker)UpdateWcButtons);
            };
            card.Controls.Add(lvWc);

            lblWcEmpty = new Label();
            lblWcEmpty.AutoSize = false;
            lblWcEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblWcEmpty.ForeColor = Theme.TextFaint;
            lblWcEmpty.Font = Theme.Base;
            lblWcEmpty.BackColor = Theme.Surface;
            Txt(lblWcEmpty, "wechat.empty");
            card.Controls.Add(lblWcEmpty);
            lblWcEmpty.BringToFront();

            pageWechat.Resize += delegate { LayoutWeChatPage(); };

            BuildWeChatRows();
            LoadWeChatRoot();
            LayoutWeChatPage();
            UpdateWcButtons();
        }

        private void LayoutWeChatPage()
        {
            if (pageWechat == null || lvWc == null) return;
            int w = pageWechat.Width;
            int h = pageWechat.Height;

            Control bar = btnWcPick == null ? null : btnWcPick.Parent;
            if (bar != null) bar.SetBounds(Theme.P(0), Theme.P(0), w, Theme.P(92));

            btnWcClean.Location = new Point(Math.Max(Theme.P(0), w - btnWcClean.Width), Theme.P(11));

            if (chkWcForce != null)
            {
                int cw = Theme.P(200);
                int cx = btnWcClean.Left - Theme.P(12) - cw;
                chkWcForce.SetBounds(Math.Max(Theme.P(300), cx), Theme.P(17), cw, Theme.P(24));
            }

            if (lblWcHint != null)
            {
                lblWcHint.Left = Theme.P(432);
                lblWcHint.Width = Math.Max(Theme.P(120), w - btnWcClean.Width - Theme.P(24) - lblWcHint.Left);
            }

            Control card = lvWc.Parent;
            card.SetBounds(Theme.P(0), Theme.P(92), w, Math.Max(Theme.P(160), h - Theme.P(92)));
            lvWc.SetBounds(Theme.P(0), Theme.P(0), Math.Max(Theme.P(200), card.Width - Theme.P(16)), Math.Max(Theme.P(120), card.Height - Theme.P(10)));
            lblWcEmpty.SetBounds(lvWc.Left, lvWc.Top, lvWc.Width, lvWc.Height);
        }

        /// <summary>四个类别行固定存在，扫描过程中只刷新数字</summary>
        private void BuildWeChatRows()
        {
            if (lvWc == null) return;
            lvWc.BeginUpdate();
            try
            {
                lvWc.Items.Clear();
                foreach (string key in WeChatScanner.Keys)
                {
                    string name = I18n.T("wechat.cat." + key);
                    var it = new ListViewItem(name);
                    it.Tag = key;
                    it.SubItems.Add("0");
                    it.SubItems.Add(Theme.Fmt(0));
                    it.SubItems.Add(I18n.T("wechat.cat." + key + ".sub"));
                    // 文件（文档）和聊天记录默认不勾，避免误删重要资料
                    it.Checked = key != WeChatScanner.File && key != WeChatScanner.Db;
                    lvWc.Items.Add(it);
                }
            }
            finally { lvWc.EndUpdate(); }
        }

        private void LoadWeChatRoot()
        {
            wcRoot = WeChatLocator.Detect();
            RefreshWcPath();
        }

        private void RefreshWcPath()
        {
            if (lblWcPath == null) return;
            bool ok = !string.IsNullOrEmpty(wcRoot) && Directory.Exists(wcRoot);
            lblWcPath.Text = I18n.T("wechat.path") + "：" + (ok ? wcRoot : I18n.T("wechat.pathNone"));
            lblWcPath.ForeColor = ok ? Theme.TextSub : Theme.High;
        }

        private void PickWeChatFolder()
        {
            if (wcBusy) return;
            using (var fb = new FolderBrowserDialog())
            {
                fb.Description = I18n.T("wechat.path");
                fb.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(wcRoot) && Directory.Exists(wcRoot)) fb.SelectedPath = wcRoot;
                if (fb.ShowDialog(this) != DialogResult.OK) return;

                wcRoot = fb.SelectedPath;
                Theme.WriteSetting(WeChatLocator.SettingKey, wcRoot);
                RefreshWcPath();
                ResetWeChatRows();
                lblWcHint.Text = I18n.T("wechat.hint.idle");
                lblWcEmpty.Visible = true;
                UpdateWcButtons();
            }
        }

        private void ResetWeChatRows()
        {
            lock (wcCats) wcCats.Clear();
            foreach (string key in WeChatScanner.Keys)
                wcCats[key] = WeChatScanner.Create(key);
            RefreshWeChatRows();
        }

        /// <summary>把扫描结果刷到列表上；扫描中由定时器反复调用</summary>
        /// <summary>
        /// 清理完成后立刻把已删掉的文件从统计里剔除，界面马上显示真实剩余，
        /// 不用等整盘重扫（2 GB 目录要扫一两分钟，用户会以为没删掉）。
        /// </summary>
        private void PruneWcCats()
        {
            foreach (KeyValuePair<string, WeChatCategory> kv in wcCats)
            {
                WeChatCategory c = kv.Value;
                var keep = new List<string>();
                long size = 0;
                foreach (string f in c.Files)
                {
                    try
                    {
                        if (!File.Exists(f)) continue;
                        keep.Add(f);
                        size += new FileInfo(f).Length;
                    }
                    catch { }
                }
                c.Files.Clear();
                c.Files.AddRange(keep);
                c.Count = keep.Count;
                c.Size = size;
            }
            RefreshWeChatRows();
        }

        private void RefreshWeChatRows()
        {
            if (lvWc == null) return;
            lvWc.BeginUpdate();
            try
            {
                foreach (ListViewItem it in lvWc.Items)
                {
                    string key = it.Tag as string;
                    WeChatCategory c = null;
                    if (key != null) wcCats.TryGetValue(key, out c);
                    it.SubItems[1].Text = (c == null ? 0 : c.Count).ToString("N0");
                    it.SubItems[2].Text = Theme.Fmt(c == null ? 0 : c.Size);
                }
            }
            finally { lvWc.EndUpdate(); }
            UpdateWcTotal();
        }

        private void UpdateWcTotal()
        {
            if (lblWcHint == null) return;
            if (wcBusy) return;   // 扫描/清理中由进度文案接管
            long total = 0;
            foreach (ListViewItem it in lvWc.Items)
            {
                if (!it.Checked) continue;
                string key = it.Tag as string;
                if (key == null) continue;
                WeChatCategory c = null;
                if (wcCats.TryGetValue(key, out c) && c != null) total += c.Size;
            }
            lblWcHint.Text = I18n.F("wechat.selTotal", Theme.Fmt(total));
        }

        private void UpdateWcButtons()
        {
            if (btnWcClean == null) return;
            bool any = false;
            foreach (ListViewItem it in lvWc.Items) if (it.Checked) any = true;
            bool enable = any && !wcBusy;
            if (btnWcClean.Enabled != enable) { btnWcClean.Enabled = enable; btnWcClean.Invalidate(); }
        }

        // ------------------------------------------------------------ 扫描
        private void StartWeChatScan()
        {
            if (wcBusy) return;
            if (!RequireLogin(FeatureGate.WeChat)) return;

            if (string.IsNullOrEmpty(wcRoot) || !Directory.Exists(wcRoot))
            {
                ShowBanner(BannerKind.Warn, I18n.T("wechat.none"), 8);
                return;
            }

            wcBusy = true;
            wcCancel = false;
            wcScanned = 0;
            ResetWeChatRows();
            lblWcEmpty.Visible = false;

            btnWcScan.Enabled = false;
            Txt(btnWcScan, "wechat.scanning");
            btnWcScan.Invalidate();
            btnWcClean.Enabled = false;
            lblWcHint.Text = I18n.F("wechat.progress", 0);

            StartWcTimer();

            string root = wcRoot;
            var target = wcCats;

            Task.Factory.StartNew(delegate
            {
                try
                {
                    WeChatScanner.Scan(root, target, delegate { return wcCancel; },
                        delegate (int n) { wcScanned = n; });
                }
                catch { }
                Ui(delegate { FinishWeChatScan(); });
            }, TaskCreationOptions.LongRunning);
        }

        private void StartWcTimer()
        {
            if (wcTimer == null)
            {
                wcTimer = new System.Windows.Forms.Timer();
                wcTimer.Interval = 250;
                wcTimer.Tick += delegate
                {
                    RefreshWeChatRows();
                    if (wcBusy) lblWcHint.Text = I18n.F("wechat.progress", wcScanned);
                };
            }
            wcTimer.Start();
        }

        private void StopWcTimer()
        {
            if (wcTimer != null) wcTimer.Stop();
        }

        private void FinishWeChatScan()
        {
            StopWcTimer();
            wcBusy = false;
            RefreshWeChatRows();

            btnWcScan.Enabled = true;
            Txt(btnWcScan, "wechat.scan");
            btnWcScan.Invalidate();

            long total = 0;
            int count = 0;
            foreach (string key in WeChatScanner.Keys)
            {
                WeChatCategory c;
                if (wcCats.TryGetValue(key, out c)) { total += c.Size; count += c.Count; }
            }

            lblWcEmpty.Visible = count == 0;
            lblWcHint.Text = count == 0
                ? I18n.T("wechat.nothing")
                : I18n.F("wechat.scanDone", wcScanned, Theme.Fmt(total));
            Log("微信专清扫描：检查 " + wcScanned + " 个文件，可清理 " + count + " 个（" + Theme.Fmt(total) + "）");
            UpdateWcButtons();
        }

        // ------------------------------------------------------------ 清理
        private void CleanWeChatSelected()
        {
            if (wcBusy) return;

            var files = new List<string>();
            long total = 0;
            foreach (ListViewItem it in lvWc.Items)
            {
                if (!it.Checked) continue;
                string key = it.Tag as string;
                WeChatCategory c;
                if (key == null || !wcCats.TryGetValue(key, out c)) continue;
                files.AddRange(c.Files);
                total += c.Size;
            }

            if (files.Count == 0)
            {
                ShowBanner(BannerKind.Warn, I18n.T("wechat.pick"), 10);
                return;
            }

            bool direct = chkWcForce != null && chkWcForce.Checked;

            string askTitle = I18n.T(direct ? "wechat.delTitleForce" : "wechat.delTitle");
            string askText = I18n.F(direct ? "wechat.delAskForce" : "wechat.delAsk", files.Count, Theme.Fmt(total));
            string askOk = I18n.T(direct ? "wechat.delOkForce" : "wechat.delOk");

            // 微信还在跑的话，直删十有八九删不掉，先把话说清楚
            if (direct && Engine.WeChatRunning())
                askText = askText + "\n\n" + I18n.T("wechat.runningHint");

            if (!MessageDialog.Ask(this, askTitle, askText, askOk, true))
                return;

            wcBusy = true;
            btnWcClean.Enabled = false;
            btnWcScan.Enabled = false;

            if (direct)
            {
                // 用户明确要求直接删：跳过回收站，管理员权限 DeleteFileW
                lblWcHint.Text = I18n.T("wechat.forcing");
                Log("微信专清：直接删除 " + files.Count + " 个文件（共 " + Theme.Fmt(total) + "）");

                Task.Factory.StartNew(delegate
                {
                    int failed = 0;
                    long freed = 0;
                    var log = new StringBuilder();
                    try { freed = Engine.ForceDeleteFiles(files, log, out failed); }
                    catch (Exception ex) { log.AppendLine("    强制删除异常：" + ex.Message); failed = files.Count; }

                    Ui(delegate { FinishWcClean(freed, failed, log, files); });
                }, TaskCreationOptions.LongRunning);
                return;
            }

            lblWcHint.Text = I18n.T("wechat.cleaning");

            Task.Factory.StartNew(delegate
            {
                long freed = 0;
                int failed = 0;
                var log = new StringBuilder();
                try
                {
                    freed = WeChatScanner.RecycleToBin(files, log, out failed);
                }
                catch { }

                if (failed <= 0)
                {
                    Ui(delegate { FinishWcClean(freed, 0, log, files); });
                    return;
                }

                // 回收站删不掉（只读、被占用、Shell 静默失败）→ 问用户要不要直接强删
                var left = new List<string>();
                foreach (string f in files)
                {
                    try { if (File.Exists(f)) left.Add(f); }
                    catch { }
                }

                Ui(delegate
                {
                    if (left.Count == 0) { FinishWcClean(freed, 0, log, files); return; }

                    bool force = MessageDialog.Ask(this, I18n.T("wechat.forceTitle"),
                        I18n.F("wechat.forceAsk", left.Count, Theme.Fmt(WeChatScanner.TotalSize(left))),
                        I18n.T("wechat.forceOk"), true);

                    if (!force) { FinishWcClean(freed, left.Count, log, null); return; }

                    lblWcHint.Text = I18n.T("wechat.forcing");

                    Task.Factory.StartNew(delegate
                    {
                        int still = 0;
                        long more = 0;
                        try { more = Engine.ForceDeleteFiles(left, log, out still); }
                        catch { }

                        Ui(delegate { FinishWcClean(freed + more, still, log, left); });
                    }, TaskCreationOptions.LongRunning);
                });
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 清理收尾：如果还有没删掉的，先查是谁占着，问用户要不要关掉微信再试一次；
        /// attempted 为 null 表示这次是重试后的收尾，不再追问。
        /// </summary>
        private void FinishWcClean(long freed, int failed, StringBuilder log, List<string> attempted)
        {
            if (failed > 0 && attempted != null)
            {
                var left = new List<string>();
                foreach (string f in attempted)
                {
                    try { if (File.Exists(f)) left.Add(f); }
                    catch { }
                }

                if (left.Count > 0)
                {
                    string who = Engine.LastLockers.Count > 0
                        ? string.Join("、", Engine.LastLockers.ToArray())
                        : "";

                    string text = who.Length > 0
                        ? I18n.F("wechat.killAsk", left.Count, who)
                        : I18n.F("wechat.killAskNoLocker", left.Count);

                    if (MessageDialog.Ask(this, I18n.T("wechat.killTitle"), text, I18n.T("wechat.killOk"), true))
                    {
                        lblWcHint.Text = I18n.T("wechat.killing");

                        Task.Factory.StartNew(delegate
                        {
                            int killed = Engine.KillWeChatProcesses();

                            var log2 = new StringBuilder();
                            if (log != null && log.Length > 0) log2.Append(log.ToString()).AppendLine();
                            log2.AppendLine("    关闭微信进程 " + killed + " 个，重试删除 " + left.Count + " 个文件");

                            int still = 0;
                            long more = 0;
                            try { more = Engine.ForceDeleteFiles(left, log2, out still); }
                            catch { }

                            Ui(delegate { FinishWcClean(freed + more, still, log2, null); });
                        }, TaskCreationOptions.LongRunning);
                        return;
                    }
                }
            }

            wcBusy = false;

            if (failed > 0)
                ShowBanner(BannerKind.Warn, I18n.F("wechat.partial", failed, Theme.Fmt(freed)), 14);
            else
                ShowBanner(BannerKind.Success, I18n.F("wechat.done", Theme.Fmt(freed)), 14);

            Log("微信专清：释放 " + Theme.Fmt(freed) + (failed > 0 ? "，仍有 " + failed + " 个文件没删掉" : ""));
            if (log != null && log.Length > 0) Log(log.ToString().TrimEnd());

            Session.PingActivity("wechat");

            PruneWcCats();         // 界面立刻反映剩余，不等重扫
            StartWeChatScan();     // 再后台重扫一遍，确认磁盘真实状态
        }

        // ------------------------------------------------------------ 调试/自检
        /// <summary>自检用：把微信目录换成指定路径（不落地到设置）</summary>
        public void TestSetWeChatRoot(string path)
        {
            wcRoot = path;
            RefreshWcPath();
        }

        /// <summary>调试用：直接开始扫描（跳过登录门禁，供 --wechatprobe 使用）</summary>
        public void TestScanWeChat()
        {
            if (wcBusy) return;
            if (string.IsNullOrEmpty(wcRoot) || !Directory.Exists(wcRoot)) return;

            wcBusy = true;
            wcCancel = false;
            wcScanned = 0;
            ResetWeChatRows();
            lblWcEmpty.Visible = false;
            lblWcHint.Text = I18n.F("wechat.progress", 0);
            StartWcTimer();

            string root = wcRoot;
            var target = wcCats;
            Task.Factory.StartNew(delegate
            {
                try { WeChatScanner.Scan(root, target, delegate { return wcCancel; }, delegate (int n) { wcScanned = n; }); }
                catch { }
                Ui(delegate { FinishWeChatScan(); });
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>调试用：页面各控件位置</summary>
        public string DescribeWeChatLayout()
        {
            var sb = new StringBuilder();
            sb.AppendLine("page=" + pageWechat.Width + "x" + pageWechat.Height);
            sb.AppendLine("pick=" + btnWcPick.Bounds + " scan=" + btnWcScan.Bounds + " clean=" + btnWcClean.Bounds);
            if (chkWcForce != null) sb.AppendLine("forceChk=" + chkWcForce.Bounds + " checked=" + chkWcForce.Checked + " text=[" + chkWcForce.Text + "]");
            sb.AppendLine("path=" + lblWcPath.Bounds + " hint=" + lblWcHint.Bounds);
            sb.AppendLine("list=" + lvWc.Bounds + " rows=" + lvWc.Items.Count);
            sb.AppendLine("hintText=" + lblWcHint.Text);
            sb.AppendLine("pathText=" + lblWcPath.Text);
            return sb.ToString();
        }

        /// <summary>调试用：扫描状态一行</summary>
        public string TestWeChatState()
        {
            long total = 0;
            var sb = new StringBuilder();
            foreach (string key in WeChatScanner.Keys)
            {
                WeChatCategory c;
                if (!wcCats.TryGetValue(key, out c)) continue;
                total += c.Size;
                sb.Append(key).Append("=").Append(c.Count).Append("/").Append(Theme.Fmt(c.Size)).Append("  ");
            }
            return "root=" + wcRoot + " scanned=" + wcScanned + " busy=" + wcBusy + " total=" + Theme.Fmt(total) + " | " + sb;
        }

        public bool TestWeChatBusy()
        {
            return wcBusy;
        }

        /// <summary>调试用：对某个类别做一次清理（回收站），返回释放字节</summary>
        public long TestCleanWeChat(string key)
        {
            return TestCleanWeChat(key, false);
        }

        /// <summary>调试用：direct = 直接强删（等同勾上「直接删除，不进回收站」）</summary>
        public long TestCleanWeChat(string key, bool direct)
        {
            WeChatCategory c;
            if (!wcCats.TryGetValue(key, out c)) return 0;

            var files = new List<string>(c.Files);
            var log = new StringBuilder();

            if (direct)
            {
                int df;
                long d = Engine.ForceDeleteFiles(files, log, out df);
                Console.WriteLine("  direct : freed=" + Theme.Fmt(d) + " failed=" + df);
                if (log.Length > 0) Console.WriteLine(log.ToString().TrimEnd());
                return d;
            }

            int failed;
            long freed = WeChatScanner.RecycleToBin(files, log, out failed);
            Console.WriteLine("  recycle: freed=" + Theme.Fmt(freed) + " failed=" + failed);

            if (failed > 0)
            {
                var left = new List<string>();
                foreach (string f in files)
                {
                    try { if (File.Exists(f)) left.Add(f); }
                    catch { }
                }
                int still;
                long more = Engine.ForceDeleteFiles(left, log, out still);
                Console.WriteLine("  force  : freed=" + Theme.Fmt(more) + " failed=" + still);
                freed += more;
            }

            if (log.Length > 0) Console.WriteLine(log.ToString().TrimEnd());
            return freed;
        }
    }
}
