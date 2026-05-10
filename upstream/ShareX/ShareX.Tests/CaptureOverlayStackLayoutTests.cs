using System.Drawing;
using Xunit;

namespace ShareX.Tests
{
    public class CaptureOverlayStackLayoutTests
    {
        [Fact]
        public void NewestCaptureGetsFrontCardPosition()
        {
            var items = CaptureOverlayStackLayout.Calculate(2);

            Assert.Equal(2, items.Count);
            Assert.True(items[0].IsPrimary);
            Assert.Equal(new Point(CaptureOverlayStackLayout.HorizontalInset, CaptureOverlayStackLayout.VerticalOffset), items[0].Location);
            Assert.Equal(CaptureOverlayStackLayout.FrontCardSize, items[0].Size);
        }

        [Fact]
        public void OlderCapturesRemainVisibleAboveNewest()
        {
            var items = CaptureOverlayStackLayout.Calculate(3);

            Assert.Equal(new Point(CaptureOverlayStackLayout.HorizontalInset, 0), items[2].Location);
            Assert.Equal(new Point(CaptureOverlayStackLayout.HorizontalInset, CaptureOverlayStackLayout.VerticalOffset), items[1].Location);
            Assert.Equal(new Point(CaptureOverlayStackLayout.HorizontalInset, CaptureOverlayStackLayout.VerticalOffset * 2), items[0].Location);
        }

        [Fact]
        public void LayoutLimitsVisibleCardsToConfiguredMaximum()
        {
            var items = CaptureOverlayStackLayout.Calculate(8);
            var container = CaptureOverlayStackLayout.GetContainerSize(8);

            Assert.Equal(CaptureOverlayStackLayout.MaxCards, items.Count);
            Assert.Equal(CaptureOverlayStackLayout.FrontCardSize.Width + CaptureOverlayStackLayout.ShadowDepth, container.Width);
            Assert.Equal(CaptureOverlayStackLayout.FrontCardSize.Height + ((CaptureOverlayStackLayout.MaxCards - 1) * CaptureOverlayStackLayout.VerticalOffset) + CaptureOverlayStackLayout.ShadowDepth, container.Height);
        }

        [Fact]
        public void DismissPolicyDoesNotDismissWhileCardIsHeldOpen()
        {
            var dismissAt = CaptureOverlayDismissPolicy.GetNextDismissAt(DateTime.UtcNow.AddSeconds(-30));

            var shouldDismiss = CaptureOverlayDismissPolicy.ShouldDismiss(DateTime.UtcNow, dismissAt, true, false);

            Assert.False(shouldDismiss);
        }

        [Fact]
        public void DismissPolicyDoesNotDismissWhilePointerIsInside()
        {
            var dismissAt = CaptureOverlayDismissPolicy.GetNextDismissAt(DateTime.UtcNow.AddSeconds(-30));

            var shouldDismiss = CaptureOverlayDismissPolicy.ShouldDismiss(DateTime.UtcNow, dismissAt, false, true);

            Assert.False(shouldDismiss);
        }

        [Fact]
        public void DismissPolicyDismissesOnlyAfterTimeoutWhenIdle()
        {
            var now = DateTime.UtcNow;
            var dismissAt = CaptureOverlayDismissPolicy.GetNextDismissAt(now);

            Assert.False(CaptureOverlayDismissPolicy.ShouldDismiss(now.AddSeconds(3), dismissAt, false, false));
            Assert.True(CaptureOverlayDismissPolicy.ShouldDismiss(now.AddMilliseconds(CaptureOverlayDismissPolicy.DismissDelayMilliseconds + 1), dismissAt, false, false));
        }
    }
}
