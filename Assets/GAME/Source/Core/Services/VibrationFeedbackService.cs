using PlatformLink;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Device;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class VibrationFeedbackService : MonoBehaviour
    {
        [SerializeField]
        private AudioSettingsService audioSettingsService;

        public void OnJump()
        {
            Vibrate(VibrationPreset.Light);
        }

        public void OnDeath()
        {
            Vibrate(VibrationPreset.Strong);
        }

        private void Vibrate(VibrationPreset preset)
        {
            if (!audioSettingsService.IsVibrationEnabled)
            {
                return;
            }

            if (!PLink.IsInitialized)
            {
                return;
            }

            PLink.Device.Vibrate(preset);
        }
    }
}
