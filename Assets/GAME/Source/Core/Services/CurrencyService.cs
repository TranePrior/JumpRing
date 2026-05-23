using System;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class CurrencyService : MonoBehaviour, ICurrencyService
    {
        private const string BalanceKey = "DiamondBalance";

        [SerializeField]
        private PlatformStorageService storageService;

        public event Action<int> BalanceChanged;

        public int Balance => storageService != null
            ? storageService.GetInt(BalanceKey, 0)
            : PlayerPrefs.GetInt(BalanceKey, 0);

        public int RunEarnings { get; private set; }

        public void ResetRunEarnings()
        {
            RunEarnings = 0;
        }

        public void Add(int amount)
        {
            RunEarnings += amount;

            var newBalance = Balance + amount;

            if (storageService != null)
            {
                storageService.SetInt(BalanceKey, newBalance);
            }
            else
            {
                PlayerPrefs.SetInt(BalanceKey, newBalance);
                PlayerPrefs.Save();
            }

            BalanceChanged?.Invoke(newBalance);
        }

        public bool Spend(int amount)
        {
            if (amount > Balance)
            {
                return false;
            }

            var newBalance = Balance - amount;

            if (storageService != null)
            {
                storageService.SetInt(BalanceKey, newBalance);
            }
            else
            {
                PlayerPrefs.SetInt(BalanceKey, newBalance);
                PlayerPrefs.Save();
            }

            BalanceChanged?.Invoke(newBalance);
            return true;
        }
    }
}
