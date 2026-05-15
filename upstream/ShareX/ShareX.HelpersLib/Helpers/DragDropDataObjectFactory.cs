#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team
*/

#endregion License Information (GPL v3)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ShareX.HelpersLib
{
    public sealed class DragDropDataObjectPackage : IDisposable
    {
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public DataObject DataObject { get; }

        internal DragDropDataObjectPackage(DataObject dataObject)
        {
            DataObject = dataObject;
        }

        internal T Track<T>(T disposable) where T : class, IDisposable
        {
            if (disposable != null)
            {
                disposables.Add(disposable);
            }

            return disposable;
        }

        public void Dispose()
        {
            for (int i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }

            disposables.Clear();
        }
    }

    public static class DragDropDataObjectFactory
    {
        public static DragDropDataObjectPackage CreateForFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            DataObject dataObject = new DataObject();
            DragDropDataObjectPackage package = new DragDropDataObjectPackage(dataObject);

            string[] paths = new[] { filePath };
            dataObject.SetData(DataFormats.FileDrop, true, paths);
            dataObject.SetData("FileNameW", true, paths);
            dataObject.SetData("FileName", true, paths);

            if (FileHelpers.IsImageFile(filePath))
            {
                AddImageRepresentations(package, filePath);
            }

            return package;
        }


        private static void AddImageRepresentations(DragDropDataObjectPackage package, string filePath)
        {
            Bitmap image = null;

            try
            {
                image = ImageHelpers.LoadImage(filePath);
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }

            if (image == null)
            {
                return;
            }

            package.Track(image);

            Bitmap imageNoTransparency = package.Track(image.CreateEmptyBitmap(PixelFormat.Format24bppRgb));

            using (Graphics g = Graphics.FromImage(imageNoTransparency))
            {
                g.Clear(Color.White);
                g.DrawImage(image, 0, 0, image.Width, image.Height);
            }

            package.DataObject.SetData(DataFormats.Bitmap, true, imageNoTransparency);

            MemoryStream pngStream = package.Track(new MemoryStream());
            image.Save(pngStream, ImageFormat.Png);
            pngStream.Position = 0;
            package.DataObject.SetData(ClipboardHelpers.FORMAT_PNG, false, pngStream);

            MemoryStream dibStream = package.Track(new MemoryStream());
            byte[] dibData = ClipboardHelpersEx.ConvertToDib(image);
            dibStream.Write(dibData, 0, dibData.Length);
            dibStream.Position = 0;
            package.DataObject.SetData(DataFormats.Dib, false, dibStream);

            // Removed DataFormats.Html to prevent Electron apps (like Discord/Slack) 
            // from treating the drop as an HTML image paste rather than a file upload.
        }

        private static string GenerateHtmlFragment(string filePath)
        {
            Uri fileUri = new Uri(filePath);
            return ClipboardHtmlFragmentBuilder.Build($"<img src=\"{fileUri.AbsoluteUri}\"/>");
        }

        private static class ClipboardHtmlFragmentBuilder
        {
            public static string Build(string html)
            {
                StringBuilder sb = new StringBuilder();

                string header = "Version:0.9\r\nStartHTML:<<<<<<<<<1\r\nEndHTML:<<<<<<<<<2\r\nStartFragment:<<<<<<<<<3\r\nEndFragment:<<<<<<<<<4\r\n";
                string startHTML = "<html>\r\n<body>\r\n";
                string startFragment = "<!--StartFragment-->";
                string endFragment = "<!--EndFragment-->";
                string endHTML = "\r\n</body>\r\n</html>";

                sb.Append(header);

                int startHTMLLength = header.Length;
                int startFragmentLength = startHTMLLength + startHTML.Length + startFragment.Length;
                int endFragmentLength = startFragmentLength + Encoding.UTF8.GetByteCount(html);
                int endHTMLLength = endFragmentLength + endFragment.Length + endHTML.Length;

                sb.Replace("<<<<<<<<<1", startHTMLLength.ToString("D10"));
                sb.Replace("<<<<<<<<<2", endHTMLLength.ToString("D10"));
                sb.Replace("<<<<<<<<<3", startFragmentLength.ToString("D10"));
                sb.Replace("<<<<<<<<<4", endFragmentLength.ToString("D10"));

                sb.Append(startHTML);
                sb.Append(startFragment);
                sb.Append(html);
                sb.Append(endFragment);
                sb.Append(endHTML);

                return sb.ToString();
            }
        }
    }
}
