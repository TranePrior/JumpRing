using System;
using UnityEngine;
using JumpRing.Game.Core.Services;

namespace JumpRing.Game.Meta
{
    public sealed class DailyRewardService : MonoBehaviour
    {
        private const string LastClaimUtcTicksKey = "DailyReward.LastClaimUtcTicks";

        [SerializeField, Min(1)]
        private int rewardAmount = 10;

        [SerializeField]
        private MonoBehaviour currencyServiceComponent;

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        public bool CanClaim
        {
            get
            {
                var lastClaimTicks = PlayerPrefs.GetString(LastClaimUtcTicksKey, string.Empty);
                if (string.IsNullOrWhiteSpace(lastClaimTicks))
                {
                    return true;
                }

                var lastClaimUtc = new DateTime(long.Parse(lastClaimTicks), DateTimeKind.Utc);
                return DateTime.UtcNow.Date > lastClaimUtc.Date;
            }
        }

        public void Claim()
        {
            if (!CanClaim)
            {
                return;
            }

            CurrencyService.Add(rewardAmount);
            PlayerPrefs.SetString(LastClaimUtcTicksKey, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
        }
    }
}
