using RetroCat.Modules.FlexibleUI.Runtime.Loaders;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Leaderboards;
using TMPro;
using UnityEngine;

namespace RetroCat.Modules.UITemplates.Common.Popups.Leaderboard
{
    public class LeaderboardLeader : MonoBehaviour
    {
        [SerializeField] private ImageVisualNetworkLoader _imageLoader;
        [SerializeField] private TMP_Text _nicknameLabel;
        [SerializeField] private TMP_Text _scoreLabel;

        public void SetUser(LeaderboardEntry leaderboardEntry)
        {
            _nicknameLabel.text = $"{leaderboardEntry.Player.PublicName}";
            _scoreLabel.text = leaderboardEntry.Score.ToString();

            _ = _imageLoader.Load(leaderboardEntry.Player.AvatarUrl);
        }
    }
}
