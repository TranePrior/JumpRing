namespace JumpRing.Game.Core.Localization
{
    /// <summary>
    /// Every user-facing string in the game. Values are serialized as ints inside
    /// <see cref="LocalizationData"/> assets and <see cref="LocalizedText"/> components, so entries
    /// may only be appended — reordering or removing one silently repoints existing labels.
    /// </summary>
    public enum LocalizationKey
    {
        TapToStart,
        GameOver,
        Score,
        BestScore,
        Menu,
        Retry,
        Shop,
        Select,
        Active,
        DoubleReward,
        NewBest,
        UpgradeLevel,
        MaxUpgradeLevel,
        SecondChanceTitle,
        Claim,
        Result,
        WatchAdDouble,
        NoAdsTitle,
        NoAdsSubtitle,
        NoAdsPurchase,
        LeaderboardAuthTitle,
        LeaderboardSignIn,
        LeaderboardYourRank,
        LeaderboardMode,
        ShareTitle,
        ShareCopyLink,
        ShareAction,
        SkinNameClassic,
        SkinNameCat,
        SkinNameFrog,
        SkinNamePenguin,
        SkinNameVampire,
        SkinNameDoctor,
        SkinNameLord,
        SettingsTitle,
        SettingsMusic,
        SettingsEffects,
        SettingsVibration,
        NoAdsPopupTitle,
        Loading,
        LeaderboardTitle,
        OurGamesTitle,
        OurGamesPlay
    }
}
