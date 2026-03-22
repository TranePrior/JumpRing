using System;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class CurrencyService : MonoBehaviour, ICurrencyService
    {
        private const string BalanceKey = "DiamondBalance";

        public event Action<int> BalanceChanged;

        public int Balance => PlayerPrefs.GetInt(BalanceKey, 0);

        public void Add(int amount)
        {
            var newBalance = Balance + amount;
            PlayerPrefs.SetInt(BalanceKey, newBalance);
            PlayerPrefs.Save();

            BalanceChanged?.Invoke(newBalance);
        }

        public bool Spend(int amount)
        {
            if (amount > Balance)
            {
                return false;
            }

            var newBalance = Balance - amount;
            PlayerPrefs.SetInt(BalanceKey, newBalance);
            PlayerPrefs.Save();

            BalanceChanged?.Invoke(newBalance);
            return true;
        }
    }
}
