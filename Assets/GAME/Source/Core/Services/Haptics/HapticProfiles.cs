using System;

namespace JumpRing.Game.Core.Services.Haptics
{
    /// <summary>
    /// Tuned haptic recipes. The web Vibration API exposes duration only — no amplitude —
    /// so "weight" is expressed through pulse length and the gaps between pulses.
    /// Pulses shorter than ~25 ms sit below the spin-up time of most phone motors and are
    /// simply not felt, which is why nothing here goes below that.
    /// </summary>
    public static class HapticProfiles
    {
        /// <summary>Run-long chatter: dropped whenever anything more meaningful is playing.</summary>
        private const int ChatterPriority = 0;

        /// <summary>Shop moments: nothing else competes for the motor while a popup is open.</summary>
        private const int RewardPriority = 1;

        /// <summary>Fires mid-run and has to cut through the jump ticks around it.</summary>
        private const int RecordPriority = 2;

        /// <summary>Ends the run, so it outranks the record cue it can land right on top of.</summary>
        private const int DeathPriority = 3;

        /// <summary>Crisp single tick; short enough to stay pleasant on a jump spam.</summary>
        private const int JumpPulseMs = 30;

        /// <summary>Keeps rapid jumps from restarting the motor before it settles.</summary>
        private const int JumpCooldownMs = 70;

        /// <summary>A touch lighter than the jump so a coin grabbed mid-jump reads as its own thing.</summary>
        private const int CoinPulseMs = 25;

        /// <summary>A coin line collected in one pass must not turn into a single long buzz.</summary>
        private const int CoinCooldownMs = 55;

        /// <summary>Light then heavy: the rising half of a "cha-ching".</summary>
        private static readonly int[] PurchasePatternMs = { 30, 40, 60 };

        /// <summary>Two ticks winding up into a payoff hit: "tick - tock - BOOM".</summary>
        private static readonly int[] RecordPatternMs = { 25, 45, 35, 45, 70 };

        /// <summary>Heavy hit decaying into two lighter taps: "boom - bam - tick".</summary>
        private static readonly int[] DeathPatternMs = { 85, 45, 40, 45, 20 };

        private static readonly HapticProfile Jump = new HapticProfile(JumpPulseMs, ChatterPriority, JumpCooldownMs);

        private static readonly HapticProfile Coin = new HapticProfile(CoinPulseMs, ChatterPriority, CoinCooldownMs);

        private static readonly HapticProfile Purchase = new HapticProfile(PurchasePatternMs, RewardPriority, cooldownMs: 0);

        private static readonly HapticProfile Record = new HapticProfile(RecordPatternMs, RecordPriority, cooldownMs: 0);

        private static readonly HapticProfile Death = new HapticProfile(DeathPatternMs, DeathPriority, cooldownMs: 0);

        public static HapticProfile Get(HapticCue cue)
        {
            return cue switch
            {
                HapticCue.Jump => Jump,
                HapticCue.Coin => Coin,
                HapticCue.Purchase => Purchase,
                HapticCue.Record => Record,
                HapticCue.Death => Death,
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, "Unknown haptic cue."),
            };
        }
    }
}
