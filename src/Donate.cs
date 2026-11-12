using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DiskMate
{
    /// <summary>
    /// 捐赠弹窗：中文界面显示支付宝 / 微信收款码（内嵌资源），其他语言显示 Ko-fi。
    /// 收款码点一下会用系统看图工具打开原图，方便放大扫码。
    /// done 模式（清理完成后弹出）右下角按钮为“我已捐赠”。
    /// </summary>
    internal class DonateDialog : DialogShell
    {
        public const string KofiUrl = "https://ko-fi.com/T1S42A3IBF";

        private const string AlipayRes = "zhifubao.jpg";
        private const string WeixinRes = "weixin.jpg";

        private Image imgAlipay, imgWeixin;

        private DonateDialog(bool done)
        {
            bool zh = I18n.Lang.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            int w = Theme.P(560);

            imgAlipay = LoadImage(AlipayRes);
            imgWeixin = LoadImage(WeixinRes);
            bool hasQr = zh && imgAlipay != null && imgWeixin != null;

            int h = hasQr ? Theme.P(468) : Theme.P(300);
            ClientSize = new Size(w, h);
            Text = I18n.T("donate.title");

            var title = new Label();
            title.AutoSize = false;
            title.Location = new Point(Theme.P(30), Theme.P(24));
            title.Size = new Size(w - Theme.P(60), Theme.P(30));
            title.Font = Theme.F(14f, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.Text = I18n.T("donate.title");
            Controls.Add(title);

            var sub = new Label();
            sub.AutoSize = false;
            sub.Location = new Point(Theme.P(30), Theme.P(58));
            sub.Size = new Size(w - Theme.P(60), hasQr ? Theme.P(40) : Theme.P(56));
            sub.ForeColor = Theme.TextSub;
            sub.Text = I18n.T("donate.sub");
            Controls.Add(sub);

            if (hasQr)
            {
                // 两张收款码并排居中：左支付宝（1708x2560），右微信（828x1124）
                int imgH = Theme.P(220);
                int wA = (int)Math.Round(imgH * 1708.0 / 2560.0);
                int wW = (int)Math.Round(imgH * 828.0 / 1124.0);
                int gap = Theme.P(48);
                int left = (w - (wA + gap + wW)) / 2;

                AddQr(imgAlipay, new Rectangle(left, Theme.P(104), wA, imgH), I18n.T("donate.alipay"), AlipayRes);
                AddQr(imgWeixin, new Rectangle(left + wA + gap, Theme.P(104), wW, imgH), I18n.T("donate.weixin"), WeixinRes);

                var hint = new Label();
                hint.AutoSize = false;
                hint.Location = new Point(Theme.P(30), Theme.P(356));
                hint.Size = new Size(w - Theme.P(60), Theme.P(18));
                hint.TextAlign = ContentAlignment.MiddleCenter;
                hint.ForeColor = Theme.TextFaint;
                hint.Font = Theme.Small;
                hint.Text = I18n.T("donate.scanHint");
                Controls.Add(hint);
            }

            var btnClose = new FlatButton();
            btnClose.Kind = done ? BtnKind.Primary : BtnKind.Secondary;
            btnClose.Text = done ? I18n.T("donate.done") : I18n.T("donate.close");
            btnClose.Size = new Size(done ? Theme.P(132) : Theme.P(96), Theme.P(38));
            btnClose.Location = new Point(w - Theme.P(30) - btnClose.Width, h - Theme.P(62));
            btnClose.Click += delegate { Close(); };
            Controls.Add(btnClose);

            var btnKofi = new FlatButton();
            btnKofi.Kind = hasQr ? BtnKind.Ghost : BtnKind.Primary;
            btnKofi.Text = I18n.T("donate.kofi");
            btnKofi.Size = new Size(hasQr ? Theme.P(178) : Theme.P(200), Theme.P(38));
            btnKofi.Location = new Point(Theme.P(30), h - Theme.P(62));
            btnKofi.Click += delegate { OpenUrl(KofiUrl); };
            Controls.Add(btnKofi);

            if (!hasQr)
            {
                var tip = new Label();
                tip.AutoSize = false;
                tip.Location = new Point(Theme.P(30), Theme.P(132));
                tip.Size = new Size(w - Theme.P(60), Theme.P(84));
                tip.ForeColor = Theme.TextFaint;
                tip.Font = Theme.Small;
                tip.Text = I18n.T("donate.kofiHint") + Environment.NewLine + KofiUrl;
                Controls.Add(tip);
            }
        }

        private void AddQr(Image img, Rectangle box, string caption, string resName)
        {
            var pic = new QrBox();
            pic.Image = img;
            pic.Bounds = box;
            pic.Cursor = Cursors.Hand;
            pic.Click += delegate { OpenEmbedded(resName); };
            Controls.Add(pic);

            var lbl = new Label();
            lbl.AutoSize = false;
            lbl.Location = new Point(box.X, box.Bottom + Theme.P(6));
            lbl.Size = new Size(box.Width, Theme.P(18));
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.ForeColor = Theme.TextSub;
            lbl.Font = Theme.Small;
            lbl.Text = caption;
            Controls.Add(lbl);
        }

        public static void Show(IWin32Window owner)
        {
            using (var d = new DonateDialog(false))
            {
                Form f = owner as Form;
                if (f != null)
                    ModalMask.Wrap(f, delegate { return d.ShowDialog(f); });
                else
                    d.ShowDialog(owner);
            }
        }

        /// <summary>清理完成后的引导弹窗：关闭按钮为“我已捐赠”。</summary>
        public static void ShowDone(IWin32Window owner)
        {
            using (var d = new DonateDialog(true))
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
                using (var d = new DonateDialog(false))
                {
                    d.Show();
                    Application.DoEvents();
                    d.Refresh();
                    Application.DoEvents();
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
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
        // ------------------------------------------------------------ resources
        private static byte[] ResourceBytes(string name)
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream(name))
                {
                    if (s == null) return null;
                    using (var ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
            catch { return null; }
        }

        internal static Image LoadImage(string name)        {
            try
            {
                byte[] data = ResourceBytes(name);
                if (data == null) return null;
                using (var ms = new MemoryStream(data))
                using (var src = Image.FromStream(ms))
                    return new Bitmap(src);
            }
            catch { return null; }
        }

        /// <summary>点收款码：把内嵌原图释放到临时目录，用系统看图工具打开（可放大）。</summary>
        internal static void OpenEmbedded(string name)
        {
            try
            {
                byte[] data = ResourceBytes(name);
                if (data == null) return;
                string dir = Path.Combine(Path.GetTempPath(), "DiskMate-donate");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, name);
                File.WriteAllBytes(file, data);
                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            }
            catch { }
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }
    }

    /// <summary>等比缩放、居中、圆角描边的收款码显示控件。</summary>
    internal class QrBox : Control
    {
        public Image Image;

        public QrBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            if (Image == null) return;

            var box = new Rectangle(Theme.P(0), Theme.P(0), Width, Height);
            using (GraphicsPath p = Theme.Round(box, 10))
            {
                using (var b = new SolidBrush(Color.White)) g.FillPath(b, p);
                g.SetClip(p);
                // 等比缩放填满，避免变形
                float scale = Math.Min((float)Width / Image.Width, (float)Height / Image.Height);
                int dw = Math.Max(1, (int)(Image.Width * scale));
                int dh = Math.Max(1, (int)(Image.Height * scale));
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Image, new Rectangle((Width - dw) / 2, (Height - dh) / 2, dw, dh));
                g.ResetClip();
                using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, p);
            }
        }
    }
}
