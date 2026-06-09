using System;
using PlatformLink;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class ScoreService : MonoBehaviour, IScoreService
    {
        private const string BestScoreKey = "BestScore";
        private const string DefaultLeaderboardId = "TopScore";
        private const float SubmitCooldown = 1.5f;

        [SerializeField]
        private PlatformStorageService storageService;

        [SerializeField]
        private string leaderboardId = DefaultLeaderboardId;

        private float _lastSubmitTime = float.NegativeInfinity;
        private int _pendingScore = -1;

        public event Action<int> ScoreChanged;

        public int CurrentScore { get; private set; }

        public int BestScore => storageService.GetInt(BestScoreKey, 0);

        public string LeaderboardId => leaderboardId;

        public void Reset()
        {
            FlushPendingScore();
            CurrentScore = 0;
            ScoreChanged?.Invoke(CurrentScore);
        }

        public void Add(int points)
        {
            CurrentScore += points;

            if (CurrentScore > BestScore)
            {
                storageService.SetInt(BestScoreKey, CurrentScore);
                ScheduleLeaderboardSubmit(CurrentScore);
            }

            ScoreChanged?.Invoke(CurrentScore);
        }

        private void ScheduleLeaderboardSubmit(int score)
        {
            if (!PLink.IsInitialized || string.IsNullOrEmpty(leaderboardId))
            {
                return;
            }

            if (Time.unscaledTime - _lastSubmitTime >= SubmitCooldown)
            {
                PLink.Leaderboard.SetScore(leaderboardId, score);
                _lastSubmitTime = Time.unscaledTime;
                _pendingScore = -1;
            }
            else
            {
                _pendingScore = score;
            }
        }

        public void FlushPendingScore()
        {
            if (_pendingScore < 0 || !PLink.IsInitialized || string.IsNullOrEmpty(leaderboardId))
            {
                return;
            }

            PLink.Leaderboard.SetScore(leaderboardId, _pendingScore);
            _lastSubmitTime = Time.unscaledTime;
            _pendingScore = -1;
        }
    }
}
