using PlatformLink;
using RetroCat.Modules.Core.UI.Activities.Popups.Core;
using RetroCat.Modules.FlexibleUI.Runtime.Popups;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Platform;
using UnityEngine;

namespace RetroCat.Modules.UITemplates.Common.Popups.OurGames
{
    public class OurGamesPopup : PopupBase
    {
        private const string OpenOurGamesEvent = "open-our-games";
        
        [SerializeField] private PopupContentLoader _loader;
        [SerializeField] private Transform _ourGamesParent;
        [SerializeField] private OurGame _ourGamePrefab;
        
        protected override void OnInit()
        {
        }

        protected override void OnOpenStarted()
        {
            PLink.Analytics.SendEvent(OpenOurGamesEvent);
            
            _loader.ShowLoading();
            LoadGames();
        }

        protected override void OnOpenFinished()
        {
        }

        protected override void OnCloseStarted()
        {
        }

        protected override void OnCloseFinished()
        {
        }

        private void LoadGames()
        {
            ClearContainer(_ourGamesParent);
            PLink.Platform.GetAllGames(OnGamesLoaded);
        }

        private void OnGamesLoaded(bool ok, AvailableGames data)
        {
            if (!ok || data == null || data.Games == null)
            {
                _loader.HideLoading();
                return;
            }

            foreach (AvailableGame game in data.Games)
            {
                if (game == null)
                    continue;

                var item = Instantiate(_ourGamePrefab, _ourGamesParent);
                item.Initialize(game);
            }

            _loader.HideLoading();
        }

        private static void ClearContainer(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
