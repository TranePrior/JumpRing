using UnityEngine;

namespace JumpRing.Game.Theming
{
    public sealed class PlayerSkinSlot : MonoBehaviour
    {
        private GameObject activeSkinInstance;
        private IPlayerSkin activeSkin;

        public IPlayerSkin Skin => activeSkin;

        public void ApplySkin(GameObject skinPrefab)
        {
            DestroyAllSkinChildren();

            if (skinPrefab == null)
            {
                return;
            }

            activeSkinInstance = Instantiate(skinPrefab, transform);
            activeSkinInstance.transform.localPosition = Vector3.zero;
            activeSkinInstance.transform.localRotation = Quaternion.identity;
            activeSkinInstance.transform.localScale = Vector3.one;

            activeSkin = activeSkinInstance.GetComponent<IPlayerSkin>();
        }

        private void DestroyAllSkinChildren()
        {
            activeSkinInstance = null;
            activeSkin = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.GetComponentInChildren<SpriteRenderer>() != null)
                {
                    child.gameObject.SetActive(false);
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
