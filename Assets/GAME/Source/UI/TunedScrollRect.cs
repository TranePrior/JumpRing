using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// ScrollRect that tames desktop wheel/touchpad input. Precision touchpads emit a
    /// dense burst of wheel events per gesture, which the stock ScrollRect simply sums,
    /// making the content fly. Each event's delta is scaled down and clamped so a
    /// touchpad swipe stops racing while a discrete mouse wheel tick stays responsive.
    /// </summary>
    public sealed class TunedScrollRect : ScrollRect
    {
        [SerializeField] private float _wheelSpeedMultiplier = 0.5f;
        [SerializeField] private float _maxStepPerEvent = 8f;

        public override void OnScroll(PointerEventData eventData)
        {
            Vector2 delta = eventData.scrollDelta;
            delta.x = Mathf.Clamp(delta.x * _wheelSpeedMultiplier, -_maxStepPerEvent, _maxStepPerEvent);
            delta.y = Mathf.Clamp(delta.y * _wheelSpeedMultiplier, -_maxStepPerEvent, _maxStepPerEvent);
            eventData.scrollDelta = delta;
            base.OnScroll(eventData);
        }
    }
}
