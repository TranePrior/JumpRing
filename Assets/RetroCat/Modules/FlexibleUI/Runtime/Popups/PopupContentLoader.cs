using DG.Tweening;
using RetroCat.Modules.FlexibleUI.Runtime.Indicators;
using UnityEngine;

namespace RetroCat.Modules.FlexibleUI.Runtime.Popups
{
    public class PopupContentLoader : MonoBehaviour
    {
        [SerializeField] private LoadingDots _loadingDots;

        [Header("Animation")]
        [SerializeField] private bool _useUnscaledTime = true;
        [SerializeField] private GameObject _loadingContainer;
        [SerializeField] private GameObject _contentContainer;

        private Tween _tween;

        public void ShowLoading()
        {
            _tween?.Kill();

            _loadingContainer.gameObject.SetActive(true);
            _contentContainer.gameObject.SetActive(false);
            _loadingDots.StartLoading();
        }

        public void HideLoading()
        {
            _loadingDots.StopLoading();
            _loadingContainer.gameObject.SetActive(false);
            _contentContainer.gameObject.SetActive(true);
        }
    }
}
