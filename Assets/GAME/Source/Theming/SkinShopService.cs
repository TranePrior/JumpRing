using System;
using System.Collections.Generic;
using JumpRing.Game.Core.Services;
using UnityEngine;

namespace JumpRing.Game.Theming
{
    public sealed class SkinShopService : MonoBehaviour, ISkinShopService
    {
        private const string OwnedSkinsKey = "OwnedSkins";
        private const string ActiveSkinKey = "ActiveSkinId";

        [SerializeField]
        private SkinPackCatalog catalog;

        [SerializeField]
        private ThemeManager themeManager;

        [SerializeField]
        private MonoBehaviour currencyServiceComponent;

        public event Action<SkinItem> SkinSelected;
        public event Action<SkinItem> SkinPurchased;

        public SkinPackCatalog Catalog => catalog;
        public SkinItem ActiveSkin { get; private set; }

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;
        private HashSet<string> ownedSkinIds = new();

        private void Awake()
        {
            LoadOwnedSkins();
            LoadActiveSkin();
        }

        public bool IsOwned(SkinItem skin)
        {
            if (skin == catalog.DefaultSkin)
            {
                return true;
            }

            return ownedSkinIds.Contains(skin.SkinId);
        }

        public bool CanAfford(SkinItem skin)
        {
            return CurrencyService.Balance >= skin.Price;
        }

        public bool TryPurchase(SkinItem skin)
        {
            if (IsOwned(skin))
            {
                return false;
            }

            if (!CurrencyService.Spend(skin.Price))
            {
                return false;
            }

            ownedSkinIds.Add(skin.SkinId);
            SaveOwnedSkins();

            SkinPurchased?.Invoke(skin);
            return true;
        }

        public void SelectSkin(SkinItem skin)
        {
            if (!IsOwned(skin))
            {
                return;
            }

            ActiveSkin = skin;
            PlayerPrefs.SetString(ActiveSkinKey, skin.SkinId);
            PlayerPrefs.Save();

            themeManager.ApplyTheme(skin.ThemeData);

            SkinSelected?.Invoke(skin);
        }

        private void LoadOwnedSkins()
        {
            ownedSkinIds = new HashSet<string>();

            var saved = PlayerPrefs.GetString(OwnedSkinsKey, "");
            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            var ids = saved.Split(',');
            foreach (var id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    ownedSkinIds.Add(id);
                }
            }
        }

        private void SaveOwnedSkins()
        {
            var joined = string.Join(",", ownedSkinIds);
            PlayerPrefs.SetString(OwnedSkinsKey, joined);
            PlayerPrefs.Save();
        }

        private void LoadActiveSkin()
        {
            var savedId = PlayerPrefs.GetString(ActiveSkinKey, "");

            if (!string.IsNullOrEmpty(savedId))
            {
                var skin = catalog.FindSkinById(savedId);
                if (skin != null && IsOwned(skin))
                {
                    ActiveSkin = skin;
                    return;
                }
            }

            ActiveSkin = catalog.DefaultSkin;
        }
    }
}
