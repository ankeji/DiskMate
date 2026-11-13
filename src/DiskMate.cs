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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public int fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        public const uint FO_DELETE = 0x0003;
        public const ushort FOF_ALLOWUNDO = 0x0040;
        public const ushort FOF_NOCONFIRMATION = 0x0010;
        public const ushort FOF_SILENT = 0x0004;
        public const ushort FOF_NOERRORUI = 0x0400;

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(int pid);

        // ---- 强制删除：直接走内核删除，不经过 Shell 回收站，也不受只读/隐藏/系统属性阻挡 ----
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeleteFileW(string lpFileName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privilege;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges,
            ref TOKEN_PRIVILEGES newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetFileAttributesW(string lpFileName, uint dwFileAttributes);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetFileAttributesW(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool MoveFileExW(string lpExistingFileName, string lpNewFileName, uint dwFlags);

        public const uint MOVEFILE_REPLACE_EXISTING = 0x00000001;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        public const uint GENERIC_READ_ACCESS = 0x80000000;
        public const uint OPEN_EXISTING_DISPOSITION = 3;
        public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        public const uint TOKEN_QUERY = 0x0008;
        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const uint SE_PRIVILEGE_ENABLED = 0x0002;

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
        /// <summary>写入 settings.ini 一项（保留其它键，AppData 优先）。</summary>
        public static void WriteSetting(string key, string value)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate");
                string p = Path.Combine(dir, "settings.ini");
                Directory.CreateDirectory(dir);

                var lines = new List<string>();
                bool replaced = false;
                if (File.Exists(p))
                {
                    foreach (string line in File.ReadAllLines(p, Encoding.UTF8))
                    {
                        int i = line.IndexOf('=');
                        if (i > 0 && line.Substring(0, i).Trim() == key)
                        {
                            lines.Add(key + "=" + value);
                            replaced = true;
                        }
                        else if (line.Trim().Length > 0)
                        {
                            lines.Add(line);
                        }
                    }
                }
                if (!replaced) lines.Add(key + "=" + value);
                File.WriteAllText(p, string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
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
                    case "chat":
                        // 对话气泡 + 三个点：问题反馈
                        g.DrawLines(pen, new[]
                        {
                            new PointF(ox + 4.4f * s, oy + 3.2f * s),
                            new PointF(ox + 11.6f * s, oy + 3.2f * s),
                            new PointF(ox + 13.2f * s, oy + 4.8f * s),
                            new PointF(ox + 13.2f * s, oy + 9.6f * s),
                            new PointF(ox + 11.6f * s, oy + 11.2f * s),
                            new PointF(ox + 7.4f * s, oy + 11.2f * s),
                            new PointF(ox + 5.6f * s, oy + 13.4f * s),
                            new PointF(ox + 5f * s, oy + 11.2f * s),
                            new PointF(ox + 3.4f * s, oy + 11.2f * s),
                            new PointF(ox + 2.8f * s, oy + 9.6f * s),
                            new PointF(ox + 2.8f * s, oy + 4.8f * s),
                            new PointF(ox + 4.4f * s, oy + 3.2f * s)
                        });
                        g.FillEllipse(brush, ox + 5.2f * s, oy + 6.9f * s, 1.7f * s, 1.7f * s);
                        g.FillEllipse(brush, ox + 7.6f * s, oy + 6.9f * s, 1.7f * s, 1.7f * s);
                        g.FillEllipse(brush, ox + 10f * s, oy + 6.9f * s, 1.7f * s, 1.7f * s);
                        break;
                    case "broom":
                        // 扫帚：微信专清
                        g.DrawLine(pen, ox + 12.2f * s, oy + 2.2f * s, ox + 8.6f * s, oy + 7.2f * s);
                        g.DrawPolygon(pen, new[]
                        {
                            new PointF(ox + 9.0f * s, oy + 6.6f * s),
                            new PointF(ox + 4.6f * s, oy + 8.4f * s),
                            new PointF(ox + 5.4f * s, oy + 13.8f * s),
                            new PointF(ox + 12.0f * s, oy + 10.6f * s)
                        });
                        g.DrawLine(pen, ox + 5.4f * s, oy + 11.0f * s, ox + 9.8f * s, oy + 8.6f * s);
                        g.DrawLine(pen, ox + 7.2f * s, oy + 9.2f * s, ox + 7.8f * s, oy + 12.4f * s);
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

        // 清理前落一个标记文件：如果进程在清理过程中消失（原生崩溃/BHO 崩溃），
        // 下次启动时能看到这个标记，从而告诉用户"上次是哪一步挂的"。
        private static string GuardFile()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate");
            try { Directory.CreateDirectory(dir); } catch { }
            return Path.Combine(dir, "recycle-running.flag");
        }

        public static void MarkRecycleStart()
        {
            try { File.WriteAllText(GuardFile(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); } catch { }
        }

        public static void MarkRecycleEnd()
        {
            try
            {
                string f = GuardFile();
                if (File.Exists(f)) File.Delete(f);
            }
            catch { }
        }

        /// <summary>取出上次未正常结束的标记（有则说明上次清理回收站时进程异常退出）</summary>
        public static string TakeRecycleCrashMark()
        {
            try
            {
                string f = GuardFile();
                if (!File.Exists(f)) return null;
                string t = File.ReadAllText(f).Trim();
                try { File.Delete(f); } catch { }
                return t;
            }
            catch { return null; }
        }

        // 逐盘清空回收站。
        // 顺序很重要：先自己遍历删除（纯 Win32 文件操作，不加载任何 shell 扩展），
        // 再用 SHEmptyRecycleBin 兜底清掉其它账户的残留和系统索引。
        // 反过来做的话，shell32 会加载第三方右键菜单/网盘/杀软的 shell 扩展，
        // 这些扩展一旦出问题会直接把整个进程带走 —— 表现为"点清理就闪退"，且无任何提示。
        public static long EmptyRecycleBins(StringBuilder log)
        {
            long before = RecycleBinSize();

            foreach (string root in FixedDriveRoots())
            {
                string bin = Path.Combine(root, "$Recycle.Bin");
                if (!Directory.Exists(bin)) continue;
                string[] sids;
                try { sids = Directory.GetDirectories(bin); } catch { continue; }
                foreach (string sid in sids) ClearRecycleFolder(sid, log);
            }

            // 文件已经自己删完了，Shell 这一步只做收尾。
            // 它可能被第三方 shell 扩展（杀软/网盘/右键菜单工具）卡住，所以给它 3 秒上限，超时就不等了。
            foreach (string root in FixedDriveRoots())
            {
                string driveRoot = root;
                try
                {
                    if (log != null) log.AppendLine("    回收站 " + driveRoot + " 交给系统收尾");
                    Task shell = Task.Factory.StartNew(delegate
                    {
                        try
                        {
                            int hr = Native.SHEmptyRecycleBin(IntPtr.Zero, driveRoot,
                                Native.SHERB_NOCONFIRMATION | Native.SHERB_NOPROGRESSUI | Native.SHERB_NOSOUND);
                            if (hr != 0 && log != null)
                                lock (log) log.AppendLine("    回收站 " + driveRoot + " Shell 返回 0x" + hr.ToString("X8"));
                        }
                        catch (Exception ex)
                        {
                            if (log != null) lock (log) log.AppendLine("    回收站 " + driveRoot + " Shell 失败：" + ex.Message);
                        }
                    });

                    if (!shell.Wait(3000) && log != null)
                        log.AppendLine("    回收站 " + driveRoot + " Shell 收尾超时（内容其实已经删掉了），不再等待");
                }
                catch (Exception ex)
                {
                    if (log != null) log.AppendLine("    回收站 " + driveRoot + " Shell 失败：" + ex.Message);
                }
            }

            long after = RecycleBinSize();
            return Math.Max(0, before - after);
        }

        // 批量删除到回收站（误删可还原）。pFrom 用 \0 连接、末尾再补一个 \0。
        public static int RecycleFiles(List<string> files, StringBuilder log)
        {
            if (files == null || files.Count == 0) return 0;
            var sb = new StringBuilder();
            foreach (string f in files)
            {
                if (string.IsNullOrEmpty(f)) continue;
                // 只读文件（微信的缓存全是 0444）直接删会被 shell 拒绝，先去掉只读位
                try
                {
                    FileAttributes attr = File.GetAttributes(f);
                    if ((attr & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(f, attr & ~FileAttributes.ReadOnly);
                }
                catch { }
                sb.Append(f).Append('\0');
            }
            sb.Append('\0');

            var op = new Native.SHFILEOPSTRUCT();
            op.wFunc = Native.FO_DELETE;
            op.pFrom = sb.ToString();
            op.fFlags = (ushort)(Native.FOF_ALLOWUNDO | Native.FOF_NOCONFIRMATION | Native.FOF_SILENT | Native.FOF_NOERRORUI);
            int hr = Native.SHFileOperation(ref op);
            if (hr != 0 && log != null) log.AppendLine("    回收站删除返回 0x" + hr.ToString("X4") + "（文件被占用或无权限）");

            int gone = 0;
            foreach (string f in files)
            {
                try { if (!File.Exists(f)) gone++; }
                catch { }
            }
            return gone;
        }

        /// <summary>当前进程是不是管理员（清单里已要求提权，正常情况下都是 true）</summary>
        public static bool IsElevated()
        {
            try
            {
                return new System.Security.Principal.WindowsPrincipal(
                    System.Security.Principal.WindowsIdentity.GetCurrent())
                    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        /// <summary>微信相关的进程名（4.x 是 Weixin，3.x 是 WeChat，另外还有一堆子进程）</summary>
        public static readonly string[] WeChatProcessNames =
        {
            "Weixin", "WeChat", "WeChatAppEx", "WeChatOCR", "WeChatPlayer",
            "WeChatBrowser", "WeChatUtility", "WeixinUpdate"
        };

        /// <summary>上一次强制删除时查到是谁占着（给界面提示用）</summary>
        public static List<string> LastLockers = new List<string>();

        /// <summary>
        /// 直接用管理员权限删除（不进回收站，不可恢复）。
        /// 第一轮：清 ReadOnly/Hidden/System → DeleteFileW（重试 3 次，每次间隔 120ms）。
        /// 第二轮：仍然失败的，takeown + icacls 接管所有权后再试。
        /// 最后把「是谁占着」查出来，写进日志并留给界面提示。
        /// </summary>
        public static long ForceDeleteFiles(List<string> files, StringBuilder log, out int failed)
        {
            failed = 0;
            LastLockers = new List<string>();
            long freed = 0;
            if (files == null || files.Count == 0) return 0;

            EnablePrivileges(log);

            var left = new List<string>();
            foreach (string f in files)
            {
                if (string.IsNullOrEmpty(f)) continue;
                long size = 0;
                try { if (!File.Exists(f)) continue; size = new FileInfo(f).Length; }
                catch { }
                if (TryDeleteFile(f, 3)) { freed += size; continue; }
                left.Add(f);
            }

            if (left.Count > 0)
            {
                foreach (string f in left) TakeOwnAndGrant(f, log);

                var still = new List<string>();
                foreach (string f in left)
                {
                    long size = 0;
                    try { if (!File.Exists(f)) continue; size = new FileInfo(f).Length; }
                    catch { }
                    if (TryDeleteFile(f, 2)) { freed += size; continue; }
                    still.Add(f);
                }
                left = still;
            }

            if (left.Count > 0)
            {
                failed = left.Count;
                List<string> lockers = FindLockers(left);
                LastLockers = lockers;

                if (log != null)
                {
                    int shown = 0;
                    foreach (string f in left)
                    {
                        if (shown++ >= 5) { log.AppendLine("    …另有 " + (left.Count - 5) + " 个同样删不掉"); break; }
                        log.AppendLine("    删不掉：" + f);
                    }
                    if (lockers.Count > 0)
                        log.AppendLine("    占用进程：" + string.Join("、", lockers.ToArray()));
                    else
                        log.AppendLine("    没有查到占用进程（可能需要管理员权限，或被安全软件拦截）");
                }
            }

            if (log != null)
                log.AppendLine("    强制删除：成功 " + (files.Count - failed) + " 个，失败 " + failed + " 个，释放 " + Theme.Fmt(freed));
            return freed;
        }

        private static bool TryDeleteFile(string f, int tries)
        {
            for (int i = 0; i < tries; i++)
            {
                if (DeleteOnce(f)) return true;
                try { System.Threading.Thread.Sleep(120); } catch { }
            }
            return false;
        }

        private static bool DeleteOnce(string f)
        {
            // 用 Win32 直接清属性：.NET 的 File.SetAttributes 在被占用时会静默失败，
            // 只读位没清掉，DeleteFileW 就只会返回 5（拒绝访问）—— 这就是之前一直删不掉的原因。
            try
            {
                uint attr = Native.GetFileAttributesW(f);
                if (attr != 0xFFFFFFFF)
                {
                    uint clear = 0x01 | 0x02 | 0x04;   // READONLY | HIDDEN | SYSTEM
                    if ((attr & clear) != 0) Native.SetFileAttributesW(f, attr & ~clear);
                }
            }
            catch { }

            try { if (Native.DeleteFileW(f)) return true; } catch { }
            try { File.Delete(f); if (!File.Exists(f)) return true; } catch { }

            return false;
        }

        /// <summary>takeown + icacls：接管文件所有权并给管理员完全控制</summary>
        private static bool TakeOwnAndGrant(string path, StringBuilder log)
        {
            try
            {
                bool ok = RunTool(ToolPath("takeown.exe"), "/f \"" + path + "\" /a");
                RunTool(ToolPath("icacls.exe"), "\"" + path + "\" /grant *S-1-5-32-544:F");
                if (!ok && log != null) log.AppendLine("    takeown 失败：" + path);
                return true;
            }
            catch (Exception ex)
            {
                if (log != null) log.AppendLine("    接管所有权异常：" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 32 位进程访问 System32 会被重定向到 SysWOW64，那里没有 takeown.exe，
        /// 所以 64 位系统上要优先用 Sysnative。
        /// </summary>
        private static string ToolPath(string exe)
        {
            try
            {
                string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                {
                    string native = Path.Combine(Path.Combine(windir, "Sysnative"), exe);
                    if (File.Exists(native)) return native;
                }
                string sys = Path.Combine(Path.Combine(windir, "System32"), exe);
                if (File.Exists(sys)) return sys;
            }
            catch { }
            return exe;
        }

        private static bool RunTool(string exe, string args)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(exe, args);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    if (p == null) return false;
                    p.StandardOutput.ReadToEnd();
                    p.WaitForExit(6000);
                    return p.HasExited && p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        /// <summary>
        /// 判断这些文件到底是不是被别的进程占着（独占打开：不共享任何模式）。
        /// 不追求查出具体 PID —— 微信的子进程会不断变化，知道"被占用" + "微信在不在跑"就够给用户指路了。
        /// </summary>
        public static List<string> FindLockers(List<string> paths)
        {
            var result = new List<string>();
            if (paths == null || paths.Count == 0) return result;

            int locked = 0;
            int checkedCount = 0;
            foreach (string p in paths)
            {
                if (checkedCount++ >= 12) break;
                if (IsFileLocked(p)) locked++;
            }
            if (locked == 0) return result;

            result.Add(WeChatRunning()
                ? "微信（Weixin.exe / WeChatAppEx.exe）"
                : "其他程序");
            return result;
        }

        /// <summary>以独占方式打开试试：打不开且返回 32（共享冲突）就是被占用了</summary>
        public static bool IsFileLocked(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                IntPtr h = Native.CreateFileW(path, Native.GENERIC_READ_ACCESS, 0, IntPtr.Zero,
                    Native.OPEN_EXISTING_DISPOSITION, 0x80, IntPtr.Zero);
                if (h == new IntPtr(-1))
                {
                    int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    return err == 32 || err == 33;
                }
                Native.CloseHandle(h);
                return false;
            }
            catch { return false; }
        }

        /// <summary>结束微信相关进程，返回结束掉的个数</summary>
        public static int KillWeChatProcesses()
        {
            int n = 0;
            foreach (string name in WeChatProcessNames)
            {
                System.Diagnostics.Process[] ps;
                try { ps = System.Diagnostics.Process.GetProcessesByName(name); }
                catch { continue; }

                foreach (System.Diagnostics.Process p in ps)
                {
                    try { p.Kill(); n++; }
                    catch { }
                    finally { try { p.Dispose(); } catch { } }
                }
            }
            try { System.Threading.Thread.Sleep(500); } catch { }
            return n;
        }

        /// <summary>当前有没有微信在跑</summary>
        public static bool WeChatRunning()
        {
            foreach (string name in WeChatProcessNames)
            {
                try
                {
                    System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcessesByName(name);
                    if (ps.Length > 0)
                    {
                        foreach (System.Diagnostics.Process p in ps) { try { p.Dispose(); } catch { } }
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        /// <summary>打开备份/还原/接管所有权三个特权，删别人占着的文件时更需要</summary>
        private static void EnablePrivileges(StringBuilder log)
        {
            string[] names = { "SeTakeOwnershipPrivilege", "SeBackupPrivilege", "SeRestorePrivilege" };
            IntPtr token;
            if (!Native.OpenProcessToken(Native.GetCurrentProcess(),
                    Native.TOKEN_ADJUST_PRIVILEGES | Native.TOKEN_QUERY, out token)) return;

            try
            {
                foreach (string name in names)
                {
                    Native.LUID luid;
                    if (!Native.LookupPrivilegeValue(null, name, out luid)) continue;

                    var tp = new Native.TOKEN_PRIVILEGES();
                    tp.PrivilegeCount = 1;
                    tp.Privilege.Luid = luid;
                    tp.Privilege.Attributes = Native.SE_PRIVILEGE_ENABLED;
                    Native.AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                }
            }
            finally { Native.CloseHandle(token); }
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
