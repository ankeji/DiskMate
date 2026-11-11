using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskMate
{
    // ============================================================ models
    public class CleanItem
    {
        public string Key = "";
        public string CatKey = "";
        public string Name = "";
        public string Category = "";
        public int Count;
        public string[] Targets = new string[0];
        public long Size;
        public string Note = "";
        public string Kind = "dir";     // dir | recycle | drivers | hiber
        public int Risk;                // 0 低 / 1 中 / 2 高
        public string Impact = "";
        public string Advice = "";
        public bool Checked;
    }

    public class DriverPkg
    {
        public string Inf;
        public string Original;
        public DateTime Date;
        public Version Ver;
        public string RawVersion;
    }

    public class ScanStep
    {
        public string Label;
        public Action<List<CleanItem>> Run;
        public ScanStep(string label, Action<List<CleanItem>> run) { Label = label; Run = run; }
    }

    internal static class Native
    {
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(int pid);

        public const uint SHERB_NOCONFIRMATION = 0x1;
        public const uint SHERB_NOPROGRESSUI = 0x2;
        public const uint SHERB_NOSOUND = 0x4;
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
    }

    // ============================================================ theme
    internal static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(245, 247, 251);
        public static readonly Color Sidebar = Color.FromArgb(252, 253, 255);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceAlt = Color.FromArgb(249, 250, 253);
        public static readonly Color Border = Color.FromArgb(232, 236, 243);
        public static readonly Color BorderStrong = Color.FromArgb(212, 219, 229);
        public static readonly Color Text = Color.FromArgb(17, 24, 39);
        public static readonly Color TextSub = Color.FromArgb(100, 110, 126);
        public static readonly Color TextFaint = Color.FromArgb(150, 160, 175);

        public static readonly Color Primary = Color.FromArgb(59, 110, 246);
        public static readonly Color PrimaryDark = Color.FromArgb(38, 88, 219);
        public static readonly Color PrimarySoft = Color.FromArgb(237, 242, 255);

        public static readonly Color Low = Color.FromArgb(22, 163, 74);
        public static readonly Color LowSoft = Color.FromArgb(235, 252, 242);
        public static readonly Color Mid = Color.FromArgb(202, 120, 8);
        public static readonly Color MidSoft = Color.FromArgb(255, 249, 235);
        public static readonly Color High = Color.FromArgb(220, 45, 55);
        public static readonly Color HighSoft = Color.FromArgb(254, 241, 242);

        public static readonly string Family = PickFamily();

        // ---------------------------------------------------------- DPI / 缩放
        // 96 DPI = 1.0。所有界面尺寸都按 96 DPI 的设计稿写，最后由 S 统一放大，
        // 因此高 DPI 屏幕上字体、间距、控件一起等比缩放，不会出现「字变大、框没变」的错位。
        public static readonly float S = DetectScale();

        /// <summary>设计稿像素 → 实际像素。</summary>
        public static int P(int v) { return (int)Math.Round(v * S); }

        public static float PF(float v) { return v * S; }

        public static Point Pt(int x, int y) { return new Point(P(x), P(y)); }

        public static Size Sz(int w, int h) { return new Size(P(w), P(h)); }

        /// <summary>按磅值建字体，实际尺寸随 S 缩放（像素单位，避免二次 DPI 放大）。</summary>
        public static Font F(float pt, FontStyle style)
        {
            return new Font(Family, pt * 4f / 3f * S, style, GraphicsUnit.Pixel);
        }

        /// <summary>窗口初始大小：按 S 放大，但不超过屏幕可用区域。</summary>
        public static Size WindowSize(int designW, int designH)
        {
            Size sz = Sz(designW, designH);
            try
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                int maxW = Math.Max(P(900), wa.Width - P(24));
                int maxH = Math.Max(P(600), wa.Height - P(48));
                if (sz.Width > maxW) sz.Width = maxW;
                if (sz.Height > maxH) sz.Height = maxH;
            }
            catch { }
            return sz;
        }

        /// <summary>读取 settings.ini 中的一项（AppData 优先，其次 exe 同目录）。</summary>
        public static string ReadSetting(string key)
        {
            try
            {
                string[] paths = new string[]
                {
                    Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate"), "settings.ini"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini")
                };
                foreach (string p in paths)
                {
                    if (!File.Exists(p)) continue;
                    foreach (string line in File.ReadAllLines(p, Encoding.UTF8))
                    {
                        int i = line.IndexOf('=');
                        if (i <= 0) continue;
                        if (line.Substring(0, i).Trim() != key) continue;
                        return line.Substring(i + 1).Trim();
                    }
                }
            }
            catch { }
            return null;
        }
        private static float DetectScale()
        {
            try
            {
                string env = Environment.GetEnvironmentVariable("DISKMATE_SCALE");
                float v;
                if (!string.IsNullOrEmpty(env) &&
                    float.TryParse(env, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                {
                    if (v >= 0.75f && v <= 3f) return v;
                }
                string saved = ReadSetting("scale");
                if (!string.IsNullOrEmpty(saved) && !saved.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    float sv;
                    if (float.TryParse(saved, NumberStyles.Float, CultureInfo.InvariantCulture, out sv) && sv >= 0.75f && sv <= 3f)
                        return sv;
                }
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    float dpi = g.DpiX;
                    if (dpi > 60f && dpi < 480f) return Math.Max(0.9f, Math.Min(3f, dpi / 96f));
                }
            }
            catch { }
            return 1f;
        }

        public static readonly Font Base = F(9.5f, FontStyle.Regular);
        public static readonly Font Bold = F(9.5f, FontStyle.Bold);
        public static readonly Font Small = F(8.5f, FontStyle.Regular);
        public static readonly Font SmallBold = F(8.5f, FontStyle.Bold);
        public static readonly Font H1 = F(15f, FontStyle.Bold);
        public static readonly Font H2 = F(11.5f, FontStyle.Bold);
        public static readonly Font Hero = F(19f, FontStyle.Bold);
        public static readonly Font Num = F(16f, FontStyle.Bold);
        public static readonly Font Mono = new Font("Consolas", 9.5f * 4f / 3f * S, FontStyle.Regular, GraphicsUnit.Pixel);

        private static string PickFamily()
        {
            try
            {
                foreach (FontFamily f in FontFamily.Families)
                {
                    string n = f.Name;
                    if (n == "Microsoft YaHei UI") return n;
                }
                foreach (FontFamily f in FontFamily.Families)
                    if (f.Name == "Microsoft YaHei") return f.Name;
            }
            catch { }
            return "Segoe UI";
        }

        public static Color RiskColor(int r) { return r == 0 ? Low : (r == 1 ? Mid : High); }
        public static Color RiskSoft(int r) { return r == 0 ? LowSoft : (r == 1 ? MidSoft : HighSoft); }
        public static string RiskText(int r) { return r == 0 ? I18n.T("risk.low") : (r == 1 ? I18n.T("risk.mid") : I18n.T("risk.high")); }

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Height) d = r.Height;
            if (d > r.Width) d = r.Width;
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color color)
        {
            using (var path = Round(r, radius))
            using (var b = new SolidBrush(color))
                g.FillPath(b, path);
        }

        public static void FillRoundGrad(Graphics g, Rectangle r, int radius, Color c1, Color c2)
        {
            using (var path = Round(r, radius))
            using (var b = new LinearGradientBrush(r, c1, c2, LinearGradientMode.Vertical))
                g.FillPath(b, path);
        }

        public static void StrokeRound(Graphics g, Rectangle r, int radius, Color color)
        {
            using (var path = Round(r, radius))
            using (var pen = new Pen(color))
                g.DrawPath(pen, path);
        }

        public static string Fmt(long bytes)
        {
            if (bytes >= 1073741824L) return (bytes / 1073741824.0).ToString("N2") + " GB";
            if (bytes >= 1048576L) return (bytes / 1048576.0).ToString("N1") + " MB";
            if (bytes >= 1024L) return (bytes / 1024.0).ToString("N0") + " KB";
            return bytes + " B";
        }

        public static TextFormatFlags Center { get { return TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter; } }
        public static TextFormatFlags LeftMid { get { return TextFormatFlags.Left | TextFormatFlags.VerticalCenter; } }
        public static TextFormatFlags RightMid { get { return TextFormatFlags.Right | TextFormatFlags.VerticalCenter; } }
    }

    // ============================================================ vector icons
    internal static class IconPainter
    {
        public static void Draw(Graphics g, RectangleF box, string kind, Color color)
        {
            float th = Math.Max(1.2f, box.Height / 11f);
            float s = Math.Min(box.Width, box.Height) / 16f;
            float ox = box.X + (box.Width - 16f * s) / 2f;
            float oy = box.Y + (box.Height - 16f * s) / 2f;

            using (var pen = new Pen(color, th))
            using (var brush = new SolidBrush(color))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                switch (kind)
                {
                    case "scan":
                        g.DrawEllipse(pen, ox + 1.6f * s, oy + 1.6f * s, 12.8f * s, 12.8f * s);
                        g.DrawLines(pen, new[]
                        {
                            new PointF(ox + 5.2f * s, oy + 8.3f * s),
                            new PointF(ox + 7.3f * s, oy + 10.4f * s),
                            new PointF(ox + 11.2f * s, oy + 5.9f * s)
                        });
                        break;
                    case "files":
                        g.DrawRectangle(pen, ox + 2.4f * s, oy + 4.4f * s, 8.4f * s, 9.6f * s);
                        g.DrawRectangle(pen, ox + 5.2f * s, oy + 2f * s, 8.4f * s, 9.6f * s);
                        g.DrawLine(pen, ox + 7.2f * s, oy + 5.2f * s, ox + 11.6f * s, oy + 5.2f * s);
                        g.DrawLine(pen, ox + 7.2f * s, oy + 7.8f * s, ox + 11.6f * s, oy + 7.8f * s);
                        break;
                    case "sliders":
                        g.DrawLine(pen, ox + 2f * s, oy + 4.2f * s, ox + 14f * s, oy + 4.2f * s);
                        g.DrawLine(pen, ox + 2f * s, oy + 8f * s, ox + 14f * s, oy + 8f * s);
                        g.DrawLine(pen, ox + 2f * s, oy + 11.8f * s, ox + 14f * s, oy + 11.8f * s);
                        g.FillEllipse(brush, ox + 9.4f * s, oy + 3f * s, 2.4f * s, 2.4f * s);
                        g.FillEllipse(brush, ox + 4.4f * s, oy + 6.8f * s, 2.4f * s, 2.4f * s);
                        g.FillEllipse(brush, ox + 10.6f * s, oy + 10.6f * s, 2.4f * s, 2.4f * s);
                        break;
                    case "list":
                        g.DrawLine(pen, ox + 5.6f * s, oy + 4f * s, ox + 14f * s, oy + 4f * s);
                        g.DrawLine(pen, ox + 5.6f * s, oy + 8f * s, ox + 14f * s, oy + 8f * s);
                        g.DrawLine(pen, ox + 5.6f * s, oy + 12f * s, ox + 11f * s, oy + 12f * s);
                        g.FillEllipse(brush, ox + 2f * s, oy + 3.1f * s, 1.8f * s, 1.8f * s);
                        g.FillEllipse(brush, ox + 2f * s, oy + 7.1f * s, 1.8f * s, 1.8f * s);
                        g.FillEllipse(brush, ox + 2f * s, oy + 11.1f * s, 1.8f * s, 1.8f * s);
                        break;
                    case "search":
                        g.DrawEllipse(pen, ox + 2.2f * s, oy + 2.2f * s, 9f * s, 9f * s);
                        g.DrawLine(pen, ox + 10.4f * s, oy + 10.4f * s, ox + 14.2f * s, oy + 14.2f * s);
                        break;
                    case "close":
                        g.DrawLine(pen, ox + 4.6f * s, oy + 4.6f * s, ox + 11.4f * s, oy + 11.4f * s);
                        g.DrawLine(pen, ox + 11.4f * s, oy + 4.6f * s, ox + 4.6f * s, oy + 11.4f * s);
                        break;
                    case "check":
                        g.DrawLines(pen, new[]
                        {
                            new PointF(ox + 3.4f * s, oy + 8.4f * s),
                            new PointF(ox + 6.6f * s, oy + 11.6f * s),
                            new PointF(ox + 12.6f * s, oy + 4.6f * s)
                        });
                        break;
                    case "warning":
                        g.DrawLines(pen, new[]
                        {
                            new PointF(ox + 8f * s, oy + 2.4f * s),
                            new PointF(ox + 14.6f * s, oy + 13.6f * s),
                            new PointF(ox + 1.4f * s, oy + 13.6f * s),
                            new PointF(ox + 8f * s, oy + 2.4f * s)
                        });
                        g.DrawLine(pen, ox + 8f * s, oy + 6.6f * s, ox + 8f * s, oy + 9.6f * s);
                        g.FillEllipse(brush, ox + 7.2f * s, oy + 11.2f * s, 1.7f * s, 1.7f * s);
                        break;
                    case "folder":
                        g.DrawLines(pen, new[]
                        {
                            new PointF(ox + 1.8f * s, oy + 12.6f * s),
                            new PointF(ox + 1.8f * s, oy + 4.4f * s),
                            new PointF(ox + 6.4f * s, oy + 4.4f * s),
                            new PointF(ox + 7.9f * s, oy + 6.4f * s),
                            new PointF(ox + 14.2f * s, oy + 6.4f * s),
                            new PointF(ox + 14.2f * s, oy + 12.6f * s),
                            new PointF(ox + 1.8f * s, oy + 12.6f * s)
                        });
                        break;
                    case "trash":
                        g.DrawLine(pen, ox + 2.6f * s, oy + 4.4f * s, ox + 13.4f * s, oy + 4.4f * s);
                        g.DrawLine(pen, ox + 6.2f * s, oy + 2.4f * s, ox + 9.8f * s, oy + 2.4f * s);
                        g.DrawLines(pen, new[]
                        {
                            new PointF(ox + 4.2f * s, oy + 4.4f * s),
                            new PointF(ox + 5.2f * s, oy + 13.6f * s),
                            new PointF(ox + 10.8f * s, oy + 13.6f * s),
                            new PointF(ox + 11.8f * s, oy + 4.4f * s)
                        });
                        g.DrawLine(pen, ox + 7f * s, oy + 6.8f * s, ox + 7f * s, oy + 11.2f * s);
                        g.DrawLine(pen, ox + 9f * s, oy + 6.8f * s, ox + 9f * s, oy + 11.2f * s);
                        break;
                    case "clock":
                        g.DrawEllipse(pen, ox + 1.8f * s, oy + 1.8f * s, 12.4f * s, 12.4f * s);
                        g.DrawLine(pen, ox + 8f * s, oy + 4.6f * s, ox + 8f * s, oy + 8.4f * s);
                        g.DrawLine(pen, ox + 8f * s, oy + 8.4f * s, ox + 10.8f * s, oy + 10f * s);
                        break;
                    case "refresh":
                        g.DrawArc(pen, ox + 2.2f * s, oy + 2.2f * s, 11.6f * s, 11.6f * s, 40, 280);
                        g.DrawLine(pen, ox + 12.4f * s, oy + 1.6f * s, ox + 13.2f * s, oy + 6.2f * s);
                        g.DrawLine(pen, ox + 13.2f * s, oy + 6.2f * s, ox + 8.8f * s, oy + 5.2f * s);
                        break;
                    case "drive":
                        g.DrawRectangle(pen, ox + 2f * s, oy + 5f * s, 12f * s, 6.4f * s);
                        g.DrawLine(pen, ox + 10.4f * s, oy + 8.2f * s, ox + 12.4f * s, oy + 8.2f * s);
                        break;
                    case "sparkle":
                        g.DrawLine(pen, ox + 8f * s, oy + 2f * s, ox + 8f * s, oy + 14f * s);
                        g.DrawLine(pen, ox + 2f * s, oy + 8f * s, ox + 14f * s, oy + 8f * s);
                        break;
                }
            }
        }
    }

    // ============================================================ engine
    internal static class Engine
    {
        public static long DirSize(string path, Func<bool> cancel)
        {
            long total = 0;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return 0;
            var stack = new Stack<string>();
            stack.Push(path);
            long guard = 0;
            while (stack.Count > 0)
            {
                if (cancel != null && (++guard & 0x3F) == 0 && cancel()) throw new OperationCanceledException();
                string dir = stack.Pop();
                try
                {
                    foreach (string f in Directory.EnumerateFiles(dir))
                        try { total += new FileInfo(f).Length; } catch { }
                }
                catch { }
                try
                {
                    foreach (string d in Directory.EnumerateDirectories(dir))
                    {
                        try
                        {
                            var di = new DirectoryInfo(d);
                            if ((di.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                            stack.Push(d);
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return total;
        }

        public static long DirSize(string path) { return DirSize(path, null); }

        // 可删性统计：只把"现在就能删掉"的字节算进 Free，被占用/无权限的单列
        public class FreeStat { public long Free; public long BlockedBytes; public int Blocked; }

        public static FreeStat Measurable(string dir, Func<bool> cancel)
        {
            var st = new FreeStat();
            MeasureTree(dir, st, cancel);
            return st;
        }

        private static void MeasureTree(string dir, FreeStat st, Func<bool> cancel)
        {
            if (cancel != null && cancel()) throw new OperationCanceledException();
            string[] files;
            try { files = Directory.GetFiles(dir); } catch { files = new string[0]; }
            foreach (string f in files)
            {
                long len = 0;
                try { len = new FileInfo(f).Length; } catch { }
                bool ok = false;
                try
                {
                    using (new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                    ok = true;
                }
                catch { }
                if (ok) st.Free += len;
                else { st.Blocked++; st.BlockedBytes += len; }
            }
            string[] dirs;
            try { dirs = Directory.GetDirectories(dir); } catch { dirs = new string[0]; }
            foreach (string d in dirs)
            {
                try { if ((new DirectoryInfo(d).Attributes & FileAttributes.ReparsePoint) != 0) continue; }
                catch { }
                MeasureTree(d, st, cancel);
            }
        }

        public static long ClearDir(string dir, StringBuilder log)
        {
            if (!Directory.Exists(dir)) return 0;
            int skipped = 0;
            long freed = ClearTree(dir, ref skipped);
            if (log != null && skipped > 0) log.AppendLine("    跳过占用中的项目 " + skipped + " 个");
            return freed;
        }

        // 逐文件删除并计费：某个文件被占用不影响同目录其它文件，也不会让整棵子树白删
        private static long ClearTree(string dir, ref int skipped)
        {
            long freed = 0;
            string[] files;
            try { files = Directory.GetFiles(dir); } catch { files = new string[0]; }
            foreach (string f in files)
            {
                long len = 0;
                try { len = new FileInfo(f).Length; } catch { }
                try
                {
                    File.SetAttributes(f, FileAttributes.Normal);
                    File.Delete(f);
                    freed += len;
                }
                catch { skipped++; }
            }
            string[] dirs;
            try { dirs = Directory.GetDirectories(dir); } catch { dirs = new string[0]; }
            foreach (string d in dirs)
            {
                bool link = false;
                try { link = (new DirectoryInfo(d).Attributes & FileAttributes.ReparsePoint) != 0; }
                catch { }
                if (link) continue;             // 不跟进符号链接/联接点
                freed += ClearTree(d, ref skipped);
                try { Directory.Delete(d, false); }
                catch { skipped++; }
            }
            return freed;
        }

        // ============================================================ recycle bin
        public static string[] FixedDriveRoots()
        {
            var list = new List<string>();
            try
            {
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (d.DriveType != DriveType.Fixed) continue;
                        if (!d.IsReady) continue;
                        list.Add(d.RootDirectory.FullName);
                    }
                    catch { }
                }
            }
            catch { }
            return list.ToArray();
        }

        // 所有固定磁盘回收站的总占用（含其它账户 SID 目录）
        public static long RecycleBinSize()
        {
            long total = 0;
            foreach (string root in FixedDriveRoots()) total += DirSize(Path.Combine(root, "$Recycle.Bin"));
            return total;
        }

        public class RecycleStat
        {
            public long Total;
            public long OrphanBytes;
            public int OrphanCount;
            public int DriveCount;
        }

        // $Rxxx 的学习：回收站里每个条目是 $R<id> 数据 + $I<id> 元数据（原始路径/删除时间）。
        // 资源管理器只显示有 $I 的条目，缺 $I 的 $R 条目叫孤儿：盘上占着空间，但界面上看不到。
        // 与资源管理器保持同一口径：Total 只统计有 $I 的条目，孤儿单列在 Orphan 字段、不计入。
        public static RecycleStat RecycleBinReport()
        {
            var st = new RecycleStat();
            foreach (string root in FixedDriveRoots())
            {
                string bin = Path.Combine(root, "$Recycle.Bin");
                if (!Directory.Exists(bin)) continue;
                long indexed = 0;
                foreach (string sid in SafeDirs(bin))
                {
                    foreach (string f in SafeFiles(sid))
                    {
                        if (!IsPayload(f)) continue;
                        long len = 0;
                        try { len = new FileInfo(f).Length; } catch { }
                        if (File.Exists(MetaOf(f))) indexed += len;
                        else { st.OrphanBytes += len; st.OrphanCount++; }
                    }
                    foreach (string d in SafeDirs(sid))
                    {
                        if (!IsPayload(d)) continue;
                        long len = DirSize(d);
                        if (File.Exists(MetaOf(d))) indexed += len;
                        else { st.OrphanBytes += len; st.OrphanCount++; }
                    }
                }
                st.Total += indexed;
                if (indexed > 0) st.DriveCount++;
            }
            return st;
        }

        private static bool IsPayload(string path)
        {
            string n = Path.GetFileName(path);
            return n.Length > 2 && n.StartsWith("$R", StringComparison.OrdinalIgnoreCase);
        }

        private static string MetaOf(string payload)
        {
            string name = Path.GetFileName(payload);
            if (name.Length < 2) return payload;
            return Path.Combine(Path.GetDirectoryName(payload), "$I" + name.Substring(2));
        }

        private static string[] SafeDirs(string dir)
        {
            try { return Directory.GetDirectories(dir); } catch { return new string[0]; }
        }

        private static string[] SafeFiles(string dir)
        {
            try { return Directory.GetFiles(dir); } catch { return new string[0]; }
        }

        public static long TotalFree()
        {
            long total = 0;
            foreach (string root in FixedDriveRoots())
            {
                try { total += new DriveInfo(root).AvailableFreeSpace; } catch { }
            }
            return total;
        }

        // 逐盘清空回收站：先走 Shell API，再对被跳过的残留（其它账户的 SID 目录、被占用的项目）直接删除
        public static long EmptyRecycleBins(StringBuilder log)
        {
            long before = RecycleBinSize();
            foreach (string root in FixedDriveRoots())
            {
                try
                {
                    int hr = Native.SHEmptyRecycleBin(IntPtr.Zero, root,
                        Native.SHERB_NOCONFIRMATION | Native.SHERB_NOPROGRESSUI | Native.SHERB_NOSOUND);
                    if (hr != 0 && log != null) log.AppendLine("    回收站 " + root + " Shell 返回 0x" + hr.ToString("X8"));
                }
                catch (Exception ex)
                {
                    if (log != null) log.AppendLine("    回收站 " + root + " Shell 失败：" + ex.Message);
                }
            }
            foreach (string root in FixedDriveRoots())
            {
                string bin = Path.Combine(root, "$Recycle.Bin");
                if (!Directory.Exists(bin)) continue;
                string[] sids;
                try { sids = Directory.GetDirectories(bin); } catch { continue; }
                foreach (string sid in sids) ClearRecycleFolder(sid, log);
            }
            long after = RecycleBinSize();
            return Math.Max(0, before - after);
        }

        private static void ClearRecycleFolder(string dir, StringBuilder log)
        {
            string[] files;
            try { files = Directory.GetFiles(dir); } catch { files = new string[0]; }
            foreach (string f in files)
            {
                try
                {
                    File.SetAttributes(f, FileAttributes.Normal);
                    File.Delete(f);
                }
                catch { if (log != null) log.AppendLine("    占用中跳过：" + Path.GetFileName(f)); }
            }
            string[] dirs;
            try { dirs = Directory.GetDirectories(dir); } catch { dirs = new string[0]; }
            foreach (string d in dirs)
            {
                try
                {
                    try
                    {
                        foreach (FileInfo fi in new DirectoryInfo(d).GetFiles("*", SearchOption.AllDirectories))
                            try { fi.Attributes = FileAttributes.Normal; } catch { }
                    }
                    catch { }
                    Directory.Delete(d, true);
                }
                catch { if (log != null) log.AppendLine("    占用中跳过：" + Path.GetFileName(d)); }
            }
        }

        public static int Run(string file, string args, StringBuilder sink, Encoding enc, int timeoutMs)
        {
            try
            {
                var psi = new ProcessStartInfo(file, args);
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                if (enc != null) psi.StandardOutputEncoding = enc;

                var so = new StringBuilder();
                var se = new StringBuilder();
                using (var p = new Process())
                {
                    p.StartInfo = psi;
                    p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) lock (so) so.AppendLine(e.Data); };
                    p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) lock (se) se.AppendLine(e.Data); };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    if (!p.WaitForExit(timeoutMs))
                    {
                        try { p.Kill(); } catch { }
                        if (sink != null) sink.AppendLine("! 超时");
                        return -1;
                    }
                }
                if (sink != null)
                {
                    string o, e2;
                    lock (so) o = so.ToString().TrimEnd();
                    lock (se) e2 = se.ToString().TrimEnd();
                    if (o.Length > 0) sink.AppendLine(o);
                    if (e2.Length > 0) sink.AppendLine(e2);
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (sink != null) sink.AppendLine("! " + ex.Message);
                return -2;
            }
        }

        public static string Capture(string file, string args, Encoding enc)
        {
            try
            {
                var psi = new ProcessStartInfo(file, args);
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                if (enc != null) psi.StandardOutputEncoding = enc;
                using (var p = Process.Start(psi))
                {
                    string o = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(120000);
                    return o;
                }
            }
            catch { return ""; }
        }

        public static Encoding Oem
        {
            get
            {
                try { return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage); }
                catch { return Encoding.Default; }
            }
        }

        public static long FreeOf(string anyPath)
        {
            try { return new DriveInfo(Path.GetPathRoot(anyPath)).AvailableFreeSpace; }
            catch { return 0; }
        }
    }
}
