#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ShareX
{
    /// <summary>
    /// Thin edge strip shown when the capture overlay stack is temporarily hidden (CleanShot-style).
    /// </summary>
    internal sealed class CaptureOverlayPeekForm : Form
    {
        private static CaptureOverlayPeekForm instance;

        private Action restoreAction;

        private CaptureOverlayPeekForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Width = 36;
            Height = 132;
            BackColor = Color.FromArgb(28, 28, 32);

            Paint += (_, e) =>
            {
                Rectangle r = ClientRectangle;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath gp = CreateRoundedPath(r, 12))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(236, 42, 44, 52)))
                using (Pen p = new Pen(Color.FromArgb(90, 255, 255, 255)))
                {
                    e.Graphics.FillPath(b, gp);
                    e.Graphics.DrawPath(p, gp);
                }

                using (Font f = new Font("Segoe UI Semibold", 8f))
                using (SolidBrush tx = new SolidBrush(Color.FromArgb(230, 245, 245, 250)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString("Show\ncaptures", f, tx, ClientRectangle, sf);
                }
            };

            KeyPreview = true;

            MouseClick += (_, __) => RestoreFromPeek();

            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    RestoreFromPeek();
                }
            };
        }

        private void RestoreFromPeek()
        {
            HidePeek();
            restoreAction?.Invoke();
            restoreAction = null;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= (int)WindowStyles.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation => true;

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void ShowPeek(Action onRestore)
        {
            if (Program.MainForm == null || Program.MainForm.IsDisposed)
            {
                return;
            }

            if (Program.MainForm.InvokeRequired)
            {
                Program.MainForm.BeginInvoke((Action)(() => ShowPeek(onRestore)));
                return;
            }

            if (instance == null || instance.IsDisposed)
            {
                instance = new CaptureOverlayPeekForm();
            }

            instance.restoreAction = onRestore;

            Rectangle wa = CaptureHelpers.GetActiveScreenWorkingArea();
            instance.Location = new Point(wa.Right - instance.Width - 6,
                wa.Top + Math.Max(24, (wa.Height - instance.Height) / 2));

            if (!instance.Visible)
            {
                instance.Show();
            }

            NativeMethods.SetWindowPos(instance.Handle, (IntPtr)NativeConstants.HWND_TOPMOST,
                instance.Left, instance.Top, instance.Width, instance.Height, SetWindowPosFlags.SWP_NOACTIVATE);
        }

        public static void HidePeek()
        {
            if (instance != null && !instance.IsDisposed && instance.Visible)
            {
                instance.Hide();
            }
        }
    }
}
