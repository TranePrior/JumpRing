namespace JumpRing.Game.Core.Services.Haptics
{
    /// <summary>
    /// Decides whether a tap is allowed to reach the motor. Pure logic, no Unity and no platform
    /// calls, so the rules that make a click feel like a click stay testable.
    /// </summary>
    /// <remarks>
    /// Two facts of the web Vibration API shape this class. A pulse shorter than roughly 25 ms
    /// never spins the motor up on Android — the player reads it as "vibration is broken" — hence
    /// <see cref="PulseDurationMs"/>. And every call cancels the one before it, so a burst of taps
    /// during a run would otherwise become one flat buzz instead of separate ticks; the minimum
    /// interval keeps each tap its own tick and keeps the motor off the rest of the time.
    /// </remarks>
    public sealed class ClickHapticPolicy
    {
        /// <summary>Length of the click tick, in milliseconds.</summary>
        public const int PulseDurationMs = 35;

        private const float MinIntervalSeconds = 0.06f;

        private float nextAllowedTime = float.NegativeInfinity;

        public bool IsEnabled { get; private set; } = true;

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        /// <summary>
        /// Returns true when this tap earns a tick, and claims the motor for the interval that follows.
        /// </summary>
        public bool TryConsume(float now)
        {
            if (!IsEnabled)
            {
                return false;
            }

            if (now < nextAllowedTime)
            {
                return false;
            }

            nextAllowedTime = now + MinIntervalSeconds;
            return true;
        }
    }
}
