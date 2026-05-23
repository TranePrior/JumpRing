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

        public int BestScore => storageService != null
            ? storageService.GetInt(BestScoreKey, 0)
            : PlayerPrefs.GetInt(BestScoreKey, 0);

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
                if (storageService != null)
                {
                    storageService.SetInt(BestScoreKey, CurrentScore);
                }
                else
                {
                    PlayerPrefs.SetInt(BestScoreKey, CurrentScore);
                    PlayerPrefs.Save();
                }
            }

            ScoreChanged?.Invoke(CurrentScore);
        }
    }
}
