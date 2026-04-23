using UnityEngine;

namespace JumpRing.Game.Theming
{
    [CreateAssetMenu(fileName = "SkinPackCatalog", menuName = "JumpRing/Skin Pack Catalog")]
    public sealed class SkinPackCatalog : ScriptableObject
    {
        [SerializeField]
        private SkinPack[] packs;

        [SerializeField]
        private SkinItem defaultSkin;

        public SkinPack[] Packs => packs;
        public SkinItem DefaultSkin => defaultSkin;

        public SkinItem FindSkinById(string skinId)
        {
            foreach (var pack in packs)
            {
                foreach (var skin in pack.Skins)
                {
                    if (skin.SkinId == skinId)
                    {
                        return skin;
                    }
                }
            }

            return null;
        }

        public SkinPack FindPackForSkin(SkinItem skin)
        {
            foreach (var pack in packs)
            {
                foreach (var s in pack.Skins)
                {
                    if (s == skin)
                    {
                        return pack;
                    }
                }
            }

            return null;
        }
    }
}
