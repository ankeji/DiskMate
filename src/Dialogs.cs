using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DiskMate
{
    /// <summary>无边框圆角弹窗基类：可拖动、Esc 关闭、绘制卡片背景。</summary>
    internal class DialogShell : Form
    {
        protected Panel body;
        public int Radius = 14;

        public DialogShell()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.Surface;
            DoubleBuffered = true;
            Font = Theme.Base;
            KeyPreview = true;
            Padding = new Padding(0);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRegion();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRegion();
        }

        private void ApplyRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (var p = Theme.Round(new Rectangle(0, 0, Width, Height), Radius))
                Region = new Region(p);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(Theme.Surface))
                e.Graphics.FillRectangle(b, ClientRectangle);
            using (var pen = new Pen(Theme.Border))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y < 84)
            {
                Native.ReleaseCapture();
                Native.SendMessage(Handle, Native.WM_NCLBUTTONDOWN, (IntPtr)Native.HTCAPTION, IntPtr.Zero);
            }
            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            base.OnKeyDown(e);
        }
    }

    // ============================================================ message dialog
    internal class MessageDialog : DialogShell
    {
        private MessageDialog(string title, string text, bool danger, bool withCancel, string okText)
        {
            int w = 460;
            int textWidth = w - 72;
            int textHeight;
            using (var g = CreateGraphics())
                textHeight = TextRenderer.MeasureText(g, text, Theme.Base, new Size(textWidth, 2000),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;

            int h = 96 + textHeight + 84;
            if (h < 240) h = 240;
            ClientSize = new Size(w, h);
            Text = title;

            var lblTitle = new Label();
            lblTitle.AutoSize = false;
            lblTitle.Location = new Point(28, 26);
            lblTitle.Size = new Size(w - 56, 30);
            lblTitle.Font = new Font(Theme.Family, 13f, FontStyle.Bold);
            lblTitle.ForeColor = Theme.Text;
            lblTitle.Text = title;
            Controls.Add(lblTitle);

            var lblText = new Label();
            lblText.AutoSize = false;
            lblText.Location = new Point(28, 66);
            lblText.Size = new Size(textWidth, textHeight + 4);
            lblText.ForeColor = Theme.TextSub;
            lblText.Text = text;
            Controls.Add(lblText);

            int by = h - 60;
            var ok = new FlatButton();
            ok.Kind = danger ? BtnKind.Danger : BtnKind.Primary;
            ok.Text = okText;
            ok.Size = new Size(112, 38);
            ok.Location = new Point(w - 28 - 112, by);
            ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(ok);

            if (withCancel)
            {
                var cancel = new FlatButton();
                cancel.Kind = BtnKind.Secondary;
                cancel.Text = I18n.T("common.cancel");
                cancel.Size = new Size(92, 38);
                cancel.Location = new Point(ok.Left - 12 - 92, by);
                cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
                Controls.Add(cancel);
            }
        }

        public static bool Ask(IWin32Window owner, string title, string text, string okText, bool danger)
        {
            using (var d = new MessageDialog(title, text, danger, true, okText))
                return d.ShowDialog(owner) == DialogResult.OK;
        }

        public static void Info(IWin32Window owner, string title, string text)
        {
            using (var d = new MessageDialog(title, text, false, false, I18n.T("common.gotit")))
                d.ShowDialog(owner);
        }
    }

    // ============================================================ confirm dialog
    internal class ConfirmDialog : DialogShell
    {
        public static bool ShowClean(IWin32Window owner, List<CleanItem> items, long total, int mid, int high)
        {
            using (var d = new ConfirmDialog())
            {
                d.BuildClean(items, total, mid, high);
                return d.ShowDialog(owner) == DialogResult.OK;
            }
        }

        private void BuildClean(List<CleanItem> items, long total, int mid, int high)
        {
            int w = 620, h = 560;
            ClientSize = new Size(w, h);
            Text = I18n.T("clean.go");

            var t = new Label();
            t.AutoSize = false;
            t.Location = new Point(28, 26);
            t.Size = new Size(w - 56, 30);
            t.Font = new Font(Theme.Family, 14f, FontStyle.Bold);
            t.ForeColor = Theme.Text;
            t.Text = I18n.F("clean.title", items.Count);
            Controls.Add(t);

            var sub = new Label();
            sub.AutoSize = false;
            sub.Location = new Point(29, 64);
            sub.Size = new Size(w - 56, 22);
            sub.ForeColor = Theme.TextSub;
            sub.Text = I18n.F("clean.sub", Theme.Fmt(total));
            Controls.Add(sub);

            var card = new CardPanel();
            card.Location = new Point(28, 98);
            card.Size = new Size(w - 56, 300);
            card.Shadow = false;
            Controls.Add(card);

            var list = new ItemList();
            list.ShowCheckBoxes = false;
            list.Dock = DockStyle.Fill;
            list.Columns[0].Width = 300;
            list.Columns[1].Width = 90;
            list.Columns[2].Width = 90;
            list.Columns[3].Width = 76;
            card.Padding = new Padding(2, 4, 2, 4);
            var host = new Panel();
            host.Dock = DockStyle.Fill;
            host.BackColor = Theme.Surface;
            card.Controls.Add(host);
            list.Dock = DockStyle.Fill;
            host.Controls.Add(list);

            foreach (CleanItem it in items)
            {
                var lvi = new ListViewItem(new[] { it.Name, it.Category, Theme.Fmt(it.Size), Theme.RiskText(it.Risk) });
                lvi.Tag = it;
                lvi.ImageIndex = 0;
                list.Items.Add(lvi);
            }

            var warn = new Label();
            warn.AutoSize = false;
            warn.Location = new Point(28, 410);
            warn.Size = new Size(w - 56, 44);
            warn.ForeColor = Theme.TextFaint;
            if (high > 0)
            {
                warn.ForeColor = Theme.High;
                warn.Text = I18n.F("clean.warnHigh", high);
            }
            else if (mid > 0)
            {
                warn.ForeColor = Theme.Mid;
                warn.Text = I18n.F("clean.warnMid", mid);
            }
            else
            {
                warn.Text = I18n.T("clean.allLow");
            }
            Controls.Add(warn);

            var tip = new Label();
            tip.AutoSize = false;
            tip.Location = new Point(28, 440);
            tip.Size = new Size(w - 56, 24);
            tip.ForeColor = Theme.TextFaint;
            tip.Font = Theme.Small;
            tip.Text = I18n.T("clean.tip");
            Controls.Add(tip);

            var cancel = new FlatButton();
            cancel.Kind = BtnKind.Secondary;
            cancel.Text = I18n.T("clean.stay");
            cancel.Size = new Size(96, 40);
            cancel.Location = new Point(w - 28 - 132 - 12 - 96, h - 64);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            var ok = new FlatButton();
            ok.Kind = high > 0 ? BtnKind.Danger : BtnKind.Primary;
            ok.Text = I18n.T("clean.go");
            ok.Size = new Size(132, 40);
            ok.Location = new Point(w - 28 - 132, h - 64);
            ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(ok);
        }
    }
}
