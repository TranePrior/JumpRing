using UnityEngine;

namespace JumpRing.Game.Theming
{
    [CreateAssetMenu(fileName = "SkinItem", menuName = "JumpRing/Skin Item")]
    public sealed class SkinItem : ScriptableObject
    {
        [SerializeField]
        private string skinId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private Sprite icon;

        [SerializeField, Min(0)]
        private int price;

        [SerializeField]
        private ThemeData themeData;

        public string SkinId => skinId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int Price => price;
        public ThemeData ThemeData => themeData;
    }
}
