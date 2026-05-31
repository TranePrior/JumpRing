using RetroCat.Modules.FlexibleUI.Runtime.Indicators;
using RetroCat.Modules.FlexibleUI.Runtime.TransitionCurtain;
using UnityEngine;

namespace RetroCat.Modules.UITemplates.Common.Curtains
{
    public class FadeLoadingCurtain : TransitionCurtainBase
    {
        [SerializeField] private LoadingCircle _loadingCircle;
        
        protected override void OnInit() { }

        protected override void OnOpenStarted()
        {
            _loadingCircle.StartLoading();
        }

        protected override void OnOpenFinished() { }

        protected override void OnCloseStarted() { }

        protected override void OnCloseFinished()
        {
            _loadingCircle.StopLoading();
        }
    }
}
