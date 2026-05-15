using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareX.ScreenCaptureLib
{
    public class JetSnapScrollingCaptureEngine : IDisposable
    {
        public Bitmap Result { get; private set; }
        public bool IsCapturing { get; private set; }
        public Rectangle SelectedRectangle => selectedRectangle;
        public int FrameCount => capturedFrames.Count;

        public Action BeforeCapture { get; set; }
        public Action AfterCapture { get; set; }

        private Rectangle selectedRectangle;
        private WindowInfo selectedWindow;
        private volatile bool stopRequested;
        private readonly List<Bitmap> capturedFrames = new List<Bitmap>();

        public bool SelectRegion()
        {
            return RegionCaptureTasks.GetRectangleRegion(out selectedRectangle, out selectedWindow, new RegionCaptureOptions());
        }

        public async Task StartCapture()
        {
            if (IsCapturing || selectedRectangle.IsEmpty) return;
            IsCapturing = true;
            stopRequested = false;
            capturedFrames.Clear();
            Result?.Dispose();
            Result = null;

            await Task.Run(() => RunCaptureLoop()).ConfigureAwait(false);

            if (capturedFrames.Count > 0)
            {
                DebugHelper.WriteLine($"[JetSnap] Stitching {capturedFrames.Count} frames...");
                Result = StitchFrames(capturedFrames);
                DebugHelper.WriteLine($"[JetSnap] Done: {(Result != null ? $"{Result.Width}x{Result.Height}" : "null")}");
            }

            foreach (var b in capturedFrames) b?.Dispose();
            capturedFrames.Clear();
            IsCapturing = false;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
        const uint MOUSEEVENTF_WHEEL = 0x0800;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);
        const int VK_ESCAPE = 0x1B;

        private void RunCaptureLoop()
        {
            try
            {
                var screenshot = new Screenshot { CaptureCursor = false };
                selectedWindow?.Activate();
                Thread.Sleep(400);

                Bitmap first = CaptureFrame(screenshot);
                if (first == null) return;
                capturedFrames.Add(first);
                DebugHelper.WriteLine($"[JetSnap] Frame 1: {first.Width}x{first.Height}");

                Bitmap lastFrame = first;

                int identicalCount = 0;

                while (!stopRequested)
                {
                    ScrollDown();

                    // Wait 400ms for smooth-scrolling animations in browsers to completely finish
                    int waited = 0;
                    while (waited < 400)
                    {
                        Thread.Sleep(50);
                        waited += 50;
                        
                        // Check if ESC is currently pressed OR was pressed since last check
                        if ((GetAsyncKeyState(VK_ESCAPE) & 0x8001) != 0)
                        {
                            DebugHelper.WriteLine("[JetSnap] ESC pressed, stopping auto-capture.");
                            stopRequested = true;
                            break;
                        }
                    }

                    if (stopRequested) break;

                    Bitmap current = CaptureFrame(screenshot);
                    if (current == null) continue;

                    if (ImageHelpers.CompareImages(current, lastFrame))
                    {
                        current.Dispose();
                        identicalCount++;

                        if (identicalCount == 1)
                        {
                            DebugHelper.WriteLine("[JetSnap] Mouse wheel did not move content; trying keyboard/page scroll fallback.");
                            ScrollDownFallback();
                            Thread.Sleep(500);
                            current = CaptureFrame(screenshot);

                            if (current != null && !ImageHelpers.CompareImages(current, lastFrame))
                            {
                                identicalCount = 0;
                                capturedFrames.Add(current);
                                lastFrame = current;
                                DebugHelper.WriteLine($"[JetSnap] Frame {capturedFrames.Count} captured after fallback scroll");
                                continue;
                            }

                            current?.Dispose();
                        }
                        
                        // If we scrolled 5 times and the image didn't change at all, we are definitely at the bottom.
                        if (identicalCount >= 5)
                        {
                            DebugHelper.WriteLine("[JetSnap] Hit bottom of page, stopping auto-capture.");
                            break;
                        }
                        continue;
                    }

                    identicalCount = 0;
                    capturedFrames.Add(current);
                    lastFrame = current;
                    DebugHelper.WriteLine($"[JetSnap] Frame {capturedFrames.Count} captured");
                }
            }
            catch (Exception ex) { DebugHelper.WriteException(ex); }
        }

        private void ScrollDown()
        {
            int cx = selectedRectangle.Left + selectedRectangle.Width / 2;
            int cy = selectedRectangle.Top + selectedRectangle.Height / 2;
            Cursor.Position = new Point(cx, cy);
            selectedWindow?.Activate();
            Thread.Sleep(50);

            // Small wheel steps preserve overlap for reliable stitching.
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, unchecked((uint)-360), UIntPtr.Zero);
        }

        private void ScrollDownFallback()
        {
            selectedWindow?.Activate();
            InputHelpers.SendKeyPress(VirtualKeyCode.NEXT);

            if (selectedWindow != null && selectedWindow.Handle != IntPtr.Zero)
            {
                NativeMethods.SendMessage(selectedWindow.Handle, (int)WindowsMessages.VSCROLL, (int)ScrollBarCommands.SB_PAGEDOWN, 0);
            }
        }

        private Bitmap CaptureFrame(Screenshot screenshot)
        {
            BeforeCapture?.Invoke();
            Thread.Sleep(100);
            Bitmap frame = screenshot.CaptureRectangle(selectedRectangle);
            AfterCapture?.Invoke();
            return frame;
        }

        public void StopCapture() => stopRequested = true;

        private Bitmap StitchFrames(List<Bitmap> frames)
        {
            return JetSnapSmartStitcher.Stitch(frames);
        }

        public void Dispose()
        {
            Result?.Dispose();
            foreach (var b in capturedFrames) b?.Dispose();
            capturedFrames.Clear();
        }
    }
}
