using JumpRing.Game.Core.Services;
using RetroCat.Modules.FlexibleUI.Runtime.Activities;
using RetroCat.Modules.UITemplates.Common.Popups.Leaderboard;
using RetroCat.Modules.UITemplates.Common.Popups.OurGames;
using RetroCat.Modules.UITemplates.Common.Popups.Share;
using RetroCat.Modules.UITemplates.Core.Popups.NoAds;
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

        [Header("Services")]
        [SerializeField] private NoAdsService _noAdsService;
        [SerializeField] private MonoBehaviour _scoreServiceComponent;
        [SerializeField] private SettingsPopupOpener _settingsPopupOpener;

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
            UIActivities.Instance.ShowActivity<NoAdsPopup>(gameObject.scene, popup =>
            {
                if (popup.gameObject.GetComponent<PopupTracker>() == null)
                {
                    popup.gameObject.AddComponent<PopupTracker>();
                }

                if (_noAdsService != null)
                {
                    popup.SetAlreadyPurchased(_noAdsService.IsNoAds);
                }
            });
        }

        private IScoreService ScoreService => (IScoreService)_scoreServiceComponent;

        private void OnLeaderboardClicked()
        {
            UIActivities.Instance.ShowActivity<LeaderboardPopup>(gameObject.scene, popup =>
            {
                if (popup.gameObject.GetComponent<PopupTracker>() == null)
                {
                    popup.gameObject.AddComponent<PopupTracker>();
                }

                if (_scoreServiceComponent != null)
                {
                    popup.SetBestScoreProvider(() => ScoreService.BestScore);
                }
            });
        }

        private void OnOurGamesClicked()
        {
            ShowPopup<OurGamesPopup>();
        }

        private void OnShareClicked()
        {
            ShowPopup<SharePopup>();
        }

        private void OnSettingsClicked()
        {
            _settingsPopupOpener.Open();
        }

        private void ShowPopup<T>() where T : ActivityBase
        {
            UIActivities.Instance.ShowActivity<T>(gameObject.scene, popup =>
            {
                if (popup.gameObject.GetComponent<PopupTracker>() == null)
                {
                    popup.gameObject.AddComponent<PopupTracker>();
                }
            });
        }
    }
}
