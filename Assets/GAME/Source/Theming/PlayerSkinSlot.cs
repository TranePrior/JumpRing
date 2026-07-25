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
                    // Deactivated first so the old skin disappears this frame, then destroyed on
                    // Unity's own schedule. DestroyImmediate tears the object down inside this loop,
                    // outside the deferred destruction cycle, which is both a visible hitch and a
                    // hazard for anything mid-update on that object.
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
