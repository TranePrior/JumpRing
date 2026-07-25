namespace JumpRing.Game.Core.Services
{
    /// <summary>
    /// Single source of truth for every persisted key.
    /// <para>
    /// <see cref="IntKeys"/> and <see cref="StringKeys"/> drive what <see cref="PlatformStorageService"/>
    /// preloads from the cloud. Keys used to be declared twice — once as a literal on the owning
    /// service and once in the composition root's preload arrays — so a service added with a new key
    /// but not registered in the arrays would read a value that was never loaded. Nothing failed
    /// loudly: the player simply lost that slice of progress when moving between devices.
    /// Declaring both from the same constants makes that impossible to get wrong silently.
    /// </para>
    /// </summary>
    public static class StorageKeys
    {
        public const string DiamondBalance = "DiamondBalance";
        public const string BestScore = "BestScore";
        public const string ConsecutiveDeaths = "BonusSpawner_ConsecutiveDeaths";
        public const string NoAdsPurchased = "NoAdsPurchased";
        public const string SettingsMusic = "Settings_Music";
        public const string SettingsEffects = "Settings_Effects";
        public const string SettingsVibration = "Settings_Vibration";

        public const string OwnedSkins = "OwnedSkins";
        public const string ActiveSkinId = "ActiveSkinId";
        public const string SkinUpgrades = "SkinUpgrades";
        public const string SelectedLanguage = "SelectedLanguage";

        public static readonly string[] IntKeys =
        {
            DiamondBalance,
            BestScore,
            ConsecutiveDeaths,
            NoAdsPurchased,
            SettingsMusic,
            SettingsEffects,
            SettingsVibration
        };

        public static readonly string[] StringKeys =
        {
            OwnedSkins,
            ActiveSkinId,
            SkinUpgrades,
            SelectedLanguage
        };
    }
}
