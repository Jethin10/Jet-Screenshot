using System;
using Xunit;

namespace ShareX.Tests
{
    public class CaptureOverlayLifetimeTests
    {
        [Fact]
        public void TouchExtendsDismissWithoutHoldingCardOpen()
        {
            DateTime now = DateTime.UtcNow;
            CaptureOverlayLifetime lifetime = new CaptureOverlayLifetime(now);

            lifetime.Touch(now);

            Assert.False(lifetime.IsHeldOpen);
            Assert.False(lifetime.ShouldDismiss(now.AddMilliseconds(CaptureOverlayDismissPolicy.DismissDelayMilliseconds - 1), false));
            Assert.True(lifetime.ShouldDismiss(now.AddMilliseconds(CaptureOverlayDismissPolicy.DismissDelayMilliseconds + 1), false));
        }

        [Fact]
        public void BeginHoldPreventsDismissUntilReleased()
        {
            DateTime now = DateTime.UtcNow;
            CaptureOverlayLifetime lifetime = new CaptureOverlayLifetime(now);

            lifetime.BeginHold(now);

            Assert.True(lifetime.IsHeldOpen);
            Assert.False(lifetime.ShouldDismiss(now.AddMinutes(5), false));

            lifetime.EndHold(now.AddSeconds(1));

            Assert.False(lifetime.IsHeldOpen);
            Assert.False(lifetime.ShouldDismiss(now.AddSeconds(2), false));
        }

        [Fact]
        public void ResetClearsHeldStateAndRearmsDismissDeadline()
        {
            DateTime now = DateTime.UtcNow;
            CaptureOverlayLifetime lifetime = new CaptureOverlayLifetime(now);
            lifetime.BeginHold(now);

            lifetime.Reset(now.AddSeconds(2));

            Assert.False(lifetime.IsHeldOpen);
            Assert.False(lifetime.ShouldDismiss(now.AddSeconds(3), false));
        }
    }
}
