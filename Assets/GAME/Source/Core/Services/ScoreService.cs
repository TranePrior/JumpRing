using System;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class ScoreService : MonoBehaviour, IScoreService
    {
        private const string BestScoreKey = "BestScore";

        [SerializeField]
        private PlatformStorageService storageService;

        public event Action<int> ScoreChanged;

        public int CurrentScore { get; private set; }

        public int BestScore => storageService.GetInt(BestScoreKey, 0);

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
                storageService.SetInt(BestScoreKey, CurrentScore);
            }

            ScoreChanged?.Invoke(CurrentScore);
        }
    }
}
