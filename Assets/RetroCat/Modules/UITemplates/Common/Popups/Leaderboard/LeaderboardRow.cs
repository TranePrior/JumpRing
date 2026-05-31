using RetroCat.Modules.FlexibleUI.Runtime.Loaders;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Leaderboards;
using TMPro;
using UnityEngine;

namespace RetroCat.Modules.UITemplates.Common.Popups.Leaderboard
{
    public class LeaderboardRow : MonoBehaviour
    {
        [SerializeField] private ImageVisualNetworkLoader _imageVisualNetworkLoader;
        [SerializeField] private TMP_Text _nicknameLabel;
        [SerializeField] private TMP_Text _valueLabel;

        [SerializeField] private GameObject _background;
        [SerializeField] private GameObject _userBackground;

        public void Initialize(LeaderboardEntry entry, bool isUser)
        {
            _nicknameLabel.text = $"{entry.Rank}. {entry.Player.PublicName}";
            _valueLabel.text = entry.Score.ToString();

            _userBackground.gameObject.SetActive(isUser);
            _background.gameObject.SetActive(!isUser);

            _ = _imageVisualNetworkLoader.Load(entry.Player.AvatarUrl);
        }
    }
}
