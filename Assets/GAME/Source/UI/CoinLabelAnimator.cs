using System.Collections;
using UnityEngine;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Gameplay;

namespace JumpRing.Game.UI
{
    public sealed class CoinLabelAnimator : MonoBehaviour
    {
        [SerializeField]
        private Transform coinLabel;

        [SerializeField]
        private BonusEffectManager bonusEffectManager;

        [SerializeField]
        private MonoBehaviour currencyServiceComponent;

        [Header("Animation")]
        [SerializeField, Range(1.1f, 2f)]
        private float punchScale = 1.4f;

        [SerializeField, Range(0.05f, 0.3f)]
        private float scaleUpDuration = 0.08f;

        [SerializeField, Range(0.05f, 0.5f)]
        private float shakeDuration = 0.12f;

        [SerializeField, Range(0.05f, 0.5f)]
        private float scaleDownDuration = 0.15f;

        [SerializeField, Range(1f, 15f)]
        private float shakeAngle = 5f;

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;
        private int lastBalance;
        private Coroutine activeAnimation;

        private void OnEnable()
        {
            lastBalance = CurrencyService.Balance;
            CurrencyService.BalanceChanged += OnBalanceChanged;
        }

        private void OnDisable()
        {
            CurrencyService.BalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(int newBalance)
        {
            if (newBalance <= lastBalance)
            {
                lastBalance = newBalance;
                return;
            }

            lastBalance = newBalance;

            if (bonusEffectManager.ActiveBonus != BonusType.ScoreBoost)
            {
                return;
            }

            if (activeAnimation != null)
            {
                StopCoroutine(activeAnimation);
                coinLabel.localScale = Vector3.one;
                coinLabel.localRotation = Quaternion.identity;
            }

            activeAnimation = StartCoroutine(PlayPunchAnimation());
        }

        private IEnumerator PlayPunchAnimation()
        {
            // Scale up
            yield return LerpScale(1f, punchScale, scaleUpDuration);

            // Shake
            var elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / shakeDuration;
                var angle = shakeAngle * Mathf.Sin(t * Mathf.PI * 4f) * (1f - t);
                coinLabel.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            coinLabel.localRotation = Quaternion.identity;

            // Scale down
            yield return LerpScale(punchScale, 1f, scaleDownDuration);

            activeAnimation = null;
        }

        private IEnumerator LerpScale(float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                var scale = Mathf.Lerp(from, to, t);
                coinLabel.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            coinLabel.localScale = new Vector3(to, to, 1f);
        }
    }
}
