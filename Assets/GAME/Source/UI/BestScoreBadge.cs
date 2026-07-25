using DG.Tweening;
using JumpRing.Game.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Best-score badge of the game over card. Has two looks: a calm slate pill with the
    /// stored record, and a gold celebration state when the finished run beat it.
    /// </summary>
    public sealed class BestScoreBadge : MonoBehaviour
    {
        private const float CelebrationDelay = 0.45f;
        private const float RaysSpinDuration = 18f;

        [Header("Parts")]
        [SerializeField]
        private RectTransform pill;

        [SerializeField]
        private Image background;

        [SerializeField]
        private TMP_Text caption;

        [SerializeField]
        private TMP_Text value;

        [SerializeField]
        private RectTransform rays;

        [Header("Palette")]
        [SerializeField]
        private Color idleBackground;

        [SerializeField]
        private Color idleCaption;

        [SerializeField]
        private Color idleValue;

        [SerializeField]
        private Color recordBackground;

        [SerializeField]
        private Color recordCaption;

        private Sequence celebration;
        private Tween raysSpin;

        public void Show(int bestScore, bool isNewRecord)
        {
            KillTweens();

            pill.localScale = Vector3.one;
            rays.gameObject.SetActive(isNewRecord);
            value.gameObject.SetActive(!isNewRecord);

            if (isNewRecord)
            {
                background.color = recordBackground;
                caption.color = recordCaption;
                caption.text = LocalizationService.Instance.GetText(LocalizationKey.NewBest);
                PlayCelebration();
                return;
            }

            background.color = idleBackground;
            caption.color = idleCaption;
            caption.text = LocalizationService.Instance.GetText(LocalizationKey.BestScore);
            value.color = idleValue;
            value.text = bestScore.ToString();
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void PlayCelebration()
        {
            rays.localRotation = Quaternion.identity;
            rays.localScale = Vector3.zero;

            celebration = DOTween.Sequence();
            celebration.AppendInterval(CelebrationDelay);
            celebration.Append(pill.DOPunchScale(Vector3.one * 0.18f, 0.45f, 8, 0.8f));
            celebration.Join(rays.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
            celebration.SetUpdate(true);

            raysSpin = rays.DOLocalRotate(new Vector3(0f, 0f, -360f), RaysSpinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }

        private void KillTweens()
        {
            celebration?.Kill();
            celebration = null;
            raysSpin?.Kill();
            raysSpin = null;
        }
    }
}
