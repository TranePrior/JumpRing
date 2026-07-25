using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Dips the visual layer of a 3D button on press so the bevel reads as real key travel.
    /// The visual layer must be a plain child, not a layout element: a layout group would
    /// overwrite the offset on every rebuild.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class Button3DPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private const float TravelDuration = 0.06f;

        [SerializeField]
        private RectTransform visual;

        [SerializeField]
        private float travel = 6f;

        private Vector2 restPosition;
        private Tween travelTween;

        private void Awake()
        {
            restPosition = visual.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            MoveTo(restPosition + Vector2.down * travel);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            MoveTo(restPosition);
        }

        private void OnDisable()
        {
            travelTween?.Kill();
            travelTween = null;
            visual.anchoredPosition = restPosition;
        }

        private void OnDestroy()
        {
            travelTween?.Kill();
        }

        private void MoveTo(Vector2 target)
        {
            travelTween?.Kill();
            travelTween = visual.DOAnchorPos(target, TravelDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }
}
