using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Core;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class WebGLFocusHandlerTests
    {
        private GameObject handlerObject;
        private WebGLFocusHandler handler;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            WebGLFocusHandler.IsAdActive = false;

            handlerObject = new GameObject("WebGLFocusHandler");
            handler = handlerObject.AddComponent<WebGLFocusHandler>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            WebGLFocusHandler.IsAdActive = false;
            Object.DestroyImmediate(handlerObject);
        }

        private void Focus(bool hasFocus)
        {
            var method = typeof(WebGLFocusHandler).GetMethod(
                "OnApplicationFocus", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "OnApplicationFocus not found");
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
            // Simulate an intentional gameplay pause (death / second-chance dialog).
            Time.timeScale = 0f;

            Focus(false);
            Assert.AreEqual(0f, Time.timeScale, "World must stay frozen while unfocused.");

            Focus(true);
            Assert.AreEqual(0f, Time.timeScale,
                "Regaining focus must NOT resume a run that was intentionally paused.");
        }

        [Test]
        public void AdActive_FocusEventsDoNotTouchTimeScale()
        {
            WebGLFocusHandler.IsAdActive = true;
            Time.timeScale = 0f; // ad already froze the game

            Focus(false);
            Focus(true);

            Assert.AreEqual(0f, Time.timeScale, "Focus handler must stay out of the way while an ad is active.");
        }

        [Test]
        public void DuplicateFocusLossEvents_DoNotCorruptRestoredScale()
        {
            // focus-lost can arrive twice (OnApplicationFocus + OnApplicationPause) on one blur.
            Focus(false);
            Focus(false);

            Focus(true);
            Assert.AreEqual(1f, Time.timeScale,
                "A duplicated focus-loss must not capture the already-zeroed scale as the restore value.");
        }
    }
}
