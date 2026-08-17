using System;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class ScoreService : MonoBehaviour, IScoreService
    {
        private const string BestScoreKey = StorageKeys.BestScore;
        private const string DefaultLeaderboardId = "TopScore";
        private const float SubmitCooldown = 1.5f;
        private const int NoBaseline = -1;

        [SerializeField]
        private PlatformStorageService storageService;

        [SerializeField]
        private string leaderboardId = DefaultLeaderboardId;

        private ThrottledScoreSubmitter _scoreSubmitter;
        private int _recordToBeat = NoBaseline;
        private bool _recordBeatenThisRun;

        public event Action<int> ScoreChanged;

        public event Action RecordBeaten;

        public int CurrentScore { get; private set; }

        public int BestScore => storageService.GetInt(BestScoreKey, 0);

        public string LeaderboardId => leaderboardId;

        public void Reset()
        {
            CurrentScore = 0;
            _recordToBeat = NoBaseline;
            _recordBeatenThisRun = false;
            ScoreChanged?.Invoke(CurrentScore);
        }

        public void Add(int points)
        {
            CaptureRecordToBeat();
            CurrentScore += points;

            if (CurrentScore > BestScore)
            {
                storageService.SetInt(BestScoreKey, CurrentScore);
                _scoreSubmitter.Submit(CurrentScore, Time.unscaledTime);
            }

            ScoreChanged?.Invoke(CurrentScore);
            TryRaiseRecordBeaten();
        }

        /// <summary>
        /// Freezes the score to beat at the start of the run. Every point past the old best
        /// rewrites the stored record, so comparing against the live one would report a new
        /// record on every single point.
        /// </summary>
        private void CaptureRecordToBeat()
        {
            if (_recordToBeat == NoBaseline)
            {
                _recordToBeat = BestScore;
            }
        }

        /// <summary>
        /// Raises the record once per run. A first-ever run has nothing to beat and stays silent.
        /// </summary>
        private void TryRaiseRecordBeaten()
        {
            if (_recordBeatenThisRun || _recordToBeat <= 0 || CurrentScore <= _recordToBeat)
            {
                return;
            }

            _recordBeatenThisRun = true;
            RecordBeaten?.Invoke();
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
