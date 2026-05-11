using System;

namespace ShareX
{
    public sealed class CaptureOverlayLifetime
    {
        private int holdCount;

        public CaptureOverlayLifetime(DateTime nowUtc)
        {
            dismissAtUtc = CaptureOverlayDismissPolicy.GetNextDismissAt(nowUtc);
        }

        private DateTime dismissAtUtc;

        public DateTime DismissAtUtc => dismissAtUtc;

        public bool IsHeldOpen => holdCount > 0;

        public void Touch(DateTime nowUtc)
        {
            dismissAtUtc = CaptureOverlayDismissPolicy.GetNextDismissAt(nowUtc);
        }

        public void BeginHold(DateTime nowUtc)
        {
            holdCount++;
            dismissAtUtc = CaptureOverlayDismissPolicy.GetNextDismissAt(nowUtc);
        }

        public void EndHold(DateTime nowUtc)
        {
            if (holdCount > 0)
            {
                holdCount--;
            }

            dismissAtUtc = CaptureOverlayDismissPolicy.GetNextDismissAt(nowUtc);
        }

        public void Reset(DateTime nowUtc)
        {
            holdCount = 0;
            dismissAtUtc = CaptureOverlayDismissPolicy.GetNextDismissAt(nowUtc);
        }

        public bool ShouldDismiss(DateTime nowUtc, bool pointerInside)
        {
            return CaptureOverlayDismissPolicy.ShouldDismiss(nowUtc, dismissAtUtc, IsHeldOpen, pointerInside);
        }
    }
}
