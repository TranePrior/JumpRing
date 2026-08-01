using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Core;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class GameplayApiServiceTests
    {
        private const PauseReason AllReasons = PauseReason.Ad | PauseReason.FocusLost | PauseReason.Dialog | PauseReason.Popup;

        private GameObject serviceObject;
        private GameplayApiService service;

        [SetUp]
        public void SetUp()
        {
            // Clear leftover reasons BEFORE the service exists, so releasing them can't arm the
            // post-ad settle window on the fresh service and swallow this test's focus events.
            PauseService.Remove(AllReasons);

            serviceObject = new GameObject("GameplayApiService");
            service = serviceObject.AddComponent<GameplayApiService>();

            // Outside play mode Unity does not run the enable/disable callbacks, so the pause
            // subscription the service takes there has to be driven by hand.
            Invoke("OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            Invoke("OnDisable");
            Object.DestroyImmediate(serviceObject);
            PauseService.Remove(AllReasons);
        }

        private void Invoke(string methodName)
        {
            var method = typeof(GameplayApiService).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"{methodName} not found");
            method.Invoke(service, null);
        }

        /// <summary>
        /// The GameplayAPI calls themselves are WebGL-only externs, so the activity flag the
        /// service derives them from is what these tests assert on.
        /// </summary>
        private bool IsActive()
        {
            var field = typeof(GameplayApiService).GetField(
                "_isActive", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "_isActive not found");
            return (bool)field.GetValue(service);
        }

        private void Focus(bool hasFocus)
        {
            // OnApplicationFocus/OnApplicationPause misfire in the editor, so drive the shared
            // core directly — same approach as WebGLFocusHandlerTests.
            var method = typeof(GameplayApiService).GetMethod(
                "HandleFocus", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "HandleFocus not found");
            method.Invoke(service, new object[] { hasFocus });
        }

        [Test]
        public void EnteringGameplayWithFocus_ReportsGameplayActive()
        {
            service.OnStateChanged(GameState.Gameplay);

            Assert.IsTrue(IsActive(), "Gameplay with focus must be reported as active.");
        }

        [Test]
        public void AdWhileInGameplay_ReportsGameplayInactive()
        {
            service.OnStateChanged(GameState.Gameplay);

            PauseService.Add(PauseReason.Ad);

            Assert.IsFalse(IsActive(), "The platform must never count ad time as gameplay.");
        }

        [Test]
        public void FocusBurstDuringAd_DoesNotOutliveTheAd()
        {
            service.OnStateChanged(GameState.Gameplay);
            PauseService.Add(PauseReason.Ad);

            // The browser blurs the canvas while the ad covers it and may never send the matching
            // focus back. Taking that blur at face value used to leave the platform convinced the
            // player never returned, so it kept showing its promo blocks over a running game.
            Focus(false);

            PauseService.Remove(PauseReason.Ad);

            Assert.IsTrue(IsActive(), "Gameplay must be reported as active again once the ad closes.");
        }

        [Test]
        public void FocusBurstRightAfterAd_IsIgnored()
        {
            service.OnStateChanged(GameState.Gameplay);
            PauseService.Add(PauseReason.Ad);
            PauseService.Remove(PauseReason.Ad);

            // Trailing blur/focus pair emitted around the canvas as the ad tears down.
            Focus(false);
            Focus(true);

            Assert.IsTrue(IsActive(), "Post-ad focus noise must not flip the gameplay flag.");
        }

        [Test]
        public void PopupWhileInGameplay_ReportsGameplayInactive()
        {
            service.OnStateChanged(GameState.Gameplay);

            PauseService.Add(PauseReason.Popup);
            Assert.IsFalse(IsActive(), "A window over a run freezes the game — that time is not gameplay.");

            PauseService.Remove(PauseReason.Popup);
            Assert.IsTrue(IsActive(), "Closing the window must resume the gameplay report.");
        }

        [Test]
        public void RealFocusLossWhilePlaying_ReportsGameplayInactive()
        {
            service.OnStateChanged(GameState.Gameplay);

            Focus(false);

            Assert.IsFalse(IsActive(), "Leaving the tab while playing must stop the gameplay report.");
        }

        [Test]
        public void LeavingGameplay_ReportsGameplayInactive()
        {
            service.OnStateChanged(GameState.Gameplay);

            service.OnStateChanged(GameState.GameOver);

            Assert.IsFalse(IsActive(), "Only the Gameplay state counts as gameplay.");
        }
    }
}
