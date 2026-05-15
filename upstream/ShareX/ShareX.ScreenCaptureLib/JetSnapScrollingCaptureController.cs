using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShareX.HelpersLib;

namespace ShareX.ScreenCaptureLib
{
    /// <summary>
    /// JetSnap scrolling capture controller orchestrates the headless capture engine and UI forms.
    /// </summary>
    public static class JetSnapScrollingCaptureController
    {
        private static JetSnapScrollingCaptureEngine engine;
        private static JetSnapScrollingCaptureStopForm stopForm;
        private static ScrollingCaptureRegionForm regionForm;
        public static bool IsCapturing { get; private set; }

        public static async Task StartCaptureAsync(Action<Bitmap> onCaptureComplete)
        {
            if (IsCapturing) return;
            IsCapturing = true;

            try
            {
                engine = new JetSnapScrollingCaptureEngine();

                // 1. User selects region
                bool regionSelected = engine.SelectRegion();
                if (!regionSelected)
                {
                    onCaptureComplete?.Invoke(null);
                    return;
                }

                // 2. Show the floating "FINISH" UI
                stopForm = new JetSnapScrollingCaptureStopForm(engine.SelectedRectangle);
                stopForm.StopRequested += () => engine.StopCapture();
                stopForm.Show();

                // 3. Show the dashed region border
                regionForm = new ScrollingCaptureRegionForm(engine.SelectedRectangle);
                regionForm.Show();
                engine.BeforeCapture = () => SetFormVisible(regionForm, false);
                engine.AfterCapture = () => SetFormVisible(regionForm, true);

                // 4. Start the capture loop (blocks until user clicks FINISH)
                await engine.StartCapture();

                // 5. Result is returned
                Bitmap finalImage = engine.Result != null ? (Bitmap)engine.Result.Clone() : null;
                onCaptureComplete?.Invoke(finalImage);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex);
                onCaptureComplete?.Invoke(null);
            }
            finally
            {
                CleanupForms();
                engine?.Dispose();
                engine = null;
                IsCapturing = false;
            }
        }

        public static void StopCapture()
        {
            engine?.StopCapture();
        }

        private static void CleanupForms()
        {
            if (stopForm != null && !stopForm.IsDisposed)
            {
                try { stopForm.Close(); stopForm.Dispose(); } catch { }
                stopForm = null;
            }

            if (regionForm != null && !regionForm.IsDisposed)
            {
                try { regionForm.Close(); regionForm.Dispose(); } catch { }
                regionForm = null;
            }
        }

        private static void SetFormVisible(Form form, bool visible)
        {
            if (form == null || form.IsDisposed)
            {
                return;
            }

            void UpdateVisibility()
            {
                if (form.IsDisposed)
                {
                    return;
                }

                form.Visible = visible;
                if (visible)
                {
                    form.Refresh();
                }
            }

            try
            {
                if (form.InvokeRequired)
                {
                    form.Invoke((Action)UpdateVisibility);
                }
                else
                {
                    UpdateVisibility();
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
