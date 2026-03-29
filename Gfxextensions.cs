using System.Drawing;
using System.Drawing.Drawing2D;

namespace Producktivity
{
    /// <summary>
    /// 全局 Graphics 扩展方法，项目中唯一定义。
    /// Form1 / ChatForm / PersonalizationForm 均引用此处。
    /// </summary>
    internal static class GfxExtensions
    {
        public static void FillRoundedRect(this Graphics g, Brush br, Rectangle r, int rad)
        {
            using var path = RoundedPath(r, rad);
            g.FillPath(br, path);
        }

        public static void DrawRoundedRect(this Graphics g, Pen pen, Rectangle r, int rad)
        {
            using var path = RoundedPath(r, rad);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedPath(Rectangle r, int rad)
        {
            int d    = rad * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}