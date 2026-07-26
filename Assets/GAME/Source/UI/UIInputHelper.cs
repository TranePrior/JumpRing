using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Checks whether a tap/click lands on an interactable UI element (Button, Toggle, etc.)
    /// rather than any Graphic with raycastTarget enabled.
    /// </summary>
    public static class UIInputHelper
    {
        private static readonly List<RaycastResult> RaycastResults = new(8);

        // Reused across taps: this runs up to twice per jump, and a fresh PointerEventData each
        // time is garbage on the exact frame the player is looking at.
        private static PointerEventData pointerData;
        private static EventSystem pointerDataOwner;

        // Static state survives domain-reload-off play sessions; the EventSystem it points at does not.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            pointerData = null;
            pointerDataOwner = null;
            RaycastResults.Clear();
        }

        /// <summary>
        /// True when the tap landed on a <see cref="Selectable"/> — a button the player meant to press.
        /// </summary>
        public static bool IsTapOverInteractableUI()
        {
            return RaycastTap(requireSelectable: true);
        }

        /// <summary>
        /// True when the tap landed on any raycast-blocking UI, button or not.
        /// </summary>
        /// <remarks>
        /// Gameplay input must use this, not <see cref="IsTapOverInteractableUI"/>: an open popup
        /// covers the screen with plain <see cref="Graphic"/>s (card body, dim overlay, labels) that
        /// are not <see cref="Selectable"/>s, so the interactable-only check let every tap that
        /// missed a button fall through to the player controller and silently restart the run
        /// behind the popup.
        /// </remarks>
        public static bool IsTapOverBlockingUI()
        {
            return RaycastTap(requireSelectable: false);
        }

        private static bool RaycastTap(bool requireSelectable)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (pointerData == null || pointerDataOwner != eventSystem)
            {
                pointerDataOwner = eventSystem;
                pointerData = new PointerEventData(eventSystem);
            }

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerData.position = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null)
            {
                pointerData.position = Mouse.current.position.ReadValue();
            }
            else
            {
                return false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            pointerData.position = (Vector2)Input.mousePosition;
#else
            return false;
#endif

            RaycastResults.Clear();
            eventSystem.RaycastAll(pointerData, RaycastResults);

            bool hit = false;

            if (requireSelectable)
            {
                for (int i = 0; i < RaycastResults.Count; i++)
                {
                    if (RaycastResults[i].gameObject.GetComponentInParent<Selectable>() != null)
                    {
                        hit = true;
                        break;
                    }
                }
            }
            else
            {
                hit = RaycastResults.Count > 0;
            }

            RaycastResults.Clear();
            return hit;
        }
    }
}
