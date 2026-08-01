namespace JumpRing.Game.Gameplay
{
    /// <summary>
    /// Swallows input for a moment after a revive puts the game back into the Ready state.
    /// </summary>
    /// <remarks>
    /// A tap landing on the frame Ready is entered is almost always the tap that got it there. The
    /// button behind a revive stops blocking raycasts the instant it is pressed, so the very same
    /// click fell through to the player controller and began the run — the player, still waiting
    /// for the "tap to start" prompt, then watched the ring drive itself into the line. A revive
    /// that was over before the player knew it had started.
    /// <para>
    /// A run started from the menu is the opposite case and must never be sampled as locked: there
    /// the tap that reached Ready is the player asking to play, and it goes on to fire the first
    /// jump in the same frame.
    /// </para>
    /// </remarks>
    public sealed class ReadyInputLock
    {
        private bool wasReadyAfterRevive;
        private float unlockTime;

        /// <summary>
        /// Call every frame, before reading input. Arms the lock on the frame a revive lands in Ready.
        /// </summary>
        public void Sample(bool isReadyAfterRevive, float now, float lockSeconds)
        {
            if (isReadyAfterRevive && !wasReadyAfterRevive)
            {
                unlockTime = now + lockSeconds;
            }

            wasReadyAfterRevive = isReadyAfterRevive;
        }

        public bool IsLocked(float now)
        {
            return now < unlockTime;
        }
    }
}
