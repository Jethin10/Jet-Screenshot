#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team
*/

#endregion License Information (GPL v3)

using System;

namespace ShareX
{
    public static class CaptureOverlayDismissPolicy
    {
        public const int DismissDelayMilliseconds = 20000;

        public static DateTime GetNextDismissAt(DateTime nowUtc)
        {
            return nowUtc.AddMilliseconds(DismissDelayMilliseconds);
        }

        public static bool ShouldDismiss(DateTime nowUtc, DateTime dismissAtUtc, bool heldOpen, bool pointerInside)
        {
            if (heldOpen || pointerInside)
            {
                return false;
            }

            return nowUtc >= dismissAtUtc;
        }
    }
}
