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
            PauseGame();
            PLink.Advertisement.InterstetialAd.Show();
            adWatchdog = StartCoroutine(AdWatchdog());
#endif
        }

        private void SubscribeToAd()
        {
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
            ResumeGame();
            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }

        private void OnFailed()
        {
            ResumeGame();
            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }

        private void PauseGame()
        {
            WebGLFocusHandler.IsAdActive = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        private void ResumeGame()
        {
            StopWatchdog();
            WebGLFocusHandler.IsAdActive = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
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
