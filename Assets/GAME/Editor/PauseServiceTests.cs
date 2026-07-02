using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Core;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class PauseServiceTests
    {
        private const PauseReason AllReasons = PauseReason.Ad | PauseReason.FocusLost | PauseReason.Dialog;

        [SetUp]
        public void SetUp()
        {
            PauseService.Remove(AllReasons);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [TearDown]
        public void TearDown()
        {
            PauseService.Remove(AllReasons);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [Test]
        public void SingleReason_PausesThenResumes()
        {
            PauseService.Add(PauseReason.Ad);
            Assert.IsTrue(PauseService.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);

            PauseService.Remove(PauseReason.Ad);
            Assert.IsFalse(PauseService.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
        }

        [Test]
        public void OverlappingReasons_StayPausedUntilAllCleared()
        {
            // This is the exact shape of the interstitial bug: an ad and a focus loss overlap,
            // and clearing focus first must NOT resume the game while the ad still holds it.
            PauseService.Add(PauseReason.Ad);
            PauseService.Add(PauseReason.FocusLost);
            Assert.AreEqual(0f, Time.timeScale);

            PauseService.Remove(PauseReason.FocusLost);
            Assert.IsTrue(PauseService.IsPaused, "Ad reason still held — must stay paused.");
            Assert.AreEqual(0f, Time.timeScale, "Releasing focus must not resume while the ad holds the pause.");

            PauseService.Remove(PauseReason.Ad);
            Assert.IsFalse(PauseService.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void DuplicateAdd_IsIdempotent()
        {
            PauseService.Add(PauseReason.Ad);
            PauseService.Add(PauseReason.Ad);

            PauseService.Remove(PauseReason.Ad);
            Assert.IsFalse(PauseService.IsPaused, "A single Remove must clear a doubly-Added reason.");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void RemoveUnheldReason_DoesNotResumeOthers()
        {
            PauseService.Add(PauseReason.Ad);

            PauseService.Remove(PauseReason.FocusLost); // never added

            Assert.IsTrue(PauseService.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
        }

        [Test]
        public void HasReason_ReflectsActiveSet()
        {
            PauseService.Add(PauseReason.Dialog);

            Assert.IsTrue(PauseService.HasReason(PauseReason.Dialog));
            Assert.IsFalse(PauseService.HasReason(PauseReason.Ad));
        }
    }
}
