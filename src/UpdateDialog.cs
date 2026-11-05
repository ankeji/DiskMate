using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskMate
{
    /// <summary>更新弹窗：版本对比 + 更新说明 + 下载进度 + 下载/安装。</summary>
    internal class UpdateDialog : DialogShell
    {
        private readonly UpdateInfo info;
        private Label lblStatus;
        private ThinProgress bar;
        private FlatButton btnInstall, btnLater;
        private volatile bool downloading;

        private UpdateDialog(UpdateInfo i)
        {
            info = i;
            int w = 520;
            ClientSize = new Size(w, 320);
            Text = I18n.F("update.newTitle", info.Version);

            var title = new Label();
            title.AutoSize = false;
            title.Location = new Point(30, 26);
            title.Size = new Size(w - 60, 30);
            title.Font = new Font(Theme.Family, 14f, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.Text = I18n.F("update.newTitle", info.Version);
            Controls.Add(title);

            var sub = new Label();
            sub.AutoSize = false;
            sub.Location = new Point(30, 60);
            sub.Size = new Size(w - 60, 20);
            sub.ForeColor = Theme.TextSub;
            sub.Font = Theme.Small;
            sub.Text = I18n.F("update.currentTo", AppInfo.Version, info.Version);
            Controls.Add(sub);

            var cap = new Label();
            cap.AutoSize = false;
            cap.Location = new Point(30, 94);
            cap.Size = new Size(w - 60, 18);
            cap.ForeColor = Theme.TextFaint;
            cap.Font = Theme.Small;
            cap.Text = I18n.T("update.notes");
            Controls.Add(cap);

            var notes = new TextBox();
            notes.Multiline = true;
            notes.ReadOnly = true;
            notes.ScrollBars = ScrollBars.Vertical;
            notes.BorderStyle = BorderStyle.FixedSingle;
            notes.BackColor = Theme.Bg;
            notes.ForeColor = Theme.TextSub;
            notes.Font = Theme.Base;
            notes.Location = new Point(30, 116);
            notes.Size = new Size(w - 60, 108);
            notes.Text = string.IsNullOrEmpty(info.Notes) ? "-" : info.Notes;
            Controls.Add(notes);

            lblStatus = new Label();
            lblStatus.AutoSize = false;
            lblStatus.Location = new Point(30, 232);
            lblStatus.Size = new Size(w - 60, 20);
            lblStatus.ForeColor = Theme.TextFaint;
            lblStatus.Font = Theme.Small;
            lblStatus.Text = "";
            Controls.Add(lblStatus);

            bar = new ThinProgress();
            bar.Location = new Point(30, 254);
            bar.Size = new Size(w - 60, 6);
            bar.Visible = false;
            Controls.Add(bar);

            btnLater = new FlatButton();
            btnLater.Kind = BtnKind.Secondary;
            btnLater.Text = I18n.T("update.later");
            btnLater.Size = new Size(96, 38);
            btnLater.Location = new Point(w - 30 - 160 - 12 - 96, 268);
            btnLater.Click += delegate { Close(); };
            Controls.Add(btnLater);

            btnInstall = new FlatButton();
            btnInstall.Kind = BtnKind.Primary;
            btnInstall.Text = I18n.T("update.install");
            btnInstall.Size = new Size(160, 38);
            btnInstall.Location = new Point(w - 30 - 160, 268);
            btnInstall.Click += delegate { StartDownload(); };
            Controls.Add(btnInstall);
        }

        /// <summary>自检用：不开模态，把弹窗渲染成 PNG 后关闭。</summary>
        internal static void RenderTo(string path, UpdateInfo info)
        {
            using (var d = new UpdateDialog(info))
            {
                d.Show();
                Application.DoEvents();
                d.Refresh();
                Application.DoEvents();
                using (var bmp = new Bitmap(d.Width, d.Height))
                {
                    d.DrawToBitmap(bmp, new Rectangle(0, 0, d.Width, d.Height));
                    bmp.Save(path, ImageFormat.Png);
                }
                d.Close();
            }
        }
        public static void Show(IWin32Window owner, UpdateInfo info)
        {
            using (var d = new UpdateDialog(info))
                d.ShowDialog(owner);
        }

        private void StartDownload()
        {
            if (downloading) return;
            downloading = true;
            btnInstall.Enabled = false;
            btnLater.Enabled = false;
            btnInstall.Invalidate();
            btnLater.Invalidate();
            bar.Visible = true;
            bar.Value = 0;
            lblStatus.Text = I18n.T("update.downloading0");

            Task.Factory.StartNew(delegate
            {
                string err;
                string path = UpdateService.Download(info,
                    delegate(long done, long total)
                    {
                        if (total <= 0) return;
                        int pct = (int)(done * 100L / total);
                        Ui(delegate
                        {
                            bar.Value = pct / 100.0;
                            lblStatus.Text = I18n.F("update.downloading", pct, Theme.Fmt(done), Theme.Fmt(total));
                        });
                    }, null, out err);

                if (path == null)
                {
                    Ui(delegate { Fail(I18n.F("update.downloadFailed", err)); });
                    return;
                }

                Ui(delegate { lblStatus.Text = I18n.T("update.verifying"); bar.Value = 1; });
                string verr;
                if (!UpdateService.Verify(path, info, out verr))
                {
                    Ui(delegate { Fail(I18n.F("update.hashFailed", verr)); });
                    return;
                }

                string ierr;
                if (!UpdateService.RunInstaller(path, out ierr))
                {
                    Ui(delegate { Fail(I18n.F("update.downloadFailed", ierr)); });
                    return;
                }

                Ui(delegate
                {
                    lblStatus.Text = I18n.T("update.installing");
                    btnLater.Visible = false;
                    btnInstall.Visible = false;
                    Application.DoEvents();
                    Application.Exit();
                });
            });
        }

        private void Fail(string message)
        {
            downloading = false;
            bar.Visible = false;
            lblStatus.ForeColor = Theme.High;
            lblStatus.Text = message;
            btnInstall.Enabled = true;
            btnLater.Enabled = true;
            btnInstall.Invalidate();
            btnLater.Invalidate();
        }

        private void Ui(Action a)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(a); }
                catch (InvalidOperationException) { }
                return;
            }
            a();
        }
    }
}
