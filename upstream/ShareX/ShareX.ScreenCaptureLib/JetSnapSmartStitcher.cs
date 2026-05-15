using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ShareX.ScreenCaptureLib
{
    public static class JetSnapSmartStitcher
    {
        public static Bitmap Stitch(List<Bitmap> frames)
        {
            if (frames == null || frames.Count == 0) return null;
            if (frames.Count == 1) return (Bitmap)frames[0].Clone();

            Bitmap first = frames.First();
            Bitmap last = frames.Last();

            // 1. Find Static Borders (Sticky Headers, Footers, Sidebars)
            var borders = FindStaticBorders(first, last);
            // The scrolling viewport is the area inside the static borders
            Rectangle viewport = new Rectangle(
                borders.Left, 
                borders.Top, 
                first.Width - borders.Left - borders.Right, 
                first.Height - borders.Top - borders.Bottom);

            // Safety check: if viewport is too small, fallback to returning just the first frame
            if (viewport.Width <= 0 || viewport.Height <= 0)
            {
                return (Bitmap)first.Clone();
            }

            // 2. Calculate Offsets and Assemble
            List<int> offsets = new List<int>();
            for (int i = 0; i < frames.Count - 1; i++)
            {
                int dy = CalculateScrollOffset(frames[i], frames[i + 1], viewport);
                offsets.Add(dy);
            }

            // Calculate total height of the final image
            // Total height = Frame0.Height + sum of all dy (newly revealed vertical pixels)
            int totalNewContentHeight = offsets.Sum();
            int finalWidth = first.Width;
            int finalHeight = first.Height + totalNewContentHeight;

            Bitmap result = new Bitmap(finalWidth, finalHeight, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                // Draw the entire first frame (this includes header, initial content, and footer)
                g.DrawImage(first, 0, 0);

                int currentY = first.Height - borders.Bottom; // Start appending right above the sticky footer

                for (int i = 1; i < frames.Count; i++)
                {
                    int dy = offsets[i - 1];
                    if (dy > 0)
                    {
                        // The new content in frame[i] is the bottom 'dy' pixels of its viewport
                        Rectangle sourceRect = new Rectangle(
                            viewport.Left,
                            viewport.Bottom - dy,
                            viewport.Width,
                            dy);

                        // Draw the new content
                        g.DrawImage(frames[i], 
                            new Rectangle(viewport.Left, currentY, viewport.Width, dy),
                            sourceRect, 
                            GraphicsUnit.Pixel);
                        
                        currentY += dy;
                    }
                }

                ExtendStaticSideBorder(g, first, borders.Left, borders.Top, borders.Bottom, finalHeight, true);
                ExtendStaticSideBorder(g, first, borders.Right, borders.Top, borders.Bottom, finalHeight, false);

                // Finally, redraw the sticky footer at the very bottom
                if (borders.Bottom > 0)
                {
                    Rectangle footerSource = new Rectangle(0, first.Height - borders.Bottom, first.Width, borders.Bottom);
                    Rectangle footerDest = new Rectangle(0, finalHeight - borders.Bottom, first.Width, borders.Bottom);
                    g.DrawImage(first, footerDest, footerSource, GraphicsUnit.Pixel);
                }
            }

            return result;
        }

        private static void ExtendStaticSideBorder(Graphics g, Bitmap source, int width, int top, int bottom, int finalHeight, bool left)
        {
            if (width <= 0)
            {
                return;
            }

            int sourceHeight = source.Height - top - bottom;
            int destinationHeight = finalHeight - top - bottom;

            if (sourceHeight <= 0 || destinationHeight <= 0)
            {
                return;
            }

            int x = left ? 0 : source.Width - width;
            Rectangle sourceRect = new Rectangle(x, top, width, sourceHeight);
            Rectangle destinationRect = new Rectangle(x, top, width, destinationHeight);
            g.DrawImage(source, destinationRect, sourceRect, GraphicsUnit.Pixel);
        }

        private struct StaticBorders
        {
            public int Top, Bottom, Left, Right;
        }

        private static unsafe StaticBorders FindStaticBorders(Bitmap first, Bitmap last)
        {
            int width = first.Width;
            int height = first.Height;

            BitmapData bd1 = first.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bd2 = last.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            StaticBorders borders = new StaticBorders();
            int maxError = 5; // allow up to 5 mismatched pixels per row/col to account for subpixel rendering/anti-aliasing

            try
            {
                byte* ptr1 = (byte*)bd1.Scan0;
                byte* ptr2 = (byte*)bd2.Scan0;
                int stride = bd1.Stride;

                // Find Top
                for (int y = 0; y < height; y++)
                {
                    if (!IsRowMatch(ptr1, ptr2, stride, y, 0, width, maxError)) break;
                    borders.Top++;
                }

                // Find Bottom
                for (int y = height - 1; y > borders.Top; y--)
                {
                    if (!IsRowMatch(ptr1, ptr2, stride, y, 0, width, maxError)) break;
                    borders.Bottom++;
                }

                // Find Left
                for (int x = 0; x < width; x++)
                {
                    if (!IsColMatch(ptr1, ptr2, stride, x, borders.Top, height - borders.Bottom, maxError)) break;
                    borders.Left++;
                }

                // Find Right
                for (int x = width - 1; x > borders.Left; x--)
                {
                    if (!IsColMatch(ptr1, ptr2, stride, x, borders.Top, height - borders.Bottom, maxError)) break;
                    borders.Right++;
                }

                // Sensible limits: don't let borders consume the entire image. Max 30% per side.
                borders.Top = Math.Min(borders.Top, height / 3);
                borders.Bottom = Math.Min(borders.Bottom, height / 3);
                borders.Left = Math.Min(borders.Left, width / 3);
                borders.Right = Math.Min(borders.Right, width / 3);
            }
            finally
            {
                first.UnlockBits(bd1);
                last.UnlockBits(bd2);
            }

            return borders;
        }

        private static unsafe bool IsRowMatch(byte* ptr1, byte* ptr2, int stride, int y, int startX, int endX, int maxError)
        {
            int errors = 0;
            int* row1 = (int*)(ptr1 + y * stride);
            int* row2 = (int*)(ptr2 + y * stride);
            for (int x = startX; x < endX; x++)
            {
                if (row1[x] != row2[x])
                {
                    errors++;
                    if (errors > maxError) return false;
                }
            }
            return true;
        }

        private static unsafe bool IsColMatch(byte* ptr1, byte* ptr2, int stride, int x, int startY, int endY, int maxError)
        {
            int errors = 0;
            for (int y = startY; y < endY; y++)
            {
                int* row1 = (int*)(ptr1 + y * stride);
                int* row2 = (int*)(ptr2 + y * stride);
                if (row1[x] != row2[x])
                {
                    errors++;
                    if (errors > maxError) return false;
                }
            }
            return true;
        }

        private static unsafe int CalculateScrollOffset(Bitmap frameA, Bitmap frameB, Rectangle viewport)
        {
            BitmapData bdA = frameA.LockBits(new Rectangle(0, 0, frameA.Width, frameA.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bdB = frameB.LockBits(new Rectangle(0, 0, frameB.Width, frameB.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int bestDy = 0;
            double bestScore = 0;

            try
            {
                byte* ptrA = (byte*)bdA.Scan0;
                byte* ptrB = (byte*)bdB.Scan0;
                int stride = bdA.Stride;

                int minOverlap = Math.Min(40, Math.Max(12, viewport.Height / 8));
                int maxSearchDy = viewport.Height - minOverlap;
                int sampleXStep = Math.Max(2, viewport.Width / 180);

                for (int dy = 1; dy <= maxSearchDy; dy++)
                {
                    int overlapHeight = viewport.Height - dy;
                    int sampleYStep = Math.Max(1, overlapHeight / 100);
                    int matches = 0;
                    int samples = 0;

                    for (int yOffset = 0; yOffset < overlapHeight; yOffset += sampleYStep)
                    {
                        int yA = viewport.Top + dy + yOffset;
                        int yB = viewport.Top + yOffset;
                        byte* rowA = ptrA + yA * stride;
                        byte* rowB = ptrB + yB * stride;

                        for (int x = viewport.Left; x < viewport.Right; x += sampleXStep)
                        {
                            samples++;

                            if (PixelsClose(rowA + x * 4, rowB + x * 4))
                            {
                                matches++;
                            }
                        }
                    }

                    if (samples == 0)
                    {
                        continue;
                    }

                    double matchRatio = (double)matches / samples;

                    if (matchRatio < 0.88)
                    {
                        continue;
                    }

                    double score = matchRatio * Math.Log(samples + 1);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDy = dy;
                    }
                }
            }
            finally
            {
                frameA.UnlockBits(bdA);
                frameB.UnlockBits(bdB);
            }

            return bestDy;
        }

        private static unsafe bool PixelsClose(byte* a, byte* b)
        {
            const int tolerance = 2;
            return Math.Abs(a[0] - b[0]) <= tolerance &&
                Math.Abs(a[1] - b[1]) <= tolerance &&
                Math.Abs(a[2] - b[2]) <= tolerance &&
                Math.Abs(a[3] - b[3]) <= tolerance;
        }
    }
}
