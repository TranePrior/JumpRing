using PlatformLink;
using RetroCat.Modules.FlexibleUI.Runtime.Loaders;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.UITemplates.Common.Popups.OurGames
{
    public class OurGame : MonoBehaviour
    {
        [SerializeField] private TMP_Text _gametitleLabel;
        [SerializeField] private Button _openGameButton;
        [SerializeField] private ImageVisualNetworkLoader _iconLoader;

        private AvailableGame _game;

        public void Initialize(AvailableGame game)
        {
            _game = game;

            _gametitleLabel.text = _game.Title;
            _openGameButton.onClick.AddListener(OnOpenGameButtonClicked);

            _ = _iconLoader.Load(game.IconUrl);
        }

        private void OnOpenGameButtonClicked()
        {
            if (PLink.IsInitialized)
                PLink.Platform.OpenLink(_game.Url);
        }

        private void OnDestroy()
        {
            _openGameButton.onClick.RemoveAllListeners();
        }
    }
}
