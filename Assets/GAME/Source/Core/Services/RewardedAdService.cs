using System;
using System.Collections;
using JumpRing.Game.Core;
using PlatformLink;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Advertisement;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class RewardedAdService : MonoBehaviour
    {
        // Last-resort guard for an ad that never fires ANY terminal event. Must stay well
        // above the real duration of a rewarded video (15-30s+) — a shorter value would fire
        // mid-ad, resume the game under the still-visible ad and drop the actual reward.
        private const float AdWatchdogSeconds = 180f;

        private Action onRewardGranted;
        private Action onAdFailed;
        private Coroutine adWatchdog;
        private bool adTerminal;
        private bool adInProgress;

        public bool CanShowAd
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return PLink.IsInitialized && PLink.Advertisement.RewardedAd.CanShow();
#endif
            }
        }

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
            StopWatchdog();
            PauseService.Remove(PauseReason.Ad);
            ClearCallbacks();
        }

        public void ShowAd(Action onReward, Action onFail = null)
        {
            if (!CanShowAd)
            {
                onFail?.Invoke();
                return;
            }

#if UNITY_EDITOR
            Debug.Log("[RewardedAdService] Editor mock: ad shown, reward granted.");
            onReward?.Invoke();
#else
            onRewardGranted = onReward;
            onAdFailed = onFail;
            adTerminal = false;
            adInProgress = true;
            PauseGame();
            PLink.Advertisement.RewardedAd.Show();
            adWatchdog = StartCoroutine(AdWatchdog());
#endif
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

        private void OnRewarded(Reward reward)
        {
            FinalizeAd(rewardGranted: true);
        }

        private void OnFailed()
        {
            FinalizeAd(rewardGranted: false);
        }

        private void OnClosed()
        {
            // If the ad was closed without a reward (player skipped), treat it as a
            // failure so the caller is always notified exactly once. On Yandex,
            // Rewarded always precedes Closed, so a real reward is never dropped here.
            FinalizeAd(rewardGranted: false);
        }

        private void FinalizeAd(bool rewardGranted)
        {
            if (adTerminal)
            {
                return;
            }

            adTerminal = true;
            adInProgress = false;
            StopWatchdog();
            ResumeGame();

            var reward = onRewardGranted;
            var fail = onAdFailed;
            ClearCallbacks();

            if (rewardGranted)
            {
                reward?.Invoke();
            }
            else
            {
                fail?.Invoke();
            }
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
            OnFailed();
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
            onRewardGranted = null;
            onAdFailed = null;
        }
    }
}
