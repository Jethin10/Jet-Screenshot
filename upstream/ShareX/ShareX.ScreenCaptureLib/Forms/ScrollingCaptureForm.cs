#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using ShareX.ScreenCaptureLib.Properties;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareX.ScreenCaptureLib
{
    public partial class ScrollingCaptureForm : Form
    {
        private static readonly object lockObject = new object();

        private static bool isProcessing = false;
        private static ScrollingCaptureForm instance;


        public event Action<Bitmap> UploadRequested;
        public event Action PlayNotificationSound;

        public ScrollingCaptureOptions Options { get; private set; }

        private ScrollingCaptureManager manager;
        private Point dragStartPosition;

        private ScrollingCaptureForm(ScrollingCaptureOptions options)
        {
            Options = options;

            InitializeComponent();
            ShareXResources.ApplyTheme(this, true);

            manager = new ScrollingCaptureManager(Options);
        }

        public static async Task StartStopScrollingCapture(ScrollingCaptureOptions options, Action<Bitmap> uploadRequested = null, Action playNotificationSound = null, bool showDialog = true)

        {
            if (isProcessing || (instance != null && instance.isClosing)) return;

            ScrollingCaptureForm formToStart = null;


            try
            {
                isProcessing = true;

                if (instance == null || instance.IsDisposed)
                {
                    lock (lockObject)
                    {
                        if (instance == null || instance.IsDisposed)
                        {
                            instance = new ScrollingCaptureForm(options);

                            if (uploadRequested != null)
                            {
                                instance.UploadRequested += uploadRequested;
                            }

                            if (playNotificationSound != null)
                            {
                                instance.PlayNotificationSound += playNotificationSound;
                            }

                            if (showDialog)
                            {
                                instance.Show();
                            }

                            formToStart = instance;
                        }
                    }
                }
                else
                {
                    formToStart = instance;
                }

                if (formToStart != null)
                {
                    await formToStart.StartStopScrollingCapture();
                }
            }
            finally
            {
                await Task.Delay(500); // Cooldown to prevent accidental double-triggers
                isProcessing = false;
            }


        }

        public async Task StartStopScrollingCapture()
        {
            if (manager.IsCapturing)
            {
                manager.StopCapture();
            }
            else if (!isClosing)
            {
                await SelectWindow();
            }
        }

        private bool isClosing = false;

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            isClosing = true;
            instance = null;
            base.OnFormClosed(e);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                manager?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ResetPictureBox()
        {
            Image temp = pbOutput.Image;
            pbOutput.Image = null;
            temp?.Dispose();
        }

        private async Task StartCapture()
        {
            this.Hide();

            btnCapture.Enabled = false;
            btnUpload.Enabled = false;
            btnCopy.Enabled = false;
            btnOptions.Enabled = false;
            lblResultSize.Text = "";
            ResetPictureBox();

            try
            {
                using (ScrollingCaptureStopForm stopForm = new ScrollingCaptureStopForm(manager.SelectedRectangle))
                {
                    stopForm.StopRequested += () => manager.StopCapture();
                    stopForm.Show();

                    ScrollingCaptureStatus status = await manager.StartCapture();

                    switch (status)
                    {
                        case ScrollingCaptureStatus.Failed:
                            pbStatus.Image = Resources.control_record;
                            break;
                        case ScrollingCaptureStatus.PartiallySuccessful:
                            pbStatus.Image = Resources.control_record_yellow;
                            break;
                        case ScrollingCaptureStatus.Successful:
                            pbStatus.Image = Resources.control_record_green;
                            break;
                    }

                    OnPlayNotificationSound();
                }
            }

            catch (Exception e)
            {
                DebugHelper.WriteException(e);

                e.ShowError();
            }

            btnCapture.Enabled = true;
            btnOptions.Enabled = true;

            if (Options.AutoUpload)
            {
                UploadResult();
                this.Close();
                return;
            }


            LoadImage(manager.Result);

            this.ForceActivate();

        }

        private void LoadImage(Bitmap bmp)
        {
            if (bmp != null)
            {
                btnUpload.Enabled = true;
                btnCopy.Enabled = true;
                pbOutput.Image = bmp;
                pOutput.AutoScrollPosition = new Point(0, 0);
                lblResultSize.Text = $"{bmp.Width}x{bmp.Height}";
            }
        }

        private async Task SelectWindow()
        {
            this.Hide();

            await Task.Delay(250);

            if (manager.SelectWindow())

            {
                await StartCapture();
            }
            else
            {
                this.ForceActivate();
            }
        }

        private void UploadResult()
        {
            if (manager.Result != null)
            {
                OnUploadRequested((Bitmap)manager.Result.Clone());
            }
        }

        private void CopyResult()
        {
            if (manager.Result != null)
            {
                ClipboardHelpers.CopyImage(manager.Result);
            }
        }

        protected void OnUploadRequested(Bitmap bmp)
        {
            UploadRequested?.Invoke(bmp);
        }

        protected void OnPlayNotificationSound()
        {
            PlayNotificationSound?.Invoke();
        }

        public static void StopCurrentCapture()
        {
            if (instance != null && !instance.isClosing)
            {
                instance.manager.StopCapture();
            }
        }


        private void ScrollingCaptureForm_Activated(object sender, EventArgs e)
        {
            manager.StopCapture();
        }

        private async void btnCapture_Click(object sender, EventArgs e)
        {
            await SelectWindow();
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            UploadResult();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            CopyResult();
        }

        private void btnOptions_Click(object sender, EventArgs e)
        {
            using (ScrollingCaptureOptionsForm scrollingCaptureOptionsForm = new ScrollingCaptureOptionsForm(Options))
            {
                scrollingCaptureOptionsForm.ShowDialog();
            }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            URLHelpers.OpenURL(Links.DocsScrollingScreenshot);
        }

        private void pbOutput_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && (pOutput.HorizontalScroll.Visible || pOutput.VerticalScroll.Visible))
            {
                pOutput.Cursor = Cursors.SizeAll;
                dragStartPosition = e.Location;
            }
        }

        private void pbOutput_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && (pOutput.HorizontalScroll.Visible || pOutput.VerticalScroll.Visible))
            {
                Point scrollOffset = new Point(e.X - dragStartPosition.X, e.Y - dragStartPosition.Y);
                pOutput.AutoScrollPosition = new Point(-pOutput.AutoScrollPosition.X - scrollOffset.X, -pOutput.AutoScrollPosition.Y - scrollOffset.Y);
                pOutput.Update();
            }
        }

        private void pbOutput_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pOutput.Cursor = Cursors.Default;
            }
        }

        private class ScrollingCaptureStopForm : Form
        {
            public event Action StopRequested;

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                    return cp;
                }
            }

            public ScrollingCaptureStopForm(Rectangle region)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.ShowInTaskbar = false;
                this.TopMost = true;
                this.BackColor = Color.FromArgb(25, 25, 25);
                this.Size = new Size(180, 50);


                Rectangle screen = Screen.FromPoint(region.Location).WorkingArea;
                this.Location = new Point(screen.Left + (screen.Width - this.Width) / 2, screen.Bottom - this.Height - 60);

                Button btnStop = new Button();
                btnStop.Text = "FINISH SCROLLING";
                btnStop.ForeColor = Color.White;
                btnStop.FlatStyle = FlatStyle.Flat;
                btnStop.FlatAppearance.BorderSize = 0;
                btnStop.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
                btnStop.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);
                btnStop.Dock = DockStyle.Fill;
                btnStop.Font = new Font("Segoe UI Semibold", 10f);
                btnStop.Cursor = Cursors.Hand;
                btnStop.Click += (s, e) =>
                {
                    btnStop.Enabled = false;
                    StopRequested?.Invoke();
                };



                this.Controls.Add(btnStop);

                this.Load += (s, e) =>
                {
                    this.Region = Region.FromHrgn(NativeMethods.CreateRoundRectRgn(0, 0, Width, Height, 8, 8));
                };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (Pen pen = new Pen(Color.FromArgb(80, 80, 80), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
        }
    }
}