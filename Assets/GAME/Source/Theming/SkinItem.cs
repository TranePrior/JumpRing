using JumpRing.Game.Core.Localization;
using UnityEngine;

namespace JumpRing.Game.Theming
{
    [CreateAssetMenu(fileName = "SkinItem", menuName = "JumpRing/Skin Item")]
    public sealed class SkinItem : ScriptableObject
    {
        [SerializeField]
        private string skinId;

        /// <summary>
        /// Shop name of the skin. A localization key rather than a literal: the shop grid is the
        /// most text-heavy screen in the game, and a name baked into the asset stayed Russian for
        /// English players no matter what the platform reported.
        /// </summary>
        [SerializeField]
        private LocalizationKey nameKey;

        [SerializeField]
        private Sprite icon;

        [SerializeField, Min(0)]
        private int price;

        [SerializeField]
        private Sprite currencyIcon;

        [SerializeField]
        private Sprite shopSprite;

        [SerializeField]
        private ThemeData themeData;

        public string SkinId => skinId;
        public LocalizationKey NameKey => nameKey;
        public Sprite Icon => icon;
        public Sprite CurrencyIcon => currencyIcon;
        public Sprite ShopSprite => shopSprite;
        public int Price => price;
        public ThemeData ThemeData => themeData;
    }
}
