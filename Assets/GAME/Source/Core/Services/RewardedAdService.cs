using System;
using JumpRing.Game.Core;
using PlatformLink;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Advertisement;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class RewardedAdService : MonoBehaviour
    {
        private Action onRewardGranted;
        private Action onAdFailed;

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
            PauseGame();
            PLink.Advertisement.RewardedAd.Show();
#endif
        }

        private void SubscribeToAd()
        {
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
            onRewardGranted?.Invoke();
            ClearCallbacks();
        }

        private void OnFailed()
        {
            ResumeGame();
            onAdFailed?.Invoke();
            ClearCallbacks();
        }

        private void OnClosed()
        {
            ResumeGame();
            ClearCallbacks();
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

        private void ClearCallbacks()
        {
            onRewardGranted = null;
            onAdFailed = null;
        }
    }
}
