using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskMate
{
    /// <summary>扁平输入框：外框与占位提示自绘；内嵌无边框 TextBox 仅在聚焦/有内容时显示</summary>
    internal class FlatInput : Control
    {
        public readonly TextBox Box;
        private string placeholder = "";
        private bool focused;

        public FlatInput()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;

            Box = new TextBox();
            Box.BorderStyle = BorderStyle.None;
            Box.Font = Theme.Base;
            Box.BackColor = Color.White;
            Box.ForeColor = Theme.Text;
            // 平时隐藏（占位文字画在外层），点击/聚焦时才显示，避免白底盖住占位提示
            Box.Visible = false;
            Box.GotFocus += delegate { focused = true; Invalidate(); };
            Box.LostFocus += delegate
            {
                focused = false;
                if (Box.TextLength == 0) Box.Visible = false;   // 空值时退回占位文字
                Invalidate();
            };
            Box.TextChanged += delegate { Invalidate(); };
            Controls.Add(Box);
        }

        public string Placeholder
        {
            get { return placeholder; }
            set { placeholder = value ?? ""; Invalidate(); }
        }

        public bool Password
        {
            get { return Box.UseSystemPasswordChar; }
            set { Box.UseSystemPasswordChar = value; }
        }

        public string Value
        {
            get { return Box.Text.Trim(); }
            set { Box.Text = value ?? ""; }
        }

        private void FocusBox()
        {
            if (!Box.Visible) Box.Visible = true;
            Box.Focus();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int pad = Theme.P(12);
            Box.SetBounds(pad, (Height - Box.PreferredHeight) / 2 + Theme.P(1), Math.Max(10, Width - pad * 2), Box.PreferredHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (SolidBrush b = new SolidBrush(Enabled ? Color.White : Theme.SurfaceAlt))
                g.FillRectangle(b, ClientRectangle);
            Color border = focused ? Theme.Primary : Theme.Border;
            using (var pen = new Pen(border, focused ? 1.6f : 1f))
            using (var path = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), Theme.P(10)))
                g.DrawPath(pen, path);

            // 占位提示：输入框为空且未聚焦时自绘（此时内嵌 TextBox 是隐藏的，不会被盖住）
            if (Box.TextLength == 0 && !focused && !string.IsNullOrEmpty(placeholder))
            {
                using (SolidBrush b = new SolidBrush(Theme.TextFaint))
                    g.DrawString(placeholder, Box.Font, b, Theme.P(13), (Height - Box.Font.Height) / 2f);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            FocusBox();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            FocusBox();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            FocusBox();
        }
    }

    /// <summary>
    /// 账号弹窗：登录（邮箱 + 密码）/ 注册（邮箱验证码 + 密码，带图形验证码）
    /// </summary>
    internal class AccountDialog : DialogShell
    {
        public event EventHandler Changed;

        private readonly int W = Theme.P(430);

        private FlatButton tabLogin, tabRegister;
        private Panel loginPanel, registerPanel, loggedPanel;

        private FlatInput lgEmail, lgPass;
        private FlatButton lgSubmit;

        private FlatInput rgEmail, rgNickname, rgPass, rgMailCode;
        private ComboBox rgIdentity;
        private FlatButton rgSendCode, rgSubmit;
        private Label rgEmailLbl, rgNickLbl, rgIdLbl, rgMailLbl, rgPassLbl;

        // 图形验证码：平时隐藏，服务端判定“发码过于频繁/登录失败过多”时才显示
        private Panel lgCapRow, rgCapRow;
        private PictureBox lgCapImage, rgCapImage;
        private FlatInput lgCapCode, rgCapCode;
        private string lgCapId, rgCapId;

        private Label status;
        private Label loggedEmail;
        private System.Windows.Forms.Timer cooldown;
        private int cooldownLeft;

        private bool busy;

        public AccountDialog()
        {
            ClientSize = new Size(W, Theme.P(530));
            Text = I18n.T("account.title");
            BuildUi();
            if (Session.IsLoggedIn) ShowLogged(); else ShowLogin();
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
            title.Text = I18n.T("account.title");
            Controls.Add(title);

            var sub = new Label();
            sub.AutoSize = false;
            sub.Location = new Point(Theme.P(30), Theme.P(54));
            sub.Size = new Size(W - Theme.P(60), Theme.P(20));
            sub.Font = Theme.Small;
            sub.ForeColor = Theme.TextSub;
            sub.Text = I18n.T("account.sub");
            Controls.Add(sub);

            tabLogin = new FlatButton();
            tabLogin.Kind = BtnKind.Primary;
            tabLogin.Text = I18n.T("account.login");
            tabLogin.Size = new Size(Theme.P(104), Theme.P(34));
            tabLogin.Location = new Point(Theme.P(30), Theme.P(86));
            tabLogin.Click += delegate { if (!busy) ShowLogin(); };
            Controls.Add(tabLogin);

            tabRegister = new FlatButton();
            tabRegister.Kind = BtnKind.Ghost;
            tabRegister.Text = I18n.T("account.register");
            tabRegister.Size = new Size(Theme.P(104), Theme.P(34));
            tabRegister.Location = new Point(Theme.P(142), Theme.P(86));
            tabRegister.Click += delegate { if (!busy) ShowRegister(); };
            Controls.Add(tabRegister);

            BuildLoginPanel();
            BuildRegisterPanel();
            BuildLoggedPanel();

            status = new Label();
            status.AutoSize = false;
            status.Location = new Point(Theme.P(30), Theme.P(476));
            status.Size = new Size(W - Theme.P(60), Theme.P(46));
            status.Font = Theme.Small;
            status.ForeColor = Theme.TextSub;
            status.BackColor = Theme.Surface;      // 不透明底色，避免文字和下层控件叠在一起看不清
            status.TextAlign = ContentAlignment.TopLeft;
            Controls.Add(status);
            status.BringToFront();                 // 悬浮在最上层，红色提示不会被面板遮住
        }

        private void BuildLoginPanel()
        {
            loginPanel = new Panel();
            loginPanel.BackColor = Theme.Surface;
            loginPanel.Location = new Point(0, Theme.P(130));
            loginPanel.Size = new Size(W, Theme.P(280));

            lgEmail = AddInput(loginPanel, I18n.T("account.email"), Theme.P(28), I18n.T("account.email.ph"), false);
            lgPass = AddInput(loginPanel, I18n.T("account.password"), Theme.P(96), I18n.T("account.login.pass.ph"), true);

            lgCapRow = MakeCapRow(loginPanel, 160, out lgCapImage, out lgCapCode);
            lgCapImage.Click += delegate { if (!busy) LoadNumericCaptcha(lgCapImage, delegate(string id) { lgCapId = id; }); };

            lgSubmit = new FlatButton();
            lgSubmit.Kind = BtnKind.Primary;
            lgSubmit.Text = I18n.T("account.login");
            lgSubmit.Size = new Size(W - Theme.P(60), Theme.P(40));
            lgSubmit.Location = new Point(Theme.P(30), Theme.P(176));
            lgSubmit.Click += delegate { DoLogin(); };
            loginPanel.Controls.Add(lgSubmit);

            Controls.Add(loginPanel);
        }

        /// <summary>验证码行（图片 + 4 位输入框），默认隐藏，服务端要求时才显示</summary>
        private Panel MakeCapRow(Panel host, int y, out PictureBox img, out FlatInput code)
        {
            var row = new Panel();
            row.BackColor = Theme.Surface;
            row.SetBounds(0, Theme.P(y), W, Theme.P(76));
            row.Visible = false;

            var label = new Label();
            label.AutoSize = false;
            label.Location = new Point(Theme.P(30), Theme.P(0));
            label.Size = new Size(W - Theme.P(60), Theme.P(18));
            label.Font = Theme.Small;
            label.ForeColor = Theme.TextSub;
            label.Text = I18n.T("account.captcha");
            row.Controls.Add(label);

            img = new PictureBox();
            img.Size = new Size(Theme.P(126), Theme.P(40));
            img.Location = new Point(Theme.P(30), Theme.P(24));
            img.SizeMode = PictureBoxSizeMode.Zoom;
            img.BackColor = Color.White;
            img.Cursor = Cursors.Hand;
            img.BorderStyle = BorderStyle.FixedSingle;
            row.Controls.Add(img);

            code = new FlatInput();
            code.Placeholder = I18n.T("account.captcha.ph");
            code.Box.MaxLength = 4;
            code.Bounds = new Rectangle(Theme.P(168), Theme.P(26), W - Theme.P(30) - Theme.P(168), Theme.P(38));
            row.Controls.Add(code);

            host.Controls.Add(row);
            return row;
        }

        /// <summary>登录页显示/隐藏图形验证码，并重排按钮位置</summary>
        private void ShowLoginCaptcha(bool show)
        {
            if (lgCapRow == null) return;
            lgCapRow.Visible = show;
            lgSubmit.Top = Theme.P(show ? 234 : 176);
            loginPanel.Height = Theme.P(show ? 302 : 280);
            if (!show) { lgCapId = null; lgCapCode.Value = ""; lgCapImage.Image = null; }
        }

        /// <summary>注册页显示/隐藏图形验证码：插到邮箱下面，整页变高，状态提示始终在面板下方</summary>
        private void ShowRegCaptcha(bool show)
        {
            if (rgCapRow == null) return;
            ReflowRegister(show);
            status.Top = Theme.P(show ? 534 : 476);
            ClientSize = new Size(W, Theme.P(show ? 592 : 530));
            status.BringToFront();
            if (!show) { rgCapId = null; rgCapCode.Value = ""; rgCapImage.Image = null; }
        }

        /// <summary>拉一张数字图形验证码</summary>
        private void LoadNumericCaptcha(PictureBox img, Action<string> onId)
        {
            img.Image = null;
            Task.Factory.StartNew(delegate
            {
                ApiResult r = Api.Get("/api/captcha/numeric", null);
                Ui(delegate
                {
                    if (!r.Ok) { SetStatus("图片验证码加载失败：" + r.Message, true); return; }
                    onId(r.DataStr("id"));
                    img.Image = Api.DecodeDataUrl(r.DataStr("image"));
                });
            });
        }

        private void BuildRegisterPanel()
        {
            registerPanel = new Panel();
            registerPanel.BackColor = Theme.Surface;
            registerPanel.Location = new Point(0, Theme.P(130));
            registerPanel.Size = new Size(W, Theme.P(336));

            int half = (W - Theme.P(60) - Theme.P(12)) / 2;

            // 1) 邮箱 + 右侧"获取验证码"按钮
            rgEmailLbl = SectionLabel(I18n.T("account.email"), Theme.P(0));
            registerPanel.Controls.Add(rgEmailLbl);

            rgEmail = new FlatInput();
            rgEmail.Placeholder = I18n.T("account.email.ph");
            rgEmail.Bounds = new Rectangle(Theme.P(30), Theme.P(20), W - Theme.P(60) - Theme.P(114) - Theme.P(12), Theme.P(38));
            registerPanel.Controls.Add(rgEmail);

            rgSendCode = new FlatButton();
            rgSendCode.Kind = BtnKind.Secondary;
            rgSendCode.Text = I18n.T("account.sendcode");
            rgSendCode.Size = new Size(Theme.P(114), Theme.P(38));
            rgSendCode.Location = new Point(W - Theme.P(30) - Theme.P(114), Theme.P(20));
            rgSendCode.Click += delegate { DoSendCode(); };
            registerPanel.Controls.Add(rgSendCode);

            // 2) 昵称（必填）+ 身份（必选，默认打工人）并排
            rgNickname = new FlatInput();
            rgNickname.Placeholder = I18n.T("account.nickname.ph");
            rgNickname.Box.MaxLength = 20;
            rgNickLbl = SectionLabel(I18n.T("account.nickname"), Theme.P(62));
            registerPanel.Controls.Add(rgNickLbl);
            rgNickname.Bounds = new Rectangle(Theme.P(30), Theme.P(82), half, Theme.P(38));
            registerPanel.Controls.Add(rgNickname);

            rgIdLbl = SectionLabel(I18n.T("account.identity"), Theme.P(62));
            rgIdLbl.Location = new Point(Theme.P(30) + half + Theme.P(12), Theme.P(62));
            rgIdLbl.Width = half;
            registerPanel.Controls.Add(rgIdLbl);

            rgIdentity = new ComboBox();
            rgIdentity.DropDownStyle = ComboBoxStyle.DropDownList;
            rgIdentity.FlatStyle = FlatStyle.Flat;
            rgIdentity.Font = Theme.Base;
            rgIdentity.ItemHeight = Theme.P(20);
            rgIdentity.Items.Add(I18n.T("account.identity.worker"));
            rgIdentity.Items.Add(I18n.T("account.identity.student"));
            rgIdentity.Items.Add(I18n.T("account.identity.other"));
            rgIdentity.SelectedIndex = 0;   // 必填，默认“打工人”
            rgIdentity.Bounds = new Rectangle(Theme.P(30) + half + Theme.P(12), Theme.P(86), half, Theme.P(30));
            registerPanel.Controls.Add(rgIdentity);

            // 3) 邮箱验证码
            rgMailLbl = SectionLabel(I18n.T("account.mailcode"), Theme.P(124));
            registerPanel.Controls.Add(rgMailLbl);
            rgMailCode = new FlatInput();
            rgMailCode.Placeholder = I18n.T("account.mailcode.ph");
            rgMailCode.Box.MaxLength = 6;
            rgMailCode.Bounds = new Rectangle(Theme.P(30), Theme.P(144), W - Theme.P(60), Theme.P(38));
            registerPanel.Controls.Add(rgMailCode);

            // 4) 密码
            rgPassLbl = SectionLabel(I18n.T("account.password"), Theme.P(192));
            registerPanel.Controls.Add(rgPassLbl);
            rgPass = new FlatInput();
            rgPass.Placeholder = I18n.T("account.pass.ph");
            rgPass.Password = true;
            rgPass.Bounds = new Rectangle(Theme.P(30), Theme.P(212), W - Theme.P(60), Theme.P(38));
            registerPanel.Controls.Add(rgPass);

            // 5) 图形验证码（默认隐藏；紧贴在邮箱输入框下面，出现时下方内容整体下移）
            rgCapRow = MakeCapRow(registerPanel, 62, out rgCapImage, out rgCapCode);
            rgCapImage.Click += delegate { if (!busy) LoadNumericCaptcha(rgCapImage, delegate(string id) { rgCapId = id; }); };

            rgSubmit = new FlatButton();
            rgSubmit.Kind = BtnKind.Primary;
            rgSubmit.Text = I18n.T("account.signup");
            rgSubmit.Size = new Size(W - Theme.P(60), Theme.P(40));
            rgSubmit.Location = new Point(Theme.P(30), Theme.P(276));
            rgSubmit.Click += delegate { DoRegister(); };
            registerPanel.Controls.Add(rgSubmit);

            ReflowRegister(false);
            Controls.Add(registerPanel);
        }

        /// <summary>
        /// 注册页重排：图形验证码插在邮箱下面，出现时下方控件整体下移 68px。
        /// 面板高度随之变化，避免内容压到底部状态提示。
        /// </summary>
        private void ReflowRegister(bool showCaptcha)
        {
            int dy = showCaptcha ? 68 : 0;
            int half = (W - Theme.P(60) - Theme.P(12)) / 2;

            if (rgCapRow != null)
            {
                rgCapRow.SetBounds(0, Theme.P(62), W, Theme.P(70));
                rgCapRow.Visible = showCaptcha;
            }

            rgNickLbl.Top = Theme.P(62 + dy);
            rgIdLbl.Top = Theme.P(62 + dy);
            rgNickname.SetBounds(Theme.P(30), Theme.P(82 + dy), half, Theme.P(38));
            rgIdentity.SetBounds(Theme.P(30) + half + Theme.P(12), Theme.P(86 + dy), half, Theme.P(30));

            rgMailLbl.Top = Theme.P(124 + dy);
            rgMailCode.SetBounds(Theme.P(30), Theme.P(144 + dy), W - Theme.P(60), Theme.P(38));

            rgPassLbl.Top = Theme.P(192 + dy);
            rgPass.SetBounds(Theme.P(30), Theme.P(212 + dy), W - Theme.P(60), Theme.P(38));

            rgSubmit.Location = new Point(Theme.P(30), Theme.P(276 + dy));
            registerPanel.Height = Theme.P(336 + dy);
        }

        private void BuildLoggedPanel()
        {
            loggedPanel = new Panel();
            loggedPanel.BackColor = Theme.Surface;
            loggedPanel.Location = new Point(0, Theme.P(130));
            loggedPanel.Size = new Size(W, Theme.P(240));

            var cap = SectionLabel(I18n.T("account.logged"), Theme.P(4));
            loggedPanel.Controls.Add(cap);

            loggedEmail = new Label();
            loggedEmail.AutoSize = false;
            loggedEmail.Location = new Point(Theme.P(30), Theme.P(30));
            loggedEmail.Size = new Size(W - Theme.P(60), Theme.P(40));
            loggedEmail.Font = Theme.F(13f, FontStyle.Bold);
            loggedEmail.ForeColor = Theme.Text;
            loggedPanel.Controls.Add(loggedEmail);

            var note = new Label();
            note.AutoSize = false;
            note.Location = new Point(Theme.P(30), Theme.P(76));
            note.Size = new Size(W - Theme.P(60), Theme.P(44));
            note.Font = Theme.Small;
            note.ForeColor = Theme.TextSub;
            note.Text = I18n.T("account.note");
            loggedPanel.Controls.Add(note);

            var outBtn = new FlatButton();
            outBtn.Kind = BtnKind.Secondary;
            outBtn.Text = I18n.T("account.logout");
            outBtn.Size = new Size(Theme.P(132), Theme.P(38));
            outBtn.Location = new Point(Theme.P(30), Theme.P(136));
            outBtn.Click += delegate
            {
                Session.Clear();
                if (Changed != null) Changed(this, EventArgs.Empty);
                ShowLogin();
                SetStatus("已退出登录", false);
            };
            loggedPanel.Controls.Add(outBtn);

            var closeBtn = new FlatButton();
            closeBtn.Kind = BtnKind.Primary;
            closeBtn.Text = I18n.T("account.close");
            closeBtn.Size = new Size(W - Theme.P(30) - Theme.P(132) - Theme.P(30) - Theme.P(10), Theme.P(38));
            closeBtn.Location = new Point(Theme.P(172), Theme.P(136));
            closeBtn.Click += delegate { Close(); };
            loggedPanel.Controls.Add(closeBtn);

            Controls.Add(loggedPanel);
        }

        private Label SectionLabel(string text, int y)
        {
            var l = new Label();
            l.AutoSize = false;
            l.Location = new Point(Theme.P(30), y);
            l.Size = new Size(W - Theme.P(60), Theme.P(18));
            l.Font = Theme.Small;
            l.ForeColor = Theme.TextSub;
            l.Text = text;
            return l;
        }

        private FlatInput AddInput(Panel host, string label, int y, string placeholder, bool password)
        {
            host.Controls.Add(SectionLabel(label, y));
            var input = new FlatInput();
            input.Placeholder = placeholder;
            input.Password = password;
            input.Bounds = new Rectangle(Theme.P(30), y + Theme.P(20), W - Theme.P(60), Theme.P(38));
            host.Controls.Add(input);
            return input;
        }

        // ------------------------------------------------------------ 状态切换
        private void ShowPanel(Panel panel, bool loginSelected)
        {
            loginPanel.Visible = panel == loginPanel;
            registerPanel.Visible = panel == registerPanel;
            loggedPanel.Visible = panel == loggedPanel;
            tabLogin.Visible = tabRegister.Visible = panel != loggedPanel;

            // 切走时把按需出现的图形验证码收起来，避免弹窗保持变高的状态
            if (panel != loginPanel) ShowLoginCaptcha(false);
            if (panel != registerPanel) ShowRegCaptcha(false);

            if (panel != loggedPanel)
            {
                tabLogin.Kind = loginSelected ? BtnKind.Primary : BtnKind.Ghost;
                tabRegister.Kind = loginSelected ? BtnKind.Ghost : BtnKind.Primary;
                tabLogin.Invalidate();
                tabRegister.Invalidate();
            }
            SetStatus("", false);
        }

        private void ShowLogin() { ShowPanel(loginPanel, true); }
        private void ShowRegister() { ShowPanel(registerPanel, false); }

        private void ShowLogged()
        {
            ShowPanel(loggedPanel, true);
            loggedEmail.Text = Session.Email + (Session.UserId > 0 ? "  (#" + Session.UserId + ")" : "");
        }

        private void SetStatus(string text, bool error)
        {
            status.Text = text ?? "";
            status.ForeColor = error ? Theme.High : Theme.TextSub;
        }

        private void SetBusy(bool value)
        {
            busy = value;
            lgSubmit.Enabled = !value;
            rgSubmit.Enabled = !value;
            rgSendCode.Enabled = !value && cooldownLeft <= 0;
        }

        // ------------------------------------------------------------ 发送邮箱验证码
        private void DoSendCode()
        {
            if (busy) return;
            string email = rgEmail.Value;
            if (email.Length == 0 || email.IndexOf('@') < 0) { SetStatus("请先填写正确的邮箱", true); return; }

            bool withCap = rgCapRow != null && rgCapRow.Visible;
            string cap = "";
            if (withCap)
            {
                if (string.IsNullOrEmpty(rgCapId))
                {
                    LoadNumericCaptcha(rgCapImage, delegate(string id) { rgCapId = id; });
                    SetStatus("图片验证码已刷新，请填入 4 位数字后再点「获取验证码」", true);
                    return;
                }
                cap = rgCapCode.Value.Trim();
                if (cap.Length < 4) { SetStatus("请填写图片里的 4 位数字", true); return; }
            }

            SetBusy(true);
            SetStatus("正在发送验证码…", false);
            Task.Factory.StartNew(delegate
            {
                string body = "{\"email\":\"" + Session.Escape(email) + "\",\"purpose\":\"register\"";
                if (withCap)
                    body += ",\"numericId\":\"" + Session.Escape(rgCapId) + "\",\"numericCode\":\"" + Session.Escape(cap) + "\"";
                body += "}";
                ApiResult r = Api.Post("/api/auth/send-code", body, null);
                Ui(delegate
                {
                    if (!r.Ok)
                    {
                        SetBusy(false);
                        if (r.Status == 428 || r.Code == "CAPTCHA_REQUIRED")
                        {
                            ShowRegCaptcha(true);
                            LoadNumericCaptcha(rgCapImage, delegate(string id) { rgCapId = id; });
                            SetStatus("为防恶意刷验证码，请先输入图片验证码，再点一次「获取验证码」", true);
                        }
                        else SetStatus(r.Message, true);
                        return;
                    }
                    if (withCap) ShowRegCaptcha(false);
                    int cd = (int)MiniJson.Num(r.Data, "cooldown");
                    if (cd <= 0) cd = 60;
                    StartCooldown(cd);
                    string dev = r.DataStr("devCode");
                    SetStatus(dev.Length > 0
                        ? "开发模式验证码：" + dev + "（服务端未配置 SMTP 授权码，已写入 var/outbox）"
                        : "验证码已发送，请查收邮箱（10 分钟内有效）", false);
                    rgSubmit.Enabled = true;
                    busy = false;
                    lgSubmit.Enabled = true;
                });
            });
        }

        private void StartCooldown(int seconds)
        {
            cooldownLeft = seconds;
            rgSendCode.Enabled = false;
            rgSendCode.Text = I18n.F("account.resend", cooldownLeft);
            rgSendCode.Invalidate();
            if (cooldown == null)
            {
                cooldown = new System.Windows.Forms.Timer();
                cooldown.Interval = 1000;
                cooldown.Tick += delegate
                {
                    cooldownLeft--;
                    if (cooldownLeft <= 0)
                    {
                        cooldown.Stop();
                        cooldownLeft = 0;
                        rgSendCode.Text = I18n.T("account.sendcode");
                        rgSendCode.Enabled = !busy;
                    }
                    else
                    {
                        rgSendCode.Text = I18n.F("account.resend", cooldownLeft);
                    }
                    rgSendCode.Invalidate();
                };
            }
            cooldown.Start();
        }

        // ------------------------------------------------------------ 注册 / 登录
        private void DoRegister()
        {
            if (busy) return;
            string email = rgEmail.Value;
            string nick = rgNickname.Value;
            string mailCode = rgMailCode.Value;
            string password = rgPass.Value;
            if (email.Length == 0) { SetStatus("请先填邮箱", true); return; }
            if (nick.Length < 2) { SetStatus("昵称至少 2 个字", true); return; }
            if (mailCode.Length != 6) { SetStatus("请填 6 位邮箱验证码", true); return; }
            if (password.Length < 8) { SetStatus("密码至少 8 位，且要含字母和数字", true); return; }
            string identity = IdentityValue();

            SetBusy(true);
            SetStatus("正在注册…", false);
            Task.Factory.StartNew(delegate
            {
                string body = "{\"email\":\"" + Session.Escape(email) +
                              "\",\"nickname\":\"" + Session.Escape(nick) +
                              "\",\"identity\":\"" + Session.Escape(identity) +
                              "\",\"code\":\"" + Session.Escape(mailCode) +
                              "\",\"password\":\"" + Session.Escape(password) + "\"}";
                ApiResult r = Api.Post("/api/auth/register", body, null);
                Ui(delegate
                {
                    SetBusy(false);
                    if (!r.Ok) { SetStatus(r.Message, true); return; }
                    Session.Apply(r.Data);
                    if (Changed != null) Changed(this, EventArgs.Empty);
                    ShowLogged();
                    SetStatus("注册成功，已登录", false);
                });
            });
        }

        private void DoLogin()
        {
            if (busy) return;
            string email = lgEmail.Value;
            string password = lgPass.Value;
            if (email.Length == 0) { SetStatus("请先填邮箱", true); return; }
            if (password.Length == 0) { SetStatus("请填密码", true); return; }

            bool withCap = lgCapRow != null && lgCapRow.Visible;
            string cap = "";
            if (withCap)
            {
                if (string.IsNullOrEmpty(lgCapId))
                {
                    LoadNumericCaptcha(lgCapImage, delegate(string id) { lgCapId = id; });
                    SetStatus("图片验证码已刷新，请填入 4 位数字后再登录", true);
                    return;
                }
                cap = lgCapCode.Value.Trim();
                if (cap.Length < 4) { SetStatus("请填写图片里的 4 位数字", true); return; }
            }

            SetBusy(true);
            SetStatus("正在登录…", false);
            Task.Factory.StartNew(delegate
            {
                string body = "{\"email\":\"" + Session.Escape(email) + "\",\"password\":\"" + Session.Escape(password) + "\"";
                if (withCap)
                    body += ",\"numericId\":\"" + Session.Escape(lgCapId) + "\",\"numericCode\":\"" + Session.Escape(cap) + "\"";
                body += "}";
                ApiResult r = Api.Post("/api/auth/login", body, null);
                Ui(delegate
                {
                    SetBusy(false);
                    if (!r.Ok)
                    {
                        if (r.Status == 428 || r.Code == "CAPTCHA_REQUIRED")
                        {
                            ShowLoginCaptcha(true);
                            LoadNumericCaptcha(lgCapImage, delegate(string id) { lgCapId = id; });
                            SetStatus(string.IsNullOrEmpty(r.Message) ? "登录过于频繁，请输入图片验证码后再登录" : r.Message + "，填好后重新登录", true);
                        }
                        else SetStatus(r.Message, true);
                        return;
                    }
                    if (withCap) ShowLoginCaptcha(false);
                    Session.Apply(r.Data);
                    if (Changed != null) Changed(this, EventArgs.Empty);
                    ShowLogged();
                    SetStatus("登录成功", false);
                });
            });
        }

        /// <summary>身份必填：0=打工人 1=学生 2=其他</summary>
        private string IdentityValue()
        {
            if (rgIdentity == null) return "worker";
            switch (rgIdentity.SelectedIndex)
            {
                case 1: return "student";
                case 2: return "other";
                default: return "worker";
            }
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

        public static void Show(IWin32Window owner, EventHandler onChanged)
        {
            using (var d = new AccountDialog())
            {
                if (onChanged != null) d.Changed += onChanged;
                Form f = owner as Form;
                if (f != null)
                    ModalMask.Wrap(f, delegate { return d.ShowDialog(f); });
                else
                    d.ShowDialog(owner);
            }
        }

        /// <summary>自检用：强制展开注册页图形验证码（渲染布局用）</summary>
        internal void PreviewRegisterCaptcha()
        {
            ShowRegister();
            ShowRegCaptcha(true);
            SetStatus("为防恶意刷验证码，请先输入图片验证码，再点一次「获取验证码」", true);
        }

        /// <summary>自检用：强制展开登录页图形验证码（渲染布局用）</summary>
        internal void PreviewLoginCaptcha()
        {
            ShowLogin();
            ShowLoginCaptcha(true);
            SetStatus("登录过于频繁，请先完成图形验证码", true);
        }

        /// <summary>自检用：把弹窗渲染成 PNG（注册页 / 登录页 / 已登录页）</summary>
        internal static void RenderTo(string path, string view)
        {
            try
            {
                Api.LoadConfig();
                Session.Load();
                if (view == "logged" && !Session.IsLoggedIn)
                {
                    Session.Email = "demo@diskmate.local";
                    Session.UserId = 42;
                    Session.AccessToken = "preview";
                }
                using (var d = new AccountDialog())
                {
                    if (view == "register") d.ShowRegister();
                    if (view == "regcap") d.PreviewRegisterCaptcha();
                    if (view == "logincap") d.PreviewLoginCaptcha();
                    d.Show();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(600);
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
}
