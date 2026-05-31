using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RetroCat.Modules.FlexibleUI.Runtime.Indicators
{
    public class LoadingDot : MonoBehaviour
    {
        [SerializeField] private Image _dotActive;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.2f;
        [SerializeField] private Ease _fadeEase = Ease.InOutSine;
        [SerializeField] private bool _useUnscaledTime = true;

        private void Awake()
        {
            SetAlphaInstant(0f);
        }

        private void OnDisable()
        {
            if (_dotActive != null)
                _dotActive.DOKill();
        }

        public void SetAlphaInstant(float alpha)
        {
            if (_dotActive == null)
                return;

            _dotActive.DOKill();

            Color color = _dotActive.color;
            color.a = alpha;
            _dotActive.color = color;
        }

        public Tween CreatePulseTween(
            float fadeDurationSeconds,
            float holdDurationSeconds,
            float cycleDelaySeconds,
            float startDelaySeconds,
            bool useUnscaledTime)
        {
            if (_dotActive == null)
                return null;

            var sequence = DOTween.Sequence();

            if (startDelaySeconds > 0f)
                sequence.AppendInterval(startDelaySeconds);

            sequence.Append(_dotActive.DOFade(1f, fadeDurationSeconds).SetEase(_fadeEase));

            if (holdDurationSeconds > 0f)
                sequence.AppendInterval(holdDurationSeconds);

            sequence.Append(_dotActive.DOFade(0f, fadeDurationSeconds).SetEase(_fadeEase));

            if (cycleDelaySeconds > 0f)
                sequence.AppendInterval(cycleDelaySeconds);

            return sequence
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(useUnscaledTime);
        }
    }
}
