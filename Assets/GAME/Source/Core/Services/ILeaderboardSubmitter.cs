namespace JumpRing.Game.Core.Services
{
    /// <summary>
    /// Transport for leaderboard score submits. Isolates the platform SDK so submit
    /// scheduling can be exercised without a platform.
    /// </summary>
    public interface ILeaderboardSubmitter
    {
        /// <summary>
        /// True once the platform can accept submits.
        /// </summary>
        bool IsAvailable { get; }

        void Submit(string leaderboardId, int score);
    }
}
