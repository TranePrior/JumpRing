using System;

namespace JumpRing.Game.Core.Localization
{
    /// <summary>
    /// Turns raw locale strings into a <see cref="Language"/>. Free of Unity and PlatformLink types
    /// so the rules Yandex moderation checks — auto-detection from the platform locale (requirement
    /// 2.14) and an English fallback for everything the game is not translated to — are covered by
    /// EditMode tests instead of a manual pass through the published build.
    /// </summary>
    public static class LanguageResolver
    {
        private const string RussianPrefix = "ru";

        /// <summary>
        /// Maps the locale the platform SDK reports for the current player.
        /// </summary>
        /// <remarks>
        /// Platform SDKs disagree on how they spell a locale: the editor stub reports "En", Yandex
        /// reports "ru", and a full tag like "ru-RU" is equally legal. An exact match against "ru"
        /// silently served English to Russian players on any of those spellings. Everything the game
        /// has no translation for resolves to English, which is what the platform requires.
        /// </remarks>
        public static Language FromPlatformLocale(string platformLocale)
        {
            return IsRussian(platformLocale) ? Language.RU : Language.EN;
        }

        private static bool IsRussian(string locale)
        {
            return !string.IsNullOrEmpty(locale)
                && locale.StartsWith(RussianPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
