using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DiskMate
{
    // ============================================================ flat scrollbar
    // 扁平细滚动条：隐藏系统原生滚动条，在内容右缘覆盖一个可拖动的圆角滑块，
    // 视觉与应用整体风格一致。三个 Source 适配 ListView / AutoScroll 面板 / 多行文本框。

    internal interface FlatScrollSource
    {
        int Position();
        int Maximum();
        void ScrollTo(int pos);
        void HideNative();
        int StepSize();
        Rectangle TrackBounds(Size client);
        void PaintBackground(Graphics g, Rectangle bounds);
    }

    internal class FlatScrollbar : Control
    {
        private Control target;
        private FlatScrollSource src;
        private Timer tick;
        private int pos, max;
        private bool drag, overThumb;
        private int grabY;

        public FlatScrollbar(Control target, FlatScrollSource src)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            this.target = target;
            this.src = src;
            TabStop = false;
            Cursor = Cursors.Hand;
            Visible = false;
            Width = Theme.P(10);
            target.Controls.Add(this);
            BringToFront();
            tick = new Timer();
            tick.Interval = 40;
            tick.Tick += delegate { Sync(); };
            target.HandleCreated += delegate { Sync(); tick.Start(); };
            target.HandleDestroyed += delegate { tick.Stop(); };
            target.Resize += delegate { Sync(); };
            if (target.IsHandleCreated) tick.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && tick != null) { tick.Stop(); tick.Dispose(); tick = null; }
            base.Dispose(disposing);
        }

        public void Sync()
        {
            if (tick == null || target == null || src == null) return;
            if (!target.IsHandleCreated || target.Disposing) return;
            try { src.HideNative(); } catch { }
            int p = 0, m = 0;
            try { p = src.Position(); m = src.Maximum(); } catch { }
            if (m <= 0 || target.ClientSize.Height < Theme.P(48) || target.ClientSize.Width < Theme.P(60))
            {
                if (drag) { drag = false; Capture = false; }
                if (Visible) Visible = false;
                return;
            }
            Rectangle b = src.TrackBounds(target.ClientSize);
            if (b != Bounds) Bounds = b;
            if (!Visible) Visible = true;
            if (p != pos || m != max) { pos = p; max = m; Invalidate(); }
        }

        // 原生滚动条被隐藏后 ListView / Panel 不再响应滚轮，统一由这里驱动
        public void Wheel(int delta)
        {
            if (delta == 0) return;
            int lines = 3;
            try { lines = SystemInformation.MouseWheelScrollLines; } catch { }
            if (lines <= 0) lines = 3;
            int step = Math.Max(Theme.P(32), src.StepSize()) * lines;
            src.ScrollTo(pos - Math.Sign(delta) * step);
            Sync();
        }

        private Rectangle ThumbRect()
        {
            int content = max + Math.Max(1, Height);
            double frac = (double)Height / content;
            int th = Math.Max(Theme.P(26), (int)Math.Round(Height * frac));
            int travel = Math.Max(1, Height - th);
            int y = max <= 0 ? 0 : (int)Math.Round(travel * (double)pos / max);
            int w = Theme.P(6);
            return new Rectangle((Width - w) / 2, y, w, th);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            src.PaintBackground(g, ClientRectangle);
            Rectangle t = ThumbRect();
            if (t.Height <= 0 || t.Bottom < 0 || t.Top > Height) return;
            Color c = drag ? Theme.Primary
                : (overThumb ? Color.FromArgb(146, 161, 189) : Color.FromArgb(201, 210, 224));
            using (GraphicsPath path = Round(t, Theme.P(3)))
            using (SolidBrush b = new SolidBrush(c))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillPath(b, path);
            }
        }

        private static GraphicsPath Round(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            if (rad * 2 >= r.Width) rad = Math.Max(1, r.Width / 2);
            p.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
            p.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
            p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
            p.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Rectangle t = ThumbRect();
            if (e.Button == MouseButtons.Left && t.Contains(e.Location))
            {
                drag = true;
                overThumb = true;
                grabY = e.Y - t.Y;
                Capture = true;
                Invalidate();
            }
            else if (e.Button == MouseButtons.Left)
            {
                int page = (int)(Height * 0.7);
                src.ScrollTo(pos + (e.Y < t.Top ? -page : page));
                Sync();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Rectangle t = ThumbRect();
            bool inThumb = t.Contains(e.Location);
            if (inThumb != overThumb) { overThumb = inThumb; Invalidate(); }
            Cursor = overThumb || drag ? Cursors.SizeNS : Cursors.Hand;
            if (drag)
            {
                int travel = Math.Max(1, Height - t.Height);
                int y = Math.Max(0, Math.Min(Height, e.Y - grabY));
                src.ScrollTo((int)Math.Round((double)y * max / travel));
                Sync();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (drag) { drag = false; Capture = false; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!drag) { overThumb = false; Invalidate(); }
        }
    }

    // ============================================================ listview
    internal class ListViewSource : FlatScrollSource
    {
        private ListView lv;
        private ItemList themed;

        public ListViewSource(ListView lv)
        {
            this.lv = lv;
            themed = lv as ItemList;
        }

        public void HideNative()
        {
            // 方案：不摘 WS_VSCROLL（摘了 ListView / Edit / Panel 的滚动能力会一起废掉），
            // 而是把控件尺寸放宽一个滚动条宽度，让原生条落到父容器边界外被裁掉，功能保留、视觉隐藏。
        }

        private int HeaderHeight()
        {
            IntPtr h = Win.SendMessage(lv.Handle, Win.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            if (h == IntPtr.Zero) return 0;
            Win.RECT r;
            Win.GetWindowRect(h, out r);
            return Math.Max(0, r.Bottom - r.Top);
        }

        public int Position()
        {
            if (lv.Items.Count == 0 || !lv.IsHandleCreated) return 0;
            try { return Math.Max(0, HeaderHeight() - lv.GetItemRect(0).Top); }
            catch { return 0; }
        }

        public int Maximum()
        {
            int n = lv.Items.Count;
            if (n == 0 || !lv.IsHandleCreated) return 0;
            try
            {
                Rectangle rl = lv.GetItemRect(n - 1);
                return Math.Max(0, rl.Bottom + Position() - lv.ClientSize.Height);
            }
            catch { return 0; }
        }

        public void ScrollTo(int p)
        {
            p = Math.Max(0, Math.Min(Maximum(), p));
            int dy = p - Position();
            if (dy != 0) Win.SendMessage(lv.Handle, Win.LVM_SCROLL, IntPtr.Zero, new IntPtr(dy));
        }

        public int StepSize()
        {
            if (lv.Items.Count == 0 || !lv.IsHandleCreated) return Theme.P(40);
            try { return Math.Max(Theme.P(24), lv.GetItemRect(0).Height); }
            catch { return Theme.P(40); }
        }

        public Rectangle TrackBounds(Size cl)
        {
            int hh = HeaderHeight();
            return new Rectangle(cl.Width - Theme.P(10), hh, Theme.P(10), Math.Max(0, cl.Height - hh));
        }

        public void PaintBackground(Graphics g, Rectangle bounds)
        {
            if (themed == null || lv.Items.Count == 0 || !lv.IsHandleCreated)
            {
                using (SolidBrush b = new SolidBrush(lv.BackColor)) g.FillRectangle(b, bounds);
                return;
            }
            try
            {
                Rectangle r0 = lv.GetItemRect(0);
                int rowH = r0.Height;
                if (rowH <= 0) { using (SolidBrush b = new SolidBrush(lv.BackColor)) g.FillRectangle(b, bounds); return; }
                int y = bounds.Top;
                int idx = (bounds.Top - r0.Top) / rowH;
                if ((bounds.Top - r0.Top) % rowH != 0) idx--;
                if (idx < 0) idx = 0;
                while (y < bounds.Bottom)
                {
                    int bandBottom = r0.Top + (idx + 1) * rowH;
                    int h = Math.Min(bandBottom, bounds.Bottom) - y;
                    if (h > 0)
                    {
                        using (SolidBrush b = new SolidBrush(themed.RowColor(idx))) g.FillRectangle(b, bounds.X, y, bounds.Width, h);
                        y += h;
                    }
                    idx++;
                }
            }
            catch
            {
                using (SolidBrush b = new SolidBrush(lv.BackColor)) g.FillRectangle(b, bounds);
            }
        }
    }

    // ============================================================ autoscroll panel
    internal class ScrollPanel : Panel
    {
        public ScrollPanel() { SetStyle(ControlStyles.Selectable, true); }

        private const int WM_MOUSEWHEEL = 0x020A;

        public void SetScroll(int y)
        {
            int max = Math.Max(0, DisplayRectangle.Height - ClientSize.Height);
            AutoScrollPosition = new Point(0, Math.Max(0, Math.Min(max, y)));
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL)
            {
                long wp = m.WParam.ToInt64();
                int delta = (short)((wp >> 16) & 0xFFFF);
                if (delta != 0)
                {
                    SetScroll(Math.Max(0, -AutoScrollPosition.Y) - Math.Sign(delta) * Theme.P(64));
                    return;   // 已处理
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            try { if (!ContainsFocus) Focus(); } catch { }   // 悬停即可接管滚轮
        }
    }

    internal class PanelSource : FlatScrollSource
    {
        private ScrollPanel p;

        public PanelSource(ScrollPanel p) { this.p = p; }

        public void HideNative()
        {
            // 同上：滚动条靠父容器裁掉，功能完整保留（AutoScroll 的滚动条不能被摘样式）
        }

        public int Position() { return Math.Max(0, -p.AutoScrollPosition.Y); }

        public int Maximum() { return Math.Max(0, p.DisplayRectangle.Height - p.ClientSize.Height); }

        public void ScrollTo(int v) { p.SetScroll(v); }

        public int StepSize() { return Theme.P(40); }

        public Rectangle TrackBounds(Size cl)
        {
            // 覆盖层挂在面板的宿主容器上（不参与 AutoScroll 的 DisplayRectangle 计算），
            // 因此直接用宿主坐标，不再需要补偿 AutoScrollPosition
            return new Rectangle(cl.Width - Theme.P(10), 0, Theme.P(10), cl.Height);
        }

        public void PaintBackground(Graphics g, Rectangle b)
        {
            using (SolidBrush br = new SolidBrush(p.BackColor)) g.FillRectangle(br, b);
        }
    }

    // ============================================================ multiline textbox
    internal class TextBoxSource : FlatScrollSource
    {
        private TextBox tb;

        public TextBoxSource(TextBox tb) { this.tb = tb; }

        public void HideNative()
        {
            // 同 ListView：保留 ES_AUTOVSCROLL 与原生条，靠父容器裁剪隐藏
        }

        public int Position() { return Win.SendMessage(tb.Handle, Win.EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32(); }

        private int VisibleLines()
        {
            Win.RECT r = new Win.RECT();
            Win.SendMessage(tb.Handle, Win.EM_GETRECT, IntPtr.Zero, ref r);
            int h = r.Bottom - r.Top;
            int lh = Math.Max(1, TextRenderer.MeasureText("Xg", tb.Font).Height);
            return Math.Max(1, h / lh);
        }

        public int Maximum()
        {
            int lines = Win.SendMessage(tb.Handle, Win.EM_GETLINECOUNT, IntPtr.Zero, IntPtr.Zero).ToInt32();
            return Math.Max(0, lines - VisibleLines());
        }

        public int StepSize()
        {
            return Math.Max(Theme.P(14), TextRenderer.MeasureText("Xg", tb.Font).Height);
        }

        public void ScrollTo(int v)
        {
            v = Math.Max(0, Math.Min(Maximum(), v));
            int dy = v - Position();
            if (dy != 0) Win.SendMessage(tb.Handle, Win.EM_LINESCROLL, IntPtr.Zero, new IntPtr(dy));
        }

        public Rectangle TrackBounds(Size cl)
        {
            return new Rectangle(cl.Width - Theme.P(10), 1, Theme.P(10), Math.Max(0, cl.Height - 2));
        }

        public void PaintBackground(Graphics g, Rectangle b)
        {
            using (SolidBrush br = new SolidBrush(tb.BackColor)) g.FillRectangle(br, b);
        }
    }

    // 让指定列吸收剩余宽度，避免横向滚动条出现
    internal static class ListViewFit
    {
        public static void Attach(ListView lv, int fitIndex, int pad)
        {
            EventHandler adjust = delegate(object s, EventArgs e)
            {
                try
                {
                    if (!lv.IsHandleCreated || lv.Columns.Count <= fitIndex) return;
                    int others = 0;
                    for (int i = 0; i < lv.Columns.Count; i++)
                        if (i != fitIndex) others += lv.Columns[i].Width;
                    int avail = lv.ClientSize.Width;
                    int style = Win.GetWindowLong(lv.Handle, Win.GWL_STYLE);
                    if ((style & Win.WS_VSCROLL) == 0) avail -= SystemInformation.VerticalScrollBarWidth;
                    int w = Math.Max(Theme.P(80), avail - others - Theme.P(pad));
                    if (lv.Columns[fitIndex].Width != w) lv.Columns[fitIndex].Width = w;
                }
                catch { }
            };
            lv.Resize += adjust;
            lv.ClientSizeChanged += adjust;
            lv.HandleCreated += delegate(object s, EventArgs e) { adjust(null, null); };
            if (lv.IsHandleCreated) adjust(null, null);
        }
    }

    // ============================================================ winapi
    internal static class Win
    {
        public const int SB_HORZ = 0;
        public const int SB_VERT = 1;
        public const int SB_BOTH = 3;
        public const int GWL_STYLE = -16;
        public const int WS_HSCROLL = 0x00100000;
        public const int WS_VSCROLL = 0x00200000;
        public const int LVM_GETHEADER = 0x101F;   // LVM_FIRST + 31
        public const int LVM_SCROLL = 0x1014;      // LVM_FIRST + 20
        public const int EM_GETFIRSTVISIBLELINE = 0x0CE;
        public const int EM_GETLINECOUNT = 0x0BA;
        public const int EM_LINESCROLL = 0x0B6;
        public const int EM_GETRECT = 0x0B2;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        [DllImport("user32.dll")]
        public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, int bShow);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        public static extern int GetWindowLong(IntPtr hWnd, int index);
    }
}
