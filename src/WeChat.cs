using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskMate
{
    /// <summary>
    /// 微信专清：微信目录每个人都不一样（默认 文档\WeChat Files，4.x 是 xwechat_files），
    /// 所以先自动猜，猜不到让用户自己选。只清 FileStorage\Cache、Video、Temp 这类可再生的缓存，
    /// 绝不碰聊天记录数据库（Msg / db_storage）。
    /// </summary>
    internal class WeChatDialog : DialogShell
    {
        public const string SettingKey = "wechat.path";

        private readonly int W = Theme.P(640);
        private readonly int H = Theme.P(520);

        private Label lblPath, lblStatus, lblTotal;
        private Panel listHost;
        private FlatButton btnClean, btnDetect, btnBrowse;
        private readonly List<Row> rows = new List<Row>();
        private string root;
        private bool busy;

        private class Row
        {
            public string Path;
            public string Title;
            public string Kind;
            public long Size;
            public bool Checked = true;
            public CheckRow Ui;
        }

        private WeChatDialog()
        {
            ClientSize = new Size(W, H);
            Text = I18n.T("wechat.title");
            BuildUi();
            root = Detect();
            if (!string.IsNullOrEmpty(root)) Theme.WriteSetting(SettingKey, root);
            RefreshPathLabel();
            ScanAsync();
        }

        // ------------------------------------------------------------ UI
        private void BuildUi()
        {
            var title = new Label();
            title.AutoSize = false;
            title.Location = new Point(Theme.P(30), Theme.P(22));
            title.Size = new Size(W - Theme.P(60), Theme.P(30));
            title.Font = Theme.F(14f, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.Text = I18n.T("wechat.title");
            Controls.Add(title);

            var sub = new Label();
            sub.AutoSize = false;
            sub.Location = new Point(Theme.P(30), Theme.P(54));
            sub.Size = new Size(W - Theme.P(60), Theme.P(38));
            sub.Font = Theme.Small;
            sub.ForeColor = Theme.TextSub;
            sub.Text = I18n.T("wechat.sub");
            Controls.Add(sub);

            var cap = new Label();
            cap.AutoSize = false;
            cap.Location = new Point(Theme.P(30), Theme.P(100));
            cap.Size = new Size(W - Theme.P(60), Theme.P(18));
            cap.Font = Theme.Small;
            cap.ForeColor = Theme.TextSub;
            cap.Text = I18n.T("wechat.path");
            Controls.Add(cap);

            lblPath = new Label();
            lblPath.AutoSize = false;
            lblPath.Location = new Point(Theme.P(30), Theme.P(120));
            lblPath.Size = new Size(W - Theme.P(240), Theme.P(38));
            lblPath.Font = Theme.Small;
            lblPath.ForeColor = Theme.Text;
            lblPath.TextAlign = ContentAlignment.MiddleLeft;
            lblPath.AutoEllipsis = true;
            Controls.Add(lblPath);

            btnDetect = new FlatButton();
            btnDetect.Kind = BtnKind.Ghost;
            btnDetect.Text = I18n.T("wechat.detect");
            btnDetect.Size = new Size(Theme.P(96), Theme.P(32));
            btnDetect.Location = new Point(W - Theme.P(30) - Theme.P(96) - Theme.P(110), Theme.P(122));
            btnDetect.Click += delegate { if (!busy) { root = Detect(); if (root != null) Theme.WriteSetting(SettingKey, root); RefreshPathLabel(); ScanAsync(); } };
            Controls.Add(btnDetect);

            btnBrowse = new FlatButton();
            btnBrowse.Kind = BtnKind.Secondary;
            btnBrowse.Text = I18n.T("wechat.browse");
            btnBrowse.Size = new Size(Theme.P(104), Theme.P(32));
            btnBrowse.Location = new Point(W - Theme.P(30) - Theme.P(104), Theme.P(122));
            btnBrowse.Click += delegate { Browse(); };
            Controls.Add(btnBrowse);

            listHost = new Panel();
            listHost.Location = new Point(Theme.P(30), Theme.P(170));
            listHost.Size = new Size(W - Theme.P(60), Theme.P(262));
            listHost.BackColor = Theme.Surface;
            listHost.AutoScroll = true;
            listHost.Paint += delegate(object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Theme.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, listHost.Width - 1, listHost.Height - 1);
            };
            listHost.Resize += delegate { LayoutRows(); };
            Controls.Add(listHost);

            lblTotal = new Label();
            lblTotal.AutoSize = false;
            lblTotal.Location = new Point(Theme.P(30), Theme.P(442));
            lblTotal.Size = new Size(Theme.P(300), Theme.P(22));
            lblTotal.Font = Theme.F(11f, FontStyle.Bold);
            lblTotal.ForeColor = Theme.Text;
            Controls.Add(lblTotal);

            lblStatus = new Label();
            lblStatus.AutoSize = false;
            lblStatus.Location = new Point(Theme.P(30), Theme.P(466));
            lblStatus.Size = new Size(W - Theme.P(60), Theme.P(20));
            lblStatus.Font = Theme.Small;
            lblStatus.ForeColor = Theme.TextSub;
            Controls.Add(lblStatus);

            btnClean = new FlatButton();
            btnClean.Kind = BtnKind.Danger;
            btnClean.Text = I18n.T("wechat.clean");
            btnClean.Size = new Size(Theme.P(132), Theme.P(38));
            btnClean.Location = new Point(W - Theme.P(30) - Theme.P(132), Theme.P(444));
            btnClean.Click += delegate { CleanAsync(); };
            Controls.Add(btnClean);
        }

        private void LayoutRows()
        {
            int y = 0;
            int w = listHost.ClientSize.Width;
            foreach (Row r in rows)
            {
                if (r.Ui == null) continue;
                r.Ui.Bounds = new Rectangle(0, y, Math.Max(10, w), Theme.P(52));
                y += Theme.P(52);
            }
        }

        private void RefreshPathLabel()
        {
            bool ok = !string.IsNullOrEmpty(root) && Directory.Exists(root);
            lblPath.Text = ok ? root : I18n.T("wechat.none");
            lblPath.ForeColor = ok ? Theme.Text : Theme.High;
        }

        private void Browse()
        {
            if (busy) return;
            using (var fb = new FolderBrowserDialog())
            {
                fb.Description = I18n.T("wechat.path");
                fb.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(root) && Directory.Exists(root)) fb.SelectedPath = root;
                if (fb.ShowDialog(this) != DialogResult.OK) return;
                root = fb.SelectedPath;
                Theme.WriteSetting(SettingKey, root);
                RefreshPathLabel();
                ScanAsync();
            }
        }

        // ------------------------------------------------------------ 目录识别
        /// <summary>先读用户上次选的目录，再猜常见位置。</summary>
        private static string Detect()
        {
            string saved = Theme.ReadSetting(SettingKey);
            if (LooksLikeWeChat(saved)) return saved;

            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var cands = new List<string>
            {
                Path.Combine(docs, "WeChat Files"),
                Path.Combine(docs, "xwechat_files"),
                Path.Combine(profile, "Documents", "WeChat Files"),
                @"D:\WeChat Files",
                @"E:\WeChat Files",
                @"D:\xwechat_files",
                @"E:\xwechat_files"
            };
            foreach (string c in cands)
                if (LooksLikeWeChat(c)) return c;

            // 兜底：从 Documents 下找一层像微信的目录
            try
            {
                if (Directory.Exists(docs))
                {
                    foreach (string d in Directory.GetDirectories(docs))
                    {
                        string n = Path.GetFileName(d).ToLowerInvariant();
                        if (n.Contains("wechat") && LooksLikeWeChat(d)) return d;
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool LooksLikeWeChat(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
                if (Directory.Exists(Path.Combine(dir, "FileStorage")) ||
                    Directory.Exists(Path.Combine(dir, "db_storage")) ||
                    Directory.Exists(Path.Combine(dir, "Msg"))) return true;
                foreach (string sub in Directory.GetDirectories(dir))
                {
                    if (Directory.Exists(Path.Combine(sub, "FileStorage")) ||
                        Directory.Exists(Path.Combine(sub, "db_storage")) ||
                        Directory.Exists(Path.Combine(sub, "Msg")) ||
                        Directory.Exists(Path.Combine(sub, "msg"))) return true;
                }
                // 目录名本身像微信目录也认（用户手选到 wxid_xxx 层级）
                string name = Path.GetFileName(dir.TrimEnd('\\', '/')).ToLowerInvariant();
                return name.Contains("wechat") || name.StartsWith("wxid_");
            }
            catch { return false; }
        }

        /// <summary>账号目录：root 本身就是账号目录就直接用，否则遍历下一层。</summary>
        private static List<string> AccountDirs(string root)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return list;
            if (Directory.Exists(Path.Combine(root, "FileStorage")) ||
                Directory.Exists(Path.Combine(root, "db_storage")) ||
                Directory.Exists(Path.Combine(root, "Msg")) ||
                Directory.Exists(Path.Combine(root, "msg")))
            {
                list.Add(root);
                return list;
            }
            try
            {
                foreach (string d in Directory.GetDirectories(root))
                {
                    string n = Path.GetFileName(d).ToLowerInvariant();
                    if (n == "all users" || n == "applet" || n == "backup") continue;
                    list.Add(d);
                }
            }
            catch { }
            return list;
        }

        private static readonly string[][] RelTargets = new string[][]
        {
            new[] { @"FileStorage\Cache", "缓存" },
            new[] { @"FileStorage\Video", "聊天视频" },
            new[] { @"FileStorage\Temp",  "临时文件" },
            new[] { @"cache",            "缓存" },
            new[] { @"temp",             "临时文件" }
        };

        private List<Row> ScanTargets(string root)
        {
            var found = new List<Row>();
            foreach (string acc in AccountDirs(root))
            {
                foreach (string[] rel in RelTargets)
                {
                    string p = Path.Combine(acc, rel[0]);
                    try
                    {
                        if (!Directory.Exists(p)) continue;
                        long size = Engine.DirSize(p);
                        if (size <= 0) continue;
                        string accName = Path.GetFileName(acc.TrimEnd('\\'));
                        found.Add(new Row { Path = p, Title = accName + " · " + rel[1], Kind = rel[1], Size = size });
                    }
                    catch { }
                }
            }
            // 小程序缓存（3.x 顶层 Applet）
            try
            {
                string applet = Path.Combine(root, "Applet");
                if (Directory.Exists(applet))
                {
                    long size = Engine.DirSize(applet);
                    if (size > 0) found.Add(new Row { Path = applet, Title = "小程序缓存", Kind = "小程序缓存", Size = size });
                }
            }
            catch { }
            return found;
        }

        // ------------------------------------------------------------ 扫描 / 清理
        private void ScanAsync()
        {
            if (busy) return;
            ClearRows();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                lblStatus.Text = I18n.T("wechat.none");
                lblStatus.ForeColor = Theme.High;
                UpdateTotal();
                return;
            }

            busy = true;
            btnClean.Enabled = false;
            lblStatus.Text = I18n.T("wechat.scanning");
            lblStatus.ForeColor = Theme.TextSub;
            string snapshot = root;

            Task.Factory.StartNew(delegate
            {
                List<Row> result = null;
                try { result = ScanTargets(snapshot); }
                catch { }
                Ui(delegate
                {
                    busy = false;
                    btnClean.Enabled = true;
                    ClearRows();
                    if (result != null)
                    {
                        foreach (Row r in result)
                        {
                            r.Ui = new CheckRow();
                            r.Ui.Title = r.Title;
                            r.Ui.Sub = r.Path;
                            r.Ui.Size = r.Size;
                            r.Ui.Checked = true;
                            r.Ui.Tag = r;
                            r.Ui.Click += delegate(object s, EventArgs e)
                            {
                                var row = (Row)((CheckRow)s).Tag;
                                row.Checked = !row.Checked;
                                row.Ui.Checked = row.Checked;
                                UpdateTotal();
                            };
                            listHost.Controls.Add(r.Ui);
                            rows.Add(r);
                        }
                        LayoutRows();
                    }
                    lblStatus.Text = rows.Count == 0 ? I18n.T("wechat.nothing") : "";
                    lblStatus.ForeColor = rows.Count == 0 ? Theme.TextSub : Theme.TextSub;
                    UpdateTotal();
                });
            });
        }

        private void ClearRows()
        {
            foreach (Row r in rows)
                if (r.Ui != null) { listHost.Controls.Remove(r.Ui); r.Ui.Dispose(); }
            rows.Clear();
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            long total = 0;
            foreach (Row r in rows) if (r.Checked) total += r.Size;
            lblTotal.Text = I18n.T("wechat.total") + "：" + Theme.Fmt(total);
        }

        private void CleanAsync()
        {
            if (busy) return;
            var chosen = new List<Row>();
            foreach (Row r in rows) if (r.Checked) chosen.Add(r);
            if (chosen.Count == 0) { lblStatus.Text = I18n.T("wechat.pick"); lblStatus.ForeColor = Theme.High; return; }

            busy = true;
            btnClean.Enabled = false;
            lblStatus.Text = I18n.T("wechat.clean") + "…";
            lblStatus.ForeColor = Theme.TextSub;

            Task.Factory.StartNew(delegate
            {
                long freed = 0;
                foreach (Row r in chosen)
                {
                    try
                    {
                        var log = new StringBuilder();
                        freed += Engine.ClearDir(r.Path, log);
                    }
                    catch { }
                }
                Ui(delegate
                {
                    busy = false;
                    btnClean.Enabled = true;
                    lblStatus.Text = I18n.F("wechat.done", Theme.Fmt(freed));
                    lblStatus.ForeColor = Theme.Low;
                    ScanAsync();
                });
            });
        }

        private void Ui(Action action)
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired) BeginInvoke(action);
                else action();
            }
            catch { }
        }

        public static void Show(IWin32Window owner)
        {
            using (var d = new WeChatDialog())
            {
                Form f = owner as Form;
                if (f != null)
                    ModalMask.Wrap(f, delegate { return d.ShowDialog(f); });
                else
                    d.ShowDialog(owner);
            }
        }

        /// <summary>自检用：把弹窗渲染成 PNG。</summary>
        internal static void RenderTo(string path)
        {
            try
            {
                using (var d = new WeChatDialog())
                {
                    d.Show();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(700);
                    Application.DoEvents();
                    d.Refresh();
                    Application.DoEvents();
                    using (var bmp = new Bitmap(d.Width, d.Height))
                    {
                        d.DrawToBitmap(bmp, new Rectangle(0, 0, d.Width, d.Height));
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    d.Close();
                }
                Console.WriteLine("ok");
            }
            catch (Exception ex)
            {
                Console.WriteLine("render failed: " + ex.Message);
            }
        }
    }

    /// <summary>一行缓存条目：方框勾选 + 标题 + 路径 + 大小。</summary>
    internal class CheckRow : Control
    {
        public bool Checked = true;
        public string Title = "";
        public string Sub = "";
        public long Size;
        private bool hover;

        public CheckRow()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
            Cursor = Cursors.Hand;
            Font = Theme.Base;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(hover ? Color.FromArgb(249, 251, 255) : Theme.Surface))
                g.FillRectangle(b, ClientRectangle);
            using (var pen = new Pen(Theme.Border))
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);

            int box = Theme.P(17);
            var r = new Rectangle(Theme.P(14), (Height - box) / 2, box, box);
            using (var path = Theme.Round(r, Theme.P(4)))
            {
                if (Checked)
                {
                    using (var b = new SolidBrush(Theme.Primary)) g.FillPath(b, path);
                    using (var pen = new Pen(Color.White, 1.8f))
                    {
                        g.DrawLine(pen, r.Left + Theme.P(4), r.Top + Theme.P(9), r.Left + Theme.P(7), r.Top + Theme.P(12));
                        g.DrawLine(pen, r.Left + Theme.P(7), r.Top + Theme.P(12), r.Left + Theme.P(13), r.Top + Theme.P(5));
                    }
                }
                else
                {
                    using (var pen = new Pen(Theme.Border, 1.4f)) g.DrawPath(pen, path);
                }
            }

            int tx = r.Right + Theme.P(12);
            int tw = Width - tx - Theme.P(96);
            TextRenderer.DrawText(g, Title, Theme.Base,
                new Rectangle(tx, Theme.P(7), tw, Theme.P(20)), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, Sub, Theme.Small,
                new Rectangle(tx, Theme.P(27), tw, Theme.P(18)), Theme.TextFaint,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.PathEllipsis);

            TextRenderer.DrawText(g, Theme.Fmt(Size), Theme.Small,
                new Rectangle(Width - Theme.P(92), 0, Theme.P(78), Height), Theme.TextSub,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }
    }
}
