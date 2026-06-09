using System;
using PlatformLink;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Purchases;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class NoAdsService : MonoBehaviour
    {
        private const string NoAdsKey = "NoAdsPurchased";

        [SerializeField]
        private string productId = "noads";

        [SerializeField]
        private PlatformStorageService storageService;

        public event Action StateChanged;

        public bool IsNoAds { get; private set; }

        public void Initialize()
        {
            IsNoAds = storageService.GetInt(NoAdsKey, 0) == 1;

            if (IsNoAds && PLink.IsInitialized)
            {
                PLink.Advertisement.InterstetialAd.NoAdMode = true;
            }

            if (PLink.IsInitialized)
            {
                SubscribeToPurchases();
            }
            else
            {
                PLink.Initilized += OnPlinkInitialized;
            }
        }

        private void OnPlinkInitialized()
        {
            PLink.Initilized -= OnPlinkInitialized;

            if (IsNoAds)
            {
                PLink.Advertisement.InterstetialAd.NoAdMode = true;
            }

            SubscribeToPurchases();
        }

        private void SubscribeToPurchases()
        {
            PLink.Purchases.Purchased += OnPurchased;
            RestorePurchases();
        }

        private void OnDestroy()
        {
            PLink.Initilized -= OnPlinkInitialized;

            if (PLink.IsInitialized)
            {
                PLink.Purchases.Purchased -= OnPurchased;
            }
        }

        private void OnPurchased(Purchase purchase)
        {
            if (purchase.ProductId != productId)
            {
                return;
            }

            ApplyNoAds();
        }

        private void RestorePurchases()
        {
            PLink.Purchases.GetPurchases(purchases =>
            {
                if (purchases == null)
                {
                    return;
                }

                foreach (var purchase in purchases)
                {
                    if (purchase.ProductId == productId)
                    {
                        ApplyNoAds();
                        PLink.Purchases.ConsumePurchase(purchase);
                        return;
                    }
                }
            });
        }

        private void ApplyNoAds()
        {
            if (IsNoAds)
            {
                return;
            }

            IsNoAds = true;
            storageService.SetInt(NoAdsKey, 1);

            if (PLink.IsInitialized)
            {
                PLink.Advertisement.InterstetialAd.NoAdMode = true;
            }

            StateChanged?.Invoke();
        }
    }
}
