using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace ShareX.Tests
{
    public class CaptureOverlayMediaSupportTests
    {
        [Fact]
        public void ImageFilesAreEligibleForOverlayAndImageActions()
        {
            string path = CreateTemporaryImageFile();

            try
            {
                Assert.True(CaptureOverlayMediaSupport.SupportsOverlay(path));
                Assert.True(CaptureOverlayMediaSupport.SupportsImageActions(path));
                Assert.False(CaptureOverlayMediaSupport.SupportsVideoActions(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void VideoFilesAreEligibleForOverlayButNotImageActions()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".mp4");
            File.WriteAllText(path, "stub");

            try
            {
                Assert.True(CaptureOverlayMediaSupport.SupportsOverlay(path));
                Assert.False(CaptureOverlayMediaSupport.SupportsImageActions(path));
                Assert.True(CaptureOverlayMediaSupport.SupportsVideoActions(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void UnsupportedFilesDoNotUseOverlay()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
            File.WriteAllText(path, "stub");

            try
            {
                Assert.False(CaptureOverlayMediaSupport.SupportsOverlay(path));
                Assert.False(CaptureOverlayMediaSupport.SupportsImageActions(path));
                Assert.False(CaptureOverlayMediaSupport.SupportsVideoActions(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string CreateTemporaryImageFile()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");

            using Bitmap bitmap = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.MediumPurple);
            bitmap.Save(path, ImageFormat.Png);

            return path;
        }
    }
}
