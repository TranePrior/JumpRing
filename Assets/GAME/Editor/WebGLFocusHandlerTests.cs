using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Core;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class WebGLFocusHandlerTests
    {
        private const PauseReason AllReasons = PauseReason.Ad | PauseReason.FocusLost | PauseReason.Dialog | PauseReason.Popup;

        private GameObject handlerObject;
        private WebGLFocusHandler handler;

        [SetUp]
        public void SetUp()
        {
            // Clear leftover reasons BEFORE the handler exists, so releasing them can't arm
            // the post-ad settle window on the fresh handler and swallow this test's focus events.
            PauseService.Remove(AllReasons);
            Time.timeScale = 1f;
            AudioListener.pause = false;

            handlerObject = new GameObject("WebGLFocusHandler");
            handler = handlerObject.AddComponent<WebGLFocusHandler>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(handlerObject);
            PauseService.Remove(AllReasons);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void Focus(bool hasFocus)
        {
            // The Unity OnApplicationFocus/OnApplicationPause callbacks are gated to WebGL builds
            // (they misfire on Play mode in the editor), so drive the shared core directly.
            var method = typeof(WebGLFocusHandler).GetMethod(
                "HandleFocus", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "HandleFocus not found");
            method.Invoke(handler, new object[] { hasFocus });
        }

        [Test]
        public void FocusLossWhilePlaying_PausesAndResumesToNormal()
        {
            Focus(false);
            Assert.AreEqual(0f, Time.timeScale, "Losing focus while playing must freeze the game.");

            Focus(true);
            Assert.AreEqual(1f, Time.timeScale, "Regaining focus while playing must resume to normal speed.");
        }

        [Test]
        public void FocusRegain_DoesNotOverrideIntentionalPause()
        {
            // An intentional gameplay pause (death / second-chance dialog) holds its own reason.
            PauseService.Add(PauseReason.Dialog);

            Focus(false);
            Assert.AreEqual(0f, Time.timeScale, "World must stay frozen while unfocused.");

            Focus(true);
            Assert.AreEqual(0f, Time.timeScale,
                "Regaining focus must NOT resume a run that was intentionally paused.");
        }

        [Test]
        public void AdActive_FocusEventsDoNotTouchTimeScale()
        {
            // An ad owns the pause while shown.
            PauseService.Add(PauseReason.Ad);

            Focus(false);
            Focus(true);

            Assert.AreEqual(0f, Time.timeScale, "Focus handler must stay out of the way while an ad is active.");
        }

        [Test]
        public void PageHidden_SilencesTheGame()
        {
            // Minimizing the browser or switching apps hides the page without blurring the canvas,
            // so the DOM bridge is the only thing that reports it. Yandex requirement 1.3.
            handler.OnPageHidden();

            Assert.IsTrue(AudioListener.pause, "A hidden page must silence the game.");
            Assert.AreEqual(0f, Time.timeScale, "A hidden page must freeze the game.");
        }

        [Test]
        public void PageVisibleAgain_RestoresSound()
        {
            handler.OnPageHidden();
            handler.OnPageVisible();

            Assert.IsFalse(AudioListener.pause, "Coming back to the page must restore the sound.");
            Assert.AreEqual(1f, Time.timeScale, "Coming back to the page must resume the game.");
        }

        [Test]
        public void PageHiddenDuringAd_LeavesTheAdInCharge()
        {
            PauseService.Add(PauseReason.Ad);

            handler.OnPageHidden();
            handler.OnPageVisible();

            Assert.IsTrue(AudioListener.pause, "The ad still owns the pause, so the game stays silent.");
            PauseService.Remove(PauseReason.Ad);
            Assert.IsFalse(AudioListener.pause,
                "Closing the ad must not leave a stray FocusLost pause behind.");
        }

        [Test]
        public void DuplicateFocusLossEvents_DoNotCorruptRestoredScale()
        {
            // focus-lost can arrive twice (OnApplicationFocus + OnApplicationPause) on one blur.
            Focus(false);
            Focus(false);

            Focus(true);
            Assert.AreEqual(1f, Time.timeScale,
                "A duplicated focus-loss must not leave a lingering pause after focus returns.");
        }
    }
}
