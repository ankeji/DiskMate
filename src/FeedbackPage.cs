using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskMate
{
    public partial class MainForm
    {
        // ---- 问题反馈页
        private Panel pageFeedback;
        private ListView lvFb;
        private Panel fbDetail;
        private TextBox txtFbDetail;
        private Label lblFbDetailHead, lblFbHint, lblFbEmpty;
        private FlatButton btnFbNew, btnFbRefresh;
        private ComboBox cmbFbStatus;
        private bool fbLoading;
        private static readonly string[] FbStatusKeys = { "all", "open", "processing", "resolved" };

        // ============================================================ feedback page
        private void BuildFeedbackPage()
        {
            pageFeedback.BackColor = Theme.Bg;

            var bar = new Panel();
            bar.BackColor = Theme.Bg;
            pageFeedback.Controls.Add(bar);

            btnFbNew = new FlatButton();
            btnFbNew.Kind = BtnKind.Primary;
            Txt(btnFbNew, "fb.new");
            btnFbNew.Size = new Size(Theme.P(132), Theme.P(36));
            btnFbNew.Location = new Point(Theme.P(0), Theme.P(10));
            btnFbNew.Click += delegate { NewFeedback(); };
            bar.Controls.Add(btnFbNew);

            btnFbRefresh = new FlatButton();
            btnFbRefresh.Kind = BtnKind.Secondary;
            Txt(btnFbRefresh, "fb.refresh");
            btnFbRefresh.Size = new Size(Theme.P(96), Theme.P(36));
            btnFbRefresh.Location = new Point(Theme.P(144), Theme.P(10));
            btnFbRefresh.Click += delegate { RefreshFeedback(true); };
            bar.Controls.Add(btnFbRefresh);

            cmbFbStatus = new ComboBox();
            cmbFbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFbStatus.FlatStyle = FlatStyle.Flat;
            cmbFbStatus.Font = Theme.Base;
            cmbFbStatus.ItemHeight = Theme.P(22);
            cmbFbStatus.Size = new Size(Theme.P(132), Theme.P(30));
            cmbFbStatus.Location = new Point(Theme.P(252), Theme.P(14));
            FillFbStatusOptions();
            cmbFbStatus.SelectedIndexChanged += delegate { RefreshFeedback(true); };
            bar.Controls.Add(cmbFbStatus);

            lblFbHint = new Label();
            lblFbHint.AutoSize = false;
            lblFbHint.AutoEllipsis = true;
            lblFbHint.Location = new Point(Theme.P(398), Theme.P(14));
            lblFbHint.Size = new Size(Theme.P(420), Theme.P(30));
            lblFbHint.ForeColor = Theme.TextSub;
            lblFbHint.Font = Theme.Small;
            Txt(lblFbHint, "fb.idle");
            bar.Controls.Add(lblFbHint);

            var card = new CardPanel();
            card.Padding = new Padding(Theme.P(1));
            card.FillColor = Theme.Surface;
            card.BorderColor = Theme.Border;
            pageFeedback.Controls.Add(card);

            lvFb = new ListView();
            lvFb.View = View.Details;
            lvFb.FullRowSelect = true;
            lvFb.BorderStyle = BorderStyle.None;
            lvFb.Font = Theme.Base;
            lvFb.BackColor = Theme.Surface;
            lvFb.HideSelection = false;
            var rows = new ImageList();
            rows.ImageSize = new Size(1, Theme.P(30));
            rows.ColorDepth = ColorDepth.Depth32Bit;
            lvFb.SmallImageList = rows;
            lvFb.Columns.Add(I18n.T("fb.col.status"), Theme.P(96));
            lvFb.Columns.Add(I18n.T("fb.col.title"), Theme.P(300));
            lvFb.Columns.Add(I18n.T("fb.col.author"), Theme.P(150));
            lvFb.Columns.Add(I18n.T("fb.col.time"), Theme.P(160));
            lvFb.SelectedIndexChanged += delegate { UpdateFbDetail(); };

            // 状态列自绘：待处理=红、处理中=橙、已解决=绿（圆点 + 同色文字）
            lvFb.OwnerDraw = true;
            lvFb.DrawColumnHeader += delegate (object s, DrawListViewColumnHeaderEventArgs e) { e.DrawDefault = true; };
            lvFb.DrawSubItem += delegate (object s, DrawListViewSubItemEventArgs e)
            {
                string status = e.SubItem == null ? null : e.SubItem.Tag as string;
                if (e.ColumnIndex != 0 || string.IsNullOrEmpty(status))
                {
                    e.DrawDefault = true;
                    return;
                }

                Color c = FbStatusColor(status);
                bool selected = (e.ItemState & ListViewItemStates.Selected) != 0;
                Rectangle b = e.Bounds;

                using (var bg = new SolidBrush(selected ? SystemColors.Highlight : lvFb.BackColor))
                    e.Graphics.FillRectangle(bg, b);

                int dot = Theme.P(7);
                int dx = b.Left + Theme.P(8);
                int dy = b.Top + (b.Height - dot) / 2;
                using (var brush = new SolidBrush(c)) e.Graphics.FillEllipse(brush, dx, dy, dot, dot);

                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, Theme.Small,
                    new Rectangle(dx + dot + Theme.P(6), b.Top, b.Width - dot - Theme.P(16), b.Height),
                    selected ? SystemColors.HighlightText : c,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            };
            card.Controls.Add(lvFb);

            lblFbEmpty = new Label();
            lblFbEmpty.AutoSize = false;
            lblFbEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblFbEmpty.ForeColor = Theme.TextFaint;
            lblFbEmpty.Font = Theme.Base;
            lblFbEmpty.Text = I18n.T("fb.empty");
            lblFbEmpty.Visible = false;
            card.Controls.Add(lblFbEmpty);
            lblFbEmpty.BringToFront();

            fbDetail = new Panel();
            fbDetail.BackColor = Theme.Surface;
            card.Controls.Add(fbDetail);

            lblFbDetailHead = new Label();
            lblFbDetailHead.AutoSize = false;
            lblFbDetailHead.AutoEllipsis = true;
            lblFbDetailHead.Font = Theme.F(10.5f, FontStyle.Bold);
            lblFbDetailHead.ForeColor = Theme.Text;
            lblFbDetailHead.Text = I18n.T("fb.detail.hint");
            fbDetail.Controls.Add(lblFbDetailHead);

            txtFbDetail = new TextBox();
            txtFbDetail.Multiline = true;
            txtFbDetail.ReadOnly = true;
            txtFbDetail.BorderStyle = BorderStyle.None;
            txtFbDetail.BackColor = Theme.Surface;
            txtFbDetail.ForeColor = Theme.TextSub;
            txtFbDetail.Font = Theme.Base;
            txtFbDetail.ScrollBars = ScrollBars.Vertical;
            txtFbDetail.TabStop = false;
            fbDetail.Controls.Add(txtFbDetail);

            pageFeedback.Resize += delegate { LayoutFeedbackPage(); };
            LayoutFeedbackPage();
        }

        private static Color FbStatusColor(string status)
        {
            switch (status)
            {
                case "processing": return Theme.Mid;
                case "resolved": return Theme.Low;
                default: return Theme.High;
            }
        }

        private void FillFbStatusOptions()
        {
            int keep = cmbFbStatus.SelectedIndex;
            cmbFbStatus.Items.Clear();
            cmbFbStatus.Items.Add(I18n.T("fb.status.all"));
            cmbFbStatus.Items.Add(I18n.T("fb.status.open"));
            cmbFbStatus.Items.Add(I18n.T("fb.status.processing"));
            cmbFbStatus.Items.Add(I18n.T("fb.status.resolved"));
            cmbFbStatus.SelectedIndex = keep >= 0 && keep < cmbFbStatus.Items.Count ? keep : 0;
        }

        private void LayoutFeedbackPage()
        {
            if (pageFeedback == null || lvFb == null) return;
            int w = pageFeedback.Width;
            int h = pageFeedback.Height;
            var bar = btnFbNew == null ? null : btnFbNew.Parent as Panel;
            if (bar != null) bar.SetBounds(Theme.P(0), Theme.P(0), w, Theme.P(52));

            var card = lvFb.Parent;
            int cardH = Math.Max(Theme.P(220), h - Theme.P(52));
            card.SetBounds(Theme.P(0), Theme.P(52), w, cardH);

            int detailH = Theme.P(176);
            int listH = Math.Max(Theme.P(120), cardH - detailH - Theme.P(26));
            lvFb.SetBounds(Theme.P(14), Theme.P(12), Math.Max(Theme.P(200), card.Width - Theme.P(28)), listH);
            lblFbEmpty.SetBounds(lvFb.Left, lvFb.Top, lvFb.Width, lvFb.Height);

            fbDetail.SetBounds(Theme.P(14), lvFb.Bottom + Theme.P(10),
                Math.Max(Theme.P(200), card.Width - Theme.P(28)), detailH);
            lblFbDetailHead.SetBounds(Theme.P(2), Theme.P(0), Math.Max(Theme.P(120), fbDetail.Width - Theme.P(4)), Theme.P(24));
            txtFbDetail.SetBounds(Theme.P(2), Theme.P(26), Math.Max(Theme.P(120), fbDetail.Width - Theme.P(6)), detailH - Theme.P(30));

            if (lblFbHint != null)
                lblFbHint.Width = Math.Max(Theme.P(120), w - lblFbHint.Left - Theme.P(4));
        }

        /// <summary>拉一次反馈列表（后台线程），force=false 时只在为空或首次进入时拉。</summary>
        private void RefreshFeedback(bool force)
        {
            if (fbLoading) return;
            if (!force && lvFb != null && lvFb.Items.Count > 0) return;
            fbLoading = true;
            if (lblFbHint != null) lblFbHint.Text = I18n.T("fb.loading");

            string status = cmbFbStatus == null ? "all" : FbStatusKeys[Math.Max(0, Math.Min(cmbFbStatus.SelectedIndex, FbStatusKeys.Length - 1))];

            Task.Factory.StartNew(delegate
            {
                string err;
                List<FeedbackItem> list = FeedbackService.Load(status, out err);
                Ui(delegate
                {
                    fbLoading = false;
                    if (list == null)
                    {
                        FillFbList(new List<FeedbackItem>());
                        lblFbHint.Text = string.IsNullOrEmpty(err) ? I18n.T("fb.loadFail") : err;
                        return;
                    }
                    FillFbList(list);
                });
            });
        }

        private void FillFbList(List<FeedbackItem> list)
        {
            if (lvFb == null) return;
            int keepId = 0;
            if (lvFb.SelectedItems.Count > 0) keepId = (int)lvFb.SelectedItems[0].Tag;

            FeedbackService.Cached.Clear();
            FeedbackService.Cached.AddRange(list);

            lvFb.BeginUpdate();
            lvFb.Items.Clear();
            foreach (FeedbackItem f in list)
            {
                var it = new ListViewItem(FeedbackService.StatusName(f.Status));
                it.SubItems[0].Tag = f.Status;   // 自绘状态列时取色
                it.SubItems.Add(f.Title);
                it.SubItems.Add(f.Mine ? f.Author + " · " + I18n.T("fb.mine") : f.Author);
                it.SubItems.Add(f.CreatedAt);
                it.Tag = f.Id;
                it.ForeColor = f.Status == "resolved" ? Theme.TextSub : Theme.Text;
                lvFb.Items.Add(it);
                if (f.Id == keepId) it.Selected = true;
            }
            lvFb.EndUpdate();

            lblFbEmpty.Visible = list.Count == 0;
            if (lblFbHint != null)
                lblFbHint.Text = I18n.F("fb.count", list.Count);

            if (list.Count > 0 && lvFb.SelectedItems.Count == 0) lvFb.Items[0].Selected = true;
            UpdateFbDetail();
        }

        private FeedbackItem SelectedFeedback()
        {
            if (lvFb == null || lvFb.SelectedItems.Count == 0) return null;
            int id = (int)lvFb.SelectedItems[0].Tag;
            foreach (FeedbackItem f in FeedbackService.Cached)
                if (f.Id == id) return f;
            return null;
        }

        private void UpdateFbDetail()
        {
            if (txtFbDetail == null || lblFbDetailHead == null) return;
            FeedbackItem f = SelectedFeedback();
            if (f == null)
            {
                lblFbDetailHead.Text = I18n.T("fb.detail.hint");
                txtFbDetail.Text = "";
                return;
            }

            lblFbDetailHead.Text = "[" + FeedbackService.StatusName(f.Status) + "] " + f.Title
                                 + "    " + f.Author + " · " + f.CreatedAt;
            string body = f.Content;
            if (!string.IsNullOrEmpty(f.Reply))
                body += Environment.NewLine + Environment.NewLine + I18n.T("fb.reply") + "：" + f.Reply;
            txtFbDetail.Text = body;
            txtFbDetail.SelectionStart = 0;
            txtFbDetail.SelectionLength = 0;
        }

        /// <summary>写反馈：后台可配置是否要求登录；提交成功后刷新列表并选中新条目。</summary>
        private void NewFeedback()
        {
            if (!RequireLogin(FeatureGate.Feedback)) return;
            if (FeedbackDialog.Show(this))
            {
                ShowBanner(BannerKind.Success, I18n.T("fb.submitted"), 5);
                RefreshFeedback(true);
            }
        }

        /// <summary>自检用：切到问题反馈页</summary>
        public void TestShowFeedback()
        {
            ShowPage(4);
            Application.DoEvents();
        }

        /// <summary>调试用：弹一条提示，用于验证悬浮条不会挤压页面</summary>
        public void TestShowBanner()
        {
            ShowBanner(BannerKind.Success, "提示条是悬浮的，不会把页面往下挤", 9);
            Application.DoEvents();
        }

        /// <summary>调试用：把反馈页各控件的位置打出来，便于检查排版</summary>
        public string DescribeFeedbackLayout()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("page=" + pageFeedback.Width + "x" + pageFeedback.Height);
            Control[] all = { btnFbNew, btnFbRefresh, cmbFbStatus, lblFbHint, lvFb, lblFbEmpty, fbDetail, lblFbDetailHead, txtFbDetail };
            string[] names = { "btnNew", "btnRefresh", "cmbStatus", "hint", "list", "empty", "detail", "detailHead", "detailBody" };
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                Rectangle r = all[i].Bounds;
                sb.AppendLine(names[i] + "=" + r.X + "," + r.Y + "," + r.Width + "," + r.Height
                              + (all[i].Visible ? "" : " (hidden)"));
            }
            sb.AppendLine("columns=" + (lvFb.Columns.Count > 0 ? string.Join("/", ColumnTexts()) : "-"));
            sb.AppendLine("rows=" + lvFb.Items.Count + " detail=" + txtFbDetail.TextLength + " chars");
            if (bannerHost != null) sb.AppendLine("banner=" + bannerHost.Bounds + (bannerHost.Visible ? "" : " (hidden)"));
            return sb.ToString();
        }

        private string[] ColumnTexts()
        {
            var list = new List<string>();
            foreach (ColumnHeader c in lvFb.Columns) list.Add(c.Text + ":" + c.Width);
            return list.ToArray();
        }

        /// <summary>自检用：渲染当前反馈页（不联网时列表为空）</summary>
        public void TestFeedbackOffline()
        {
            Api.Offline = true;
            RefreshFeedback(true);
        }
    }
}
