using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DiskMate
{
    // ============================================================ card panel
    internal class CardPanel : Panel
    {
        public int Radius = 14;
        public Color BorderColor = Theme.Border;
        public Color FillColor = Theme.Surface;
        public bool Shadow = true;

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            // 子控件（Label 等）继承这个背景色，必须是卡片本身的白色，
            // 否则文字周围会出现灰色方块。圆角外的区域在 OnPaint 里用父容器颜色覆盖。
            BackColor = Theme.Surface;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            if (Shadow)
            {
                for (int i = 3; i >= 1; i--)
                    Theme.FillRound(g, new Rectangle(i, i + 1, Width - 1 - i * 2, Height - 1 - i * 2), Radius,
                        Color.FromArgb(6, 20, 40, 90));
            }
            Theme.FillRound(g, r, Radius, FillColor);
            Theme.StrokeRound(g, r, Radius, BorderColor);
        }
    }

    // ============================================================ button
    internal enum BtnKind { Primary, Secondary, Ghost, Danger, Link }

    internal class FlatButton : Button
    {
        public BtnKind Kind = BtnKind.Secondary;
        public string Icon = "";
        public bool Active;
        public int Radius = 9;
        private bool hover, down;

        public FlatButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            Font = Theme.Base;
            Cursor = Cursors.Hand;
            Size = new Size(104, 36);
            UseVisualStyleBackColor = false;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);

            Color bg, fg, border;
            switch (Kind)
            {
                case BtnKind.Primary:
                    bg = !Enabled ? Color.FromArgb(197, 208, 230) : (hover || down ? Theme.PrimaryDark : Theme.Primary);
                    fg = Color.White; border = Color.Transparent;
                    break;
                case BtnKind.Danger:
                    bg = !Enabled ? Color.FromArgb(232, 200, 202) : (hover ? Color.FromArgb(190, 32, 42) : Theme.High);
                    fg = Color.White; border = Color.Transparent;
                    break;
                case BtnKind.Ghost:
                    bg = hover ? Theme.PrimarySoft : Color.Transparent;
                    fg = Active ? Theme.Primary : Theme.TextSub;
                    border = Color.Transparent;
                    break;
                case BtnKind.Link:
                    bg = Color.Transparent;
                    fg = Enabled ? (hover ? Theme.PrimaryDark : Theme.Primary) : Theme.TextFaint;
                    border = Color.Transparent;
                    break;
                default:
                    bg = !Enabled ? Color.FromArgb(247, 248, 251) : (hover ? Color.FromArgb(245, 248, 253) : Color.White);
                    fg = Enabled ? Theme.Text : Theme.TextFaint;
                    border = Enabled ? Theme.BorderStrong : Theme.Border;
                    break;
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            if (bg != Color.Transparent) Theme.FillRound(g, rect, Radius, bg);
            if (border != Color.Transparent) Theme.StrokeRound(g, rect, Radius, border);

            bool hasIcon = !string.IsNullOrEmpty(Icon);
            Size ts = TextRenderer.MeasureText(g, Text, Font, new Size(Width, Height), TextFormatFlags.NoPadding);
            int iconW = hasIcon ? 16 : 0;
            int gap = hasIcon && Text.Length > 0 ? 8 : 0;
            int total = ts.Width + iconW + gap;
            int x = Math.Max(6, (Width - total) / 2);
            if (hasIcon)
            {
                IconPainter.Draw(g, new RectangleF(x, (Height - 16) / 2f, 16, 16), Icon, fg);
                x += iconW + gap;
            }
            if (Text.Length > 0)
                TextRenderer.DrawText(g, Text, Active ? Theme.Bold : Font,
                    new Rectangle(x, 0, Math.Max(4, Width - x - 6), Height), fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (Focused && Enabled)
                Theme.StrokeRound(g, new Rectangle(1, 1, Width - 3, Height - 3), Radius - 1, Color.FromArgb(90, Theme.Primary));
        }
    }

    // ============================================================ nav item
    internal class NavItem : Control
    {
        public bool Active;
        public string Icon = "scan";
        private bool hover;

        public NavItem()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 42;
            Cursor = Cursors.Hand;
            Font = Theme.Base;
            BackColor = Theme.Sidebar;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Sidebar);

            var r = new Rectangle(10, 1, Width - 20, Height - 2);
            if (Active) Theme.FillRound(g, r, 10, Theme.PrimarySoft);
            else if (hover) Theme.FillRound(g, r, 10, Color.FromArgb(244, 247, 252));

            if (Active)
            {
                var bar = new Rectangle(0, Height / 2 - 9, 3, 18);
                Theme.FillRound(g, bar, 2, Theme.Primary);
            }

            Color fg = Active ? Theme.Primary : (hover ? Theme.Text : Theme.TextSub);
            IconPainter.Draw(g, new RectangleF(24, (Height - 17) / 2f, 17, 17), Icon, fg);
            TextRenderer.DrawText(g, Text, Active ? Theme.Bold : Font,
                new Rectangle(52, 0, Width - 62, Height), fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    // ============================================================ badge / pill
    internal class Badge : Control
    {
        public Color Accent = Theme.Primary;
        public Color Soft = Theme.PrimarySoft;
        public bool Bordered;

        public Badge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.Small;
            Size = new Size(72, 24);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Theme.FillRound(g, r, Height / 2, Soft);
            if (Bordered) Theme.StrokeRound(g, r, Height / 2, Color.FromArgb(70, Accent));
            TextRenderer.DrawText(g, Text, Font, r, Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ============================================================ metric card
    internal class MetricCard : Panel
    {
        public string Title = "";
        public string Value = "0 B";
        public string Sub = "";
        public Color Accent = Theme.Primary;
        public bool Selected;
        public bool Clickable;
        private bool hover;

        public MetricCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Size = new Size(200, 92);
            Cursor = Cursors.Default;
        }

        public void SetClickable(bool on) { Clickable = on; Cursor = on ? Cursors.Hand : Cursors.Default; }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = Selected ? Theme.Surface : (hover && Clickable ? Color.FromArgb(252, 253, 255) : Theme.Surface);
            Theme.FillRound(g, r, 12, fill);
            Theme.StrokeRound(g, r, 12, Selected ? Accent : Theme.Border);

            if (Selected)
                Theme.FillRound(g, new Rectangle(0, 18, 3, Height - 36), 2, Accent);

            var dot = new Rectangle(20, 22, 8, 8);
            using (var b = new SolidBrush(Accent))
                g.FillEllipse(b, dot);

            TextRenderer.DrawText(g, Title, Theme.Small, new Rectangle(34, 16, Width - 44, 20), Theme.TextSub, Theme.LeftMid);
            TextRenderer.DrawText(g, Value, Theme.Num, new Rectangle(19, 36, Width - 30, 28), Theme.Text, Theme.LeftMid);
            TextRenderer.DrawText(g, Sub, Theme.Small, new Rectangle(20, Height - 30, Width - 34, 18), Theme.TextFaint, Theme.LeftMid);

            if (Selected)
                TextRenderer.DrawText(g, "筛选中", Theme.Small, new Rectangle(0, 16, Width - 18, 20), Accent, Theme.RightMid);
        }
    }

    // ============================================================ disk meter
    internal class DiskMeter : Control
    {
        public char Letter = 'C';
        public long Total, Free;

        /// <summary>行高按字体实际高度算，避免高 DPI 下文字压到进度条上。</summary>
        private static int RowH
        {
            get { return Math.Max(Theme.P(18), Theme.Small.Height + Theme.P(2)); }
        }

        /// <summary>控件建议高度（两行文字 + 进度条 + 间距）。</summary>
        public static int PreferredHeight
        {
            get { return RowH * 2 + Theme.P(16); }
        }

        public DiskMeter()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Sidebar;
            Size = new Size(Theme.P(184), PreferredHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Sidebar);

            int rowH = RowH;
            int barH = Theme.P(7);
            int gap = Theme.P(4);
            int barY = rowH + gap;
            int line2Y = barY + barH + Theme.P(3);

            if (Total <= 0)
            {
                TextRenderer.DrawText(g, I18n.F("drive.none", Letter.ToString()), Theme.Small,
                    new Rectangle(0, 0, Width, rowH), Theme.TextFaint, Theme.LeftMid);
                return;
            }

            double usedPct = (Total - Free) * 100.0 / Total;

            int letterW = Math.Min(Width, Theme.P(24));
            int pctW = Theme.P(44);
            TextRenderer.DrawText(g, Letter + ":", Theme.SmallBold,
                new Rectangle(0, 0, letterW, rowH), Theme.Text, Theme.LeftMid);
            TextRenderer.DrawText(g, I18n.F("drive.free", Theme.Fmt(Free)), Theme.Small,
                new Rectangle(letterW + Theme.P(4), 0, Math.Max(0, Width - letterW - pctW - Theme.P(8)), rowH),
                Theme.TextSub, Theme.LeftMid);
            TextRenderer.DrawText(g, usedPct.ToString("N0") + "%", Theme.Small,
                new Rectangle(Math.Max(0, Width - pctW), 0, pctW, rowH), Theme.TextFaint, Theme.RightMid);

            var bar = new Rectangle(0, barY, Width, barH);
            Theme.FillRound(g, bar, Theme.P(4), Color.FromArgb(238, 241, 246));
            int w = (int)Math.Round(Width * Math.Min(1.0, usedPct / 100.0));
            if (w > Theme.P(3))
            {
                int inset = Theme.P(1);
                Theme.FillRound(g, new Rectangle(inset, barY + inset, Math.Max(1, w - inset * 2), Math.Max(1, barH - inset * 2)),
                    Theme.P(3), usedPct > 90 ? Theme.High : (usedPct > 75 ? Theme.Mid : Theme.Primary));
            }

            TextRenderer.DrawText(g, I18n.F("drive.total", Theme.Fmt(Total)), Theme.Small,
                new Rectangle(0, line2Y, Width, rowH), Theme.TextFaint, Theme.LeftMid);
        }
    }
    // ============================================================ spinner
    internal class Spinner : Control
    {
        private Timer timer;
        private float angle;

        public Spinner()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Size = new Size(44, 44);
        }

        public void Start()
        {
            if (timer == null)
            {
                timer = new Timer();
                timer.Interval = 33;
                timer.Tick += delegate { angle = (angle + 12f) % 360f; Invalidate(); };
            }
            timer.Start();
        }

        public void Stop() { if (timer != null) timer.Stop(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            int d = Math.Min(Width, Height) - 8;
            if (d <= 2) return;
            var r = new Rectangle(4, 4, d, d);
            using (var pen = new Pen(Color.FromArgb(228, 233, 242), 3f))
                g.DrawArc(pen, r, 0, 360);
            for (int i = 0; i < 6; i++)
            {
                int alpha = 40 + i * 30;
                using (var pen = new Pen(Color.FromArgb(Math.Min(255, alpha), Theme.Primary), 3f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, r, angle + i * 7f, 22f);
                }
            }
        }
    }

    // ============================================================ thin progress
    internal class ThinProgress : Control
    {
        private double value;
        public bool Marquee;

        public ThinProgress()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Size = new Size(320, 8);
        }

        public double Value
        {
            get { return value; }
            set { this.value = Math.Max(0, Math.Min(1, value)); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            var track = new Rectangle(0, 0, Width, Height);
            Theme.FillRound(g, track, Height / 2, Color.FromArgb(232, 236, 243));
            int w = (int)Math.Round((Width - 2) * (Marquee ? 0.35 : value));
            if (w < 4) return;
            var fill = new Rectangle(1, 1, w, Height - 2);
            Theme.FillRoundGrad(g, fill, (Height - 2) / 2, Color.FromArgb(96, 138, 255), Theme.Primary);
        }
    }

    // ============================================================ banner
    internal enum BannerKind { Info, Success, Warn, Error }

    internal class Banner : Control
    {
        public BannerKind Kind = BannerKind.Info;
        public string Message = "";
        private Timer hide;
        private Rectangle closeRect;
        private bool hoverClose;

        public event EventHandler Dismissed;

        public Banner()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Height = 48;
            Font = Theme.Base;
            Cursor = Cursors.Default;
        }

        public void Show(BannerKind kind, string message, int seconds)
        {
            Kind = kind;
            Message = message;
            Invalidate();
            if (hide == null)
            {
                hide = new Timer();
                hide.Interval = 1000;
                hide.Tick += delegate
                {
                    hide.Stop();
                    if (Dismissed != null) Dismissed(this, EventArgs.Empty);
                };
            }
            hide.Stop();
            if (seconds > 0) hide.Start();
            hide.Interval = Math.Max(1000, seconds * 1000);
        }

        public void DismissNow()
        {
            if (hide != null) hide.Stop();
            if (Dismissed != null) Dismissed(this, EventArgs.Empty);
        }

        private Color Accent { get { return Kind == BannerKind.Success ? Theme.Low : (Kind == BannerKind.Warn ? Theme.Mid : (Kind == BannerKind.Error ? Theme.High : Theme.Primary)); } }
        private Color Soft { get { return Kind == BannerKind.Success ? Theme.LowSoft : (Kind == BannerKind.Warn ? Theme.MidSoft : (Kind == BannerKind.Error ? Theme.HighSoft : Theme.PrimarySoft)); } }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool now = closeRect.Contains(e.Location);
            if (now != hoverClose) { hoverClose = now; Cursor = now ? Cursors.Hand : Cursors.Default; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { hoverClose = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (closeRect.Contains(e.Location)) DismissNow();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            if (Message.Length == 0) return;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Theme.FillRound(g, r, 10, Soft);
            IconPainter.Draw(g, new RectangleF(16, (Height - 18) / 2f, 18, 18), Kind == BannerKind.Warn ? "warning" : "check", Accent);
            TextRenderer.DrawText(g, Message, Theme.Base, new Rectangle(46, 0, Width - 92, Height), Accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            closeRect = new Rectangle(Width - 38, (Height - 22) / 2, 22, 22);
            if (hoverClose) Theme.FillRound(g, closeRect, 6, Color.FromArgb(30, Accent));
            IconPainter.Draw(g, new RectangleF(closeRect.X + 4, closeRect.Y + 4, 14, 14), "close", Color.FromArgb(180, Accent));
        }
    }

    // ============================================================ search box
    internal class SearchBox : Panel
    {
        private TextBox input;
        private bool focused;
        public string Placeholder = "搜索清理项";
        public event EventHandler TextChanged2;

        public SearchBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
            Size = new Size(260, 38);
            Cursor = Cursors.IBeam;

            input = new TextBox();
            input.BorderStyle = BorderStyle.None;
            input.Font = Theme.Base;
            input.ForeColor = Theme.Text;
            input.BackColor = Theme.Surface;
            input.Location = new Point(36, 11);
            input.Width = Width - 56;
            input.TextChanged += delegate { if (TextChanged2 != null) TextChanged2(this, EventArgs.Empty); Invalidate(); };
            input.GotFocus += delegate { focused = true; Invalidate(); };
            input.LostFocus += delegate { focused = false; Invalidate(); };
            input.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { input.Text = ""; }
            };
            Controls.Add(input);
            Click += delegate { input.Focus(); };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (input != null) input.Width = Math.Max(20, Width - 56);
        }

        public string Query { get { return input.Text.Trim(); } }

        public void Clear() { input.Text = ""; }

        public void FocusInput() { input.Focus(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Theme.FillRound(g, r, 9, Theme.Surface);
            Theme.StrokeRound(g, r, 9, focused ? Theme.Primary : Theme.BorderStrong);
            IconPainter.Draw(g, new RectangleF(13, (Height - 15) / 2f, 15, 15), "search", focused ? Theme.Primary : Theme.TextFaint);
            if (input.TextLength == 0 && !focused)
            {
                using (var b = new SolidBrush(Theme.TextFaint))
                    g.DrawString(Placeholder, Theme.Base, b, 36, 11);
            }
        }
    }

    // ============================================================ item list
    internal class ItemList : ListView
    {
        public event EventHandler CheckedChanged;
        public bool ShowCheckBoxes = true;
        public int SortColumn = 2;
        public bool SortDesc = true;
        private int hover = -1;

        public ItemList()
        {
            OwnerDraw = true;
            View = View.Details;
            FullRowSelect = true;
            MultiSelect = false;
            HideSelection = false;
            BorderStyle = BorderStyle.None;
            HeaderStyle = ColumnHeaderStyle.Clickable;
            Font = Theme.Base;
            BackColor = Theme.Surface;
            var il = new ImageList();
            il.ImageSize = new Size(1, 46);
            il.ColorDepth = ColorDepth.Depth32Bit;
            SmallImageList = il;
            Columns.Add(I18n.T("col.name"), 340);
            Columns.Add(I18n.T("col.cat"), 100);
            Columns.Add(I18n.T("col.size"), 104, HorizontalAlignment.Right);
            Columns.Add(I18n.T("col.risk"), 88, HorizontalAlignment.Center);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var info = HitTest(e.Location);
            int idx = info.Item != null ? info.Item.Index : -1;
            if (idx != hover) { hover = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (ShowCheckBoxes)
            {
                var info = HitTest(e.Location);
                if (info.Item != null)
                {
                    var r = GetItemRect(info.Item.Index);
                    if (e.X >= r.Left && e.X - r.Left <= 40)
                    {
                        var it = info.Item.Tag as CleanItem;
                        if (it != null)
                        {
                            it.Checked = !it.Checked;
                            Invalidate();
                            if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
                        }
                    }
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (ShowCheckBoxes && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Insert) && SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in SelectedItems)
                {
                    var it = lvi.Tag as CleanItem;
                    if (it != null) it.Checked = !it.Checked;
                }
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            using (var b = new SolidBrush(Color.FromArgb(250, 251, 253)))
                e.Graphics.FillRectangle(b, e.Bounds);
            using (var pen = new Pen(Theme.Border))
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            if (e.ColumnIndex == 2) flags |= TextFormatFlags.Right;
            else if (e.ColumnIndex == 3) flags |= TextFormatFlags.HorizontalCenter;
            else flags |= TextFormatFlags.Left;

            int padLeft = e.ColumnIndex == 0 && ShowCheckBoxes ? 44 : 12;
            var r = new Rectangle(e.Bounds.Left + padLeft, e.Bounds.Top, e.Bounds.Width - padLeft - 8, e.Bounds.Height);
            Color c = SortColumn == e.ColumnIndex ? Theme.Text : Theme.TextSub;
            TextRenderer.DrawText(e.Graphics, e.Header.Text, Theme.SmallBold, r, c, flags);

            if (SortColumn == e.ColumnIndex)
            {
                int ax = e.ColumnIndex == 2 ? r.Right : (e.ColumnIndex == 3 ? r.Right : r.Left + TextRenderer.MeasureText(e.Header.Text, Theme.SmallBold).Width + 6);
                int ay = e.Bounds.Top + e.Bounds.Height / 2;
                using (var b = new SolidBrush(Theme.TextSub))
                {
                    Point[] pts = SortDesc
                        ? new[] { new Point(ax, ay - 2), new Point(ax + 7, ay - 2), new Point(ax + 3, ay + 2) }
                        : new[] { new Point(ax, ay + 2), new Point(ax + 7, ay + 2), new Point(ax + 3, ay - 2) };
                    e.Graphics.FillPolygon(b, pts);
                }
            }
        }

        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            var g = e.Graphics;
            bool selected = (e.State & ListViewItemStates.Selected) != 0;
            Color back = selected ? Theme.PrimarySoft : (e.ItemIndex == hover ? Color.FromArgb(249, 251, 255)
                : (e.ItemIndex % 2 == 1 ? Color.FromArgb(252, 253, 255) : Theme.Surface));
            using (var b = new SolidBrush(back))
                g.FillRectangle(b, e.Bounds);
            using (var pen = new Pen(Color.FromArgb(241, 243, 247)))
                g.DrawLine(pen, e.Bounds.Left + 12, e.Bounds.Bottom - 1, e.Bounds.Right - 12, e.Bounds.Bottom - 1);
        }

        protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
            var g = e.Graphics;
            var it = e.Item.Tag as CleanItem;
            if (it == null) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = e.Bounds;

            if (e.ColumnIndex == 0)
            {
                int textLeft = 12;
                if (ShowCheckBoxes)
                {
                    var box = new Rectangle(r.Left + 14, r.Top + (r.Height - 18) / 2, 18, 18);
                    if (it.Checked)
                    {
                        Theme.FillRound(g, box, 5, Theme.Primary);
                        using (var pen = new Pen(Color.White, 2f))
                        {
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;
                            g.DrawLines(pen, new[]
                            {
                                new Point(box.Left + 4, box.Top + 9),
                                new Point(box.Left + 8, box.Top + 13),
                                new Point(box.Left + 14, box.Top + 5)
                            });
                        }
                    }
                    else
                    {
                        Theme.FillRound(g, box, 5, Color.White);
                        Theme.StrokeRound(g, box, 5, it.Risk == 0 ? Theme.BorderStrong : Color.FromArgb(160, Theme.RiskColor(it.Risk)));
                    }
                    textLeft = box.Right + 12 - r.Left;
                }
                var textRect = new Rectangle(r.Left + textLeft, r.Top, Math.Max(20, r.Width - textLeft - 8), r.Height);
                TextRenderer.DrawText(g, it.Name, Theme.Base, textRect, Theme.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else if (e.ColumnIndex == 1)
            {
                TextRenderer.DrawText(g, it.Category, Theme.Small, new Rectangle(r.Left + 12, r.Top, r.Width - 20, r.Height), Theme.TextSub,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else if (e.ColumnIndex == 2)
            {
                TextRenderer.DrawText(g, Theme.Fmt(it.Size), Theme.Base, new Rectangle(r.Left, r.Top, r.Width - 14, r.Height), Theme.Text,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
            else if (e.ColumnIndex == 3)
            {
                string label = Theme.RiskText(it.Risk);
                Size sz = TextRenderer.MeasureText(g, label, Theme.Small);
                int pw = sz.Width + 18;
                var pill = new Rectangle(r.Left + (r.Width - pw) / 2, r.Top + (r.Height - 22) / 2, pw, 22);
                Theme.FillRound(g, pill, 11, Theme.RiskSoft(it.Risk));
                TextRenderer.DrawText(g, label, Theme.Small, pill, Theme.RiskColor(it.Risk),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
