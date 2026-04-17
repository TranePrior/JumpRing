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
            if (activeSkinInstance != null)
            {
                Destroy(activeSkinInstance);
                activeSkinInstance = null;
                activeSkin = null;
            }

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
    }
}
