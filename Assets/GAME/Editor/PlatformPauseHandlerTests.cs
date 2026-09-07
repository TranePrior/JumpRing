using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Core;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// The stop button in the Yandex SDK panel reaches the game only through the platform's
    /// pause/resume events — the page stays visible, so nothing else reports it.
    /// </summary>
    [TestFixture]
    public sealed class PlatformPauseHandlerTests
    {
        private GameObject handlerObject;
        private PlatformPauseHandler handler;

        [SetUp]
        public void SetUp()
        {
            PauseService.Remove(PauseReason.All);
            Time.timeScale = 1f;
            AudioListener.pause = false;

            handlerObject = new GameObject("PlatformPauseHandler");
            handler = handlerObject.AddComponent<PlatformPauseHandler>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(handlerObject);
            PauseService.Remove(PauseReason.All);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [Test]
        public void PlatformPause_FreezesAndSilencesTheGame()
        {
            handler.OnPlatformPause();

            Assert.AreEqual(0f, Time.timeScale, "The platform stop button must freeze the game.");
            Assert.IsTrue(AudioListener.pause, "The platform stop button must silence the game.");
        }

        [Test]
        public void PlatformResume_RestoresTheGame()
        {
            handler.OnPlatformPause();
            handler.OnPlatformResume();

            Assert.AreEqual(1f, Time.timeScale, "Resuming from the panel must run the game again.");
            Assert.IsFalse(AudioListener.pause, "Resuming from the panel must restore the sound.");
        }

        [Test]
        public void DuplicatePauseEvents_ClearWithASingleResume()
        {
            // The platform pauses for its own reasons too (store window, startup ad), so the same
            // event can arrive twice around one interruption.
            handler.OnPlatformPause();
            handler.OnPlatformPause();

            handler.OnPlatformResume();

            Assert.AreEqual(1f, Time.timeScale, "A duplicated platform pause must not outlive its resume.");
        }

        [Test]
        public void PlatformResume_DoesNotOverrideAnIntentionalPause()
        {
            // The platform pauses before a fullscreen ad and resumes after it. That resume must not
            // restart a run frozen by the death dialog behind the ad.
            PauseService.Add(PauseReason.Dialog);

            handler.OnPlatformPause();
            handler.OnPlatformResume();

            Assert.AreEqual(0f, Time.timeScale,
                "A platform resume must not restart a run that a dialog deliberately froze.");
        }

        [Test]
        public void PlatformPause_SurvivesAFocusRegain()
        {
            // The panel keeps the page visible, so the browser can hand focus back while the
            // platform still holds the game stopped.
            var focusObject = new GameObject("WebGLFocusHandler");
            var focusHandler = focusObject.AddComponent<WebGLFocusHandler>();

            handler.OnPlatformPause();
            focusHandler.OnPageVisible();

            Assert.AreEqual(0f, Time.timeScale,
                "Focus coming back must not resume a game the platform stopped.");

            Object.DestroyImmediate(focusObject);
        }

        [Test]
        public void HandlerTornDown_ReleasesThePlatformPause()
        {
            // A scene reload destroys the receiver; a pause left behind would freeze the fresh
            // scene with nothing alive to release it.
            handler.OnPlatformPause();

            InvokeOnDisable();

            Assert.AreEqual(1f, Time.timeScale, "A destroyed handler must not strand the game paused.");
        }

        private void InvokeOnDisable()
        {
            // Outside play mode Unity does not run the enable/disable callbacks.
            var method = typeof(PlatformPauseHandler).GetMethod(
                "OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "OnDisable not found");
            method.Invoke(handler, null);
        }
    }
}
