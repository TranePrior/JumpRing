using System;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class ScoreService : MonoBehaviour, IScoreService
    {
        private const string BestScoreKey = "BestScore";

        public event Action<int> ScoreChanged;

        public int CurrentScore { get; private set; }

        public int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);

        public void Reset()
        {
            CurrentScore = 0;
            ScoreChanged?.Invoke(CurrentScore);
        }

        public void Add(int points)
        {
            CurrentScore += points;

            if (CurrentScore > BestScore)
            {
                PlayerPrefs.SetInt(BestScoreKey, CurrentScore);
                PlayerPrefs.Save();
            }

            ScoreChanged?.Invoke(CurrentScore);
        }
    }
}
