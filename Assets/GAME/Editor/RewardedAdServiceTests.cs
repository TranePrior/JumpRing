using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Core;
using JumpRing.Game.Core.Services;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class RewardedAdServiceTests
    {
        private const PauseReason AllReasons = PauseReason.Ad | PauseReason.FocusLost | PauseReason.Dialog | PauseReason.Popup;

        private GameObject serviceObject;
        private RewardedAdService service;
        private int rewardCount;
        private int failCount;
        private RewardedAdResult? lastResult;
        private int resultCount;

        [SetUp]
        public void SetUp()
        {
            PauseService.Remove(AllReasons);
            rewardCount = 0;
            failCount = 0;
            lastResult = null;
            resultCount = 0;

            serviceObject = new GameObject("RewardedAdService");
            service = serviceObject.AddComponent<RewardedAdService>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(serviceObject);
            PauseService.Remove(AllReasons);
        }

        /// <summary>
        /// Puts the service in the state <c>ShowAd</c> leaves it in. The real entry point can not
        /// be used here: it refuses to run without an initialized platform, and the watchdog
        /// coroutine it starts needs play mode.
        /// </summary>
        private void StartAd()
        {
            SetField("onAdFinished", (Action<RewardedAdResult>)(result =>
            {
                resultCount++;
                lastResult = result;

                if (result == RewardedAdResult.Rewarded)
                {
                    rewardCount++;
                }
                else
                {
                    failCount++;
                }
            }));
            SetField("adTerminal", false);
            SetField("adInProgress", true);
            SetField("rewardEarned", false);
            PauseService.Add(PauseReason.Ad);
        }

        private void SetField(string name, object value)
        {
            var field = typeof(RewardedAdService).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{name} not found");
            field.SetValue(service, value);
        }

        private void Invoke(string methodName)
        {
            var method = typeof(RewardedAdService).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"{methodName} not found");

            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var type = parameters[i].ParameterType;
                args[i] = type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            method.Invoke(service, args);
        }

        private static bool IsAdPause => PauseService.HasReason(PauseReason.Ad);

        [Test]
        public void Rewarded_DoesNotFinalizeWhileTheAdIsStillOnScreen()
        {
            StartAd();

            // Yandex grants the reward mid-video, before the ad closes. Acting on it here used to
            // resume the game and unmute it underneath a still-visible ad, and ran the whole
            // revive flow where the player could not see it.
            Invoke("OnRewarded");

            Assert.IsTrue(IsAdPause, "The game must stay paused until the ad actually closes.");
            Assert.AreEqual(0, rewardCount, "The reward must not be delivered mid-ad.");
            Assert.AreEqual(0, failCount, "Nothing failed here.");
        }

        [Test]
        public void RewardedThenClosed_DeliversTheRewardOnce()
        {
            StartAd();

            Invoke("OnRewarded");
            Invoke("OnClosed");

            Assert.IsFalse(IsAdPause, "Closing the ad must release the ad pause.");
            Assert.AreEqual(1, rewardCount, "A watched ad must pay out exactly once.");
            Assert.AreEqual(0, failCount, "A watched ad is not a failure.");
        }

        [Test]
        public void ClosedWithoutReward_ReportsTheVideoAsSkipped()
        {
            StartAd();

            Invoke("OnClosed");

            Assert.IsFalse(IsAdPause, "Closing the ad must release the ad pause.");
            Assert.AreEqual(0, rewardCount, "A skipped ad must not pay out.");
            Assert.AreEqual(1, failCount, "A skipped ad must notify the caller exactly once.");
            Assert.AreEqual(RewardedAdResult.Skipped, lastResult,
                "Closing the video is the player's own decision, not a broken ad.");
        }

        [Test]
        public void FailedWithoutReward_ReportsAPlatformFailure()
        {
            StartAd();

            // The platform broke the ad. Reporting this as a skip let callers punish the player
            // for something they were never given the chance to do.
            Invoke("OnFailed");

            Assert.IsFalse(IsAdPause, "A failed ad must release the ad pause.");
            Assert.AreEqual(0, rewardCount, "A failed ad must not pay out.");
            Assert.AreEqual(RewardedAdResult.Failed, lastResult,
                "An error from the platform is not the player skipping the video.");
        }

        [Test]
        public void RewardedThenFailed_StillDeliversTheReward()
        {
            StartAd();

            // An error after the reward was granted must not swallow it.
            Invoke("OnRewarded");
            Invoke("OnFailed");

            Assert.AreEqual(1, rewardCount, "A granted reward must survive a late error.");
            Assert.AreEqual(0, failCount, "The player watched the ad — that is not a failure.");
        }

        [Test]
        public void ShowAd_WhileAnAdIsAlreadyRunning_IsRefused()
        {
            StartAd();

            int secondCallbacks = 0;
            bool started = service.ShowAd(_ => secondCallbacks++);

            Assert.IsFalse(started, "A second ad must not start on top of a running one.");

            Invoke("OnRewarded");
            Invoke("OnClosed");

            Assert.AreEqual(1, rewardCount, "The running ad must still pay out its own caller.");
            Assert.AreEqual(0, secondCallbacks, "The refused call must not have stolen the callbacks.");
        }

        [Test]
        public void ShowAd_WithNothingToShow_LeavesTheGameAlone()
        {
            // No platform in edit mode, so this is the "the ad slot is dead" path. It used to
            // invoke onFail synchronously, which read to the caller exactly like a skipped video.
            int callbacks = 0;
            bool started = service.ShowAd(_ => callbacks++);

            Assert.IsFalse(started, "An ad that can not be shown must report that it never started.");
            Assert.AreEqual(0, callbacks, "A dead ad slot must not report an outcome at all.");
            Assert.IsFalse(IsAdPause, "A refused ad must not pause the game.");
        }

        [Test]
        public void RepeatedTerminalEvents_NotifyTheCallerOnce()
        {
            StartAd();

            Invoke("OnRewarded");
            Invoke("OnClosed");
            Invoke("OnClosed");
            Invoke("OnFailed");

            Assert.AreEqual(1, rewardCount, "Trailing ad events must not pay out again.");
            Assert.AreEqual(0, failCount, "Trailing ad events must not report a failure.");
            Assert.AreEqual(1, resultCount, "The caller must hear about the outcome exactly once.");
        }
    }
}
