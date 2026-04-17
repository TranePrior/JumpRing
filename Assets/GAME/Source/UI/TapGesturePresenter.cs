using JumpRing.Game.Core.State;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    public sealed class TapGesturePresenter : MonoBehaviour
    {
        [SerializeField]
        private Image tapGestureImage;

        [SerializeField]
        private MonoBehaviour gameStateMachineComponent;

        [Header("Pulse Animation")]
        [SerializeField, Min(0.01f)]
        private float pulseSpeed = 2f;

        [SerializeField, Min(0f)]
        private float scaleMin = 0.85f;

        [SerializeField, Min(0f)]
        private float scaleMax = 1.1f;

        [Header("Fade Animation")]
        [SerializeField, Range(0f, 1f)]
        private float alphaMin = 0.4f;

        [SerializeField, Range(0f, 1f)]
        private float alphaMax = 1f;

        private RectTransform tapGestureRect;
        private bool isAnimating;

        private IGameStateMachine GameStateMachine => (IGameStateMachine)gameStateMachineComponent;

        private void Awake()
        {
            tapGestureRect = tapGestureImage.rectTransform;
        }

        private void OnEnable()
        {
            GameStateMachine.StateChanged += OnStateChanged;
            OnStateChanged(GameStateMachine.CurrentState);
        }

        private void OnDisable()
        {
            GameStateMachine.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (!isAnimating)
            {
                return;
            }

            var t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;

            var scale = Mathf.Lerp(scaleMin, scaleMax, t);
            tapGestureRect.localScale = new Vector3(scale, scale, 1f);

            var alpha = Mathf.Lerp(alphaMin, alphaMax, t);
            var color = tapGestureImage.color;
            color.a = alpha;
            tapGestureImage.color = color;
        }

        private void OnStateChanged(GameState state)
        {
            var isReady = state == GameState.Ready;
            tapGestureImage.enabled = isReady;
            isAnimating = isReady;

            if (!isReady)
            {
                tapGestureRect.localScale = Vector3.one;
            }
        }
    }
}
