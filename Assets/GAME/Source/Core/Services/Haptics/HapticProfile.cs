using System;

namespace JumpRing.Game.Core.Services.Haptics
{
    /// <summary>
    /// One haptic recipe: either a single pulse or an on/off pattern, both in milliseconds.
    /// <see cref="Priority"/> decides which cue may cut another one short, and
    /// <see cref="BlockMs"/> is how long the cue owns the motor after it starts.
    /// </summary>
    public readonly struct HapticProfile
    {
        public int DurationMs { get; }

        public int[] PatternMs { get; }

        public bool HasPattern { get; }

        public int Priority { get; }

        public int BlockMs { get; }

        public HapticProfile(int durationMs, int priority, int cooldownMs)
        {
            DurationMs = durationMs;
            PatternMs = Array.Empty<int>();
            HasPattern = false;
            Priority = priority;
            BlockMs = Math.Max(durationMs, cooldownMs);
        }

        public HapticProfile(int[] patternMs, int priority, int cooldownMs)
        {
            DurationMs = 0;
            PatternMs = patternMs;
            HasPattern = true;
            Priority = priority;
            BlockMs = Math.Max(Sum(patternMs), cooldownMs);
        }

        private static int Sum(int[] values)
        {
            int total = 0;

            for (int i = 0; i < values.Length; i++)
            {
                total += values[i];
            }

            return total;
        }
    }
}
