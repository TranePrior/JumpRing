using DG.Tweening;
using UnityEngine;

namespace RetroCat.Modules.FlexibleUI.Runtime.Indicators
{
    public class LoadingDots : LoadIndicatorBase
    {
        [Header("Dependencies")]
        [SerializeField] private LoadingDot[] _loadingDots;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float _fadeDurationSeconds = 0.2f;
        [SerializeField, Min(0f)] private float _holdDurationSeconds = 0.05f;
        [SerializeField, Min(0f)] private float _staggerSeconds = 0.15f;
        [SerializeField, Min(0f)] private float _cycleDelaySeconds = 0.15f;
        [SerializeField] private bool _useUnscaledTime = true;

        private Tween _tween;

        public override void StartLoading()
        {
            StopLoading();

            if (_loadingDots == null || _loadingDots.Length == 0)
                return;

            var sequence = DOTween.Sequence().SetUpdate(_useUnscaledTime);
            float maxStartDelay = 0f;
            for (int i = 0; i < _loadingDots.Length; i++)
            {
                if (_loadingDots[i] != null)
                    maxStartDelay = i * _staggerSeconds;
            }

            for (int i = 0; i < _loadingDots.Length; i++)
            {
                LoadingDot dot = _loadingDots[i];
                if (dot == null)
                    continue;

                dot.SetAlphaInstant(0f);

                float startDelay = i * _staggerSeconds;
                float syncedCycleDelay = _cycleDelaySeconds + Mathf.Max(0f, maxStartDelay - startDelay);
                Tween dotTween = dot.CreatePulseTween(
                    _fadeDurationSeconds,
                    _holdDurationSeconds,
                    syncedCycleDelay,
                    startDelay,
                    _useUnscaledTime);

                if (dotTween != null)
                {
                    dotTween.Pause();
                    sequence.Join(dotTween);
                }
            }

            _tween = sequence.Play();
        }

        public override void SetProgress(float progress) { }

        public override void StopLoading()
        {
            _tween?.Kill();
            _tween = null;

            if (_loadingDots == null)
                return;

            foreach (LoadingDot dot in _loadingDots)
                dot?.SetAlphaInstant(0f);
        }
    }
}
