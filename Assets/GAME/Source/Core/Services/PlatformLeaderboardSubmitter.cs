using PlatformLink;

namespace JumpRing.Game.Core.Services
{
    public sealed class PlatformLeaderboardSubmitter : ILeaderboardSubmitter
    {
        public bool IsAvailable => PLink.IsInitialized;

        public void Submit(string leaderboardId, int score)
        {
            PLink.Leaderboard.SetScore(leaderboardId, score);
        }
    }
}
