namespace JumpRing.Game.Core.Services.Haptics
{
    /// <summary>
    /// Guards the single vibration motor. Every new call to the platform replaces whatever is
    /// currently playing, so an unfiltered stream of cues turns a designed pattern into a
    /// flat buzz. A cue is dropped while a running cue of equal or higher priority owns the
    /// motor, and passes through once that window closes or when it outranks the running one.
    /// </summary>
    public sealed class HapticScheduler
    {
        private const float MsToSeconds = 0.001f;

        private float _blockedUntil = float.NegativeInfinity;
        private int _activePriority;

        /// <summary>
        /// Returns whether the cue may reach the motor, and reserves the motor when it may.
        /// </summary>
        public bool TryConsume(in HapticProfile profile, float now)
        {
            if (now < _blockedUntil && profile.Priority <= _activePriority)
            {
                return false;
            }

            _blockedUntil = now + profile.BlockMs * MsToSeconds;
            _activePriority = profile.Priority;
            return true;
        }
    }
}
