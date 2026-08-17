namespace JumpRing.Game.Core.Services.Haptics
{
    /// <summary>
    /// Gameplay moment a haptic response is played for.
    /// </summary>
    public enum HapticCue : byte
    {
        Jump = 0,
        Coin = 1,
        Purchase = 2,
        Record = 3,
        Death = 4,
    }
}
