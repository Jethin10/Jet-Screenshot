#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team
*/

#endregion License Information (GPL v3)

using System.Collections.Generic;
using System.Drawing;

namespace ShareX
{
    public static class CaptureOverlayStackLayout
    {
        public const int MaxCards = 5;

        // CleanShot X style — compact cards, fully visible (not overlapping)
        public static readonly Size FrontCardSize = new Size(220, 165);
        public static readonly Size BackCardSize = new Size(220, 165);

        // Gap between fully-visible cards (vertical list, not overlap stack)
        public const int CardGap = 10;
        public const int ShadowDepth = 4;

        public static List<CaptureOverlayStackItem> Calculate(int count)
        {
            List<CaptureOverlayStackItem> items = new List<CaptureOverlayStackItem>();

            int visibleCount = count > MaxCards ? MaxCards : count;

            // CleanShot X layout: newest card at bottom, older cards above
            // Cards are fully visible (no overlap), separated by CardGap
            for (int i = 0; i < visibleCount; i++)
            {
                int displayIndex = visibleCount - 1 - i;
                bool isPrimary = i == 0;
                Size size = FrontCardSize; // All cards same size

                int x = 0;
                int y = displayIndex * (FrontCardSize.Height + CardGap);

                items.Add(new CaptureOverlayStackItem
                {
                    DisplayIndex = displayIndex,
                    IsPrimary = isPrimary,
                    Location = new Point(x, y),
                    Size = size
                });
            }

            return items;
        }

        public static Size GetContainerSize(int count)
        {
            int visibleCount = count > MaxCards ? MaxCards : count;

            if (visibleCount <= 0)
            {
                return Size.Empty;
            }

            int height = (visibleCount * FrontCardSize.Height) + ((visibleCount - 1) * CardGap) + ShadowDepth;
            int width = FrontCardSize.Width + ShadowDepth;

            return new Size(width, height);
        }
    }

    public sealed class CaptureOverlayStackItem
    {
        public int DisplayIndex { get; set; }
        public bool IsPrimary { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; }
    }
}
