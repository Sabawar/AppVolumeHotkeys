using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AppVolumeHotkeys;

internal static class AppIconFactory
{
    public static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using var background = new LinearGradientBrush(
                new Rectangle(0, 0, 32, 32),
                Color.FromArgb(24, 122, 191),
                Color.FromArgb(35, 178, 118),
                45f);
            graphics.FillEllipse(background, 2, 2, 28, 28);

            using var darkPen = new Pen(Color.FromArgb(20, 58, 74), 2f);
            graphics.DrawEllipse(darkPen, 2, 2, 28, 28);

            using var whiteBrush = new SolidBrush(Color.White);
            var speaker = new Point[]
            {
                new(8, 14),
                new(13, 14),
                new(19, 9),
                new(19, 23),
                new(13, 18),
                new(8, 18)
            };
            graphics.FillPolygon(whiteBrush, speaker);

            using var wavePen = new Pen(Color.White, 2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            graphics.DrawArc(wavePen, 18, 11, 7, 10, -45, 90);
            graphics.DrawArc(wavePen, 20, 8, 9, 16, -45, 90);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
