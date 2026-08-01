using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Core;
using JumpRing.Game.UI;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// A window opened over the game must freeze it. Flipping a settings toggle used to leave the
    /// run going behind the window, which was enough to die while looking at the settings.
    /// </summary>
    [TestFixture]
    public sealed class PopupPauseTests
    {
        private const PauseReason AllReasons =
            PauseReason.Ad | PauseReason.FocusLost | PauseReason.Dialog | PauseReason.Popup;

        [SetUp]
        public void SetUp()
        {
            ResetTrackerCount();
            PauseService.Remove(AllReasons);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [TearDown]
        public void TearDown()
        {
            ResetTrackerCount();
            PauseService.Remove(AllReasons);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [Test]
        public void OpeningPopup_FreezesTheGame()
        {
            var popup = OpenPopup();

            Assert.IsTrue(PauseService.HasReason(PauseReason.Popup));
            Assert.AreEqual(0f, Time.timeScale, "The game must not keep running behind an open window.");

            ClosePopup(popup);
        }

        [Test]
        public void ClosingPopup_ResumesTheGame()
        {
            var popup = OpenPopup();

            ClosePopup(popup);

            Assert.IsFalse(PauseService.HasReason(PauseReason.Popup));
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void StackedPopups_StayPausedUntilTheLastCloses()
        {
            var first = OpenPopup();
            var second = OpenPopup();

            ClosePopup(second);

            Assert.IsTrue(PauseService.HasReason(PauseReason.Popup), "One window is still open.");
            Assert.AreEqual(0f, Time.timeScale);

            ClosePopup(first);

            Assert.IsFalse(PauseService.HasReason(PauseReason.Popup));
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void PopupPause_DoesNotMuteAudio()
        {
            // The settings window toggles music and effects. Muting the listener under it would
            // make every toggle read as broken.
            var popup = OpenPopup();

            Assert.IsFalse(AudioListener.pause, "A popup freezes the game but must not silence it.");

            ClosePopup(popup);
        }

        [Test]
        public void AdOverPopup_KeepsAudioMutedUntilTheAdEnds()
        {
            var popup = OpenPopup();

            PauseService.Add(PauseReason.Ad);
            Assert.IsTrue(AudioListener.pause, "An ad still has to silence the game.");

            PauseService.Remove(PauseReason.Ad);
            Assert.IsFalse(AudioListener.pause);
            Assert.AreEqual(0f, Time.timeScale, "The window is still open — the game stays frozen.");

            ClosePopup(popup);
        }

        [Test]
        public void PopupOverDialog_ReadsAsAnExternalPause()
        {
            // SecondChancePresenter tells its own freeze apart from an external one this way. A
            // window stacked on top of it counts as external, so it must not be filtered out with
            // the dialog's own reason.
            PauseService.Add(PauseReason.Dialog);
            Assert.IsFalse(PauseService.HasAnyReasonExcept(PauseReason.Dialog));

            var popup = OpenPopup();

            Assert.IsTrue(PauseService.HasAnyReasonExcept(PauseReason.Dialog));

            ClosePopup(popup);
            PauseService.Remove(PauseReason.Dialog);
        }

        [Test]
        public void ResetState_ClearsAPopupPauseLeftFromThePreviousSession()
        {
            var popup = OpenPopup();

            InvokeTrackerStatic("ResetState");

            Assert.IsFalse(PauseService.HasReason(PauseReason.Popup),
                "A pause left over from a previous play session must not strand the game at timeScale 0.");
            Assert.AreEqual(1f, Time.timeScale);

            Object.DestroyImmediate(popup);
        }

        /// <summary>
        /// Outside play mode Unity does not run the enable/disable callbacks, so the tracker's
        /// open/close edges have to be driven by hand — same approach as GameplayApiServiceTests.
        /// </summary>
        private static GameObject OpenPopup()
        {
            var popupObject = new GameObject("Popup");
            var tracker = popupObject.AddComponent<PopupTracker>();
            InvokeTrackerInstance(tracker, "OnEnable");
            return popupObject;
        }

        private static void ClosePopup(GameObject popupObject)
        {
            InvokeTrackerInstance(popupObject.GetComponent<PopupTracker>(), "OnDisable");
            Object.DestroyImmediate(popupObject);
        }

        private static void InvokeTrackerInstance(PopupTracker tracker, string methodName)
        {
            var method = typeof(PopupTracker).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"{methodName} not found");
            method.Invoke(tracker, null);
        }

        private static void InvokeTrackerStatic(string methodName)
        {
            var method = typeof(PopupTracker).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, $"{methodName} not found");
            method.Invoke(null, null);
        }

        private static void ResetTrackerCount()
        {
            InvokeTrackerStatic("ResetState");
        }
    }
}
