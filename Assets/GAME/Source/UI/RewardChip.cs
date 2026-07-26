using DG.Tweening;
using TMPro;
using UnityEngine;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Coin chip of the game over card: counts the run earnings up on open and reacts
    /// with a gold flash when the reward gets doubled.
    /// </summary>
    public sealed class RewardChip : MonoBehaviour
    {
        private const string AmountFormat = "+{0}";
        private const float CountDuration = 0.5f;
        private const float DoubleCountDuration = 0.4f;

        [SerializeField]
        private RectTransform coinIcon;

        [SerializeField]
        private TMP_Text amountLabel;

        private Sequence iconSequence;
        private Tween countTween;

        /// <summary>
        /// Writes the value the chip will end on, without animating. Lets the owner lay the card
        /// out at its final width before the count-up starts, so the chip does not resize while
        /// the digits tick.
        /// </summary>
        public void PrepareLayout(int amount)
        {
            amountLabel.SetText(AmountFormat, amount);
        }

        public void Show(int amount)
        {
            KillTweens();

            coinIcon.localScale = Vector3.one;
            coinIcon.localRotation = Quaternion.identity;
            countTween = NumberTween.Play(amountLabel, 0, amount, CountDuration, AmountFormat);
        }

        public void ShowDoubled(int amount)
        {
            KillTweens();

            countTween = NumberTween.Play(amountLabel, amount / 2, amount, DoubleCountDuration, AmountFormat);

            iconSequence = DOTween.Sequence();
            iconSequence.Append(coinIcon.DOPunchScale(Vector3.one * 0.35f, 0.45f, 9, 0.7f));
            iconSequence.Join(coinIcon.DOLocalRotate(new Vector3(0f, 360f, 0f), 0.45f, RotateMode.FastBeyond360));
            iconSequence.SetUpdate(true);
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void KillTweens()
        {
            iconSequence?.Kill();
            iconSequence = null;
            countTween?.Kill();
            countTween = null;
        }
    }
}
