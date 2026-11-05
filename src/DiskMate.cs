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

        public static readonly Font Base = new Font(Family, 9.5f);
        public static readonly Font Bold = new Font(Family, 9.5f, FontStyle.Bold);
        public static readonly Font Small = new Font(Family, 8.5f);
        public static readonly Font SmallBold = new Font(Family, 8.5f, FontStyle.Bold);
        public static readonly Font H1 = new Font(Family, 15f, FontStyle.Bold);
        public static readonly Font H2 = new Font(Family, 11.5f, FontStyle.Bold);
        public static readonly Font Hero = new Font(Family, 19f, FontStyle.Bold);
        public static readonly Font Num = new Font(Family, 16f, FontStyle.Bold);
        public static readonly Font Mono = new Font("Consolas", 9.5f);

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

        public static long ClearDir(string dir, StringBuilder log)
        {
            long freed = 0;
            int skipped = 0;
            if (!Directory.Exists(dir)) return 0;
            string[] files;
            try { files = Directory.GetFiles(dir); } catch { files = new string[0]; }
            foreach (string f in files)
            {
                try
                {
                    long len = 0;
                    try { len = new FileInfo(f).Length; } catch { }
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
                try
                {
                    long len = DirSize(d);
                    Directory.Delete(d, true);
                    freed += len;
                }
                catch { skipped++; }
            }
            if (log != null && skipped > 0) log.AppendLine("    跳过占用中的项目 " + skipped + " 个");
            return freed;
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
