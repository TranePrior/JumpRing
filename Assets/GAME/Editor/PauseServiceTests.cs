using System;
using System.Reflection;
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

        [Test]
        public void ResetState_ClearsLeftoverPause()
        {
            PauseService.Add(PauseReason.Ad);

            InvokeResetState();

            Assert.IsFalse(PauseService.IsPaused, "A pause left over from a previous session must not survive the reset.");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
        }

        [Test]
        public void ResetState_DropsSubscribersFromThePreviousSession()
        {
            // With domain reload disabled, both the reason flags and the subscriber list survive
            // between play sessions. The subscribers point at objects destroyed with the previous
            // session, so the reset must drop them before it applies the cleared state.
            var notifications = 0;
            Action stale = () => notifications++;
            PauseService.ReasonsChanged += stale;

            InvokeResetState();
            PauseService.Add(PauseReason.Ad);

            Assert.AreEqual(0, notifications, "A subscriber registered before the reset must never be notified after it.");
        }

        private static void InvokeResetState()
        {
            var reset = typeof(PauseService).GetMethod(
                "ResetState", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(reset, "ResetState not found");
            reset.Invoke(null, null);
        }
    }
}
