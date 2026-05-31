using PlatformLink;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Social;
using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.UITemplates.Common.Popups.Share
{
    [RequireComponent(typeof(Button))]
    public class SocialPlatformShareButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private SocialPlatform _socialPlatform;

        [Header("Share")]
        [SerializeField] private string _shareMessage = "Check out this game!";

#if UNITY_EDITOR
        private void Reset()
        {
            _button = GetComponent<Button>();
        }
#endif

        private void OnEnable()
        {
            _button.onClick.AddListener(OpenSocialPlatformShare);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OpenSocialPlatformShare);
        }

        private void OpenSocialPlatformShare()
        {
            if (!PLink.IsInitialized)
                return;

            ShareRequest request = new ShareRequest(text: _shareMessage, url: PLink.Environment.AppUrl);
            string shareLink = PLink.Social.CreateShareLink(_socialPlatform, request);

            Debug.Log(shareLink);
            PLink.Platform.OpenLink(shareLink);
        }
    }
}
