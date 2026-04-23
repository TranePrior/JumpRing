using UnityEngine;

namespace JumpRing.Game.Theming
{
    public enum SkinPackType
    {
        Rings = 0,
        Games = 1,
    }

    [CreateAssetMenu(fileName = "SkinPack", menuName = "JumpRing/Skin Pack")]
    public sealed class SkinPack : ScriptableObject
    {
        [SerializeField]
        private string packId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private SkinPackType packType;

        [SerializeField]
        private SkinItem[] skins;

        public string PackId => packId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public SkinPackType PackType => packType;
        public SkinItem[] Skins => skins;
    }
}
