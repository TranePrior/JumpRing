using JumpRing.Game.Core.Services.Haptics;
using NUnit.Framework;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class HapticSchedulerTests
    {
        private const float Now = 10f;

        private HapticScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new HapticScheduler();
        }

        [Test]
        public void FirstCue_PassesThrough()
        {
            Assert.IsTrue(_scheduler.TryConsume(HapticProfiles.Get(HapticCue.Jump), Now));
        }

        [Test]
        public void SecondJumpInsideCooldown_IsDropped()
        {
            HapticProfile jump = HapticProfiles.Get(HapticCue.Jump);
            _scheduler.TryConsume(jump, Now);

            Assert.IsFalse(_scheduler.TryConsume(jump, Now + 0.03f));
        }

        [Test]
        public void SecondJumpAfterCooldown_PassesThrough()
        {
            HapticProfile jump = HapticProfiles.Get(HapticCue.Jump);
            _scheduler.TryConsume(jump, Now);

            Assert.IsTrue(_scheduler.TryConsume(jump, Now + jump.BlockMs * 0.001f));
        }

        [Test]
        public void DeathInterruptsRunningJump()
        {
            _scheduler.TryConsume(HapticProfiles.Get(HapticCue.Jump), Now);

            Assert.IsTrue(_scheduler.TryConsume(HapticProfiles.Get(HapticCue.Death), Now + 0.01f));
        }

        [Test]
        public void JumpDoesNotInterruptRunningDeath()
        {
            _scheduler.TryConsume(HapticProfiles.Get(HapticCue.Death), Now);

            Assert.IsFalse(_scheduler.TryConsume(HapticProfiles.Get(HapticCue.Jump), Now + 0.05f));
        }

        [Test]
        public void JumpPassesOnceDeathPatternFinished()
        {
            HapticProfile death = HapticProfiles.Get(HapticCue.Death);
            _scheduler.TryConsume(death, Now);

            Assert.IsTrue(_scheduler.TryConsume(HapticProfiles.Get(HapticCue.Jump), Now + death.BlockMs * 0.001f));
        }

        [Test]
        public void RepeatedDeath_IsDroppedInsideItsOwnPattern()
        {
            HapticProfile death = HapticProfiles.Get(HapticCue.Death);
            _scheduler.TryConsume(death, Now);

            Assert.IsFalse(_scheduler.TryConsume(death, Now + 0.05f));
        }

        [Test]
        public void DeathProfile_UsesPattern()
        {
            HapticProfile death = HapticProfiles.Get(HapticCue.Death);

            Assert.IsTrue(death.HasPattern);
            Assert.Greater(death.PatternMs.Length, 1);
            Assert.AreEqual(235, death.BlockMs);
        }

        [Test]
        public void JumpProfile_UsesSinglePulse()
        {
            HapticProfile jump = HapticProfiles.Get(HapticCue.Jump);

            Assert.IsFalse(jump.HasPattern);
            Assert.AreEqual(30, jump.DurationMs);
        }
        [Test]
        public void CoinDoesNotInterruptRunningJump()
        {
            _scheduler.TryConsume(HapticProfiles.Get(HapticCue.Jump), Now);

            Assert.IsFalse(_scheduler.TryConsume(HapticProfiles.Get(HapticCue.Coin), Now + 0.02f));
        }

        [Test]
        public void CoinLineInsideCooldown_PlaysOnceNotAsOneBuzz()
        {
            HapticProfile coin = HapticProfiles.Get(HapticCue.Coin);
            _scheduler.TryConsume(coin, Now);

            Assert.IsFalse(_scheduler.TryConsume(coin, Now + 0.02f));
            Assert.IsTrue(_scheduler.TryConsume(coin, Now + coin.BlockMs * 0.001f));
        }

        [Test]
        public void RecordInterruptsRunningCoin()
        {
            _scheduler.TryConsume(HapticProfiles.Get(HapticCue.Coin), Now);

            Assert.IsTrue(_scheduler.TryConsume(HapticProfiles.Get(HapticCue.Record), Now + 0.01f));
        }

        [Test]
        public void DeathInterruptsRunningRecord()
        {
            _scheduler.TryConsume(HapticProfiles.Get(HapticCue.Record), Now);

            Assert.IsTrue(_scheduler.TryConsume(HapticProfiles.Get(HapticCue.Death), Now + 0.05f));
        }

        [Test]
        public void RecordDoesNotInterruptRunningDeath()
        {
            _scheduler.TryConsume(HapticProfiles.Get(HapticCue.Death), Now);

            Assert.IsFalse(_scheduler.TryConsume(HapticProfiles.Get(HapticCue.Record), Now + 0.05f));
        }

        [Test]
        public void PurchaseProfile_UsesPattern()
        {
            HapticProfile purchase = HapticProfiles.Get(HapticCue.Purchase);

            Assert.IsTrue(purchase.HasPattern);
            Assert.AreEqual(130, purchase.BlockMs);
        }

        [Test]
        public void RecordProfile_UsesPattern()
        {
            HapticProfile record = HapticProfiles.Get(HapticCue.Record);

            Assert.IsTrue(record.HasPattern);
            Assert.AreEqual(220, record.BlockMs);
        }

        [Test]
        public void CoinProfile_UsesSinglePulse()
        {
            HapticProfile coin = HapticProfiles.Get(HapticCue.Coin);

            Assert.IsFalse(coin.HasPattern);
            Assert.AreEqual(25, coin.DurationMs);
        }
    }
}
