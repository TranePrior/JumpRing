using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Owns the visual state of the rewarded-ad button: reward payout, cooldown countdown
    /// and the "no ad available" fallback look. Cooldown timing itself lives in ShopPresenter.
    /// </summary>
    public sealed class WatchAdButtonView : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private Image adIcon;

        [SerializeField]
        private GameObject rewardGroup;

        [SerializeField]
        private TMP_Text cooldownLabel;

        [SerializeField]
        private Color dimIconColor = new(0.62f, 0.62f, 0.68f, 1f);

        [SerializeField]
        private float pulseScale = 1.06f;

        [SerializeField]
        private float pulseDuration = 0.7f;

        private enum VisualState
        {
            None,
            Available,
            Cooldown,
            Unavailable
        }

        private Tween pulseTween;
        private int shownSeconds = -1;

        // ShopPresenter drives these from Update while the shop is open, so the state-changing part
        // has to be guarded. Re-applying SetActive and interactable every frame is not free:
        // touching Selectable.interactable runs DoStateTransition and can dirty the canvas.
        private VisualState shownState = VisualState.None;

        public void ShowAvailable()
        {
            if (shownState == VisualState.Available)
            {
                return;
            }

            shownState = VisualState.Available;
            button.interactable = true;
            adIcon.gameObject.SetActive(true);
            adIcon.color = Color.white;
            rewardGroup.SetActive(true);
            cooldownLabel.gameObject.SetActive(false);
            shownSeconds = -1;
            StartPulse();
        }

        /// <summary>
        /// Hides the ad icon so the countdown owns the whole button — during cooldown the
        /// icon carries no information and only pushes the timer off center.
        /// </summary>
        public void ShowCooldown(float remainingSeconds)
        {
            if (shownState != VisualState.Cooldown)
            {
                shownState = VisualState.Cooldown;
                SetLockedLook();
                adIcon.gameObject.SetActive(false);
                cooldownLabel.gameObject.SetActive(true);
            }

            int total = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            if (total == shownSeconds)
            {
                return;
            }

            shownSeconds = total;
            cooldownLabel.SetText("{0}:{1:00}", total / 60, total % 60);
        }

        public void ShowUnavailable()
        {
            if (shownState == VisualState.Unavailable)
            {
                return;
            }

            shownState = VisualState.Unavailable;
            SetLockedLook();
            adIcon.gameObject.SetActive(true);
            adIcon.color = dimIconColor;
            cooldownLabel.gameObject.SetActive(false);
            shownSeconds = -1;
        }

        private void SetLockedLook()
        {
            button.interactable = false;
            rewardGroup.SetActive(false);
            StopPulse();
        }

        private void StartPulse()
        {
            if (pulseTween != null)
            {
                return;
            }

            pulseTween = transform
                .DOScale(pulseScale, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void StopPulse()
        {
            pulseTween?.Kill();
            pulseTween = null;
            transform.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            StopPulse();

            // The next activation re-applies the look from scratch; without this the guards would
            // skip it and the button could come back showing a stale state.
            shownState = VisualState.None;
            shownSeconds = -1;
        }
    }
}
