using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace DiskMate
{
    /// <summary>
    /// DiskMate 标志：圆角渐变方块 + 白色圆环（C = C 盘）+ 对勾（清理完成）。
    /// 侧边栏、欢迎页与 exe 图标共用同一套绘制代码。
    /// </summary>
    internal static class Logo
    {
        public static void Draw(Graphics g, RectangleF r, float radius)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundPath(r, radius))
            using (var brush = new LinearGradientBrush(
                new RectangleF(r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2),
                Color.FromArgb(114, 154, 255), Color.FromArgb(36, 82, 222), LinearGradientMode.ForwardDiagonal))
            {
                g.FillPath(brush, path);
            }

            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            float d = r.Width * 0.54f;
            float w = Math.Max(1f, r.Width * 0.085f);

            using (var pen = new Pen(Color.FromArgb(240, 255, 255, 255), w))
                g.DrawEllipse(pen, cx - d / 2f, cy - d / 2f, d, d);

            using (var pen = new Pen(Color.White, w * 1.1f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, new[]
                {
                    new PointF(cx - d * 0.25f, cy + d * 0.01f),
                    new PointF(cx - d * 0.06f, cy + d * 0.20f),
                    new PointF(cx + d * 0.27f, cy - d * 0.23f)
                });
            }
        }

        public static GraphicsPath RoundPath(RectangleF r, float radius)
        {
            var p = new GraphicsPath();
            float dd = radius * 2f;
            p.AddArc(r.X, r.Y, dd, dd, 180, 90);
            p.AddArc(r.Right - dd, r.Y, dd, dd, 270, 90);
            p.AddArc(r.Right - dd, r.Bottom - dd, dd, dd, 0, 90);
            p.AddArc(r.X, r.Bottom - dd, dd, dd, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>把标志写成多尺寸 ICO（全部使用 PNG 帧，Vista 以上系统原生支持）。</summary>
        public static void SaveIco(string path)
        {
            int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 };
            var frames = new List<byte[]>();
            foreach (int s in sizes)
            {
                using (var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Transparent);
                        float pad = s * 0.055f;
                        Draw(g, new RectangleF(pad, pad, s - pad * 2f, s - pad * 2f), s * 0.23f);
                    }
                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        frames.Add(ms.ToArray());
                    }
                }
            }

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((ushort)0);
                bw.Write((ushort)1);
                bw.Write((ushort)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    byte dim = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
                    bw.Write(dim);
                    bw.Write(dim);
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((ushort)1);
                    bw.Write((ushort)32);
                    bw.Write(frames[i].Length);
                    bw.Write(offset);
                    offset += frames[i].Length;
                }
                foreach (byte[] f in frames) bw.Write(f);
            }
        }
    }
}
