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

        public static bool IsTapOverInteractableUI()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            var pointerData = new PointerEventData(eventSystem);

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

            for (int i = 0; i < RaycastResults.Count; i++)
            {
                if (RaycastResults[i].gameObject.GetComponentInParent<Selectable>() != null)
                {
                    RaycastResults.Clear();
                    return true;
                }
            }

            RaycastResults.Clear();
            return false;
        }
    }
}
