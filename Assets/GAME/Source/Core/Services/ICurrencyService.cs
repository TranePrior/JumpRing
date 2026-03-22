using System;

namespace JumpRing.Game.Core.Services
{
    public interface ICurrencyService
    {
        event Action<int> BalanceChanged;

        int Balance { get; }

        void Add(int amount);

        bool Spend(int amount);
    }
}
