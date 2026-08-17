using System;

namespace JumpRing.Game.Core.Services
{
    public interface IScoreService
    {
        event Action<int> ScoreChanged;

        /// <summary>Raised once per run, the moment the previous record is passed.</summary>
        event Action RecordBeaten;

        int CurrentScore { get; }

        int BestScore { get; }

        void Reset();

        void Add(int points);
    }
}
