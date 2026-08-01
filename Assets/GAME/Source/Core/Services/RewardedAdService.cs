using System;
using System.Collections;
using JumpRing.Game.Core;
using PlatformLink;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Advertisement;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    /// <summary>
    /// How a rewarded ad ended. <see cref="Skipped"/> and <see cref="Failed"/> both mean "no
    /// reward", but they are not the same answer: one is the player declining, the other is the
    /// platform letting them down. Collapsing them made a dead ad slot cost the player their run.
    /// </summary>
    public enum RewardedAdResult
    {
        /// <summary>The video was watched through and the reward was granted.</summary>
        Rewarded,

        /// <summary>The player closed the video before earning the reward.</summary>
        Skipped,

        /// <summary>The ad broke, or never reported anything. Nothing was asked of the player.</summary>
        Failed
    }

    public sealed class RewardedAdService : MonoBehaviour
    {
        // Last-resort guard for an ad that never fires ANY terminal event. Must stay well
        // above the real duration of a rewarded video (15-30s+) — a shorter value would fire
        // mid-ad, resume the game under the still-visible ad and drop the actual reward.
        // It is also the length of time the game sits frozen when a broken ad never reports
        // anything at all, so the headroom over a real video is deliberately not generous.
        private const float AdWatchdogSeconds = 60f;

        private Action<RewardedAdResult> onAdFinished;
        private Coroutine adWatchdog;
        private bool adTerminal;
        private bool adInProgress;
        private bool rewardEarned;

        public bool CanShowAd => PLink.IsInitialized && PLink.Advertisement.RewardedAd.CanShow();

        private void OnEnable()
        {
            if (PLink.IsInitialized)
            {
                SubscribeToAd();
            }
            else
            {
                PLink.Initilized += SubscribeToAd;
            }
        }

        private void OnDisable()
        {
            PLink.Initilized -= SubscribeToAd;
            UnsubscribeFromAd();
            AbortPendingAd();
        }

        // Disabling this object kills the watchdog coroutine silently, so without this the
        // PauseReason.Ad taken in ShowAd would never be released and the game would stay at
        // timeScale 0 for the rest of the session. Callbacks are dropped rather than invoked:
        // the objects waiting on them are being torn down together with this one.
        private void AbortPendingAd()
        {
            if (!adInProgress)
            {
                return;
            }

            adTerminal = true;
            adInProgress = false;
            rewardEarned = false;
            StopWatchdog();
            PauseService.Remove(PauseReason.Ad);
            ClearCallbacks();
        }

        /// <summary>
        /// Starts a rewarded ad. <paramref name="onFinished"/> runs exactly once, with the outcome,
        /// as soon as the ad reaches a terminal state — but only when this returned true.
        /// </summary>
        /// <returns>
        /// False when the ad never started, so the caller can keep its own flow alive instead of
        /// treating a platform that had nothing to show as a player who skipped the video.
        /// </returns>
        public bool ShowAd(Action<RewardedAdResult> onFinished)
        {
            // A rewarded video takes seconds to appear on Yandex, and the button that started it
            // stays on screen for all of them. Without this, a second click re-armed adTerminal and
            // leaked the running watchdog coroutine — the orphan then fired 60s later and finalized
            // whatever unrelated ad happened to be playing by then, resuming the game under it.
            if (adInProgress)
            {
                return false;
            }

            if (!CanShowAd)
            {
                return false;
            }

            onAdFinished = onFinished;
            adTerminal = false;
            adInProgress = true;
            rewardEarned = false;
            PauseGame();
            PLink.Advertisement.RewardedAd.Show();
            StopWatchdog();
            adWatchdog = StartCoroutine(AdWatchdog());
            return true;
        }

        private void SubscribeToAd()
        {
            // Also runs as the PLink.Initilized handler; without this a second Initilized would
            // subscribe the ad callbacks twice and deliver every terminal event twice.
            PLink.Initilized -= SubscribeToAd;

            PLink.Advertisement.RewardedAd.Rewarded += OnRewarded;
            PLink.Advertisement.RewardedAd.Failed += OnFailed;
            PLink.Advertisement.RewardedAd.Closed += OnClosed;
        }

        private void UnsubscribeFromAd()
        {
            if (!PLink.IsInitialized)
            {
                return;
            }

            PLink.Advertisement.RewardedAd.Rewarded -= OnRewarded;
            PLink.Advertisement.RewardedAd.Failed -= OnFailed;
            PLink.Advertisement.RewardedAd.Closed -= OnClosed;
        }

        // On Yandex the reward is granted while the video is still on screen — Rewarded fires
        // before Closed. Finalizing here would resume the game (and unmute it) underneath a
        // visible ad and run the whole revive/reward flow where the player can't see it, so the
        // reward is only recorded and everything else waits for the ad to actually close.
        private void OnRewarded(Reward reward)
        {
            rewardEarned = true;
        }

        // An error from the platform. Whatever the player did, they were not the one who ended this.
        private void OnFailed()
        {
            FinalizeAd(rewardEarned ? RewardedAdResult.Rewarded : RewardedAdResult.Failed);
        }

        // Closed with no preceding Rewarded means the player chose to skip the video.
        private void OnClosed()
        {
            FinalizeAd(rewardEarned ? RewardedAdResult.Rewarded : RewardedAdResult.Skipped);
        }

        private void FinalizeAd(RewardedAdResult result)
        {
            if (adTerminal)
            {
                return;
            }

            adTerminal = true;
            adInProgress = false;
            rewardEarned = false;
            StopWatchdog();
            ResumeGame();

            var finished = onAdFinished;
            ClearCallbacks();
            finished?.Invoke(result);
        }

        private void PauseGame()
        {
            PauseService.Add(PauseReason.Ad);
        }

        private void ResumeGame()
        {
            StopWatchdog();
            PauseService.Remove(PauseReason.Ad);
        }

        private IEnumerator AdWatchdog()
        {
            yield return new WaitForSecondsRealtime(AdWatchdogSeconds);
            adWatchdog = null;
            // An ad that granted the reward and then never closed must still pay out. One that
            // reported nothing at all is broken, not skipped — the player never got to decide.
            FinalizeAd(rewardEarned ? RewardedAdResult.Rewarded : RewardedAdResult.Failed);
        }

        private void StopWatchdog()
        {
            if (adWatchdog != null)
            {
                StopCoroutine(adWatchdog);
                adWatchdog = null;
            }
        }

        private void ClearCallbacks()
        {
            onAdFinished = null;
        }
    }
}
