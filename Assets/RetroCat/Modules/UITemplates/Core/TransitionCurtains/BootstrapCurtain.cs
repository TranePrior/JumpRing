using RetroCat.Modules.FlexibleUI.Runtime.Indicators;
using RetroCat.Modules.FlexibleUI.Runtime.TransitionCurtain;
using UnityEngine;
using UnityEngine.Serialization;

namespace RetroCat.Modules.UITemplates.Core.TransitionCurtains
{
    public class BootstrapCurtain : TransitionCurtainBase
    {
        [SerializeField] private bool _useLoadingIndicator;
        [FormerlySerializedAs("_loadingIndicator")] [SerializeField] private LoadIndicatorBase loadingIndicatorBase;

        protected override void OnInit() { }

        protected override void OnOpenStarted()
        {
            if (_useLoadingIndicator)
                loadingIndicatorBase.StartLoading();
        }

        protected override void OnOpenFinished() { }

        protected override void OnCloseStarted() { }

        protected override void OnCloseFinished()
        {
            if (_useLoadingIndicator)
                loadingIndicatorBase.StopLoading();
        }
    }
}
