using System;
using System.Collections.Generic;
using JumpRing.Game.Core.Services;
using UnityEngine;

namespace JumpRing.Game.Theming
{
    public sealed class RingSizeUpgradeService : MonoBehaviour
    {
        private const string UpgradesKey = "SkinUpgrades";
        private const float MaxScale = 1.3f;
        private const float BaseScale = 1f;

        [SerializeField, Range(0.01f, 0.5f)]
        private float scaleStep = 0.05f;

        [SerializeField, Min(1)]
        private int maxLevel = 6;

        [SerializeField]
        private float[] levelPriceMultipliers = { 1f, 1.5f, 2f, 3f, 4f, 5.5f };

        [SerializeField]
        private MonoBehaviour currencyServiceComponent;

        [SerializeField]
        private PlatformStorageService storageService;

        public event Action<SkinItem, int> SkinUpgraded;

        private Dictionary<string, int> upgradeLevels = new();

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        public void Initialize()
        {
            LoadUpgrades();
        }

        public int GetLevel(string skinId)
        {
            return upgradeLevels.TryGetValue(skinId, out var level) ? level : 0;
        }

        public int MaxLevel => maxLevel;

        public float GetBonusScale(string skinId)
        {
            return GetLevel(skinId) * scaleStep;
        }

        public float GetTotalScale(string skinId)
        {
            return Mathf.Min(BaseScale + GetBonusScale(skinId), MaxScale);
        }

        private const int FreeSkinsUpgradeBasePrice = 150;

        public int GetUpgradePrice(SkinItem skin)
        {
            int level = GetLevel(skin.SkinId);
            float multiplier = level < levelPriceMultipliers.Length
                ? levelPriceMultipliers[level]
                : levelPriceMultipliers[^1];
            int basePrice = skin.Price > 0 ? skin.Price : FreeSkinsUpgradeBasePrice;
            return Mathf.RoundToInt(basePrice * multiplier);
        }

        public bool IsMaxed(string skinId)
        {
            return GetLevel(skinId) >= maxLevel;
        }

        public bool CanAffordUpgrade(SkinItem skin)
        {
            return CurrencyService.Balance >= GetUpgradePrice(skin);
        }

        public bool TryUpgrade(SkinItem skin)
        {
            if (IsMaxed(skin.SkinId))
            {
                return false;
            }

            int price = GetUpgradePrice(skin);
            if (!CurrencyService.Spend(price))
            {
                return false;
            }

            int newLevel = GetLevel(skin.SkinId) + 1;
            upgradeLevels[skin.SkinId] = newLevel;
            SaveUpgrades();

            SkinUpgraded?.Invoke(skin, newLevel);
            return true;
        }

        private void LoadUpgrades()
        {
            upgradeLevels = new Dictionary<string, int>();

            var saved = storageService.GetString(UpgradesKey, "");
            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            var entries = saved.Split(',');
            foreach (var entry in entries)
            {
                var parts = entry.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var level))
                {
                    upgradeLevels[parts[0]] = level;
                }
            }
        }

        private void SaveUpgrades()
        {
            var parts = new List<string>();
            foreach (var kvp in upgradeLevels)
            {
                parts.Add($"{kvp.Key}:{kvp.Value}");
            }

            var joined = string.Join(",", parts);
            storageService.SetString(UpgradesKey, joined);
        }
    }
}
