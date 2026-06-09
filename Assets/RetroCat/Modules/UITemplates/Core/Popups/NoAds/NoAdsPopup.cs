using PlatformLink;
using RetroCat.Modules.Core.UI.Activities.Popups.Core;
using UnityEngine;

namespace RetroCat.Modules.UITemplates.Core.Popups.NoAds
{
    public class NoAdsPopup : PopupBase
    {
        private const string OpenNoAdsEvent = "open-no-ads";

        [SerializeField] private GameObject _purchaseRoot;
        [SerializeField] private GameObject _purchasedRoot;

        public void SetAlreadyPurchased(bool purchased)
        {
            if (_purchaseRoot != null)
            {
                _purchaseRoot.SetActive(!purchased);
            }

            if (_purchasedRoot != null)
            {
                _purchasedRoot.SetActive(purchased);
            }
        }

        protected override void OnInit() { }

        protected override void OnOpenStarted()
        {
            if (PLink.IsInitialized)
            {
                PLink.Analytics.SendEvent(OpenNoAdsEvent);
            }
        }

        protected override void OnOpenFinished() { }

        protected override void OnCloseStarted() { }

        protected override void OnCloseFinished() { }
    }
}
