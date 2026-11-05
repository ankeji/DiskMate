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
            AutoScaleMode = AutoScaleMode.None;
            Padding = new Padding(Theme.P(0));
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
            using (var p = Theme.Round(new Rectangle(Theme.P(0), Theme.P(0), Width, Height), Radius))
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
            int w = Theme.P(460);
            int textWidth = w - Theme.P(72);
            int textHeight;
            using (var g = CreateGraphics())
                textHeight = TextRenderer.MeasureText(g, text, Theme.Base, new Size(textWidth, Theme.P(2000)),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;

            int h = Theme.P(96) + textHeight + Theme.P(84);
            if (h < 240) h = 240;
            ClientSize = new Size(w, h);
            Text = title;

            var lblTitle = new Label();
            lblTitle.AutoSize = false;
            lblTitle.Location = new Point(Theme.P(28), Theme.P(26));
            lblTitle.Size = new Size(w - Theme.P(56), Theme.P(30));
            lblTitle.Font = Theme.F(13f, FontStyle.Bold);
            lblTitle.ForeColor = Theme.Text;
            lblTitle.Text = title;
            Controls.Add(lblTitle);

            var lblText = new Label();
            lblText.AutoSize = false;
            lblText.Location = new Point(Theme.P(28), Theme.P(66));
            lblText.Size = new Size(textWidth, textHeight + Theme.P(4));
            lblText.ForeColor = Theme.TextSub;
            lblText.Text = text;
            Controls.Add(lblText);

            int by = h - 60;
            var ok = new FlatButton();
            ok.Kind = danger ? BtnKind.Danger : BtnKind.Primary;
            ok.Text = okText;
            ok.Size = new Size(Theme.P(112), Theme.P(38));
            ok.Location = new Point(w - Theme.P(28) - Theme.P(112), by);
            ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(ok);

            if (withCancel)
            {
                var cancel = new FlatButton();
                cancel.Kind = BtnKind.Secondary;
                cancel.Text = I18n.T("common.cancel");
                cancel.Size = new Size(Theme.P(92), Theme.P(38));
                cancel.Location = new Point(ok.Left - Theme.P(12) - Theme.P(92), by);
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
            int w = Theme.P(620), h = Theme.P(560);
            ClientSize = new Size(w, h);
            Text = I18n.T("clean.go");

            var t = new Label();
            t.AutoSize = false;
            t.Location = new Point(Theme.P(28), Theme.P(26));
            t.Size = new Size(w - Theme.P(56), Theme.P(30));
            t.Font = Theme.F(14f, FontStyle.Bold);
            t.ForeColor = Theme.Text;
            t.Text = I18n.F("clean.title", items.Count);
            Controls.Add(t);

            var sub = new Label();
            sub.AutoSize = false;
            sub.Location = new Point(Theme.P(29), Theme.P(64));
            sub.Size = new Size(w - Theme.P(56), Theme.P(22));
            sub.ForeColor = Theme.TextSub;
            sub.Text = I18n.F("clean.sub", Theme.Fmt(total));
            Controls.Add(sub);

            var card = new CardPanel();
            card.Location = new Point(Theme.P(28), Theme.P(98));
            card.Size = new Size(w - Theme.P(56), Theme.P(300));
            card.Shadow = false;
            Controls.Add(card);

            var list = new ItemList();
            list.ShowCheckBoxes = false;
            list.Dock = DockStyle.Fill;
            list.Columns[0].Width = Theme.P(300);
            list.Columns[1].Width = Theme.P(90);
            list.Columns[2].Width = Theme.P(90);
            list.Columns[3].Width = Theme.P(76);
            card.Padding = new Padding(Theme.P(2), Theme.P(4), Theme.P(2), Theme.P(4));
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
            warn.Location = new Point(Theme.P(28), Theme.P(410));
            warn.Size = new Size(w - Theme.P(56), Theme.P(44));
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
            tip.Location = new Point(Theme.P(28), Theme.P(440));
            tip.Size = new Size(w - Theme.P(56), Theme.P(24));
            tip.ForeColor = Theme.TextFaint;
            tip.Font = Theme.Small;
            tip.Text = I18n.T("clean.tip");
            Controls.Add(tip);

            var cancel = new FlatButton();
            cancel.Kind = BtnKind.Secondary;
            cancel.Text = I18n.T("clean.stay");
            cancel.Size = new Size(Theme.P(96), Theme.P(40));
            cancel.Location = new Point(w - Theme.P(28) - Theme.P(132) - Theme.P(12) - Theme.P(96), h - Theme.P(64));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            var ok = new FlatButton();
            ok.Kind = high > 0 ? BtnKind.Danger : BtnKind.Primary;
            ok.Text = I18n.T("clean.go");
            ok.Size = new Size(Theme.P(132), Theme.P(40));
            ok.Location = new Point(w - Theme.P(28) - Theme.P(132), h - Theme.P(64));
            ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(ok);
        }
    }
}
