using JumpRing.Game.Core.Services.Haptics;
using NUnit.Framework;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// The web Vibration API cancels the running pulse on every new call, so an unthrottled tap
    /// stream turns a row of distinct ticks into one continuous buzz.
    /// </summary>
    [TestFixture]
    public sealed class ClickHapticPolicyTests
    {
        private ClickHapticPolicy policy;

        [SetUp]
        public void SetUp()
        {
            policy = new ClickHapticPolicy();
        }

        [Test]
        public void TheFirstClick_Ticks()
        {
            Assert.IsTrue(policy.TryConsume(0f), "A player's first tap must be felt.");
        }

        [Test]
        public void VibrationIsOnByDefault()
        {
            Assert.IsTrue(policy.IsEnabled);
        }

        [Test]
        public void SwitchedOff_NothingReachesTheMotor()
        {
            policy.SetEnabled(false);

            Assert.IsFalse(policy.TryConsume(0f));
            Assert.IsFalse(policy.TryConsume(100f), "Being off is not a matter of timing.");
        }

        [Test]
        public void SwitchingBackOn_TicksAgain()
        {
            policy.SetEnabled(false);
            policy.TryConsume(0f);
            policy.SetEnabled(true);

            Assert.IsTrue(policy.TryConsume(0f), "A refused tap must not claim the motor.");
        }

        [Test]
        public void AMashedButton_DoesNotBecomeOneLongBuzz()
        {
            Assert.IsTrue(policy.TryConsume(0f));
            Assert.IsFalse(policy.TryConsume(0.01f));
            Assert.IsFalse(policy.TryConsume(0.03f));
        }

        [Test]
        public void SeparatedClicks_EachGetTheirOwnTick()
        {
            Assert.IsTrue(policy.TryConsume(0f));
            Assert.IsTrue(policy.TryConsume(0.2f));
            Assert.IsTrue(policy.TryConsume(0.4f));
        }

        [Test]
        public void ARefusedClick_DoesNotPushTheWindowFurtherOut()
        {
            policy.TryConsume(0f);

            // Mashing during the window must not keep the next real tap waiting forever.
            for (float t = 0.01f; t < 0.06f; t += 0.01f)
            {
                policy.TryConsume(t);
            }

            Assert.IsTrue(policy.TryConsume(0.06f));
        }
    }
}
