using System.Collections.Generic;
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
        private RectTransform[] rebuildRoots;

        private void Awake()
        {
            layoutGroups = root.GetComponentsInChildren<LayoutGroup>(true);
            sizeFitters = root.GetComponentsInChildren<ContentSizeFitter>(true);
            rebuildRoots = CollectRebuildRoots();
        }

        /// <summary>
        /// Re-enables the layout drivers, lays <see cref="root"/> out immediately, then disables
        /// them again. Call it while <see cref="root"/> is active and holds its final content.
        /// </summary>
        public void Rebuild()
        {
            SetLayoutEnabled(true);

            for (int i = 0; i < rebuildRoots.Length; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rebuildRoots[i]);
            }

            SetLayoutEnabled(false);
        }

        /// <remarks>
        /// <see cref="LayoutRebuilder.ForceRebuildLayoutImmediate"/> only descends into the children
        /// of objects that are layout controllers themselves. A group whose parent chain up to
        /// <see cref="root"/> passes through a plain RectTransform is never reached — the double
        /// reward button's icon+label row sits under the button's visual, so its children stayed at
        /// zero size and the label rendered centred on the button's bottom-left corner. Such groups
        /// are collected as rebuild roots of their own, parents first so they inherit final sizes.
        /// </remarks>
        private RectTransform[] CollectRebuildRoots()
        {
            List<RectTransform> roots = new List<RectTransform> { root };

            for (int i = 0; i < layoutGroups.Length; i++)
            {
                RectTransform rect = (RectTransform)layoutGroups[i].transform;

                if (rect != root && !IsReachedFromRoot(rect))
                {
                    roots.Add(rect);
                }
            }

            return roots.ToArray();
        }

        private bool IsReachedFromRoot(RectTransform rect)
        {
            for (Transform current = rect.parent; current != null; current = current.parent)
            {
                if (current.GetComponent<ILayoutController>() == null)
                {
                    return false;
                }

                if (current == root)
                {
                    return true;
                }
            }

            return false;
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
