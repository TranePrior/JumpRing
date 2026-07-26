using System;
using DG.Tweening;
using UnityEngine;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Open/close behaviour shared by the game's modal cards.
    /// </summary>
    /// <remarks>
    /// Game over, second chance and double reward each carried their own copy of this — same
    /// panel/canvas-group pair, same sequence field, same AnimateOpen/AnimateClose calls. The copies
    /// drifted: the game over card grew an immediate-close path and raycast handling that the
    /// second chance card never got, so the same popup bug had to be fixed twice. One
    /// implementation here means a fix lands on every window at once.
    /// </remarks>
    public abstract class PopupWindow : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private CanvasGroup panelCanvasGroup;

        private Sequence windowSequence;

        protected GameObject Panel => panel;

        protected bool IsWindowOpen => panel.activeSelf;

        /// <summary>
        /// Makes the card visible without animating, so the owner can fill in content and lay it
        /// out before <see cref="PlayOpenAnimation"/> starts the tweens.
        /// </summary>
        protected void ActivateWindow()
        {
            panel.SetActive(true);
        }

        protected void PlayOpenAnimation()
        {
            windowSequence?.Kill();
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
            windowSequence = WindowAnimations.AnimateOpen(panelCanvasGroup, panel.transform);
        }

        protected void OpenWindow()
        {
            ActivateWindow();
            PlayOpenAnimation();
        }

        /// <summary>
        /// Plays the close animation, then deactivates the card and runs <paramref name="onComplete"/>.
        /// Input is refused immediately rather than at the end of the tween, so the card cannot be
        /// clicked twice while it animates away.
        /// </summary>
        protected void CloseWindow(Action onComplete = null)
        {
            windowSequence?.Kill();
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            windowSequence = WindowAnimations.AnimateClose(panelCanvasGroup, panel.transform, panel);

            if (onComplete != null)
            {
                windowSequence.AppendCallback(() => onComplete());
            }
        }

        /// <summary>
        /// Drops the card on the spot, with no animation, and resets the transform the open tween
        /// scales. Used when the state that owns the window is left from somewhere else.
        /// </summary>
        protected void CloseWindowImmediate()
        {
            windowSequence?.Kill();
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.alpha = 0f;
            panel.transform.localScale = Vector3.one;
            panel.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            windowSequence?.Kill();
        }
    }
}
