using System;
using System.Collections;
using JumpRing.Game.Core;
using PlatformLink;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class InterstitialAdService : MonoBehaviour
    {
        private const float CooldownSeconds = 60f;
        // Last-resort guard for an ad that never fires a terminal event. Kept well above real
        // ad duration so it can't fire mid-ad and resume the game under a visible interstitial.
        private const float AdWatchdogSeconds = 180f;

        [SerializeField]
        private NoAdsService noAdsService;

        private float lastShowTime = float.NegativeInfinity;
        private Action onComplete;
        private Coroutine adWatchdog;
        private bool adInProgress;

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
        // PauseReason.Ad taken in TryShow would never be released and the game would stay at
        // timeScale 0 for the rest of the session. The callback is dropped rather than invoked:
        // the objects waiting on it are being torn down together with this one.
        private void AbortPendingAd()
        {
            if (!adInProgress)
            {
                return;
            }

            adInProgress = false;
            onComplete = null;
            StopWatchdog();
            PauseService.Remove(PauseReason.Ad);
        }

        /// <summary>
        /// Tries to show interstitial ad. Calls onDone when ad closes or immediately if ad can't be shown.
        /// </summary>
        public void TryShow(Action onDone)
        {
            if (noAdsService != null && noAdsService.IsNoAds)
            {
                onDone?.Invoke();
                return;
            }

            if (Time.realtimeSinceStartup - lastShowTime < CooldownSeconds)
            {
                onDone?.Invoke();
                return;
            }

#if UNITY_EDITOR
            Debug.Log("[InterstitialAdService] Editor mock: interstitial shown.");
            onDone?.Invoke();
            return;
#else
            if (!PLink.IsInitialized || !PLink.Advertisement.InterstetialAd.CanShow())
            {
                onDone?.Invoke();
                return;
            }

            onComplete = onDone;
            lastShowTime = Time.realtimeSinceStartup;
            adInProgress = true;
            PauseGame();
            PLink.Advertisement.InterstetialAd.Show();
            adWatchdog = StartCoroutine(AdWatchdog());
#endif
        }

        private void SubscribeToAd()
        {
            // Also runs as the PLink.Initilized handler; without this a second Initilized would
            // subscribe the ad callbacks twice and deliver every terminal event twice.
            PLink.Initilized -= SubscribeToAd;

            PLink.Advertisement.InterstetialAd.Closed += OnClosed;
            PLink.Advertisement.InterstetialAd.Failed += OnFailed;
        }

        private void UnsubscribeFromAd()
        {
            if (!PLink.IsInitialized)
            {
                return;
            }

            PLink.Advertisement.InterstetialAd.Closed -= OnClosed;
            PLink.Advertisement.InterstetialAd.Failed -= OnFailed;
        }

        private void OnClosed()
        {
            adInProgress = false;
            ResumeGame();
            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }

        private void OnFailed()
        {
            adInProgress = false;
            ResumeGame();
            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
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
    }
}
