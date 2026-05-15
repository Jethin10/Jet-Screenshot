using ShareX.HelpersLib;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShareX.ScreenCaptureLib
{
    public class JetSnapScrollingCaptureStopForm : Form
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

        public JetSnapScrollingCaptureStopForm(Rectangle region)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(25, 25, 25);
            this.Size = new Size(180, 50);

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Left + (screen.Width - this.Width) / 2, screen.Top + 60);

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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                SetWindowDisplayAffinity(this.Handle, WDA_EXCLUDEFROMCAPTURE);
            }
            catch { }
        }
    }
}
