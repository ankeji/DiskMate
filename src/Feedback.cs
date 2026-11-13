using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskMate
{
    /// <summary>一条问题反馈（服务端 /api/feedback 返回的条目）</summary>
    internal class FeedbackItem
    {
        public int Id;
        public string Title = "";
        public string Content = "";
        public string Status = "open";     // open / processing / resolved
        public string Reply = "";
        public string Author = "";
        public string CreatedAt = "";
        public string UpdatedAt = "";
        public bool Mine;
    }

    /// <summary>问题反馈的读写（后端 DiskMate-admin 的 /api/feedback）</summary>
    internal static class FeedbackService
    {
        public const int TitleMax = 50;
        public const int ContentMax = 500;

        /// <summary>最近一次拉取到的列表，供页面刷新时复用</summary>
        public static readonly List<FeedbackItem> Cached = new List<FeedbackItem>();

        public static string StatusName(string status)
        {
            switch (status)
            {
                case "processing": return I18n.T("fb.status.processing");
                case "resolved": return I18n.T("fb.status.resolved");
                default: return I18n.T("fb.status.open");
            }
        }

        /// <summary>拉列表。status 传 all / open / processing / resolved。失败返回 null 并给 error。</summary>
        public static List<FeedbackItem> Load(string status, out string error)
        {
            error = "";
            string path = "/api/feedback?pageSize=50";
            if (!string.IsNullOrEmpty(status) && status != "all") path += "&status=" + status;

            ApiResult r = Api.Get(path, Session.IsLoggedIn ? Session.AccessToken : null);
            if (!r.Ok)
            {
                error = r.Message;
                return null;
            }

            List<object> arr = MiniJson.Arr(r.Data, "items");
            var list = new List<FeedbackItem>();
            if (arr != null)
            {
                foreach (object o in arr)
                {
                    Dictionary<string, object> j = MiniJson.Obj(o);
                    if (j == null) continue;
                    var f = new FeedbackItem();
                    f.Id = (int)MiniJson.Num(j, "id");
                    f.Title = MiniJson.Str(j, "title");
                    f.Content = MiniJson.Str(j, "content");
                    f.Status = MiniJson.Str(j, "status");
                    f.Reply = MiniJson.Str(j, "reply");
                    f.Author = MiniJson.Str(j, "author");
                    f.CreatedAt = MiniJson.Str(j, "createdAt");
                    f.UpdatedAt = MiniJson.Str(j, "updatedAt");
                    f.Mine = MiniJson.Str(j, "mine") == "true" || MiniJson.Str(j, "mine") == "True";
                    list.Add(f);
                }
            }
            return list;
        }

        /// <summary>提交反馈。成功返回新 id，失败返回 0 并给 error。</summary>
        public static int Submit(string title, string content, out string error)
        {
            error = "";
            string body = "{\"title\":\"" + Session.Escape(title.Trim()) + "\""
                        + ",\"content\":\"" + Session.Escape(content.Trim()) + "\""
                        + ",\"version\":\"" + Session.Escape(AppInfo.Version) + "\"}";

            ApiResult r = Api.Post("/api/feedback", body, Session.IsLoggedIn ? Session.AccessToken : null);
            if (!r.Ok)
            {
                error = r.Message;
                return 0;
            }
            return (int)MiniJson.Num(r.Data, "id");
        }
    }

    /// <summary>写反馈弹窗：标题（50 字）+ 内容（500 字），带实时字数统计。</summary>
    internal class FeedbackDialog : DialogShell
    {
        private readonly TextBox txtTitle;
        private readonly TextBox txtContent;
        private readonly Label lblTitleCount;
        private readonly Label lblContentCount;
        private readonly Label lblError;
        private readonly FlatButton btnSubmit;
        private bool sending;

        private FeedbackDialog()
        {
            int w = Theme.P(560);
            int h = Theme.P(506);
            ClientSize = new Size(w, h);
            Text = I18n.T("fbd.title");

            var title = new Label();
            title.AutoSize = false;
            title.Location = new Point(Theme.P(28), Theme.P(24));
            title.Size = new Size(w - Theme.P(56), Theme.P(30));
            title.Font = Theme.F(14f, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.Text = I18n.T("fbd.title");
            Controls.Add(title);

            var sub = new Label();
            sub.AutoSize = false;
            sub.Location = new Point(Theme.P(28), Theme.P(56));
            sub.Size = new Size(w - Theme.P(56), Theme.P(22));
            sub.Font = Theme.Small;
            sub.ForeColor = Theme.TextSub;
            sub.Text = I18n.T("fbd.sub");
            Controls.Add(sub);

            var lblT = new Label();
            lblT.AutoSize = false;
            lblT.Location = new Point(Theme.P(28), Theme.P(92));
            lblT.Size = new Size(Theme.P(200), Theme.P(20));
            lblT.Font = Theme.Small;
            lblT.ForeColor = Theme.TextSub;
            lblT.Text = I18n.T("fbd.label.title");
            Controls.Add(lblT);

            txtTitle = MakeBox(false);
            txtTitle.MaxLength = FeedbackService.TitleMax;
            Controls.Add(Border(txtTitle, new Rectangle(Theme.P(28), Theme.P(118), w - Theme.P(56), Theme.P(42)), false));

            lblTitleCount = new Label();
            lblTitleCount.AutoSize = false;
            lblTitleCount.TextAlign = ContentAlignment.MiddleRight;
            lblTitleCount.Font = Theme.Small;
            lblTitleCount.ForeColor = Theme.TextFaint;
            lblTitleCount.SetBounds(w - Theme.P(28) - Theme.P(70), Theme.P(92), Theme.P(70), Theme.P(20));
            Controls.Add(lblTitleCount);

            var lblC = new Label();
            lblC.AutoSize = false;
            lblC.Location = new Point(Theme.P(28), Theme.P(170));
            lblC.Size = new Size(Theme.P(200), Theme.P(20));
            lblC.Font = Theme.Small;
            lblC.ForeColor = Theme.TextSub;
            lblC.Text = I18n.T("fbd.label.content");
            Controls.Add(lblC);

            txtContent = MakeBox(true);
            txtContent.MaxLength = FeedbackService.ContentMax;
            txtContent.ScrollBars = ScrollBars.Vertical;
            Controls.Add(Border(txtContent, new Rectangle(Theme.P(28), Theme.P(194), w - Theme.P(56), Theme.P(180)), true));

            lblContentCount = new Label();
            lblContentCount.AutoSize = false;
            lblContentCount.TextAlign = ContentAlignment.MiddleRight;
            lblContentCount.Font = Theme.Small;
            lblContentCount.ForeColor = Theme.TextFaint;
            lblContentCount.SetBounds(w - Theme.P(28) - Theme.P(96), Theme.P(170), Theme.P(96), Theme.P(20));
            Controls.Add(lblContentCount);

            lblError = new Label();
            lblError.AutoSize = false;
            lblError.Font = Theme.Small;
            lblError.ForeColor = Theme.High;
            lblError.Location = new Point(Theme.P(28), Theme.P(382));
            lblError.Size = new Size(w - Theme.P(220), Theme.P(36));
            lblError.Text = "";
            Controls.Add(lblError);

            var btnCancel = new FlatButton();
            btnCancel.Kind = BtnKind.Secondary;
            btnCancel.Text = I18n.T("fbd.cancel");
            btnCancel.Size = new Size(Theme.P(96), Theme.P(38));
            btnCancel.Location = new Point(w - Theme.P(28) - Theme.P(96) - Theme.P(12) - Theme.P(112), Theme.P(424));
            btnCancel.Click += delegate { Close(); };
            Controls.Add(btnCancel);

            btnSubmit = new FlatButton();
            btnSubmit.Kind = BtnKind.Primary;
            btnSubmit.Text = I18n.T("fbd.submit");
            btnSubmit.Size = new Size(Theme.P(112), Theme.P(38));
            btnSubmit.Location = new Point(w - Theme.P(28) - Theme.P(112), Theme.P(424));
            btnSubmit.Click += delegate { Submit(); };
            Controls.Add(btnSubmit);

            txtTitle.TextChanged += delegate { UpdateCounts(); };
            txtContent.TextChanged += delegate { UpdateCounts(); };
            txtTitle.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtContent.Focus(); }
            };
            txtContent.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter && e.Control) { e.SuppressKeyPress = true; Submit(); }
            };
            UpdateCounts();

            Shown += delegate
            {
                try { Activate(); txtTitle.Focus(); } catch { }
            };
        }

        private static TextBox MakeBox(bool multiline)
        {
            var t = new TextBox();
            t.BorderStyle = BorderStyle.None;
            t.Font = Theme.Base;
            t.BackColor = Color.White;
            t.ForeColor = Theme.Text;
            t.Multiline = multiline;
            if (multiline) t.AcceptsReturn = true;
            return t;
        }

        /// <summary>
        /// 给无边框输入框套一个圆角描边底板。
        /// 注意：输入框的坐标是相对底板（Panel）的，不能直接用弹窗坐标，否则会被裁到面板外、点不到也打不了字。
        /// </summary>
        private static Panel Border(TextBox box, Rectangle bounds, bool multiline)
        {
            var p = new Panel();
            p.BackColor = Color.White;
            p.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            p.Cursor = Cursors.IBeam;
            p.Paint += delegate (object s, PaintEventArgs e)
            {
                var panel = (Panel)s;
                using (var pen = new Pen(Theme.Border))
                using (var path = Theme.Round(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 8))
                    e.Graphics.DrawPath(pen, path);
            };

            int padX = Theme.P(10);
            int innerW = Math.Max(Theme.P(40), p.Width - padX * 2);
            if (multiline)
            {
                box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                box.SetBounds(padX, Theme.P(8), innerW, Math.Max(Theme.P(40), p.Height - Theme.P(16)));
            }
            else
            {
                box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                box.SetBounds(padX, Theme.P(12), innerW, Theme.P(20));
            }

            // 点底板空白处也能聚焦到输入框
            p.Click += delegate { box.Focus(); };
            p.Controls.Add(box);
            return p;
        }

        private void UpdateCounts()
        {
            lblTitleCount.Text = txtTitle.TextLength + "/" + FeedbackService.TitleMax;
            lblContentCount.Text = txtContent.TextLength + "/" + FeedbackService.ContentMax;
        }

        private void Submit()
        {
            if (sending) return;
            string title = txtTitle.Text.Trim();
            string content = txtContent.Text.Trim();

            if (title.Length == 0) { ShowError(I18n.T("fbd.needTitle")); txtTitle.Focus(); return; }
            if (content.Length == 0) { ShowError(I18n.T("fbd.needContent")); txtContent.Focus(); return; }

            sending = true;
            lblError.Text = "";
            btnSubmit.Enabled = false;
            btnSubmit.Text = I18n.T("fbd.sending");
            btnSubmit.Invalidate();

            Task.Factory.StartNew(delegate
            {
                string err;
                int id = FeedbackService.Submit(title, content, out err);
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        sending = false;
                        btnSubmit.Enabled = true;
                        btnSubmit.Text = I18n.T("fbd.submit");
                        btnSubmit.Invalidate();
                        if (id > 0)
                        {
                            DialogResult = DialogResult.OK;
                            Close();
                        }
                        else
                        {
                            ShowError(string.IsNullOrEmpty(err) ? I18n.T("fbd.failed") : err);
                        }
                    });
                }
                catch { }
            });
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
        }

        /// <summary>自检用：离屏渲染写反馈弹窗</summary>
        internal static void RenderTo(string path)
        {
            try
            {
                using (var d = new FeedbackDialog())
                {
                    d.txtTitle.Text = "大文件扫描卡住";
                    d.txtContent.Text = "扫 D 盘的时候进度条停在 30% 不动，重启后恢复正常。系统是 Win11 23H2。";
                    d.UpdateCounts();
                    d.Show();
                    Application.DoEvents();
                    d.Refresh();
                    Application.DoEvents();
                    // 模拟真人点击 + 键入：能进字符才算这个弹窗是能用的
                    string beforeTitle = d.txtTitle.Text;
                    string beforeContent = d.txtContent.Text;
                    Native.SendMessage(d.txtContent.Handle, 0x0201, (IntPtr)1, (IntPtr)((8 << 16) | 20));
                    Native.SendMessage(d.txtContent.Handle, 0x0202, IntPtr.Zero, IntPtr.Zero);
                    Native.SendMessage(d.txtContent.Handle, 0x0102, (IntPtr)'X', IntPtr.Zero);   // WM_CHAR
                    Application.DoEvents();
                    Console.WriteLine("input: focused=" + d.txtContent.Focused
                        + " grew=" + (d.txtContent.TextLength > beforeContent.Length));
                    d.txtTitle.Text = beforeTitle;
                    d.txtContent.Text = beforeContent;
                    Application.DoEvents();
                    Console.WriteLine("dialog=" + d.Width + "x" + d.Height
                        + " titleBox=" + d.txtTitle.Parent.Bounds + " inner=" + d.txtTitle.Bounds
                        + " contentBox=" + d.txtContent.Parent.Bounds + " inner=" + d.txtContent.Bounds
                        + " canFocus=" + d.txtTitle.CanFocus + "/" + d.txtContent.CanFocus
                        + " submit=" + d.btnSubmit.Bounds);
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

        /// <summary>打开写反馈弹窗；提交成功返回 true。</summary>
        public static bool Show(IWin32Window owner)
        {
            using (var d = new FeedbackDialog())
            {
                Form f = owner as Form;
                DialogResult r;
                if (f != null)
                {
                    r = DialogResult.Cancel;
                    ModalMask.Wrap(f, delegate { r = d.ShowDialog(f); return r; });
                }
                else r = d.ShowDialog(owner);
                return r == DialogResult.OK;
            }
        }
    }
}
