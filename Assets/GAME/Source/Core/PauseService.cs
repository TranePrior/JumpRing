using System;
using UnityEngine;

namespace JumpRing.Game.Core
{
    /// <summary>
    /// Reasons that can hold the game paused. Combined as flags so independent systems
    /// (ads, focus loss, dialogs) can request and release a pause without stomping on each other.
    /// </summary>
    [Flags]
    public enum PauseReason
    {
        None = 0,
        Ad = 1 << 0,
        FocusLost = 1 << 1,

        // Intentional gameplay pause (death / second-chance dialog). Route such pauses through
        // PauseService instead of writing Time.timeScale directly, so a focus blur/regain cycle
        // can't resume a run that a dialog deliberately froze.
        Dialog = 1 << 2,

        // A modal UI window (settings, leaderboard, our games, no-ads) is open over the game.
        // Kept apart from Dialog so a window that freezes the game itself can still tell its own
        // freeze from one held by a window stacked on top of it.
        Popup = 1 << 3
    }

    /// <summary>
    /// Single source of truth for the game pause. The game is paused while any reason is active;
    /// <see cref="Time.timeScale"/> and <see cref="AudioListener.pause"/> are derived from the
    /// current reason set, so no caller ever writes them directly. This removes the timeScale
    /// races that happened when ads and the focus handler each drove timeScale independently.
    /// </summary>
    public static class PauseService
    {
        // Reasons that silence the game on top of freezing it. Popup is deliberately absent: the
        // settings window toggles music and effects, and a muted AudioListener under it would make
        // every toggle look dead.
        private const PauseReason MutingReasons = PauseReason.Ad | PauseReason.FocusLost | PauseReason.Dialog;

        private static PauseReason reasons;

        /// <summary>Raised whenever the active reason set changes.</summary>
        public static event Action ReasonsChanged;

        public static bool IsPaused => reasons != PauseReason.None;

        public static bool HasReason(PauseReason reason) => (reasons & reason) != 0;

        /// <summary>
        /// True while something other than <paramref name="ignored"/> holds the pause. A window that
        /// takes a pause reason of its own uses this to tell its own freeze — which it keeps running
        /// under — apart from an external one it must yield to.
        /// </summary>
        public static bool HasAnyReasonExcept(PauseReason ignored) => (reasons & ~ignored) != PauseReason.None;

        public static void Add(PauseReason reason)
        {
            var next = reasons | reason;
            if (next == reasons)
            {
                return;
            }

            reasons = next;
            Apply();
        }

        public static void Remove(PauseReason reason)
        {
            var next = reasons & ~reason;
            if (next == reasons)
            {
                return;
            }

            reasons = next;
            Apply();
        }

        private static void Apply()
        {
            var paused = reasons != PauseReason.None;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = (reasons & MutingReasons) != PauseReason.None;
            ReasonsChanged?.Invoke();
        }

        // Static state survives when the editor runs with domain reload disabled; reset it before
        // the scene loads so a leftover pause from a previous play session can't strand the game.
        // The subscriber list survives too, so it is dropped before Apply() — otherwise the reset
        // would notify delegates pointing at objects destroyed with the previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            ReasonsChanged = null;
            reasons = PauseReason.None;
            Apply();
        }
    }
}
