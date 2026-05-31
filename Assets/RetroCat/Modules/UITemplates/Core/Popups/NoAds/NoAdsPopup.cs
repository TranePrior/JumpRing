using PlatformLink;
using RetroCat.Modules.Core.UI.Activities.Popups.Core;

namespace RetroCat.Modules.UITemplates.Core.Popups.NoAds
{
    public class NoAdsPopup : PopupBase
    {
        private const string OpenNoAdsEvent = "open-no-ads";
        
        protected override void OnInit() { }

        protected override void OnOpenStarted()
        {
            PLink.Analytics.SendEvent(OpenNoAdsEvent);
        }

        protected override void OnOpenFinished() { }

        protected override void OnCloseStarted() { }

        protected override void OnCloseFinished() { }
    }
}
