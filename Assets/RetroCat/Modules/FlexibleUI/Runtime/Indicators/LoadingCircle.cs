using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.FlexibleUI.Runtime.Indicators
{
    public class LoadingCircle : LoadIndicatorBase
    {
        [SerializeField] private Image _loadingCircle;

        private Tween _tween;
        
        public override void StartLoading()
        {
            _loadingCircle.gameObject.SetActive(true);
            
            _tween = _loadingCircle.transform
                .DORotate(new Vector3(0f, 0f, -360f), 1f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }

        public override void SetProgress(float progress) { }

        public override void StopLoading()
        {
            _tween?.Kill();
            _loadingCircle.gameObject.SetActive(false);
        }
    }
}
