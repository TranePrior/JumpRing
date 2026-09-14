using JumpRing.Game.Core.Services;
using RetroCat.Modules.FlexibleUI.Runtime.Activities;
using RetroCat.Modules.UITemplates.Core.Popups.Settings;
using UnityEngine;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Opens the settings window and wires it to the services behind it. Shared by the menu icon
    /// bar and the in-run pause button, which open the very same window from two different places.
    /// </summary>
    /// <remarks>
    /// The window carries the pause: <see cref="PopupTracker"/> freezes and silences the game while
    /// it is up, which is what makes the pause button a pause button.
    /// </remarks>
    public sealed class SettingsPopupOpener : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private AudioSettingsService _audioSettingsService;

        public void Open()
        {
            UIActivities.Instance.ShowActivity<SettingsPopup>(gameObject.scene, popup =>
            {
                if (popup.gameObject.GetComponent<PopupTracker>() == null)
                {
                    popup.gameObject.AddComponent<PopupTracker>();
                }

                popup.SetInitialState(
                    _audioSettingsService.IsMusicEnabled,
                    _audioSettingsService.IsEffectsEnabled);

                popup.MusicChanged += _audioSettingsService.SetMusic;
                popup.EffectsChanged += _audioSettingsService.SetEffects;
            });
        }
    }
}
