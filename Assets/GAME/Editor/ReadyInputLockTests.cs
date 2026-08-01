using NUnit.Framework;
using JumpRing.Game.Gameplay;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// The click that started an ad revive used to fall straight through into the run: the button
    /// stops blocking raycasts the moment it is pressed, and Ready is entered on that same frame.
    /// </summary>
    [TestFixture]
    public sealed class ReadyInputLockTests
    {
        private const float LockSeconds = 0.3f;

        private ReadyInputLock inputLock;

        [SetUp]
        public void SetUp()
        {
            inputLock = new ReadyInputLock();
        }

        [Test]
        public void ARevive_LocksInputOnTheSameFrame()
        {
            inputLock.Sample(isReadyAfterRevive: false, now: 10f, LockSeconds);
            inputLock.Sample(isReadyAfterRevive: true, now: 10f, LockSeconds);

            Assert.IsTrue(inputLock.IsLocked(10f),
                "The tap that caused the revive arrives on the very frame Ready is entered.");
        }

        [Test]
        public void TheLockExpiresOnTime()
        {
            inputLock.Sample(isReadyAfterRevive: false, now: 10f, LockSeconds);
            inputLock.Sample(isReadyAfterRevive: true, now: 10f, LockSeconds);

            Assert.IsTrue(inputLock.IsLocked(10f + LockSeconds - 0.01f), "Still inside the window.");
            Assert.IsFalse(inputLock.IsLocked(10f + LockSeconds),
                "The player must be able to start the run right after the window closes.");
        }

        [Test]
        public void StayingInReady_DoesNotKeepReArmingTheLock()
        {
            inputLock.Sample(isReadyAfterRevive: false, now: 0f, LockSeconds);
            inputLock.Sample(isReadyAfterRevive: true, now: 0f, LockSeconds);

            // A player who thinks it over for a while must not be locked out again every frame.
            for (float t = 0f; t <= 2f; t += 0.1f)
            {
                inputLock.Sample(isReadyAfterRevive: true, now: t, LockSeconds);
            }

            Assert.IsFalse(inputLock.IsLocked(2f), "The lock arms once per revive.");
        }

        [Test]
        public void ASecondRevive_ArmsTheLockAgain()
        {
            inputLock.Sample(isReadyAfterRevive: true, now: 0f, LockSeconds);
            inputLock.Sample(isReadyAfterRevive: false, now: 5f, LockSeconds);

            // A second death and a second revive in the same run.
            inputLock.Sample(isReadyAfterRevive: true, now: 9f, LockSeconds);

            Assert.IsTrue(inputLock.IsLocked(9f), "Every revive gets its own lock.");
        }

        [Test]
        public void ARunStartedFromTheMenu_IsNeverLocked()
        {
            // Ready reached without a revive is never sampled as locked, so the tap that started
            // the run goes on to fire the first jump in the same frame.
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                inputLock.Sample(isReadyAfterRevive: false, now: t, LockSeconds);
                Assert.IsFalse(inputLock.IsLocked(t),
                    "Tap to start must not cost the player a second tap.");
            }
        }

        [Test]
        public void NeverReviving_LeavesInputAlone()
        {
            inputLock.Sample(isReadyAfterRevive: false, now: 0f, LockSeconds);

            Assert.IsFalse(inputLock.IsLocked(0f),
                "Nothing happened — a fresh lock must not swallow the first tap of a session.");
        }
    }
}
