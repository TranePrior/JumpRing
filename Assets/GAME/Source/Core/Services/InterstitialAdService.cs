using System;
using JumpRing.Game.Core;
using PlatformLink;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class InterstitialAdService : MonoBehaviour
    {
        private const float CooldownSeconds = 60f;

        private float lastShowTime = float.NegativeInfinity;
        private Action onComplete;

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
            WebGLFocusHandler.IsAdActive = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
