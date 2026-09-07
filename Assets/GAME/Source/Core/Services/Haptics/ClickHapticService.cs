using PlatformLink;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Device;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JumpRing.Game.Core.Services.Haptics
{
    /// <summary>
    /// Ticks the phone on every tap — the jump, the buttons, the shop cards, all of it. One
    /// listener on the raw pointer instead of a hook per button: a click is a click wherever it
    /// lands, and no new UI can forget to ask for its haptic.
    /// </summary>
    /// <remarks>
    /// Android runs this through <c>navigator.vibrate</c>. iOS has no Vibration API at all, so the
    /// WebGL template falls back to Safari's switch tick (iOS 17.4+) — see plugin.js. Whether
    /// either exists is what <see cref="IsSupported"/> reports, and the settings window hides the
    /// vibration row when it does not, rather than offering a dead switch.
    /// </remarks>
    public sealed class ClickHapticService : MonoBehaviour
    {
        [SerializeField]
        private PlatformStorageService storageService;

        private readonly ClickHapticPolicy policy = new ClickHapticPolicy();

        public bool IsEnabled => policy.IsEnabled;

        /// <summary>
        /// Asked late rather than cached at startup: the answer comes from the browser through
        /// PLink, which is still initializing while saves are being loaded.
        /// </summary>
        public bool IsSupported => PLink.IsInitialized && PLink.Device.IsVibrationSupported();

        public void Initialize()
        {
            policy.SetEnabled(storageService.GetInt(StorageKeys.SettingsVibration, 1) == 1);
        }

        public void SetEnabled(bool enabled)
        {
            policy.SetEnabled(enabled);
            storageService.SetInt(StorageKeys.SettingsVibration, enabled ? 1 : 0);
        }

        private void Update()
        {
            if (!WasClickPressed())
            {
                return;
            }

            // Unscaled: taps keep arriving while the game is frozen behind a popup.
            if (!policy.TryConsume(Time.unscaledTime))
            {
                return;
            }

            if (!PLink.IsInitialized)
            {
                return;
            }

            PLink.Device.Vibrate(new VibrationSettings(ClickHapticPolicy.PulseDurationMs));
        }

        private static bool WasClickPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }
    }
}
