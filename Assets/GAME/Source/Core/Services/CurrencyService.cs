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

        public int Balance => storageService.GetInt(BalanceKey, 0);

        public int RunEarnings { get; private set; }

        public void ResetRunEarnings()
        {
            RunEarnings = 0;
        }

        public void Add(int amount)
        {
            if (amount < 0)
            {
                return;
            }

            RunEarnings += amount;

            var newBalance = Balance + amount;
            storageService.SetInt(BalanceKey, newBalance);
            BalanceChanged?.Invoke(newBalance);
        }

        public bool Spend(int amount)
        {
            if (amount < 0)
            {
                return false;
            }

            if (amount > Balance)
            {
                return false;
            }

            var newBalance = Balance - amount;
            storageService.SetInt(BalanceKey, newBalance);
            BalanceChanged?.Invoke(newBalance);
            return true;
        }
    }
}
