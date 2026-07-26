using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Lays a card out once and then switches its layout drivers off.
    /// </summary>
    /// <remarks>
    /// The game over card nests eight <see cref="LayoutGroup"/>s and three
    /// <see cref="ContentSizeFitter"/>s. Every <c>SetText</c> on a label inside that tree marks the
    /// whole chain dirty, so the count-up tweens used to rebuild it on every frame of the opening
    /// animation — and the fitters resized the card mid-count whenever the digit count changed.
    /// Calling <see cref="Rebuild"/> after the final values are written lays the card out at its
    /// final size and freezes it, so the tweens only regenerate the text meshes.
    /// </remarks>
    public sealed class LayoutFreezer : MonoBehaviour
    {
        [SerializeField]
        private RectTransform root;

        private LayoutGroup[] layoutGroups;
        private ContentSizeFitter[] sizeFitters;

        private void Awake()
        {
            layoutGroups = root.GetComponentsInChildren<LayoutGroup>(true);
            sizeFitters = root.GetComponentsInChildren<ContentSizeFitter>(true);
        }

        /// <summary>
        /// Re-enables the layout drivers, lays <see cref="root"/> out immediately, then disables
        /// them again. Call it while <see cref="root"/> is active and holds its final content.
        /// </summary>
        public void Rebuild()
        {
            SetLayoutEnabled(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            SetLayoutEnabled(false);
        }

        private void SetLayoutEnabled(bool value)
        {
            for (int i = 0; i < layoutGroups.Length; i++)
            {
                layoutGroups[i].enabled = value;
            }

            for (int i = 0; i < sizeFitters.Length; i++)
            {
                sizeFitters[i].enabled = value;
            }
        }
    }
}
