using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace DiskMate
{
    /// <summary>
    /// 服务端公告：后台录入，客户端启动时拉最新一条。
    /// 点「我已知晓」或右上角关闭都把 id 记到本地，之后不再重复弹同一条。
    /// </summary>
    internal class Announcement
    {
        public int Id;
        public string Title = "";
        public string Body = "";
        public string Level = "info";

        private static string SeenFile
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate");
                try { Directory.CreateDirectory(dir); } catch { }
                return Path.Combine(dir, "announcement.seen");
            }
        }

        public static int ReadSeenId()
        {
            try
            {
                if (!File.Exists(SeenFile)) return 0;
                int id;
                return int.TryParse(File.ReadAllText(SeenFile).Trim(), out id) ? id : 0;
            }
            catch { return 0; }
        }

        public static void MarkSeen(int id)
        {
            try { File.WriteAllText(SeenFile, id.ToString()); } catch { }
        }

        /// <summary>拉最新公告。网络失败或无公告都返回 null —— 公告不能拖慢启动。</summary>
        public static Announcement Fetch()
        {
            ApiResult r = Api.Get("/api/announcements/latest", null);
            if (!r.Ok) return null;
            Dictionary<string, object> d = MiniJson.Obj(r.Json == null ? null : GetData(r.Json));
            if (d == null) return null;

            var a = new Announcement();
            a.Id = (int)MiniJson.Num(d, "id");
            a.Title = MiniJson.Str(d, "title");
            a.Body = MiniJson.Str(d, "body");
            a.Level = MiniJson.Str(d, "level");
            if (a.Id <= 0 || a.Title.Length == 0) return null;
            return a;
        }

        private static object GetData(Dictionary<string, object> json)
        {
            object v;
            return json != null && json.TryGetValue("data", out v) ? v : null;
        }
    }

    /// <summary>公告弹窗：标题 + 可滚动正文 + 「我已知晓」</summary>
    internal class AnnouncementDialog : DialogShell
    {
        public static void ShowAnnouncement(IWin32Window owner, Announcement a)
        {
            using (var d = new AnnouncementDialog())
            {
                d.Build(a);
                Form f = owner as Form;
                if (f != null) ModalMask.Wrap(f, delegate { return d.ShowDialog(f); });
                else d.ShowDialog(owner);
            }
        }

        /// <summary>自检/截图用：离屏渲染一张公告弹窗</summary>
        internal static void RenderTo(string path, string title, string body, string level)
        {
            try
            {
                using (var d = new AnnouncementDialog())
                {
                    d.Build(new Announcement { Id = 1, Title = title, Body = body, Level = level });
                    d.Show();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(400);
                    Application.DoEvents();
                    d.Refresh();
                    Application.DoEvents();
                    using (var bmp = new Bitmap(d.Width, d.Height))
                    {
                        d.DrawToBitmap(bmp, new Rectangle(0, 0, d.Width, d.Height));
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("announce render failed: " + ex.Message);
            }
        }

        private void Build(Announcement a)
        {
            int w = Theme.P(540);
            int innerW = w - Theme.P(56);

            Color accent = Theme.Primary;
            string tag = I18n.T("announce.level.info");
            if (a.Level == "warn") { accent = Theme.Mid; tag = I18n.T("announce.level.warn"); }
            else if (a.Level == "important") { accent = Theme.High; tag = I18n.T("announce.level.important"); }

            // 正文按实际排版高度算，超过 320 就固定高度并用滚动条
            int bodyH;
            using (var g = CreateGraphics())
                bodyH = TextRenderer.MeasureText(g, a.Body, Theme.Base, new Size(innerW, Theme.P(4000)),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
            bodyH = Math.Max(Theme.P(60), Math.Min(bodyH + Theme.P(8), Theme.P(320)));

            int titleY = Theme.P(26);
            int bodyY = titleY + Theme.P(44);
            int btnY = bodyY + bodyH + Theme.P(22);
            int h = btnY + Theme.P(38) + Theme.P(24);
            ClientSize = new Size(w, h);
            Text = a.Title;

            // 顶部级别色条
            var bar = new Panel();
            bar.BackColor = accent;
            bar.SetBounds(0, 0, w, Theme.P(4));
            Controls.Add(bar);

            var lblTag = new Label();
            lblTag.AutoSize = false;
            lblTag.TextAlign = ContentAlignment.MiddleCenter;
            lblTag.Font = Theme.F(9f, FontStyle.Bold);
            lblTag.ForeColor = accent;
            lblTag.BackColor = Theme.Surface;
            lblTag.Text = tag;
            lblTag.SetBounds(Theme.P(28), titleY, Theme.P(56), Theme.P(20));
            lblTag.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(accent, 1f))
                using (var p = Theme.Round(new Rectangle(0, 0, lblTag.Width - 1, lblTag.Height - 1), 5))
                    e.Graphics.DrawPath(pen, p);
            };
            Controls.Add(lblTag);

            var lblTitle = new Label();
            lblTitle.AutoSize = false;
            lblTitle.Font = Theme.F(13f, FontStyle.Bold);
            lblTitle.ForeColor = Theme.Text;
            lblTitle.Text = a.Title;
            lblTitle.AutoEllipsis = true;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.SetBounds(Theme.P(28) + Theme.P(56) + Theme.P(10), titleY - Theme.P(3),
                innerW - Theme.P(56) - Theme.P(10), Theme.P(28));
            Controls.Add(lblTitle);

            var txt = new TextBox();
            txt.Multiline = true;
            txt.ReadOnly = true;
            txt.BorderStyle = BorderStyle.None;
            txt.BackColor = Theme.Surface;
            txt.ForeColor = Theme.TextSub;
            txt.Font = Theme.Base;
            txt.ScrollBars = ScrollBars.Vertical;
            txt.TabStop = false;
            txt.Text = a.Body.Replace("\r\n", "\n").Replace("\n", "\r\n");
            txt.SetBounds(Theme.P(28), bodyY, innerW, bodyH);
            Controls.Add(txt);

            var ok = new FlatButton();
            ok.Kind = BtnKind.Primary;
            ok.Text = I18n.T("announce.gotit");
            ok.Size = new Size(Theme.P(112), Theme.P(38));
            ok.Location = new Point(w - Theme.P(28) - Theme.P(112), btnY);
            ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(ok);
        }
    }
}
