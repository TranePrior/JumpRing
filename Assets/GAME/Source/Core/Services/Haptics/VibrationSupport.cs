using PlatformLink;

namespace JumpRing.Game.Core.Services.Haptics
{
    /// <summary>
    /// Whether the current platform can vibrate at all. iOS browsers ship no Vibration API,
    /// so on iPhone the settings toggle would be a dead switch and the UI hides it instead.
    /// </summary>
    public static class VibrationSupport
    {
        public static bool IsAvailable
        {
            get
            {
#if UNITY_EDITOR
                // The editor target reports no motor; keep the row visible for layout work.
                return true;
#else
                return PLink.IsInitialized && PLink.Device.IsVibrationSupported();
#endif
            }
        }
    }
}
