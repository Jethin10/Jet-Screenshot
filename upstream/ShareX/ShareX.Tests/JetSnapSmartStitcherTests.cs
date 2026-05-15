using System.Drawing;
using ShareX.ScreenCaptureLib;
using Xunit;

namespace ShareX.Tests
{
    public class JetSnapSmartStitcherTests
    {
        [Fact]
        public void StitchExtendsStaticSideBordersAcrossAppendedContent()
        {
            using Bitmap first = CreateFrame(0);
            using Bitmap second = CreateFrame(20);

            using Bitmap stitched = JetSnapSmartStitcher.Stitch(new List<Bitmap> { first, second });

            Assert.Equal(120, stitched.Height);
            Assert.Equal(Color.Red.ToArgb(), stitched.GetPixel(2, 105).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), stitched.GetPixel(97, 105).ToArgb());
        }

        private static Bitmap CreateFrame(int scrollOffset)
        {
            Bitmap frame = new Bitmap(100, 100);

            using Graphics g = Graphics.FromImage(frame);
            g.Clear(Color.White);

            using Brush header = new SolidBrush(Color.Black);
            using Brush footer = new SolidBrush(Color.Gray);
            using Brush left = new SolidBrush(Color.Red);
            using Brush right = new SolidBrush(Color.Blue);

            g.FillRectangle(header, 0, 0, 100, 10);
            g.FillRectangle(footer, 0, 90, 100, 10);
            g.FillRectangle(left, 0, 10, 5, 80);
            g.FillRectangle(right, 95, 10, 5, 80);

            for (int y = 0; y < 80; y++)
            {
                int contentY = y + scrollOffset;
                Color color = Color.FromArgb(255, contentY, 255 - contentY, (contentY * 3) % 255);
                using Brush row = new SolidBrush(color);
                g.FillRectangle(row, 5, 10 + y, 90, 1);
            }

            return frame;
        }
    }
}
