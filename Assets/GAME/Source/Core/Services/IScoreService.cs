using System;

namespace JumpRing.Game.Core.Services
{
    public interface IScoreService
    {
        event Action<int> ScoreChanged;

        int CurrentScore { get; }

        int BestScore { get; }

        void Reset();

        void Add(int points);
    }
}
