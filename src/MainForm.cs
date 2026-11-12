using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskMate
{
    public partial class MainForm : Form
    {
        // ---- shell
        private Panel sidebar, mainHost, topbar, content, bannerHost;
        private NavItem navClean, navBig, navLog, navWechat, navContact;
        private Label lblPageTitle, lblPageSub, lblLastScan, lblAdmin, lblVersion;
        private FlowLayoutPanel rightBox;
        private FlatButton btnLang;
        private FlatButton btnUpdate;
        private FlatButton btnDonate;
        private FlatButton btnAccount;
        private UpdateInfo pendingUpdate;
        private bool checkingUpdate;
        private DiskMeter dmC, dmD;
        private int meterTop;
        private Badge chipAdmin;
        private Banner banner;

        // ---- pages
        private Panel pageClean, pageBig, pageLog, pageWechat;

        // ---- clean page: welcome
        private Panel pageWelcome;
        private FlatButton btnStart;

        // ---- clean page: scanning
        private Panel pageScanning;
        private Spinner spinner;
        private Label lblScanTitle, lblScanStep, lblScanCount;
        private ThinProgress progressScan;
        private FlatButton btnCancelScan;

        // ---- clean page: working
        private Panel pageWorking;
        private Label lblWorkStep, lblWorkCount;
        private ThinProgress progressWork;

        // ---- clean page: results
        private Panel pageResults;
        private MetricCard cardTotal, cardLow, cardMid, cardHigh;
        private SearchBox search;
        private Label lblSelected, lblEmpty;
        private FlatButton btnSelectLow, btnSelectNone, btnCleanNow, btnRescan;
        private Panel listHost;
        private ItemList lvItems;
        private CardPanel detailCard;
        private Panel detailScroll;
        private Panel detailScrollHost;
        private FlatScrollbar detailScrollBar;
        private Label lblDetName, lblDetSize, lblDetImpact, lblDetAdvice, lblDetPath;
        private Badge badgeDetRisk;
        private FlatButton btnDetToggle, btnOpenFolder;

        // ---- big files
        private ListView lvBig;
        private FlatButton btnBigScan, btnBigReveal, btnBigDelete;
        private Label lblBigHint;
        private Label lblBigEmpty;
        private ComboBox cmbThreshold, cmbDrive;
        private readonly List<DriveInfo> bigDrives = new List<DriveInfo>();

        // ---- log
        private RichTextBox txtLog;
        private string logDir, logFile;

        // ---- state
        private List<CleanItem> all = new List<CleanItem>();
        private int filterRisk = -1;
        private CleanItem current;
        private int activePage;
        private int phase = -1;
        private bool busy;
        private volatile bool scanCancel;
        private int scanTotal;
        private DateTime? lastScanAt;

        private static readonly long[] BigThresholds = { 100L << 20, 200L << 20, 500L << 20, 1024L << 20 };

        public MainForm()
        {
            logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate", "logs");
            try { Directory.CreateDirectory(logDir); } catch { }
            logFile = Path.Combine(logDir, "diskmate-" + DateTime.Now.ToString("yyyyMMdd") + ".log");

            I18n.LoadSaved();
            I18n.Changed += delegate { Retext(); };

            Text = I18n.T("app.title");
            Font = Theme.Base;
            BackColor = Theme.Bg;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }
            Size winSize = Theme.WindowSize(1120, 760);
            ClientSize = winSize;
            MinimumSize = new Size(Math.Min(Theme.P(1040), winSize.Width), Math.Min(Theme.P(700), winSize.Height));
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.None;   // 自己按 Theme.S 缩放，避免 WinForms 二次放大

            BuildShell();
            BuildCleanPage();
            BuildBigPage();
            BuildLogPage();
            BuildWeChatPage();
            RefreshDrives();

            ShowPage(0);
            ShowPhase(-1);
            StartSilentUpdateCheck();
            StartAnnouncementCheck();
            BootstrapAccount();
            Log("程序启动  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Log("管理员权限：" + (IsAdmin() ? "是" : "否"));
            CheckInterruptedCleanup();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                if (activePage == 1) StartBigScan();
                else if (activePage == 0) BeginScan();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.F)
            {
                if (activePage == 0 && phase == 1) { search.FocusInput(); e.Handled = true; }
            }
            base.OnKeyDown(e);
        }

        /// <summary>上次清理回收站时进程被异常终止的话，在日志里留一条，便于用户回传</summary>
        private void CheckInterruptedCleanup()
        {
            try
            {
                string when = Engine.TakeRecycleCrashMark();
                if (when == null) return;
                Log("⚠ 上次清理回收站时程序异常退出（" + when + "）");
                Log("  可能是安全软件/网盘的 shell 扩展把进程带走了；已改为先直接删除、再交给系统收尾");
                Log("  如仍反复出现，请把本页日志发给开发者（日志目录：" + logDir + "）");
            }
            catch { }
        }

        private static bool IsAdmin()
        {
            try { return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator); }
            catch { return false; }
        }

        // ============================================================ shell
        private void BuildShell()
        {
            sidebar = new Panel();
            sidebar.Dock = DockStyle.Left;
            sidebar.Width = Theme.P(216);
            sidebar.BackColor = Theme.Sidebar;
            sidebar.Paint += delegate(object s, PaintEventArgs e) { PaintSidebar(e.Graphics); };
            sidebar.Resize += delegate { LayoutSidebar(); };
            Controls.Add(sidebar);

            navClean = MakeNav(I18n.T("nav.clean"), "scan");
            navBig = MakeNav(I18n.T("nav.big"), "files");
            navLog = MakeNav(I18n.T("nav.log"), "list");
            navClean.Tag = "nav.clean";
            navBig.Tag = "nav.big";
            navLog.Tag = "nav.log";

            navClean.Click += delegate { ShowPage(0); };
            navBig.Click += delegate { ShowPage(1); };
            navLog.Click += delegate { ShowPage(2); };

            // 微信专清：和清理体检/大文件/日志一样占一整行，点进独立的清理页面
            navWechat = MakeNav(I18n.T("wechat.btn"), "broom");
            navWechat.Tag = "wechat.btn";
            navWechat.Click += delegate
            {
                if (!RequireLogin(FeatureGate.WeChat)) return;
                ShowPage(3);
            };

            // 问题反馈：和清理体检/大文件/日志一样占一整行，点击弹出二维码
            navContact = MakeNav(I18n.T("contact.btn"), "chat");
            navContact.Click += delegate { ContactDialog.Show(this); };

            btnLang = new FlatButton();
            btnLang.Kind = BtnKind.Secondary;
            btnLang.Size = new Size(Theme.P(184), Theme.P(34));
            btnLang.Font = Theme.Small;
            btnLang.Text = LangText();
            btnLang.Click += delegate { ShowLangMenu(); };
            sidebar.Controls.Add(btnLang);

            btnUpdate = new FlatButton();
            btnUpdate.Kind = BtnKind.Ghost;
            btnUpdate.Size = new Size(Theme.P(184), Theme.P(28));
            btnUpdate.Font = Theme.Small;
            btnUpdate.Text = I18n.F("update.btn", AppInfo.Version);
            btnUpdate.Click += delegate
            {
                if (pendingUpdate != null) { UpdateDialog.Show(this, pendingUpdate); return; }
                CheckUpdate(false);
            };
            sidebar.Controls.Add(btnUpdate);

            btnDonate = new FlatButton();
            btnDonate.Kind = BtnKind.Ghost;
            btnDonate.Size = new Size(Theme.P(184), Theme.P(26));
            btnDonate.Font = Theme.Small;
            btnDonate.Text = I18n.T("donate.btn");
            btnDonate.Click += delegate { DonateDialog.Show(this); };
            sidebar.Controls.Add(btnDonate);

            btnAccount = new FlatButton();
            btnAccount.Kind = BtnKind.Ghost;
            btnAccount.Size = new Size(Theme.P(184), Theme.P(26));
            btnAccount.Font = Theme.Small;
            btnAccount.Text = "登录 / 注册";
            btnAccount.Click += delegate { OpenAccount(); };
            sidebar.Controls.Add(btnAccount);

            dmC = new DiskMeter(); dmC.Letter = 'C';
            dmD = new DiskMeter(); dmD.Letter = 'D';
            sidebar.Controls.Add(dmC);
            sidebar.Controls.Add(dmD);

            lblAdmin = new Label();
            lblAdmin.AutoSize = false;
            lblAdmin.Height = Theme.P(20);
            lblAdmin.ForeColor = Theme.TextSub;
            lblAdmin.Font = Theme.Small;
            lblAdmin.Text = IsAdmin() ? I18n.T("admin.yes") : I18n.T("admin.no");
            sidebar.Controls.Add(lblAdmin);

            lblVersion = new Label();
            lblVersion.AutoSize = false;
            lblVersion.Height = Theme.P(16);
            lblVersion.ForeColor = Theme.TextFaint;
            lblVersion.Font = Theme.Small;
            lblVersion.Text = "DiskMate " + AppInfo.Version;
            sidebar.Controls.Add(lblVersion);

            mainHost = new Panel();
            mainHost.Dock = DockStyle.Fill;
            mainHost.BackColor = Theme.Bg;
            Controls.Add(mainHost);
            mainHost.BringToFront();

            topbar = new Panel();
            topbar.BackColor = Theme.Bg;
            mainHost.Controls.Add(topbar);

            lblPageTitle = new Label();
            lblPageTitle.AutoSize = false;
            lblPageTitle.Location = new Point(Theme.P(26), Theme.P(18));
            lblPageTitle.Size = new Size(Theme.P(520), Theme.P(32));
            lblPageTitle.Font = Theme.H1;
            lblPageTitle.ForeColor = Theme.Text;
            lblPageTitle.Text = I18n.T("nav.clean");
            topbar.Controls.Add(lblPageTitle);

            lblPageSub = new Label();
            lblPageSub.AutoSize = false;
            lblPageSub.Location = new Point(Theme.P(28), Theme.P(50));
            lblPageSub.Size = new Size(Theme.P(620), Theme.P(20));
            lblPageSub.Font = Theme.Small;
            lblPageSub.ForeColor = Theme.TextSub;
            lblPageSub.Text = "";
            topbar.Controls.Add(lblPageSub);

            rightBox = new FlowLayoutPanel();            rightBox.Dock = DockStyle.Right;
            rightBox.Width = Theme.P(340);
            rightBox.FlowDirection = FlowDirection.RightToLeft;
            rightBox.WrapContents = false;
            rightBox.BackColor = Theme.Bg;
            rightBox.Padding = new Padding(Theme.P(0), Theme.P(28), Theme.P(26), Theme.P(0));
            topbar.Controls.Add(rightBox);

            chipAdmin = new Badge();
            chipAdmin.Size = new Size(Theme.P(88), Theme.P(24));
            chipAdmin.Margin = new Padding(Theme.P(10), Theme.P(0), Theme.P(0), Theme.P(0));
            chipAdmin.Font = Theme.Small;
            rightBox.Controls.Add(chipAdmin);

            btnRescan = new FlatButton();
            btnRescan.Kind = BtnKind.Secondary;
            Txt(btnRescan, "btn.rescan");
            btnRescan.Icon = "refresh";
            btnRescan.Size = new Size(Theme.P(118), Theme.P(34));
            btnRescan.Margin = new Padding(Theme.P(10), Theme.P(0), Theme.P(0), Theme.P(0));
            btnRescan.Visible = false;
            btnRescan.Click += delegate { BeginScan(); };
            rightBox.Controls.Add(btnRescan);

            lblLastScan = new Label();
            lblLastScan.AutoSize = true;
            lblLastScan.ForeColor = Theme.TextFaint;
            lblLastScan.Font = Theme.Small;
            lblLastScan.Margin = new Padding(Theme.P(0), Theme.P(4), Theme.P(0), Theme.P(0));
            Txt(lblLastScan, "lastScan.none");
            rightBox.Controls.Add(lblLastScan);

            bannerHost = new Panel();
            bannerHost.Height = Theme.P(0);
            bannerHost.Visible = false;
            bannerHost.BackColor = Theme.Bg;
            bannerHost.Padding = new Padding(Theme.P(24), Theme.P(0), Theme.P(24), Theme.P(12));
            mainHost.Controls.Add(bannerHost);

            banner = new Banner();
            banner.Dock = DockStyle.Fill;
            banner.Height = Theme.P(48);
            banner.Dismissed += delegate { bannerHost.Visible = false; LayoutMain(); };
            bannerHost.Controls.Add(banner);

            content = new Panel();
            content.BackColor = Theme.Bg;
            content.Padding = new Padding(Theme.P(24), Theme.P(0), Theme.P(24), Theme.P(16));
            mainHost.Controls.Add(content);

            pageClean = new Panel(); pageClean.Dock = DockStyle.Fill; pageClean.BackColor = Theme.Bg; pageClean.Visible = false; content.Controls.Add(pageClean);
            pageBig = new Panel(); pageBig.Dock = DockStyle.Fill; pageBig.BackColor = Theme.Bg; pageBig.Visible = false; content.Controls.Add(pageBig);
            pageLog = new Panel(); pageLog.Dock = DockStyle.Fill; pageLog.BackColor = Theme.Bg; pageLog.Visible = false; content.Controls.Add(pageLog);
            pageWechat = new Panel(); pageWechat.Dock = DockStyle.Fill; pageWechat.BackColor = Theme.Bg; pageWechat.Visible = false; content.Controls.Add(pageWechat);

            LayoutSidebar();
            mainHost.Resize += delegate { LayoutMain(); };
            LayoutMain();
            UpdateAdminChip();
        }

        private void LayoutMain()
        {
            if (topbar == null || content == null || mainHost == null) return;
            int w = mainHost.Width;
            int h = mainHost.Height;
            int y = Theme.P(0);
            topbar.SetBounds(Theme.P(0), y, w, Theme.P(78)); y += Theme.P(78);
            if (rightBox != null && lblPageSub != null)
            {
                int subW = Math.Max(Theme.P(160), w - rightBox.Width - Theme.P(56));
                lblPageSub.Width = subW;
                lblPageTitle.Width = subW;
            }
            int bh = bannerHost.Visible ? Theme.P(60) : 0;
            bannerHost.SetBounds(Theme.P(0), y, w, bh); y += bh;
            content.SetBounds(Theme.P(0), y, w, Math.Max(Theme.P(0), h - y));
        }

        private NavItem MakeNav(string text, string icon)
        {
            var b = new NavItem();
            b.Text = text;
            b.Icon = icon;
            b.Width = sidebar.Width;
            sidebar.Controls.Add(b);
            return b;
        }

        private void UpdateAdminChip()
        {
            bool admin = IsAdmin();
            chipAdmin.Text = admin ? I18n.T("admin.chipYes") : I18n.T("admin.chipNo");
            chipAdmin.Accent = admin ? Theme.Low : Theme.Mid;
            chipAdmin.Soft = admin ? Theme.LowSoft : Theme.MidSoft;
            chipAdmin.Invalidate();
        }

        private void LayoutSidebar()
        {
            if (sidebar == null || navClean == null || navBig == null || navLog == null || navWechat == null || navContact == null) return;
            if (dmC == null || dmD == null || lblAdmin == null || lblVersion == null) return;

            int y = Theme.P(98);
            // 显示顺序：清理体检 → 微信专清 → 大文件 → 日志 → 问题反馈（页面索引不变）
            NavItem[] navs = { navClean, navWechat, navBig, navLog, navContact };
            foreach (NavItem n in navs)
            {
                n.Location = new Point(Theme.P(0), y);
                n.Width = sidebar.Width;
                y += Theme.P(46);
            }

            if (btnLang != null)
            {
                btnLang.Width = sidebar.Width - Theme.P(36);
                btnLang.Location = new Point(Theme.P(18), y + Theme.P(6));
            }
            if (btnUpdate != null)
            {
                btnUpdate.Width = sidebar.Width - Theme.P(36);
                btnUpdate.Location = new Point(Theme.P(18), y + Theme.P(44));
            }
            if (btnDonate != null)
            {
                btnDonate.Width = sidebar.Width - Theme.P(36);
                btnDonate.Location = new Point(Theme.P(18), y + Theme.P(78));
            }

            // 底部区：版本 → 权限 → D 盘 → C 盘 → 登录注册按钮，从下往上依次堆叠，间距与控件高度都随 DPI 缩放
            int meterH = DiskMeter.PreferredHeight;

            int bottom = sidebar.Height - Theme.P(10);
            lblVersion.Location = new Point(Theme.P(22), bottom - lblVersion.Height);
            lblVersion.Width = sidebar.Width - Theme.P(40);

            int adminY = lblVersion.Top - Theme.P(2) - lblAdmin.Height;
            lblAdmin.Location = new Point(Theme.P(22), adminY);
            lblAdmin.Width = sidebar.Width - Theme.P(40);

            int dmDY = adminY - Theme.P(8) - meterH;
            int dmCY = dmDY - Theme.P(4) - meterH;

            // 垂直空间不足时不显示磁盘条（例如小屏 + 200% 缩放），避免和上方按钮叠在一起
            int accSpace = btnAccount != null ? btnAccount.Height + Theme.P(12) : 0;
            int limit = (btnDonate != null ? btnDonate.Bottom : Theme.P(346)) + Theme.P(10) + accSpace;
            bool roomForMeters = dmCY >= limit;
            dmC.Visible = roomForMeters;
            dmD.Visible = roomForMeters;
            meterTop = roomForMeters ? dmCY : (adminY - Theme.P(8) - Theme.P(14));

            dmD.Location = new Point(Theme.P(16), dmDY);
            dmD.Width = sidebar.Width - Theme.P(32);
            dmD.Height = meterH;
            dmC.Location = new Point(Theme.P(16), dmCY);
            dmC.Width = sidebar.Width - Theme.P(32);
            dmC.Height = meterH;

            // 登录注册按钮放在盘容量展示（C 盘条）的上面
            if (btnAccount != null)
            {
                btnAccount.Width = sidebar.Width - Theme.P(36);
                int accTop = (roomForMeters ? dmCY : meterTop) - Theme.P(12) - btnAccount.Height;
                int minTop = (btnDonate != null ? btnDonate.Bottom : Theme.P(346)) + Theme.P(4);
                if (accTop < minTop) accTop = minTop;
                btnAccount.Location = new Point(Theme.P(18), accTop);
            }
            sidebar.Invalidate();
        }

        private void PaintSidebar(Graphics g)
        {
            if (dmC == null) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Theme.Border))
                g.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);

            var logo = new Rectangle(Theme.P(20), Theme.P(24), Theme.P(34), Theme.P(34));
            Logo.Draw(g, new RectangleF(logo.X, logo.Y, logo.Width, logo.Height), Theme.P(10));

            TextRenderer.DrawText(g, "DiskMate", Theme.F(12.5f, FontStyle.Bold),
                new Rectangle(Theme.P(62), Theme.P(26), Theme.P(140), Theme.P(22)), Theme.Text, Theme.LeftMid);
            TextRenderer.DrawText(g, I18n.T("app.tagline"), Theme.Small, new Rectangle(Theme.P(63), Theme.P(46), Theme.P(146), Theme.P(18)), Theme.TextFaint, Theme.LeftMid);

            using (var pen = new Pen(Theme.Border))
                g.DrawLine(pen, Theme.P(18), meterTop, sidebar.Width - Theme.P(18), meterTop);
        }

        // ============================================================ 账号
        private void BootstrapAccount()
        {
            Api.LoadConfig();
            Session.Load();
            FeatureGate.Load();   // 先读本地缓存立刻可用，再后台拉服务端最新开关
            UpdateAccountButton();
            Log("账号服务：" + Api.BaseUrl + (Session.IsLoggedIn ? "（已登录 " + Session.Email + "）" : "（未登录）"));
            Log("功能开关：" + FeatureGate.Describe());
            if (!Session.IsLoggedIn) return;

            Task.Factory.StartNew(delegate
            {
                ApiResult r = Session.AuthedGet("/api/auth/me");
                if (!r.Ok) Session.Clear();
                Ui(delegate { UpdateAccountButton(); });
            });
        }

        private void OpenAccount()
        {
            AccountDialog.Show(this, delegate { UpdateAccountButton(); });
        }

        private void UpdateAccountButton()
        {
            if (btnAccount == null || btnAccount.IsDisposed) return;
            string text = Session.IsLoggedIn ? Session.Email : "登录 / 注册";
            if (text.Length > 26) text = text.Substring(0, 25) + "…";
            if (btnAccount.Text != text)
            {
                btnAccount.Text = text;
                btnAccount.Invalidate();
            }
        }

        /// <summary>自检用：切到大文件页并填几行假数据（不真扫描）</summary>
        public void TestShowBig()
        {
            ShowPage(1);
            if (lvBig == null) return;
            string[] paths =
            {
                @"C:\Windows\System32\DriverStore\FileRepository\nvhm.inf_amd64\display.driver\nvldumdx.dll",
                @"D:\Users\hjs2125\AppData\Local\Temp\a-very-long-temporary-file-name-used-for-layout-checking.vhdx",
                @"C:\Program Files\Some App\resources\big-asset-bundle.pak",
                @"C:\hiberfil.sys"
            };
            long[] sizes = { 3L << 30, 1380000000, 640000000, 210000000 };
            lvBig.BeginUpdate();
            try
            {
                lvBig.Items.Clear();
                for (int i = 0; i < paths.Length; i++)
                    lvBig.Items.Add(new ListViewItem(new[] { Theme.Fmt(sizes[i]), paths[i] }));
            }
            finally { lvBig.EndUpdate(); }
            if (lblBigEmpty != null) lblBigEmpty.Visible = lvBig.Items.Count == 0;
            if (lvBig.Items.Count > 0) { lvBig.Items[0].Selected = true; lvBig.Items[0].Focused = true; }
            UpdateBigButtons();
            if (lblBigHint != null) lblBigHint.Text = I18n.F("big.done", 123456, lvBig.Items.Count, Theme.Fmt(200L << 20));
            Application.DoEvents();
        }

        private void ShowPage(int idx)
        {
            activePage = idx;
            pageClean.Visible = idx == 0;
            pageBig.Visible = idx == 1;
            pageLog.Visible = idx == 2;
            pageWechat.Visible = idx == 3;

            navClean.Active = idx == 0;
            navBig.Active = idx == 1;
            navLog.Active = idx == 2;
            navWechat.Active = idx == 3;
            navClean.Invalidate(); navBig.Invalidate(); navLog.Invalidate(); navWechat.Invalidate();

            string titleKey, subKey;
            switch (idx)
            {
                case 0:
                    titleKey = "nav.clean";
                    subKey = "page.clean.sub";
                    break;
                case 1:
                    titleKey = "nav.big";
                    subKey = "page.big.sub";
                    break;
                case 3:
                    titleKey = "wechat.btn";
                    subKey = "page.wechat.sub";
                    break;
                default:
                    titleKey = "nav.log";
                    subKey = "page.log.sub";
                    break;
            }
            lblPageTitle.Text = I18n.T(titleKey);
            lblPageSub.Text = I18n.T(subKey);
            RefreshDrives();
        }

        /// <summary>-1 欢迎 / 0 扫描中 / 1 结果 / 3 清理中</summary>
        private void ShowPhase(int p)
        {
            phase = p;
            pageWelcome.Visible = p == -1;
            pageScanning.Visible = p == 0;
            pageResults.Visible = p == 1;
            pageWorking.Visible = p == 3;
            btnRescan.Visible = p == 1;
        }

        // ============================================================ i18n helpers
        /// <summary>给控件挂上 i18n key，同时按当前语言写入文本；切换语言时由 ApplyTags 重放。</summary>
        private static void Txt(Control c, string key, params object[] args)
        {
            if (c == null) return;
            c.Tag = key;
            string v = args.Length == 0 ? I18n.T(key) : I18n.F(key, args);
            var card = c as MetricCard;
            if (card != null) { card.Title = v; return; }
            var box = c as SearchBox;
            if (box != null) { box.Placeholder = v; box.Invalidate(); return; }
            c.Text = v;
        }

        private static void ApplyTags(Control root)
        {
            foreach (Control c in root.Controls)
            {
                string key = c.Tag as string;
                if (!string.IsNullOrEmpty(key))
                {
                    string v = I18n.T(key);
                    var card = c as MetricCard;
                    var box = c as SearchBox;
                    if (card != null) card.Title = v;
                    else if (box != null) { box.Placeholder = v; box.Invalidate(); }
                    else c.Text = v;
                }
                ApplyTags(c);
            }
        }

        private string LangText()
        {
            return I18n.T("lang.label") + " · " + I18n.NativeName(I18n.Lang);
        }

        private void ShowLangMenu()
        {
            var menu = new ContextMenuStrip();
            menu.RenderMode = ToolStripRenderMode.System;
            menu.Font = Theme.Base;
            string[] codes = I18n.Codes;
            foreach (string code in codes)
            {
                string c2 = code;
                var item = new ToolStripMenuItem(I18n.NativeName(code));
                item.Checked = string.Equals(code, I18n.Lang, StringComparison.OrdinalIgnoreCase);
                item.Click += delegate
                {
                    if (string.Equals(c2, I18n.Lang, StringComparison.OrdinalIgnoreCase)) return;
                    I18n.Set(c2);
                    ShowBanner(BannerKind.Info, I18n.T("toast.lang"), 4);
                };
                menu.Items.Add(item);
            }
            menu.Show(btnLang, new Point(Theme.P(0), btnLang.Height + Theme.P(4)));
        }

        /// <summary>语言切换后重放所有文本、列表与详情。</summary>
        private void Retext()
        {
            Text = I18n.T("app.title");
            ApplyTags(this);
            if (navClean != null) navClean.Invalidate();
            if (navBig != null) navBig.Invalidate();
            if (navLog != null) navLog.Invalidate();
            if (navContact != null) { navContact.Text = I18n.T("contact.btn"); navContact.Invalidate(); }
            if (navWechat != null) { navWechat.Text = I18n.T("wechat.btn"); navWechat.Invalidate(); }
            if (btnLang != null) btnLang.Text = LangText();
            UpdateButtonText();
            if (btnDonate != null) btnDonate.Text = I18n.T("donate.btn");
            if (lblAdmin != null) lblAdmin.Text = IsAdmin() ? I18n.T("admin.yes") : I18n.T("admin.no");
            if (btnRescan != null) btnRescan.Text = I18n.T("btn.rescan");
            if (lblEmpty != null) lblEmpty.Text = all.Count == 0 ? I18n.T("empty.none") : I18n.T("empty.filter");
            if (btnSelectLow != null) btnSelectLow.Text = I18n.T("btn.selectLow");
            if (btnSelectNone != null) btnSelectNone.Text = I18n.T("btn.selectNone");
            if (btnBigScan != null) btnBigScan.Text = I18n.T("big.btn");
            if (lblBigHint != null && lvBig != null && lvBig.Items.Count == 0) lblBigHint.Text = I18n.T("big.idle");
            if (cmbThreshold != null) FillThresholds();
            if (lblBigEmpty != null && lblBigEmpty.Visible) lblBigEmpty.Text = I18n.T("big.idle");
            UpdateAdminChip();
            UpdateLastScanText();

            if (lvItems != null)
            {
                lvItems.Columns[0].Text = I18n.T("col.name");
                lvItems.Columns[1].Text = I18n.T("col.cat");
                lvItems.Columns[2].Text = I18n.T("col.size");
                lvItems.Columns[3].Text = I18n.T("col.risk");
            }
            if (lvBig != null)
            {
                lvBig.Columns[0].Text = I18n.T("big.colsize");
                lvBig.Columns[1].Text = I18n.T("big.colpath");
            }
            foreach (CleanItem it in all) LocalizeItem(it);

            ShowPage(activePage);
            if (phase == 1)
            {
                BindItems();
                RefreshStats();
                UpdateSelection();
                UpdateDetail();
            }
            else if (phase == -1 && lblBigHint != null)
            {
                // 大文件页的空状态提示
            }
            sidebar.Invalidate();
            Invalidate(true);
        }

        // ============================================================ updates
        /// <summary>启动后拉一次公告；和上次已读的 id 相同就不再打扰</summary>
        private void StartAnnouncementCheck()
        {
            Task.Factory.StartNew(delegate
            {
                try { System.Threading.Thread.Sleep(1800); }
                catch { }
                Announcement a;
                try { a = Announcement.Fetch(); }
                catch { return; }
                if (a == null) return;
                if (Announcement.ReadSeenId() == a.Id) return;
                Ui(delegate
                {
                    try
                    {
                        AnnouncementDialog.ShowAnnouncement(this, a);
                        Announcement.MarkSeen(a.Id);   // 点 ✕ 也算已读，避免每次启动都弹
                    }
                    catch { }
                });
            });
        }

        /// <summary>启动后 2.5 秒静默查一次，有新版本只在提示条里说一声，不打扰用户。</summary>
        private void StartSilentUpdateCheck()
        {
            if (UpdateService.Disabled) return;
            Task.Factory.StartNew(delegate
            {
                try { System.Threading.Thread.Sleep(2500); }
                catch { }
                string err;
                UpdateInfo info = UpdateService.Check(out err);
                if (info == null) return;
                Ui(delegate
                {
                    pendingUpdate = info;
                    UpdateButtonText();
                    ShowBanner(BannerKind.Info, I18n.F("update.banner", info.Version), 8);
                });
            });
        }

        /// <summary>手动检查：结果用提示条反馈，有新版本直接开更新弹窗。</summary>
        private void CheckUpdate(bool silent)
        {
            if (checkingUpdate) return;
            checkingUpdate = true;
            if (btnUpdate != null)
            {
                btnUpdate.Enabled = false;
                btnUpdate.Text = I18n.T("update.checking");
                btnUpdate.Invalidate();
            }
            Task.Factory.StartNew(delegate
            {
                string err;
                UpdateInfo info = UpdateService.Check(out err);
                Ui(delegate
                {
                    checkingUpdate = false;
                    if (btnUpdate != null) btnUpdate.Enabled = true;
                    UpdateButtonText();
                    if (err != null)
                    {
                        if (!silent)
                        {
                            string msg = err == "not configured" ? I18n.T("update.notConfigured") : I18n.F("update.failed", err);
                            ShowBanner(BannerKind.Warn, msg, 8);
                        }
                        return;
                    }
                    if (info == null)
                    {
                        if (!silent) ShowBanner(BannerKind.Success, I18n.F("update.uptodate", AppInfo.Version), 5);
                        return;
                    }
                    pendingUpdate = info;
                    UpdateButtonText();
                    UpdateDialog.Show(this, info);
                });
            });
        }

        private void UpdateButtonText()
        {
            if (btnUpdate == null) return;
            btnUpdate.Text = pendingUpdate != null
                ? I18n.F("update.availableBtn", pendingUpdate.Version)
                : I18n.F("update.btn", AppInfo.Version);
            btnUpdate.Kind = pendingUpdate != null ? BtnKind.Primary : BtnKind.Ghost;
            btnUpdate.Invalidate();
        }

        private void UpdateLastScanText()
        {            if (lblLastScan == null) return;
            if (lastScanAt.HasValue)
                lblLastScan.Text = I18n.F("lastScan.prefix", lastScanAt.Value.ToString("HH:mm"));
            else
                lblLastScan.Text = I18n.T("lastScan.none");
        }

        private void ShowBanner(BannerKind kind, string text, int seconds)
        {
            banner.Show(kind, text, seconds);
            bannerHost.Visible = true;
            LayoutMain();
        }

        // ============================================================ ui thread helpers
        private void Ui(Action action)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((Action)delegate
                {
                    if (!IsDisposed) action();
                });
            }
            catch { }
        }

        private void Log(string line)
        {
            string s = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line;
            if (txtLog != null && txtLog.TextLength < 400000) txtLog.AppendText(s + Environment.NewLine);
            try { File.AppendAllText(logFile, s + Environment.NewLine, Encoding.UTF8); } catch { }
        }

        private void RefreshDrives()
        {
            var drives = new List<DriveInfo>();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                try { if (d.IsReady && d.DriveType == DriveType.Fixed) drives.Add(d); } catch { }
            }
            for (int i = 0; i < 2; i++)
            {
                DiskMeter m = i == 0 ? dmC : dmD;
                if (i < drives.Count)
                {
                    m.Total = drives[i].TotalSize;
                    m.Free = drives[i].AvailableFreeSpace;
                }
                else
                {
                    m.Total = 0; m.Free = 0;
                }
                m.Invalidate();
            }
        }

        // ============================================================ 登录门禁
        /// <summary>
        /// 该功能是否必须登录由服务端下发（后台「功能开关」可随时改）；
        /// 需要登录而当前未登录时，直接弹登录框，登录成功即继续。
        /// </summary>
        private bool RequireLogin(string feature)
        {
            if (!FeatureGate.LoginRequired(feature)) return true;   // 后台把它设成免登录
            if (Session.IsLoggedIn) return true;
            AccountDialog.Show(this, delegate { UpdateAccountButton(); });
            if (Session.IsLoggedIn) return true;
            ShowBanner(BannerKind.Info, I18n.T("login.required"), 6);
            return false;
        }

        // ============================================================ scanning
        private void BeginScan()
        {
            if (busy) return;
            if (!RequireLogin(FeatureGate.Clean)) return;
            ShowPage(0);
            ShowPhase(0);
            busy = true;
            scanCancel = false;
            lblScanTitle.Text = I18n.T("scan.title");
            lblScanStep.Text = I18n.T("scan.preparing");
            lblScanCount.Text = "";
            progressScan.Value = 0;
            spinner.Start();
            Log("开始扫描");

            List<ScanStep> steps = BuildScanSteps();
            scanTotal = steps.Count;

            Task.Factory.StartNew(delegate
            {
                var result = new List<CleanItem>();
                string err = null;
                bool canceled = false;
                try
                {
                    for (int i = 0; i < steps.Count; i++)
                    {
                        if (scanCancel) { canceled = true; break; }
                        ScanStep step = steps[i];
                        int idx = i;
                        int total = steps.Count;
                        string label = step.Label;
                        Ui(delegate
                        {
                            lblScanStep.Text = I18n.F("scan.current", label);
                            lblScanCount.Text = (idx + 1) + " / " + total;
                            progressScan.Value = (double)idx / Math.Max(1, total);
                        });
                        step.Run(result);
                    }
                }
                catch (OperationCanceledException) { canceled = true; }
                catch (Exception ex) { err = ex.Message; }

                Ui(delegate
                {
                    spinner.Stop();
                    busy = false;
                    if (canceled)
                    {
                        Log("扫描已取消");
                        ShowPhase(all.Count > 0 ? 1 : -1);
                        ShowBanner(BannerKind.Warn, I18n.T("scan.cancelled"), 6);
                        return;
                    }
                    if (err != null)
                    {
                        Log("扫描失败：" + err);
                        ShowPhase(all.Count > 0 ? 1 : -1);
                        ShowBanner(BannerKind.Error, I18n.F("scan.failed", err), 9);
                        return;
                    }

                    AssignRisk(result);
                    result.Sort(delegate(CleanItem a, CleanItem b) { return b.Size.CompareTo(a.Size); });
                    all = result;
                    filterRisk = -1;
                    search.Clear();
                    cardLow.Selected = false; cardMid.Selected = false; cardHigh.Selected = false;
                    cardTotal.Selected = false;
                    lastScanAt = DateTime.Now; UpdateLastScanText();
                    BindItems();
                    RefreshStats();
                    ShowPhase(1);
                    Log(string.Format("扫描完成：{0} 项，合计 {1}", all.Count, Theme.Fmt(TotalSize())));
                    ShowBanner(BannerKind.Success,
                        I18n.F("scan.done", all.Count, Theme.Fmt(TotalSize())), 9);
                    Session.PingActivity("scan");   // 用了一次扫描，记一次活跃
                });
            }, TaskCreationOptions.LongRunning);
        }

        private void CancelScan()
        {
            scanCancel = true;
            lblScanStep.Text = I18n.T("scan.cancelling");
            btnCancelScan.Enabled = false;
        }

        private void AddDir(List<CleanItem> list, string key, string name, string cat, string[] dirs, string note)
        {
            if (dirs == null) return;
            long total = 0, blockedBytes = 0;
            int blocked = 0;
            var valid = new List<string>();
            foreach (string d in dirs)
            {
                if (scanCancel) throw new OperationCanceledException();
                if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                // 只统计"现在就能删"的大小，被占用的文件不计入（否则会出现显示 100 MB、清理释放 0）
                Engine.FreeStat fs = Engine.Measurable(d, delegate { return scanCancel; });
                if (fs.Free <= 0)
                {
                    blockedBytes += fs.BlockedBytes;
                    blocked += fs.Blocked;
                    continue;
                }
                total += fs.Free;
                blockedBytes += fs.BlockedBytes;
                blocked += fs.Blocked;
                valid.Add(d);
            }
            if (valid.Count == 0) return;
            if (blocked > 0)
                note = note + "；另有 " + Theme.Fmt(blockedBytes) + " 被占用(" + blocked + " 个文件)无法删除，未计入";
            var it = new CleanItem
            {
                Key = key,
                Name = name,
                Category = cat,
                Targets = valid.ToArray(),
                Size = total,
                Note = note,
                Kind = "dir"
            };
            it.CatKey = CatKeyOf(cat);
            it.Name = I18n.T("item." + key + ".name", name);
            it.Category = I18n.T("cat." + it.CatKey, cat);
            list.Add(it);
        }

        /// <summary>把中文类别名映射成语言包 key，已经是 key 的直接返回。</summary>
        private static string CatKeyOf(string cat)
        {
            switch (cat)
            {
                case "临时": return "temp";
                case "开发": return "dev";
                case "应用": return "app";
                case "浏览器": return "browser";
                case "系统": return "sys";
            }
            return cat;
        }

        /// <summary>按语言包重新设置项目名称、类别、后果与建议。</summary>
        private static void LocalizeItem(CleanItem it)
        {
            if (it == null || string.IsNullOrEmpty(it.Key)) return;
            if (it.Key == "drivers") it.Name = I18n.F("item.drivers.name", it.Count);
            else it.Name = I18n.T("item." + it.Key + ".name", it.Name);
            it.Category = I18n.T("cat." + it.CatKey, it.Category);
            it.Impact = I18n.T("item." + it.Key + ".impact", it.Impact);
            it.Advice = I18n.T("item." + it.Key + ".advice", it.Advice);
        }

        private static string[] CollectBrowserCache(string userData)
        {
            var found = new List<string>();
            if (!Directory.Exists(userData)) return found.ToArray();
            try
            {
                foreach (string profileDir in Directory.EnumerateDirectories(userData))
                {
                    string leaf = Path.GetFileName(profileDir);
                    if (leaf == "Crashpad") continue;
                    string[] rel = { "Cache", "Code Cache", "GPUCache", Path.Combine("Service Worker", "CacheStorage") };
                    foreach (string r in rel)
                    {
                        string p = Path.Combine(profileDir, r);
                        if (Directory.Exists(p)) found.Add(p);
                    }
                }
            }
            catch { }
            return found.ToArray();
        }

        // ============================================================ cleaning
        private void StartClean()
        {
            var chosen = new List<CleanItem>();
            long total = 0;
            int mid = 0, high = 0;
            foreach (CleanItem it in all)
            {
                if (!it.Checked) continue;
                chosen.Add(it);
                total += it.Size;
                if (it.Risk == 1) mid++;
                if (it.Risk == 2) high++;
            }
            if (chosen.Count == 0)
            {
                ShowBanner(BannerKind.Warn, I18n.T("toast.pickNone"), 6);
                return;
            }
            if (!ConfirmDialog.ShowClean(this, chosen, total, mid, high)) return;

            busy = true;
            btnCleanNow.Enabled = false;
            lblWorkStep.Text = I18n.T("work.preparing");
            lblWorkCount.Text = "";
            progressWork.Value = 0;
            ShowPhase(3);
            Log("开始清理，共 " + chosen.Count + " 项");

            Task.Factory.StartNew(delegate
            {
                var log = new StringBuilder();
                long freed = 0;
                long before = Engine.TotalFree();
                int count = chosen.Count;

                for (int i = 0; i < count; i++)
                {
                    CleanItem it = chosen[i];
                    int idx = i;
                    Ui(delegate
                    {
                        lblWorkStep.Text = I18n.F("work.current", it.Name);
                        lblWorkCount.Text = (idx + 1) + " / " + count;
                        progressWork.Value = (double)idx / Math.Max(1, count);
                    });
                    try
                    {
                        long f = Execute(it, log);
                        freed += f;
                        lock (log) log.AppendLine("    " + it.Name + " → 释放 " + Theme.Fmt(f));
                    }
                    catch (Exception ex) { lock (log) log.AppendLine("    失败：" + ex.Message); }
                }
                long after = Engine.TotalFree();

                var remain = new Dictionary<CleanItem, long>();
                foreach (CleanItem it in chosen)
                {
                    long r = 0;
                    try
                    {
                        if (it.Kind == "dir")
                        {
                            // 与扫描同口径：只统计还能删的字节，被占用部分不显示（避免"显示有大小、清理释放 0"）
                            foreach (string t in it.Targets)
                            {
                                try { r += Engine.Measurable(t, null).Free; }
                                catch { }
                            }
                        }
                        else if (it.Kind == "hiber")
                        {
                            string hf = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "hiberfil.sys");
                            r = File.Exists(hf) ? it.Size : 0;
                        }
                        else if (it.Kind == "recycle")
                        {
                            try { r = Engine.RecycleBinReport().Total; }
                            catch { r = 0; }
                        }
                    }
                    catch { }
                    remain[it] = r;
                }

                Ui(delegate
                {
                    foreach (KeyValuePair<CleanItem, long> kv in remain)
                    {
                        if (kv.Value <= 0) all.Remove(kv.Key);
                        else kv.Key.Size = kv.Value;
                    }
                    busy = false;
                    RefreshDrives();
                    RefreshStats();
                    BindItems();
                    ShowPhase(1);
                    if (log.Length > 0) Log(log.ToString().TrimEnd());
                    long delta = Math.Max(0, after - before);
                    if (delta <= 0 && freed > 0) delta = freed;   // 期间有其它进程写盘时，用实际删除量兜底
                    Log(string.Format("清理完成：释放 {0}，可用空间增加 {1}", Theme.Fmt(freed), Theme.Fmt(delta)));
                    ShowBanner(BannerKind.Success,
                        I18n.F("toast.cleanDone", Theme.Fmt(freed), Theme.Fmt(delta)), 12);
                    Session.PingActivity("clean");   // 用了一次清理，记一次活跃
                    if (freed > 0) DonateDialog.ShowDone(this);   // 清理完成后引导捐赠，点“我已捐赠”关闭
                });
            }, TaskCreationOptions.LongRunning);
        }

        private long Execute(CleanItem it, StringBuilder log)
        {
            switch (it.Kind)
            {
                case "dir":
                    {
                        long freed = 0;
                        foreach (string t in it.Targets)
                        {
                            long b = 0, a = 0;
                            try { b = Engine.DirSize(t); } catch { }
                            long del = Engine.ClearDir(t, log);
                            try { a = Engine.DirSize(t); } catch { }
                            freed += Math.Max(del, Math.Max(0, b - a));   // 以目录实际缩小量为准
                        }
                        return freed;
                    }
                case "recycle":
                    {
                        Engine.MarkRecycleStart();
                        long f = 0;
                        try { f = Engine.EmptyRecycleBins(log); }
                        finally { Engine.MarkRecycleEnd(); }
                        long remain = 0;
                        try { remain = Engine.RecycleBinReport().Total; }
                        catch { }
                        if (log != null && remain > 0) log.AppendLine("    回收站仍显示 " + Theme.Fmt(remain) + "（条目被占用或无权限删除）");
                        return f;
                    }
                case "hiber":
                    {
                        Engine.Run("powercfg.exe", "-h off", log, null, 60000);
                        string hf = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "hiberfil.sys");
                        return File.Exists(hf) ? 0 : it.Size;   // 关不掉就如实返回 0
                    }
                case "drivers":
                    return PruneDrivers(log);
                default:
                    return 0;
            }
        }

        // ============================================================ results binding
        private List<CleanItem> VisibleItems()
        {
            var list = new List<CleanItem>();
            string q = search == null ? "" : search.Query.ToLowerInvariant();
            foreach (CleanItem it in all)
            {
                if (filterRisk >= 0 && it.Risk != filterRisk) continue;
                if (q.Length > 0)
                {
                    string hay = (it.Name + " " + it.Category + " " + it.Note).ToLowerInvariant();
                    if (hay.IndexOf(q) < 0) continue;
                }
                list.Add(it);
            }
            int col = lvItems == null ? 2 : lvItems.SortColumn;
            bool desc = lvItems == null || lvItems.SortDesc;
            list.Sort(delegate(CleanItem a, CleanItem b)
            {
                int c;
                switch (col)
                {
                    case 0: c = string.Compare(a.Name, b.Name, StringComparison.CurrentCulture); break;
                    case 1: c = string.Compare(a.Category, b.Category, StringComparison.CurrentCulture); break;
                    case 3: c = a.Risk.CompareTo(b.Risk); break;
                    default: c = a.Size.CompareTo(b.Size); break;
                }
                if (c == 0) c = b.Size.CompareTo(a.Size);
                return desc ? -c : c;
            });
            return list;
        }

        private void BindItems()
        {
            lvItems.BeginUpdate();
            lvItems.Items.Clear();
            foreach (CleanItem it in VisibleItems())
            {
                var lvi = new ListViewItem(new[] { it.Name, it.Category, Theme.Fmt(it.Size), Theme.RiskText(it.Risk) });
                lvi.Tag = it;
                lvi.ImageIndex = 0;
                lvItems.Items.Add(lvi);
            }
            lvItems.EndUpdate();
            lblEmpty.Visible = lvItems.Items.Count == 0;
            lblEmpty.Text = all.Count == 0 ? I18n.T("empty.none") : I18n.T("empty.filter");
            UpdateSelection();
            UpdateDetail();
        }

        private void SetFilter(int risk)
        {
            filterRisk = risk;
            cardTotal.Selected = risk < 0;
            cardLow.Selected = risk == 0;
            cardMid.Selected = risk == 1;
            cardHigh.Selected = risk == 2;
            cardTotal.Invalidate(); cardLow.Invalidate(); cardMid.Invalidate(); cardHigh.Invalidate();
            BindItems();
        }

        private long TotalSize()
        {
            long t = 0;
            foreach (CleanItem it in all) t += it.Size;
            return t;
        }

        private void RefreshStats()
        {
            int[] count = new int[3];
            long[] bytes = new long[3];
            foreach (CleanItem it in all)
            {
                if (it.Risk < 0 || it.Risk > 2) continue;
                count[it.Risk]++;
                bytes[it.Risk] += it.Size;
            }
            cardTotal.Value = Theme.Fmt(TotalSize());
            cardTotal.Sub = all.Count + " 个清理项";
            cardLow.Value = Theme.Fmt(bytes[0]); cardLow.Sub = count[0] + " 项";
            cardMid.Value = Theme.Fmt(bytes[1]); cardMid.Sub = count[1] + " 项";
            cardHigh.Value = Theme.Fmt(bytes[2]); cardHigh.Sub = count[2] + " 项";
            cardTotal.Invalidate(); cardLow.Invalidate(); cardMid.Invalidate(); cardHigh.Invalidate();
        }

        private void UpdateSelection()
        {
            int n = Theme.P(0);
            long size = 0;
            foreach (CleanItem it in all)
            {
                if (!it.Checked) continue;
                n++;
                size += it.Size;
            }
            lblSelected.Text = n == 0 ? I18n.T("sel.none") : I18n.F("sel.some", n, Theme.Fmt(size));
            btnCleanNow.Enabled = n > 0 && !busy;
            btnCleanNow.Text = n == 0 ? I18n.T("btn.clean") : I18n.F("btn.cleanN", Theme.Fmt(size));
            btnCleanNow.Invalidate();
        }

        private void SelectOnlyLow()
        {
            foreach (CleanItem it in all) it.Checked = it.Risk == 0;
            lvItems.Invalidate();
            UpdateSelection();
            UpdateDetail();
        }

        private void SelectNone()
        {
            foreach (CleanItem it in all) it.Checked = false;
            lvItems.Invalidate();
            UpdateSelection();
            UpdateDetail();
        }

        private void UpdateDetail()
        {
            if (lvItems.SelectedItems.Count == 0)
            {
                current = null;
                lblDetName.Text = I18n.T("det.nothing");
                lblDetSize.Text = "";
                lblDetImpact.Text = I18n.T("det.hint");
                lblDetAdvice.Text = "";
                lblDetPath.Text = "";
                badgeDetRisk.Visible = false;
                btnDetToggle.Visible = false;
                btnOpenFolder.Visible = false;
                LayoutDetail();
                return;
            }
            current = lvItems.SelectedItems[0].Tag as CleanItem;
            if (current == null) return;
            lblDetName.Text = current.Name;
            lblDetSize.Text = Theme.Fmt(current.Size);
            lblDetImpact.Text = current.Impact;
            lblDetAdvice.Text = string.IsNullOrEmpty(current.Advice) ? current.Note : current.Advice;
            badgeDetRisk.Text = Theme.RiskText(current.Risk);
            badgeDetRisk.Accent = Theme.RiskColor(current.Risk);
            badgeDetRisk.Soft = Theme.RiskSoft(current.Risk);
            badgeDetRisk.Visible = true;
            badgeDetRisk.Invalidate();

            var sb = new StringBuilder();
            if (current.Targets != null)
            {
                int shown = Theme.P(0);
                foreach (string t in current.Targets)
                {
                    if (shown >= 3) { sb.AppendLine("…"); break; }
                    sb.AppendLine(t);
                    shown++;
                }
            }
            if (sb.Length == 0) sb.Append(current.Note);
            lblDetPath.Text = sb.ToString().TrimEnd();

            bool hasFolder = current.Targets != null && current.Targets.Length > 0 && Directory.Exists(current.Targets[0]);
            btnOpenFolder.Visible = hasFolder;
            btnDetToggle.Visible = true;
            btnDetToggle.Text = current.Checked ? I18n.T("det.remove") : I18n.T("det.add");
            btnDetToggle.Kind = current.Checked ? BtnKind.Secondary : BtnKind.Primary;
            btnDetToggle.Invalidate();
            LayoutDetail();
        }

        private void OpenCurrentFolder()
        {
            if (current == null || current.Targets == null) return;
            foreach (string t in current.Targets)
            {
                if (Directory.Exists(t))
                {
                    try { Process.Start("explorer.exe", "\"" + t + "\""); } catch { }
                    return;
                }
            }
        }

        // ============================================================ drivers
        public class DriverPlan
        {
            public List<DriverPkg> Remove = new List<DriverPkg>();
            public long EstimatedBytes;
        }

        private static List<DriverPkg> ParseDrivers(string text)
        {
            var list = new List<DriverPkg>();
            DriverPkg cur = null;
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                int idx = line.IndexOf(':');
                if (idx <= 0) continue;
                string key = line.Substring(0, idx).Trim();
                string val = line.Substring(idx + 1).Trim();

                if (key == "发布名称" || key.Equals("Published Name", StringComparison.OrdinalIgnoreCase))
                {
                    if (cur != null) list.Add(cur);
                    cur = new DriverPkg();
                    cur.Inf = val;
                    continue;
                }
                if (cur == null) continue;
                if (key == "原始名称" || key.Equals("Original Name", StringComparison.OrdinalIgnoreCase)) cur.Original = val;
                else if (key == "驱动程序版本" || key.Equals("Driver Version", StringComparison.OrdinalIgnoreCase))
                {
                    cur.RawVersion = val;
                    string[] parts = val.Split(' ');
                    cur.Date = DateTime.MinValue;
                    cur.Ver = new Version(0, 0, 0, 0);
                    if (parts.Length >= 2)
                    {
                        DateTime dt;
                        if (DateTime.TryParseExact(parts[0], new[] { "MM/dd/yyyy", "M/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) cur.Date = dt;
                        Version v;
                        if (Version.TryParse(parts[1], out v)) cur.Ver = v;
                    }
                }
            }
            if (cur != null) list.Add(cur);
            return list;
        }

        internal static DriverPlan BuildDriverPlan()
        {
            var plan = new DriverPlan();
            List<DriverPkg> pkgs = ParseDrivers(Engine.Capture("pnputil.exe", "/enum-drivers", Engine.Oem));
            if (pkgs.Count == 0) return plan;

            var groups = new Dictionary<string, List<DriverPkg>>(StringComparer.OrdinalIgnoreCase);
            foreach (DriverPkg p in pkgs)
            {
                if (string.IsNullOrEmpty(p.Original)) continue;
                List<DriverPkg> g;
                if (!groups.TryGetValue(p.Original, out g)) { g = new List<DriverPkg>(); groups[p.Original] = g; }
                g.Add(p);
            }
            foreach (KeyValuePair<string, List<DriverPkg>> kv in groups)
            {
                if (kv.Value.Count <= 1) continue;
                kv.Value.Sort(delegate(DriverPkg a, DriverPkg b)
                {
                    int c = a.Date.CompareTo(b.Date);
                    return c != 0 ? c : a.Ver.CompareTo(b.Ver);
                });
                for (int i = 0; i < kv.Value.Count - 1; i++) plan.Remove.Add(kv.Value[i]);
            }

            string repo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "DriverStore", "FileRepository");
            long total = 0;
            var keep = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (string d in Directory.EnumerateDirectories(repo))
                {
                    long sz = Engine.DirSize(d);
                    total += sz;
                    string name = Path.GetFileName(d);
                    int us = name.IndexOf('_');
                    string family = us > 0 ? name.Substring(0, us) : name;
                    long cur2;
                    if (!keep.TryGetValue(family, out cur2) || sz > cur2) keep[family] = sz;
                }
                long kept = 0;
                foreach (long v in keep.Values) kept += v;
                plan.EstimatedBytes = Math.Max(0, total - kept);
            }
            catch { }
            return plan;
        }

        private static long PruneDrivers(StringBuilder log)
        {
            long before = Engine.FreeOf(Environment.SystemDirectory);
            DriverPlan plan = BuildDriverPlan();
            int ok = 0, skip = 0;
            foreach (DriverPkg p in plan.Remove)
            {
                var sink = new StringBuilder();
                int code = Engine.Run("pnputil.exe", "/delete-driver " + p.Inf, sink, null, 120000);
                if (code == 0) ok++; else skip++;
            }
            log.AppendLine(string.Format("旧驱动包：删除 {0} 个，系统拒绝 {1} 个，释放 {2}", ok, skip,
                Theme.Fmt(Math.Max(0, Engine.FreeOf(Environment.SystemDirectory) - before))));
            return Math.Max(0, Engine.FreeOf(Environment.SystemDirectory) - before);
        }

        // ============================================================ test hooks
        public bool TestBusy { get { return busy; } }
        public int TestPhase { get { return phase; } }
        public int TestCount { get { return all.Count; } }
        public long TestTotal { get { return TotalSize(); } }
        public void TestBeginScan() { BeginScan(); }

        /// <summary>切换语言后把关键控件文本dump出来，供 --selftest 校验。</summary>
        public string TestLanguage(string code)
        {
            I18n.Set(code);
            var sb = new StringBuilder();
            sb.AppendLine("lang=" + I18n.Lang + "  (" + I18n.NativeName(I18n.Lang) + ")");
            sb.AppendLine("  nav=" + navClean.Text + " / " + navBig.Text + " / " + navLog.Text + " / " + navWechat.Text + " / " + navContact.Text);
            sb.AppendLine("  page=" + lblPageTitle.Text + " — " + lblPageSub.Text);
            sb.AppendLine("  bar=" + btnSelectLow.Text + " | " + btnSelectNone.Text + " | " + btnCleanNow.Text + " | " + btnRescan.Text);
            sb.AppendLine("  sel=" + lblSelected.Text + " | ph=" + search.Placeholder + " | empty=" + lblEmpty.Text);
            sb.AppendLine("  cards=" + cardTotal.Title + " / " + cardLow.Title + " / " + cardMid.Title + " / " + cardHigh.Title);
            sb.AppendLine("  detail=" + btnDetToggle.Text + " | " + btnOpenFolder.Text);
            sb.AppendLine("  lang.btn=" + btnLang.Text + " | chip=" + chipAdmin.Text + " | admin=" + lblAdmin.Text);
            if (all.Count > 0)
            {
                CleanItem it = all[0];
                sb.AppendLine("  item0=" + it.Name + " [" + it.Category + "]");
                sb.AppendLine("        impact=" + it.Impact);
                sb.AppendLine("        advice=" + it.Advice);
            }
            return sb.ToString();
        }

        public void TestResize(Size windowSize)        {
            Size = windowSize;
            PerformLayout();
            Application.DoEvents();
        }

        private void CheckOverlap(StringBuilder sb, string tag, Control a, Control b)
        {
            if (a == null || b == null) return;
            if (!a.Visible || !b.Visible)
            {
                sb.AppendLine("  " + tag + " : hidden (a=" + a.Visible + ", b=" + b.Visible + ")");
                return;
            }
            Rectangle ra = new Rectangle(PointToClient(a.PointToScreen(Point.Empty)), a.Size);
            Rectangle rb = new Rectangle(PointToClient(b.PointToScreen(Point.Empty)), b.Size);
            sb.AppendLine("  " + tag + (ra.IntersectsWith(rb) ? "  >>> OVERLAP " : "  ok ") + ra + " vs " + rb);
        }

        public string TestOverlaps()
        {
            var sb = new StringBuilder();
            sb.AppendLine("window=" + Size + " client=" + ClientSize);
            CheckOverlap(sb, "selected / 全选低风险", lblSelected, btnSelectLow);
            CheckOverlap(sb, "search / 全选低风险", search, btnSelectLow);
            CheckOverlap(sb, "detailScroll / 加入清理", detailScroll, btnDetToggle);
            CheckOverlap(sb, "detailScroll / 打开目录", detailScroll, btnOpenFolder);
            CheckOverlap(sb, "listCard / detailCard", lvItems, detailCard);
            CheckOverlap(sb, "dmC / btnDonate", dmC, btnDonate);
            CheckOverlap(sb, "dmC / dmD", dmC, dmD);
            CheckOverlap(sb, "dmD / lblAdmin", dmD, lblAdmin);
            CheckOverlap(sb, "lblAdmin / lblVersion", lblAdmin, lblVersion);
            sb.AppendLine("  dmC=" + (dmC == null ? "null" : dmC.Bounds.ToString() + " vis=" + dmC.Visible)
                + " dmD=" + (dmD == null ? "null" : dmD.Bounds.ToString() + " vis=" + dmD.Visible)
                + " meterH=" + (dmC == null ? 0 : DiskMeter.PreferredHeight) + " fontH=" + Theme.Small.Height);            sb.AppendLine("  detailCard=" + detailCard.Bounds + " scroll=" + detailScroll.Bounds
                + " toggle=" + btnDetToggle.Bounds + " folder=" + btnOpenFolder.Bounds);
            return sb.ToString();
        }

        public string TestLayoutDump()        {
            var sb = new StringBuilder();
            sb.AppendLine("content=" + content.Bounds + " pageResults=" + pageResults.Bounds);
            foreach (Control c in pageResults.Controls)
            {
                sb.AppendLine("  " + c.GetType().Name + " " + c.Bounds + " vis=" + c.Visible);
                foreach (Control cc in c.Controls)
                    sb.AppendLine("      " + cc.GetType().Name + " " + cc.Bounds + " vis=" + cc.Visible);
            }
            sb.AppendLine("detailCard=" + detailCard.Bounds + " parentVisible=" + detailCard.Parent.Visible);
            sb.AppendLine("listHost=" + listHost.Bounds + " lvItems=" + lvItems.Bounds + " rows=" + lvItems.Items.Count);
            sb.AppendLine("sidebar: accounts=" + (btnAccount == null ? "无" : btnAccount.Bounds.ToString())
                + " version=" + (lblVersion == null ? "无" : lblVersion.Bounds.ToString())
                + " admin=" + (lblAdmin == null ? "无" : lblAdmin.Bounds.ToString())
                + " meters=" + (dmC == null ? "无" : dmC.Bounds.ToString()) + "/" + (dmD == null ? "无" : dmD.Bounds.ToString()));
            return sb.ToString();
        }

        public void TestShowResults(List<CleanItem> items)
        {
            all = items;
            filterRisk = -1;
            lastScanAt = DateTime.Now;
            UpdateLastScanText();
            BindItems();
            RefreshStats();
            ShowPhase(1);
            if (lvItems.Items.Count > 0)
            {
                lvItems.Items[0].Selected = true;
                UpdateDetail();
            }
            SetFilter(1); SetFilter(2); SetFilter(-1);
            SelectOnlyLow();
            UpdateSelection();
            if (lvItems.Items.Count > 0)
            {
                lvItems.Items[0].Selected = true;
                lvItems.Items[0].Focused = true;
                UpdateDetail();
            }
        }

        /// <summary>自检用：验证列表滚动链路（滚轮 → 位置 → LVM_SCROLL）。</summary>
        public string TestScrollProbe()
        {
            string lv = lvItems == null ? "scroll: 无列表" : lvItems.TestScrollProbe();
            string dt = "detail: 无面板";
            if (detailScrollBar != null && detailScroll is ScrollPanel)
            {
                var src = new PanelSource((ScrollPanel)detailScroll);
                detailScrollBar.Sync();
                int d0 = src.Position();
                src.ScrollTo(src.Maximum());
                int d1 = src.Position();
                src.ScrollTo(0);
                int d2 = src.Position();
                dt = "detail: pos=" + d0 + " max=" + src.Maximum() + " 到底=" + d1 + " 回顶=" + d2
                    + " overlay=" + (detailScrollBar.Visible ? "显示" : "隐藏") + " " + detailScrollBar.Bounds
                    + " host=" + detailScrollHost.Bounds + " panel=" + detailScroll.Bounds;
            }
            return lv + "  |  " + dt;
        }

        /// <summary>自检：所有可见控件必须在父容器内，文字不得溢出控件宽度（DPI 缩放回归用）。</summary>
        public string TestTextFit()        {
            var sb = new StringBuilder();
            int bad = 0;
            List<Control> all = new List<Control>();
            Collect(this, all);
            foreach (Control c in all)
            {
                if (!c.Visible || c.Parent == null) continue;
                Rectangle r = c.Bounds;
                Rectangle pc = c.Parent.ClientRectangle;
                bool allowOverflow = c.Parent is ScrollableControl && ((ScrollableControl)c.Parent).AutoScroll;
                // 扁平滚动条方案：内容控件故意比宿主宽一个滚动条宽度，让原生条被裁掉
                bool scrollClip = r.Width > pc.Width
                    && r.Width - pc.Width <= SystemInformation.VerticalScrollBarWidth
                    && r.Height <= pc.Height + Theme.P(2)
                    && r.Left >= -Theme.P(2) && r.Top >= -Theme.P(2);
                if (!allowOverflow && !scrollClip && (r.Right > pc.Width + Theme.P(2) || r.Bottom > pc.Height + Theme.P(2) || r.Left < -Theme.P(2) || r.Top < -Theme.P(2)))
                {
                    sb.AppendLine("  CLIP " + Name(c) + " " + r + " 超出父容器 " + pc);
                    bad++;
                }
                string txt = c.Text;
                if (string.IsNullOrEmpty(txt)) continue;
                if (c is TextBox || c is ListView || c is TreeView || c is ComboBox) continue;
                if (c is Label && ((Label)c).AutoEllipsis) continue;   // 设计师明确允许省略
                if (c.Height >= c.Font.Height * 2) continue;   // 允许多行换行的标签
                if (c.AutoSize) continue;                       // 自适应尺寸不会被裁
                Size need = TextRenderer.MeasureText(txt, c.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                if (need.Width > c.Width + Theme.P(2))
                {
                    sb.AppendLine("  TEXT " + Name(c) + " 文字需 " + need.Width + "px，控件宽 " + c.Width + "px：\"" + txt + "\"");
                    bad++;
                }
            }
            sb.AppendLine("  issues=" + bad + "  controls=" + all.Count);
            return sb.ToString();
        }

        private static void Collect(Control root, List<Control> into)
        {
            foreach (Control c in root.Controls)
            {
                into.Add(c);
                if (c.Controls.Count > 0) Collect(c, into);
            }
        }

        private static string Name(Control c)
        {
            string n = c.GetType().Name;
            if (!string.IsNullOrEmpty(c.Name)) n += "#" + c.Name;
            return n;
        }
        // ============================================================ selftest
        public List<CleanItem> SelfTestScan()
        {
            var list = new List<CleanItem>();
            foreach (ScanStep s in BuildScanSteps())
            {
                try { s.Run(list); } catch { }
            }
            AssignRisk(list);
            list.Sort(delegate(CleanItem a, CleanItem b) { return b.Size.CompareTo(a.Size); });
            return list;
        }
    }
}
