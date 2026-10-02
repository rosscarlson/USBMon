using System.Drawing;
using System.Drawing.Drawing2D;

namespace USBMon;

/// <summary>Draws a simple, dependency-free icon at runtime instead of shipping a binary asset.</summary>
internal static class AppIcon
{
    public static Icon Create()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var back = new SolidBrush(Color.FromArgb(0x1A, 0x5C, 0x9A));
            FillRoundedRectangle(g, back, new Rectangle(1, 1, 30, 30), 6);

            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("USB", font, textBrush, new RectangleF(0, 0, 32, 32), format);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle bounds, int radius)
    {
        using var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
