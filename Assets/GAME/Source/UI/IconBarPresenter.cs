using RetroCat.Modules.FlexibleUI.Runtime.Activities;
using RetroCat.Modules.UITemplates.Common.Popups.Leaderboard;
using RetroCat.Modules.UITemplates.Common.Popups.OurGames;
using RetroCat.Modules.UITemplates.Common.Popups.Share;
using RetroCat.Modules.UITemplates.Core.Popups.NoAds;
using RetroCat.Modules.UITemplates.Core.Popups.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    public sealed class IconBarPresenter : MonoBehaviour
    {
        [Header("Icon Buttons")]
        [SerializeField] private Button _noAdsButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _ourGamesButton;
        [SerializeField] private Button _shareButton;
        [SerializeField] private Button _settingsButton;

        private void OnEnable()
        {
            _noAdsButton.onClick.AddListener(OnNoAdsClicked);
            _leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            _ourGamesButton.onClick.AddListener(OnOurGamesClicked);
            _shareButton.onClick.AddListener(OnShareClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDisable()
        {
            _noAdsButton.onClick.RemoveListener(OnNoAdsClicked);
            _leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
            _ourGamesButton.onClick.RemoveListener(OnOurGamesClicked);
            _shareButton.onClick.RemoveListener(OnShareClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        private void OnNoAdsClicked()
        {
            UIActivities.Instance.ShowActivity<NoAdsPopup>(gameObject.scene);
        }

        private void OnLeaderboardClicked()
        {
            UIActivities.Instance.ShowActivity<LeaderboardPopup>(gameObject.scene);
        }

        private void OnOurGamesClicked()
        {
            UIActivities.Instance.ShowActivity<OurGamesPopup>(gameObject.scene);
        }

        private void OnShareClicked()
        {
            UIActivities.Instance.ShowActivity<SharePopup>(gameObject.scene);
        }

        private void OnSettingsClicked()
        {
            UIActivities.Instance.ShowActivity<SettingsPopup>(gameObject.scene);
        }
    }
}
