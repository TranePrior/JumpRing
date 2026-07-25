namespace JumpRing.Game.Core.Services
{
    /// <summary>
    /// Rate-limits leaderboard submits. Platforms reject a second submit inside their own
    /// rate window and drop that score, so sending early loses the record instead of
    /// delivering it sooner. A score queued during the cooldown is sent by <see cref="Tick"/>
    /// once the window closes, and only the latest queued score survives.
    /// </summary>
    public sealed class ThrottledScoreSubmitter
    {
        private const int NoPendingScore = -1;

        private readonly ILeaderboardSubmitter _submitter;
        private readonly string _leaderboardId;
        private readonly float _cooldown;

        private float _lastSubmitTime = float.NegativeInfinity;
        private int _pendingScore = NoPendingScore;

        public ThrottledScoreSubmitter(ILeaderboardSubmitter submitter, string leaderboardId, float cooldown)
        {
            _submitter = submitter;
            _leaderboardId = leaderboardId;
            _cooldown = cooldown;
        }

        public bool HasPendingScore => _pendingScore != NoPendingScore;

        /// <summary>
        /// Queues a score, sending it right away when no cooldown is in effect.
        /// </summary>
        public void Submit(int score, float now)
        {
            _pendingScore = score;
            Tick(now);
        }

        /// <summary>
        /// Sends the queued score once its cooldown has elapsed. Call every frame.
        /// </summary>
        public void Tick(float now)
        {
            if (!CanSend || now - _lastSubmitTime < _cooldown)
            {
                return;
            }

            Send(now);
        }

        /// <summary>
        /// Sends the queued score without waiting out the cooldown. For teardown only: no
        /// frames follow, so a deferred send would never happen and the record would be lost.
        /// </summary>
        public void SubmitPendingImmediately(float now)
        {
            if (!CanSend)
            {
                return;
            }

            Send(now);
        }

        private bool CanSend => HasPendingScore && !string.IsNullOrEmpty(_leaderboardId) && _submitter.IsAvailable;

        private void Send(float now)
        {
            _submitter.Submit(_leaderboardId, _pendingScore);
            _lastSubmitTime = now;
            _pendingScore = NoPendingScore;
        }
    }
}
