using System;
using System.Drawing;
using System.Windows.Forms;

namespace DiskMate
{
    /// <summary>
    /// 联系我们：显示内嵌的微信二维码。点图片会用系统看图工具打开原图，方便放大扫码。
    /// </summary>
    internal class ContactDialog : DialogShell
    {
        public const string ResName = "contact.jpg";

        private Image img;

        private ContactDialog()
        {
            int w = Theme.P(420);
            int qr = Theme.P(292);
            int top = Theme.P(104);
            int h = top + qr + Theme.P(12) + Theme.P(20) + Theme.P(66);

            ClientSize = new Size(w, h);
            Text = I18n.T("contact.title");
            img = DonateDialog.LoadImage(ResName);

            var title = new Label();
            title.AutoSize = false;
            title.Location = new Point(Theme.P(30), Theme.P(26));
            title.Size = new Size(w - Theme.P(60), Theme.P(30));
            title.Font = Theme.F(14f, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.Text = I18n.T("contact.title");
            Controls.Add(title);

            var sub = new Label();
            sub.AutoSize = false;
            sub.Location = new Point(Theme.P(30), Theme.P(58));
            sub.Size = new Size(w - Theme.P(60), Theme.P(22));
            sub.ForeColor = Theme.TextSub;
            sub.Font = Theme.Small;
            sub.Text = I18n.T("contact.sub");
            Controls.Add(sub);

            var qrBox = new QrBox();
            qrBox.Image = img;
            qrBox.Bounds = new Rectangle((w - qr) / 2, top, qr, qr);
            qrBox.Cursor = Cursors.Hand;
            qrBox.Click += delegate { DonateDialog.OpenEmbedded(ResName); };
            Controls.Add(qrBox);

            var hint = new Label();
            hint.AutoSize = false;
            hint.Location = new Point(Theme.P(30), top + qr + Theme.P(10));
            hint.Size = new Size(w - Theme.P(60), Theme.P(20));
            hint.TextAlign = ContentAlignment.MiddleCenter;
            hint.ForeColor = Theme.TextFaint;
            hint.Font = Theme.Small;
            hint.Text = I18n.T("contact.hint");
            Controls.Add(hint);

            var btnClose = new FlatButton();
            btnClose.Kind = BtnKind.Primary;
            btnClose.Text = I18n.T("contact.close");
            btnClose.Size = new Size(Theme.P(120), Theme.P(38));
            btnClose.Location = new Point((w - Theme.P(120)) / 2, h - Theme.P(58));
            btnClose.Click += delegate { Close(); };
            Controls.Add(btnClose);
        }

        public static void Show(IWin32Window owner)
        {
            using (var d = new ContactDialog())
                d.ShowDialog(owner);
        }

        /// <summary>自检用：把弹窗渲染成 PNG。</summary>
        internal static void RenderTo(string path)
        {
            try
            {
                using (var d = new ContactDialog())
                {
                    d.Show();
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
