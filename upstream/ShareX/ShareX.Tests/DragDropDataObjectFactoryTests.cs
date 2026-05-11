using ShareX.HelpersLib;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Xunit;

namespace ShareX.Tests
{
    public class DragDropDataObjectFactoryTests
    {
        [Fact]
        public void ImageFilesAdvertiseFileAndRichImageFormats()
        {
            string path = CreateTemporaryImageFile();

            try
            {
                using DragDropDataObjectPackage package = DragDropDataObjectFactory.CreateForFile(path);

                Assert.NotNull(package);
                Assert.True(package.DataObject.GetDataPresent(DataFormats.FileDrop, false));
                Assert.True(package.DataObject.GetDataPresent(DataFormats.Bitmap, false));
                Assert.True(package.DataObject.GetDataPresent(ClipboardHelpers.FORMAT_PNG, false));
                Assert.True(package.DataObject.GetDataPresent(DataFormats.Dib, false));
                Assert.True(package.DataObject.GetDataPresent(DataFormats.Html, false));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void NonImageFilesKeepFileDragBehaviorWithoutImageFormats()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".mp4");
            File.WriteAllText(path, "stub");

            try
            {
                using DragDropDataObjectPackage package = DragDropDataObjectFactory.CreateForFile(path);

                Assert.NotNull(package);
                Assert.True(package.DataObject.GetDataPresent(DataFormats.FileDrop, false));
                Assert.True(package.DataObject.GetDataPresent(DataFormats.UnicodeText, false));
                Assert.False(package.DataObject.GetDataPresent(DataFormats.Bitmap, false));
                Assert.False(package.DataObject.GetDataPresent(ClipboardHelpers.FORMAT_PNG, false));
                Assert.False(package.DataObject.GetDataPresent(DataFormats.Dib, false));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void MissingFilesReturnNullPackage()
        {
            using DragDropDataObjectPackage package = DragDropDataObjectFactory.CreateForFile(Path.Combine(Path.GetTempPath(), "missing-" + Path.GetRandomFileName() + ".png"));

            Assert.Null(package);
        }

        private static string CreateTemporaryImageFile()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");

            using Bitmap bitmap = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Transparent);
            graphics.FillRectangle(Brushes.DeepSkyBlue, 0, 0, 4, 4);
            bitmap.Save(path, ImageFormat.Png);

            return path;
        }
    }
}
