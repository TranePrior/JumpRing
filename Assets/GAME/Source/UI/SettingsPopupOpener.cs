using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.Services.Haptics;
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
        [SerializeField] private ClickHapticService _clickHapticService;

        public void Open()
        {
            UIActivities.Instance.ShowActivity<SettingsPopup>(gameObject.scene, popup =>
            {
                if (popup.gameObject.GetComponent<PopupTracker>() == null)
                {
                    popup.gameObject.AddComponent<PopupTracker>();
                }

                // iOS below 17.4 and desktop browsers have nothing to vibrate: hide the row
                // instead of offering a switch that does nothing.
                popup.SetVibrationsAvailable(_clickHapticService.IsSupported);

                popup.SetInitialState(
                    _audioSettingsService.IsMusicEnabled,
                    _audioSettingsService.IsEffectsEnabled,
                    _clickHapticService.IsEnabled);

                popup.MusicChanged += _audioSettingsService.SetMusic;
                popup.EffectsChanged += _audioSettingsService.SetEffects;
                popup.VibrationsChanged += _clickHapticService.SetEnabled;
            });
        }
    }
}
