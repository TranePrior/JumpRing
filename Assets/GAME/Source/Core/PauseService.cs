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
        Dialog = 1 << 2
    }

    /// <summary>
    /// Single source of truth for the game pause. The game is paused while any reason is active;
    /// <see cref="Time.timeScale"/> and <see cref="AudioListener.pause"/> are derived from the
    /// current reason set, so no caller ever writes them directly. This removes the timeScale
    /// races that happened when ads and the focus handler each drove timeScale independently.
    /// </summary>
    public static class PauseService
    {
        private static PauseReason reasons;

        /// <summary>Raised whenever the active reason set changes.</summary>
        public static event Action ReasonsChanged;

        public static bool IsPaused => reasons != PauseReason.None;

        public static bool HasReason(PauseReason reason) => (reasons & reason) != 0;

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
            AudioListener.pause = paused;
            ReasonsChanged?.Invoke();
        }

        // Static state survives when the editor runs with domain reload disabled; reset it before
        // the scene loads so a leftover pause from a previous play session can't strand the game.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            reasons = PauseReason.None;
            Apply();
        }
    }
}
