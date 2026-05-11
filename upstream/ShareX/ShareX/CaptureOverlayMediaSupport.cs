#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System.IO;

namespace ShareX
{
    public static class CaptureOverlayMediaSupport
    {
        public static bool SupportsOverlay(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && File.Exists(filePath) &&
                (FileHelpers.IsImageFile(filePath) || FileHelpers.IsVideoFile(filePath));
        }

        public static bool SupportsImageActions(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && File.Exists(filePath) && FileHelpers.IsImageFile(filePath);
        }

        public static bool SupportsVideoActions(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && File.Exists(filePath) && FileHelpers.IsVideoFile(filePath);
        }
    }
}
