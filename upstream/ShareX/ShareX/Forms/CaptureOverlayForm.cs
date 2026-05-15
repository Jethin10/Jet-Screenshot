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
using ShareX.ScreenCaptureLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ShareX
{
    public class CaptureOverlayForm : Form
    {
        private const int ScreenMargin = 18;
        private static CaptureOverlayForm instance;

        private readonly List<CaptureOverlayCard> cards = new List<CaptureOverlayCard>();

        protected override bool ShowWithoutActivation => true;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && Visible && cards.Count > 0)
            {
                HideStackTemporary();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= (int)WindowStyles.WS_EX_TOOLWINDOW;
                return createParams;
            }
        }

        private CaptureOverlayForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.Magenta;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            TransparencyKey = Color.Magenta;

            KeyPreview = true;

            MouseEnter += (_, __) => UpdateCardStates();
            MouseLeave += (_, __) => UpdateCardStates();
            FormClosed += (_, __) =>
            {
                CaptureOverlayPeekForm.HidePeek();
                instance = null;
            };
        }

        private void HideStackTemporary()
        {
            if (!Visible || cards.Count == 0)
            {
                return;
            }

            CaptureOverlayPeekForm.ShowPeek(RestoreStackFromPeek);
            Hide();
        }

        private void RestoreStackFromPeek()
        {
            if (IsDisposed || cards.Count == 0)
            {
                return;
            }

            Show();
            NativeMethods.SetWindowPos(Handle, (IntPtr)NativeConstants.HWND_TOPMOST, Left, Top, Width, Height, SetWindowPosFlags.SWP_NOACTIVATE);
        }

        public static void RequestHideStackTemporary()
        {
            instance?.HideStackTemporary();
        }

        public static void RestoreStack()
        {
            instance?.RestoreStackFromPeek();
        }

        public static void ShowCapture(WorkerTask task)
        {
            if (task?.Info == null || string.IsNullOrEmpty(task.Info.FilePath) || !File.Exists(task.Info.FilePath))
            {
                return;
            }

            if (Program.MainForm == null || Program.MainForm.IsDisposed)
            {
                return;
            }

            if (Program.MainForm.InvokeRequired)
            {
                Program.MainForm.BeginInvoke((Action)(() => ShowCapture(task)));
                return;
            }

            if (instance == null || instance.IsDisposed)
            {
                instance = new CaptureOverlayForm();
            }

            instance.AddCapture(task);
        }

        private void AddCapture(WorkerTask task)
        {
            CaptureOverlayCard card = new CaptureOverlayCard(task);
            card.DismissRequested += Card_DismissRequested;
            card.EditRequested += Card_EditRequested;
            card.PinRequested += Card_PinRequested;
            card.HoldRequested += _ => UpdateCardStates();
            card.DragCompleted += Card_DragCompleted;

            Controls.Add(card);
            cards.Insert(0, card);

            while (cards.Count > CaptureOverlayStackLayout.MaxCards)
            {
                RemoveCard(cards[cards.Count - 1], false);
            }

            CaptureOverlayPeekForm.HidePeek();
            LayoutCards(animateNewCapture: true, animateRelayout: true, brandNewCard: card);

            if (!Visible)
            {
                Show();
                // Removed BeginFormEntranceNudge(); to eliminate whole-form location animation lag
            }

            NativeMethods.SetWindowPos(Handle, (IntPtr)NativeConstants.HWND_TOPMOST, Left, Top, Width, Height, SetWindowPosFlags.SWP_NOACTIVATE);
        }

        private void BeginFormEntranceNudge()
        {
            // Removed. Animating a TransparencyKey form's Location causes severe DWM lag in WinForms.
            // The cards will still animate smoothly inside the form instead.
        }

        private void Card_EditRequested(CaptureOverlayCard card)
        {
            string filePath = card.Task.Info.FilePath;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            TaskSettings taskSettings = card.Task.Info.TaskSettings ?? TaskSettings.GetDefaultTaskSettings();

            // Load image and open legacy editor directly — no selector dialog
            Bitmap bmp = ImageHelpers.LoadImage(filePath);
            if (bmp == null) return;

            bmp = ImageHelpers.NonIndexedBitmap(bmp);
            Bitmap editedBmp = null;

            using (bmp)
            {
                RegionCaptureOptions options = taskSettings.CaptureSettingsReference.SurfaceOptions;

                using (RegionCaptureForm form = new RegionCaptureForm(RegionCaptureMode.Editor, options, bmp))
                {
                    form.ImageFilePath = filePath;

                    form.SaveImageRequested += (output, newFilePath) =>
                    {
                        using (output)
                        {
                            if (string.IsNullOrEmpty(newFilePath)) newFilePath = filePath;
                            ImageHelpers.SaveImage(output, newFilePath);
                        }
                        return newFilePath;
                    };

                    form.SaveImageAsRequested += (output, newFilePath) =>
                    {
                        using (output)
                        {
                            if (string.IsNullOrEmpty(newFilePath)) newFilePath = filePath;
                            newFilePath = ImageHelpers.SaveImageFileDialog(output, newFilePath);
                        }
                        return newFilePath;
                    };

                    form.CopyImageRequested += TaskHelpers.MainFormCopyImage;
                    form.UploadImageRequested += output => TaskHelpers.MainFormUploadImage(output, taskSettings);
                    form.PrintImageRequested += TaskHelpers.MainFormPrintImage;

                    bool wasVisible = Visible;
                    if (wasVisible) Hide();

                    form.ShowDialog();

                    if (wasVisible)
                    {
                        Show();
                        NativeMethods.SetWindowPos(Handle, (IntPtr)NativeConstants.HWND_TOPMOST, Left, Top, Width, Height, SetWindowPosFlags.SWP_NOACTIVATE);
                    }

                    switch (form.Result)
                    {
                        case RegionResult.Close:
                        case RegionResult.AnnotateCancelTask:
                            break;
                        case RegionResult.Region:
                        case RegionResult.AnnotateRunAfterCaptureTasks:
                            editedBmp = form.GetResultImage();
                            break;
                        case RegionResult.Fullscreen:
                        case RegionResult.AnnotateContinueTask:
                            editedBmp = (Bitmap)form.Canvas.Clone();
                            break;
                    }
                }
            }

            if (editedBmp != null)
            {
                try
                {
                    // Save edited image back to the same file
                    ImageHelpers.SaveImage(editedBmp, filePath);
                    editedBmp.Dispose();

                    // Refresh the card thumbnail from disk
                    card.RefreshThumbnail();
                }
                catch { }
            }

            // Ensure the card stays open and interactive after returning from editor
            card.TouchInteract();
            CaptureOverlayForm.RestoreStack();
            instance?.UpdateCardStates();
        }

        private void Card_PinRequested(CaptureOverlayCard card)
        {
            TaskHelpers.PinToScreen(card.Task.Info.FilePath, card.Task.Info.TaskSettings);
            RemoveCard(card);
        }

        private void Card_DragCompleted(CaptureOverlayCard card, DragDropEffects effect)
        {
            if (effect != DragDropEffects.None)
            {
                RemoveCard(card);
            }
        }

        private void Card_DismissRequested(CaptureOverlayCard card)
        {
            RemoveCard(card);
        }

        private void RemoveCard(CaptureOverlayCard card)
        {
            RemoveCard(card, true);
        }

        private void RemoveCard(CaptureOverlayCard card, bool relayout)
        {
            if (card == null || !cards.Remove(card))
            {
                return;
            }

            Controls.Remove(card);
            card.Dispose();

            if (cards.Count == 0)
            {
                Hide();
                return;
            }

            if (relayout)
            {
                LayoutCards(animateNewCapture: false, animateRelayout: true, brandNewCard: null);
            }
        }

        private void LayoutCards(bool animateNewCapture, bool animateRelayout, CaptureOverlayCard brandNewCard)
        {
            SuspendLayout();

            if (cards.Count == 0)
            {
                ClientSize = Size.Empty;
                ResumeLayout();
                return;
            }

            var previousLocations = new Dictionary<CaptureOverlayCard, Point>();
            foreach (CaptureOverlayCard c in cards)
            {
                previousLocations[c] = c.Location;
            }

            var items = CaptureOverlayStackLayout.Calculate(cards.Count);

            for (int i = cards.Count - 1; i >= 0; i--)
            {
                CaptureOverlayCard card = cards[i];
                CaptureOverlayStackItem item = items[i];
                card.SetPrimaryState(item.IsPrimary);

                bool isBrandNew = brandNewCard != null && ReferenceEquals(card, brandNewCard);
                Point target = item.Location;

                if (isBrandNew && animateNewCapture)
                {
                    card.ApplyBounds(target, item.Size, playAppear: true);
                }
                else if (animateRelayout && previousLocations.TryGetValue(card, out Point oldPt) && card.HasCompletedInitialLayout &&
                         (oldPt != target || card.Size != item.Size))
                {
                    card.BeginRelayoutSlide(oldPt, target, item.Size);
                }
                else
                {
                    card.ApplyBoundsImmediate(target, item.Size);
                }

                card.BringToFront();
            }

            ClientSize = CaptureOverlayStackLayout.GetContainerSize(cards.Count);

            Rectangle workingArea = CaptureHelpers.GetActiveScreenWorkingArea();
            Location = new Point(Math.Max(workingArea.Left + ScreenMargin, workingArea.Right - Width - ScreenMargin),
                Math.Max(workingArea.Top + ScreenMargin, workingArea.Bottom - Height - ScreenMargin));

            ResumeLayout();
            UpdateCardStates();
        }

        private void UpdateCardStates()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                // All cards are fully visible in CleanShot X layout
                cards[i].SetContextVisibility(true);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        // P/Invoke for creating large drag cursors (Bitmap.GetHicon() is limited to ~32px)
        [StructLayout(LayoutKind.Sequential)]
        private struct CURSOR_ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref CURSOR_ICONINFO piconinfo);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private static Cursor CreateLargeCursor(Bitmap bmp, int hotspotX, int hotspotY)
        {
            if (bmp == null) return null;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr hMask = IntPtr.Zero;
            try
            {
                hBitmap = bmp.GetHbitmap(Color.FromArgb(0, 0, 0, 0));
                using (Bitmap mask = new Bitmap(bmp.Width, bmp.Height))
                {
                    using (Graphics mg = Graphics.FromImage(mask))
                    {
                        mg.Clear(Color.Black);
                    }
                    hMask = mask.GetHbitmap();
                }
                CURSOR_ICONINFO ci = new CURSOR_ICONINFO
                {
                    fIcon = false,
                    xHotspot = hotspotX,
                    yHotspot = hotspotY,
                    hbmMask = hMask,
                    hbmColor = hBitmap
                };
                IntPtr hCursor = CreateIconIndirect(ref ci);
                if (hCursor != IntPtr.Zero)
                {
                    return new Cursor(hCursor);
                }
            }
            catch { }
            finally
            {
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                if (hMask != IntPtr.Zero) DeleteObject(hMask);
            }
            return null;
        }

        private sealed class CaptureOverlayCard : Panel
        {
            private const int CardCornerRadius = 10;
            private const int SideInset = 3;
            private const int ThumbTop = 3;
            private const int BottomReserve = 3;
            private bool isHovering;

            // Card-level drag support
            private Rectangle cardDragBox;
            private Cursor dragCursor;

            public bool HasCompletedInitialLayout { get; private set; }

            private readonly TaskThumbnailPanel thumbnailPanel;
            private readonly Button btnCopyCapsule;
            private readonly Button btnSaveCapsule;
            private readonly Button btnCornerPin;
            private readonly Button btnCornerClose;

            private readonly Button btnCornerEdit;
            private readonly Button btnCornerUpload;
            private readonly Timer dismissTimer;
            private readonly ContextMenuStrip contextMenu;
            private readonly CaptureOverlayLifetime lifetime;
            private Font symbolFont;
            private readonly bool supportsImageActions;
            private readonly bool supportsVideoActions;

            private Timer motionTimer;
            private MotionPhase motionPhase;
            private int motionFrame;
            private Point appearTargetLocation;
            private Point dismissStartLocation;
            private Point relayoutStartLocation;
            private Point relayoutTargetLocation;
            private Action motionCompleteCallback;

            private enum MotionPhase
            {
                Idle,
                Appear,
                Dismiss,
                Relayout
            }

            private const int MotionIntervalMs = 12;
            private const int AppearFrames = 8;
            private const int DismissFrames = 7;
            private const int RelayoutFrames = 9;
            private const int AppearSlidePx = 20;
            private const int DismissSlidePx = 24;

            // NOTE: WS_EX_LAYERED was removed. It blocked mouse events
            // to all child controls (buttons, thumbnail) on Windows,
            // making click and drag completely non-functional.

            public WorkerTask Task { get; }
            public bool IsPrimary { get; private set; }
            public bool HeldOpen => lifetime.IsHeldOpen;

            public event Action<CaptureOverlayCard> DismissRequested;
            public event Action<CaptureOverlayCard> EditRequested;
            public event Action<CaptureOverlayCard> PinRequested;
            public event Action<CaptureOverlayCard> HoldRequested;
            public event Action<CaptureOverlayCard, DragDropEffects> DragCompleted;

            public CaptureOverlayCard(WorkerTask task)
            {
                Task = task;
                supportsImageActions = CaptureOverlayMediaSupport.SupportsImageActions(Task?.Info?.FilePath);
                supportsVideoActions = CaptureOverlayMediaSupport.SupportsVideoActions(Task?.Info?.FilePath);
                lifetime = new CaptureOverlayLifetime(DateTime.UtcNow);

                // Opaque dark BackColor — Region clips the rounded shape.
                // Pixels outside Region show form's Magenta (TransparencyKey).
                BackColor = Color.FromArgb(255, 28, 30, 34);
                DoubleBuffered = true;
                Padding = new Padding(0);

                symbolFont = CreateFluentSymbolFont(9f);

                thumbnailPanel = new TaskThumbnailPanel(task)
                {
                    ClickAction = supportsImageActions ? ThumbnailViewClickAction.Select : ThumbnailViewClickAction.OpenFile,
                    TitleLocation = ThumbnailTitleLocation.Bottom,
                    TitleVisible = false
                };
                thumbnailPanel.UpdateThumbnail(GetPreviewImageForThumbnail());
                thumbnailPanel.UpdateStatus();
                thumbnailPanel.MouseEnter += Interactive_MouseEnter;
                thumbnailPanel.MouseLeave += Interactive_MouseLeave;
                thumbnailPanel.ThumbnailDragCompleted += ThumbnailPanel_ThumbnailDragCompleted;

                // Create drag cursor image — screenshot thumbnail follows cursor
                thumbnailPanel.DragCursorImage = CreateDragCursorPreview(task);

                // No filename label — CleanShot X shows only the image

                // CleanShot X style: compact toolbar buttons in a row
                // Close | Edit | Copy | Save | Upload — small 24px icons
                btnCornerClose = CreateToolbarButton("\uE711");
                btnCornerClose.Click += (_, __) =>
                {
                    TouchInteract();
                    BeginDismissThenNotify();
                };

                btnCornerEdit = CreateToolbarButton("\uE70F");
                btnCornerEdit.Click += (_, __) =>
                {
                    TouchInteract();
                    if (supportsImageActions)
                    {
                        EditRequested?.Invoke(this);
                    }
                    else if (supportsVideoActions)
                    {
                        FileHelpers.OpenFile(Task.Info.FilePath);
                    }
                };

                btnCopyCapsule = CreateToolbarButton("\uE8C8"); // Copy icon
                btnCopyCapsule.Click += (_, __) =>
                {
                    TouchInteract();
                    if (supportsImageActions)
                    {
                        ClipboardHelpers.CopyImageFromFile(Task.Info.FilePath);
                    }
                    else
                    {
                        ClipboardHelpers.CopyFile(Task.Info.FilePath);
                    }
                };

                btnSaveCapsule = CreateToolbarButton("\uE74E"); // Save icon
                btnSaveCapsule.Click += (_, __) =>
                {
                    TouchInteract();
                    if (supportsImageActions)
                    {
                        Bitmap bmp = Task.Image;
                        bool disposeBmp = false;

                        if (bmp == null && File.Exists(Task.Info.FilePath))
                        {
                            bmp = ImageHelpers.LoadImage(Task.Info.FilePath);
                            disposeBmp = bmp != null;
                        }

                        if (bmp != null)
                        {
                            try
                            {
                                ImageHelpers.SaveImageFileDialog(bmp, Task.Info.FilePath);
                            }
                            finally
                            {
                                if (disposeBmp)
                                {
                                    bmp.Dispose();
                                }
                            }
                        }
                    }
                    else
                    {
                        SaveNonImageFile();
                    }
                };

                btnCornerUpload = CreateToolbarButton("\uE898"); // Upload icon
                btnCornerUpload.Click += (_, __) =>
                {
                    TouchInteract();
                    UploadManager.UploadFile(Task.Info.FilePath, Task.Info.TaskSettings);
                };



                btnCornerPin = CreateToolbarButton("\uE718"); // Pin icon
                btnCornerPin.Click += (_, __) =>
                {
                    TouchInteract();
                    if (supportsImageActions)
                    {
                        RunDismissAnimation(() => PinRequested?.Invoke(this));
                    }
                };

                WireChromeHover(btnCornerPin);
                WireChromeHover(btnCornerClose);
                WireChromeHover(btnCornerEdit);
                WireChromeHover(btnCornerUpload);

                WireChromeHover(btnCopyCapsule);
                WireChromeHover(btnSaveCapsule);

                contextMenu = CreateContextMenu();
                contextMenu.Opening += (_, __) =>
                {
                    if (!IsPrimary)
                    {
                        return;
                    }

                    lifetime.BeginHold(DateTime.UtcNow);
                    HoldRequested?.Invoke(this);
                };
                contextMenu.Closed += (_, __) =>
                {
                    if (!IsPrimary)
                    {
                        return;
                    }

                    lifetime.EndHold(DateTime.UtcNow);
                    SyncChromeVisibility(true);
                };
                ContextMenuStrip = contextMenu;
                thumbnailPanel.ContextMenuStrip = contextMenu;

                dismissTimer = new Timer
                {
                    Interval = 50,
                    Enabled = true
                };
                dismissTimer.Tick += DismissTimer_Tick;

                Controls.Add(thumbnailPanel);
                Controls.Add(btnCornerClose);

                Controls.Add(btnCornerEdit);
                Controls.Add(btnCopyCapsule);
                Controls.Add(btnSaveCapsule);
                Controls.Add(btnCornerUpload);
                Controls.Add(btnCornerPin);

                MouseEnter += Interactive_MouseEnter;
                MouseLeave += Interactive_MouseLeave;
                MouseDown += Card_MouseDown;
                MouseMove += Card_MouseMove;
                MouseUp += Card_MouseUp;
                Paint += CaptureOverlayCard_Paint;

                HandleCreated += (_, __) => ApplyCircularRegions();
                SyncChromeVisibility(false);
            }

            // SetLayerAlpha removed — WS_EX_LAYERED was causing all click/drag
            // failures. We now use Visible = true/false for show/hide.
            private void SetCardVisible(bool visible)
            {
                if (IsHandleCreated && Visible != visible)
                {
                    Visible = visible;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    dismissTimer?.Dispose();
                    motionTimer?.Dispose();
                    motionTimer = null;

                    if (dragCursor != null)
                    {
                        dragCursor.Dispose();
                        dragCursor = null;
                    }

                    if (symbolFont != null)
                    {
                        symbolFont.Dispose();
                        symbolFont = null;
                    }
                }

                base.Dispose(disposing);
            }

            private static Font CreateFluentSymbolFont(float size)
            {
                string[] candidates = { "Segoe Fluent Icons", "Segoe MDL2 Assets" };

                foreach (string family in candidates)
                {
                    try
                    {
                        return new Font(family, size, FontStyle.Regular, GraphicsUnit.Point);
                    }
                    catch
                    {
                    }
                }

                return new Font("Segoe UI Symbol", size, FontStyle.Regular, GraphicsUnit.Point);
            }

            // All toolbar buttons are the same: small 24px icon buttons
            private Button CreateToolbarButton(string glyph)
            {
                Button btn = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    Font = symbolFont,
                    ForeColor = Color.FromArgb(190, 192, 200),
                    BackColor = Color.FromArgb(255, 46, 48, 56),
                    Size = new Size(24, 24),
                    TabStop = false,
                    Text = glyph,
                    UseVisualStyleBackColor = false,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 68, 70, 80);
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 38, 40, 48);

                return btn;
            }

            // Card-level drag — lets you drag the file from anywhere on the card
            private void Card_MouseDown(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    Size dragSize = new Size(10, 10);
                    cardDragBox = new Rectangle(
                        new Point(e.X - dragSize.Width / 2, e.Y - dragSize.Height / 2),
                        dragSize);
                }
            }

            private void Card_MouseMove(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && cardDragBox != Rectangle.Empty && !cardDragBox.Contains(e.X, e.Y))
                {
                    if (Task.Info != null && !string.IsNullOrEmpty(Task.Info.FilePath) && File.Exists(Task.Info.FilePath))
                    {
                        // Create drag cursor with screenshot thumbnail
                        CreateDragCursorFromImage();

                        try
                        {
                            using (DragDropDataObjectPackage dataPackage = DragDropDataObjectFactory.CreateForFile(Task.Info.FilePath))
                            {
                                if (dataPackage == null)
                                {
                                    cardDragBox = Rectangle.Empty;
                                    return;
                                }

                                cardDragBox = Rectangle.Empty;
                                DragDropEffects effect = DoDragDrop(dataPackage.DataObject, DragDropEffects.Copy | DragDropEffects.Move);

                                if (effect != DragDropEffects.None)
                                {
                                    DragCompleted?.Invoke(this, effect);
                                }
                            }
                        }
                        finally
                        {
                            DisposeDragCursor();
                        }
                    }
                    else
                    {
                        cardDragBox = Rectangle.Empty;
                    }
                }
            }

            private void Card_MouseUp(object sender, MouseEventArgs e)
            {
                cardDragBox = Rectangle.Empty;
            }

            // Show the screenshot thumbnail as cursor during drag
            protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
            {
                if (dragCursor != null)
                {
                    gfbevent.UseDefaultCursors = false;
                    Cursor.Current = dragCursor;
                }
                else
                {
                    base.OnGiveFeedback(gfbevent);
                }
            }

            private void CreateDragCursorFromImage()
            {
                DisposeDragCursor();

                try
                {
                    Bitmap source = GetBestPreviewImage(Task.Info.FilePath);
                    bool disposeSource = false;

                    if (source != null)
                    {
                        disposeSource = true;
                    }

                    if (source != null)
                    {
                        using (Bitmap dragThumb = CreateRoundedDragThumbnail(source, 128, 10))
                        {
                            if (dragThumb != null)
                            {
                                dragCursor = CreateLargeCursor(dragThumb, dragThumb.Width / 2, dragThumb.Height / 2);
                            }
                        }

                        if (disposeSource)
                        {
                            source.Dispose();
                        }
                    }
                }
                catch
                {
                    dragCursor = null;
                }
            }

            private void DisposeDragCursor()
            {
                if (dragCursor != null)
                {
                    dragCursor.Dispose();
                    dragCursor = null;
                }
            }

            private static Bitmap CreateRoundedDragThumbnail(Bitmap source, int maxSize, int radius)
            {
                if (source == null) return null;

                try
                {
                    float scale = Math.Min((float)maxSize / source.Width, (float)maxSize / source.Height);
                    int thumbW = Math.Max(1, (int)(source.Width * scale));
                    int thumbH = Math.Max(1, (int)(source.Height * scale));
                    int pad = 8;
                    int totalW = thumbW + pad * 2;
                    int totalH = thumbH + pad * 2;

                    Bitmap result = new Bitmap(totalW, totalH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(result))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.Clear(Color.Transparent);

                        // Drop shadow layers for floating effect
                        for (int s = 5; s >= 1; s--)
                        {
                            int alpha = Math.Max(1, 22 / s);
                            using (SolidBrush sb = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                            using (GraphicsPath sp = CreateRoundedRectangle(
                                new Rectangle(pad - s / 2, pad + s, thumbW + s, thumbH + s / 2), radius + 2))
                            {
                                g.FillPath(sb, sp);
                            }
                        }

                        // Rounded thumbnail
                        using (GraphicsPath clip = CreateRoundedRectangle(
                            new Rectangle(pad, pad, thumbW, thumbH), radius))
                        {
                            g.SetClip(clip);
                            g.DrawImage(source, pad, pad, thumbW, thumbH);
                            g.ResetClip();
                            using (Pen border = new Pen(Color.FromArgb(45, 255, 255, 255), 1f))
                            {
                                g.DrawPath(border, clip);
                            }
                        }
                    }

                    return result;
                }
                catch
                {
                    return null;
                }
            }

            private void ApplyCircularRegions()
            {
                ApplyRoundedRegion(btnCornerClose);
                ApplyRoundedRegion(btnCornerEdit);
                ApplyRoundedRegion(btnCopyCapsule);
                ApplyRoundedRegion(btnSaveCapsule);
                ApplyRoundedRegion(btnCornerUpload);
                ApplyRoundedRegion(btnCornerPin);
            }

            private static void ApplyRoundedRegion(Control c)
            {
                if (!c.IsHandleCreated || c.Width < 4 || c.Height < 4)
                {
                    return;
                }

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(0, 0, c.Width - 1, c.Height - 1);
                    c.Region = new Region(gp);
                }
            }

            private void WireChromeHover(Control c)
            {
                c.MouseEnter += Interactive_MouseEnter;
                c.MouseLeave += Interactive_MouseLeave;
            }

            public void TouchInteract()
            {
                lifetime.Touch(DateTime.UtcNow);
                HoldRequested?.Invoke(this);
            }

            private static float ClampMotionT(float t) => t < 0f ? 0f : (t > 1f ? 1f : t);

            private static float EaseOutCubicMotion(float t)
            {
                t = ClampMotionT(t);
                return 1f - (float)Math.Pow(1 - t, 3);
            }

            private static float EaseInCubicMotion(float t)
            {
                t = ClampMotionT(t);
                return t * t * t;
            }

            private void EnsureMotionTimer()
            {
                if (motionTimer != null)
                {
                    return;
                }

                motionTimer = new Timer
                {
                    Interval = MotionIntervalMs
                };
                motionTimer.Tick += MotionTimer_Tick;
            }

            private void MotionTimer_Tick(object sender, EventArgs e)
            {
                motionFrame++;

                if (motionPhase == MotionPhase.Appear)
                {
                    float t = motionFrame / (float)AppearFrames;

                    if (t >= 1f)
                    {
                        Location = appearTargetLocation;
                        SetCardVisible(true);
                        motionPhase = MotionPhase.Idle;
                        motionTimer.Stop();
                        HasCompletedInitialLayout = true;
                        return;
                    }

                    float u = EaseOutCubicMotion(t);
                    int y = appearTargetLocation.Y + (int)Math.Round(AppearSlidePx * (1 - u));
                    Location = new Point(appearTargetLocation.X, y);
                    SetCardVisible(true);
                    return;
                }

                if (motionPhase == MotionPhase.Dismiss)
                {
                    float t = motionFrame / (float)DismissFrames;

                    if (t >= 1f)
                    {
                        Location = new Point(dismissStartLocation.X, dismissStartLocation.Y + DismissSlidePx);
                        SetCardVisible(false);
                        motionPhase = MotionPhase.Idle;
                        motionTimer.Stop();
                        Action cb = motionCompleteCallback;
                        motionCompleteCallback = null;
                        cb?.Invoke();
                        return;
                    }

                    float u = EaseInCubicMotion(t);
                    int y = dismissStartLocation.Y + (int)Math.Round(DismissSlidePx * u);
                    Location = new Point(dismissStartLocation.X, y);
                    return;
                }

                if (motionPhase == MotionPhase.Relayout)
                {
                    float t = motionFrame / (float)RelayoutFrames;

                    if (t >= 1f)
                    {
                        Location = relayoutTargetLocation;
                        SetCardVisible(true);
                        motionPhase = MotionPhase.Idle;
                        motionTimer.Stop();
                        HasCompletedInitialLayout = true;
                        return;
                    }

                    float u = EaseOutCubicMotion(t);
                    int x = relayoutStartLocation.X + (int)Math.Round((relayoutTargetLocation.X - relayoutStartLocation.X) * u);
                    int y = relayoutStartLocation.Y + (int)Math.Round((relayoutTargetLocation.Y - relayoutStartLocation.Y) * u);
                    Location = new Point(x, y);
                }
            }

            public void ApplyBoundsImmediate(Point targetLocation, Size targetSize)
            {
                motionTimer?.Stop();
                motionPhase = MotionPhase.Idle;
                motionCompleteCallback = null;

                Size = targetSize;
                appearTargetLocation = targetLocation;
                Location = targetLocation;
                SetCardVisible(true);
                UpdatePresentation();
                HasCompletedInitialLayout = true;
            }

            public void ApplyBounds(Point targetLocation, Size targetSize, bool playAppear)
            {
                motionTimer?.Stop();
                motionPhase = MotionPhase.Idle;
                motionCompleteCallback = null;

                Size = targetSize;
                appearTargetLocation = targetLocation;
                UpdatePresentation();

                if (playAppear && IsPrimary)
                {
                    motionPhase = MotionPhase.Appear;
                    motionFrame = 0;
                    Location = new Point(appearTargetLocation.X, appearTargetLocation.Y + AppearSlidePx);
                    SetCardVisible(true);
                    EnsureMotionTimer();
                    motionTimer.Start();
                }
                else
                {
                    Location = appearTargetLocation;
                    SetCardVisible(true);
                    HasCompletedInitialLayout = true;
                }
            }

            public void BeginRelayoutSlide(Point from, Point targetLocation, Size targetSize)
            {
                dismissTimer.Stop();
                motionTimer?.Stop();
                motionCompleteCallback = null;
                motionPhase = MotionPhase.Idle;

                Size = targetSize;
                appearTargetLocation = targetLocation;
                relayoutStartLocation = from;
                relayoutTargetLocation = targetLocation;
                Location = from;
                SetCardVisible(true);
                UpdatePresentation();

                motionPhase = MotionPhase.Relayout;
                motionFrame = 0;
                EnsureMotionTimer();
                motionTimer.Start();
            }

            public void RunDismissAnimation(Action onComplete)
            {
                if (motionPhase == MotionPhase.Dismiss)
                {
                    return;
                }

                dismissTimer.Stop();
                motionTimer?.Stop();

                if (motionPhase == MotionPhase.Appear)
                {
                    Location = appearTargetLocation;
                    motionPhase = MotionPhase.Idle;
                }

                SetCardVisible(true);
                motionPhase = MotionPhase.Dismiss;
                motionFrame = 0;
                dismissStartLocation = Location;
                motionCompleteCallback = onComplete;
                EnsureMotionTimer();
                motionTimer.Start();
            }

            private void BeginDismissThenNotify()
            {
                RunDismissAnimation(() => DismissRequested?.Invoke(this));
            }

            private async void TriggerScrollingCapture()
            {
                await TaskHelpers.OpenScrollingCapture(Task.Info?.TaskSettings);
            }

            public void UpdatePresentation()
            {
                // Thumbnail fills the entire card with minimal border
                int thumbHeight = Math.Max(60, Height - ThumbTop - BottomReserve);

                thumbnailPanel.Location = new Point(SideInset, ThumbTop);
                thumbnailPanel.ThumbnailSize = new Size(Math.Max(80, Width - SideInset * 2), thumbHeight);
                thumbnailPanel.UpdateThumbnail(GetPreviewImageForThumbnail());

                // Toolbar overlays bottom of the thumbnail on hover
                const int btnSize = 24;
                const int btnGap = 6;
                int toolbarBtnCount = 6;
                int totalToolbarWidth = toolbarBtnCount * btnSize + (toolbarBtnCount - 1) * btnGap;
                int toolbarX = (Width - totalToolbarWidth) / 2;
                int toolbarY = Height - btnSize - 8; // 8px from bottom

                Button[] toolbar = { btnCornerClose, btnCornerEdit, btnCopyCapsule, btnSaveCapsule, btnCornerUpload, btnCornerPin };
                for (int i = 0; i < toolbar.Length; i++)
                {
                    toolbar[i].Location = new Point(toolbarX + i * (btnSize + btnGap), toolbarY);
                }


                // Region clips panel to rounded rect
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(Point.Empty, Size), CardCornerRadius))
                {
                    Region = new Region(path);
                }

                ApplyCircularRegions();

                foreach (Button b in toolbar)
                {
                    b.BringToFront();
                }

                Invalidate();
            }

            /// <summary>
            /// Reloads the thumbnail after the image has been edited in-place.
            /// Reads the updated file from disk and refreshes the drag cursor.
            /// </summary>
            public void RefreshThumbnail()
            {
                // Pass null to force reload from disk via Task.Info.FilePath
                thumbnailPanel.UpdateThumbnail(null);

                // Rebuild drag cursor with the edited image from disk
                thumbnailPanel.DragCursorImage?.Dispose();
                try
                {
                    using (Bitmap fresh = GetBestPreviewImage(Task.Info.FilePath))
                    {
                        thumbnailPanel.DragCursorImage = fresh != null
                            ? CreateRoundedDragThumbnail(fresh, 128, 10)
                            : null;
                    }
                }
                catch
                {
                    thumbnailPanel.DragCursorImage = null;
                }

                Invalidate();
            }


            public void SetPrimaryState(bool isPrimary)
            {
                if (IsPrimary != isPrimary)
                {
                    IsPrimary = isPrimary;
                    lifetime.Reset(DateTime.UtcNow);
                }
                else
                {
                    IsPrimary = isPrimary;
                }
            }

            public void SetContextVisibility(bool isFrontCard)
            {
                SyncChromeVisibility(isFrontCard);
            }

            private void SyncChromeVisibility(bool isFrontCard)
            {
                // All buttons only visible on hover (CleanShot X style)
                bool showActions = isHovering;
                btnCopyCapsule.Visible = showActions;
                btnSaveCapsule.Visible = showActions;
                btnCornerPin.Visible = showActions && supportsImageActions;
                btnCornerClose.Visible = showActions;
                btnCornerEdit.Visible = showActions;
                btnCornerUpload.Visible = showActions;

                Invalidate();
            }

            private void DismissTimer_Tick(object sender, EventArgs e)
            {
                bool pointerInside = ClientRectangle.Contains(PointToClient(Cursor.Position));

                if (isHovering && !pointerInside)
                {
                    isHovering = false;
                    lifetime.EndHold(DateTime.UtcNow);
                    SyncChromeVisibility(true);
                }
                else if (!isHovering && pointerInside)
                {
                    isHovering = true;
                    lifetime.BeginHold(DateTime.UtcNow);
                    HoldRequested?.Invoke(this);
                    SyncChromeVisibility(true);
                }

                if (!IsPrimary)
                {
                    return;
                }

                if (lifetime.ShouldDismiss(DateTime.UtcNow, pointerInside))
                {
                    dismissTimer.Stop();
                    BeginDismissThenNotify();
                }
            }

            private void ThumbnailPanel_ThumbnailDragCompleted(TaskThumbnailPanel panel, DragDropEffects effect)
            {
                dismissTimer.Stop();
                DragCompleted?.Invoke(this, effect);
            }

            private ContextMenuStrip CreateContextMenu()
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                if (supportsImageActions)
                {
                    menu.Items.Add("Annotate…", null, (_, __) =>
                    {
                        lifetime.Touch(DateTime.UtcNow);
                        EditRequested?.Invoke(this);
                    });
                    menu.Items.Add("Copy image", null, (_, __) => ClipboardHelpers.CopyImageFromFile(Task.Info.FilePath));
                    menu.Items.Add("Pin to screen", null, (_, __) =>
                    {
                        TouchInteract();
                        RunDismissAnimation(() => PinRequested?.Invoke(this));
                    });
                }
                else if (supportsVideoActions)
                {
                    menu.Items.Add("Open", null, (_, __) => FileHelpers.OpenFile(Task.Info.FilePath));
                    menu.Items.Add("Copy file", null, (_, __) => ClipboardHelpers.CopyFile(Task.Info.FilePath));
                }

                menu.Items.Add("Upload", null, (_, __) => UploadManager.UploadFile(Task.Info.FilePath, Task.Info.TaskSettings));
                menu.Items.Add("Show in folder", null, (_, __) => FileHelpers.OpenFolderWithFile(Task.Info.FilePath));
                menu.Items.Add("Hide overlays temporarily", null, (_, __) => CaptureOverlayForm.RequestHideStackTemporary());
                menu.Items.Add("Dismiss", null, (_, __) => BeginDismissThenNotify());
                ShareXResources.ApplyCustomThemeToContextMenuStrip(menu);
                return menu;
            }

            private Bitmap GetPreviewImageForThumbnail()
            {
                return supportsImageActions ? Task.Image : null;
            }

            private void SaveNonImageFile()
            {
                if (string.IsNullOrEmpty(Task?.Info?.FilePath) || !File.Exists(Task.Info.FilePath))
                {
                    return;
                }

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.FileName = Path.GetFileName(Task.Info.FilePath);
                    saveFileDialog.InitialDirectory = Path.GetDirectoryName(Task.Info.FilePath);
                    saveFileDialog.OverwritePrompt = true;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(saveFileDialog.FileName))
                    {
                        File.Copy(Task.Info.FilePath, saveFileDialog.FileName, true);
                    }
                }
            }

            private Bitmap CreateDragCursorPreview(WorkerTask task)
            {
                using (Bitmap preview = GetBestPreviewImage(task?.Info?.FilePath))
                {
                    return preview != null ? CreateRoundedDragThumbnail(preview, 128, 10) : null;
                }
            }

            private static Bitmap GetBestPreviewImage(string filePath)
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return null;
                }

                try
                {
                    if (FileHelpers.IsImageFile(filePath))
                    {
                        return ImageHelpers.LoadImage(filePath);
                    }

                    if (FileHelpers.IsVideoFile(filePath))
                    {
                        return NativeMethods.GetFileThumbnail(filePath, new Size(256, 256));
                    }
                }
                catch
                {
                }

                return null;
            }

            private void Interactive_MouseEnter(object sender, EventArgs e)
            {
                // Rely on DismissTimer_Tick for robust hover detection instead
            }

            private void Interactive_MouseLeave(object sender, EventArgs e)
            {
                // Rely on DismissTimer_Tick for robust hover detection instead
            }

            private void CaptureOverlayCard_Paint(object sender, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle cardRect = new Rectangle(0, 0, Width, Height);

                // Clean dark gradient
                Color topColor = IsPrimary ? Color.FromArgb(255, 36, 38, 44) : Color.FromArgb(255, 32, 34, 38);
                Color bottomColor = IsPrimary ? Color.FromArgb(255, 24, 26, 30) : Color.FromArgb(255, 22, 24, 28);

                using (LinearGradientBrush fill = new LinearGradientBrush(cardRect, topColor, bottomColor, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(fill, cardRect);
                }

                // 1px subtle border
                using (GraphicsPath borderPath = CreateRoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), CardCornerRadius))
                using (Pen border = new Pen(Color.FromArgb(IsPrimary ? 30 : 16, 255, 255, 255)))
                {
                    g.DrawPath(border, borderPath);
                }

                // On hover: dark gradient at bottom so toolbar icons are readable
                if (isHovering)
                {
                    Rectangle gradientRect = new Rectangle(0, Height - 44, Width, 44);
                    using (LinearGradientBrush hoverGrad = new LinearGradientBrush(
                        gradientRect,
                        Color.FromArgb(0, 0, 0, 0),
                        Color.FromArgb(180, 0, 0, 0),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(hoverGrad, gradientRect);
                    }
                }
            }
        }
    }
}
