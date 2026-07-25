using System;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class ScoreService : MonoBehaviour, IScoreService
    {
        private const string BestScoreKey = StorageKeys.BestScore;
        private const string DefaultLeaderboardId = "TopScore";
        private const float SubmitCooldown = 1.5f;

        [SerializeField]
        private PlatformStorageService storageService;

        [SerializeField]
        private string leaderboardId = DefaultLeaderboardId;

        private ThrottledScoreSubmitter _scoreSubmitter;

        public event Action<int> ScoreChanged;

        public int CurrentScore { get; private set; }

        public int BestScore => storageService.GetInt(BestScoreKey, 0);

        public string LeaderboardId => leaderboardId;

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
                _scoreSubmitter.Submit(CurrentScore, Time.unscaledTime);
            }

            ScoreChanged?.Invoke(CurrentScore);
        }

        private void Awake()
        {
            _scoreSubmitter = new ThrottledScoreSubmitter(
                new PlatformLeaderboardSubmitter(), leaderboardId, SubmitCooldown);
        }

        private void Update()
        {
            _scoreSubmitter.Tick(Time.unscaledTime);
        }

        private void OnApplicationQuit()
        {
            _scoreSubmitter.SubmitPendingImmediately(Time.unscaledTime);
        }
    }
}
