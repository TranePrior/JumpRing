using JumpRing.Game.Core.Services.Haptics;
using JumpRing.Game.Theming;
using PlatformLink;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Device;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class VibrationFeedbackService : MonoBehaviour
    {
        [SerializeField]
        private AudioSettingsService audioSettingsService;

        private readonly HapticScheduler scheduler = new HapticScheduler();

        public void OnJump()
        {
            Play(HapticCue.Jump);
        }

        public void OnCoinCollected(Vector3 position, int amount)
        {
            Play(HapticCue.Coin);
        }

        public void OnSkinPurchased(SkinItem skin)
        {
            Play(HapticCue.Purchase);
        }

        public void OnSkinUpgraded(SkinItem skin, int level)
        {
            Play(HapticCue.Purchase);
        }

        public void OnRecordBeaten()
        {
            Play(HapticCue.Record);
        }

        public void OnDeath()
        {
            Play(HapticCue.Death);
        }

        private void Play(HapticCue cue)
        {
            if (!audioSettingsService.IsVibrationEnabled)
            {
                return;
            }

            if (!PLink.IsInitialized)
            {
                return;
            }

            HapticProfile profile = HapticProfiles.Get(cue);

            // Unscaled: the death cue fires while the game is already frozen for the death popup.
            if (!scheduler.TryConsume(profile, Time.unscaledTime))
            {
                return;
            }

            VibrationSettings settings = profile.HasPattern
                ? new VibrationSettings(profile.PatternMs)
                : new VibrationSettings(profile.DurationMs);

            PLink.Device.Vibrate(settings);
        }
    }
}
