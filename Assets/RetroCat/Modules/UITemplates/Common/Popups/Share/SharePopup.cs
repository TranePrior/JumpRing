using PlatformLink;
using RetroCat.Modules.Core.UI.Activities.Popups.Core;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Social;
using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.UITemplates.Common.Popups.Share
{
    public class SharePopup : PopupBase
    {
        private const string OpenShareEvent = "open-share";

        [SerializeField] private Button _copyButton;
        [SerializeField] private Button _shareButton;

        [Header("Share")]
        [SerializeField] private string _shareMessage = "Check out this game!";

        protected override void OnInit() { }

        protected override void OnOpenStarted()
        {
            if (PLink.IsInitialized)
                PLink.Analytics.SendEvent(OpenShareEvent);

            _copyButton.onClick.AddListener(OnCopyClicked);
            _shareButton.onClick.AddListener(OnShareClicked);
        }

        protected override void OnOpenFinished() { }

        protected override void OnCloseStarted()
        {
            _copyButton.onClick.RemoveListener(OnCopyClicked);
            _shareButton.onClick.RemoveListener(OnShareClicked);
        }

        protected override void OnCloseFinished() { }

        private void OnCopyClicked()
        {
            PLink.Device.CopyToClipboard(PLink.Environment.AppUrl);
        }

        private void OnShareClicked()
        {
            if (!PLink.IsInitialized)
                return;

            ShareRequest request = new ShareRequest(text: _shareMessage, url: PLink.Environment.AppUrl);

            if (PLink.Social.IsShareDialogAvailable())
                PLink.Social.ShowShareDialog(request);
        }
    }
}
