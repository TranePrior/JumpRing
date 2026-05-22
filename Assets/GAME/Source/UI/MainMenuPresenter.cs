using JumpRing.Game.Core.State;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using JumpRing.Game.Gameplay;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace JumpRing.Game.UI
{
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private GameStateMachine gameStateMachine;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private ShopPresenter shopPresenter;

        [Header("Tap To Start")]
        [SerializeField]
        private TMP_Text tapToStartLabel;

        [Header("Pulse Animation")]
        [SerializeField, Min(0.01f)]
        private float pulseSpeed = 1.5f;

        [SerializeField, Range(0f, 1f)]
        private float alphaMin = 0.4f;

        [SerializeField, Range(0f, 1f)]
        private float alphaMax = 1f;

        private bool isVisible;

        private void OnEnable()
        {
            if (shopPresenter == null)
            {
                shopPresenter = GetComponentInChildren<ShopPresenter>(true);
            }

            gameStateMachine.StateChanged += OnStateChanged;
            OnStateChanged(gameStateMachine.CurrentState);
        }

        private void OnDisable()
        {
            gameStateMachine.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (!isVisible) return;

            AnimateTapToStart();
            DetectTapToStart();
        }

        private void AnimateTapToStart()
        {
            var t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            var alpha = Mathf.Lerp(alphaMin, alphaMax, t);

            if (tapToStartLabel != null)
            {
                var c = tapToStartLabel.color;
                c.a = alpha;
                tapToStartLabel.color = c;
            }

        }

        private void DetectTapToStart()
        {
            if (!WasTapPressed()) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            runSessionController.StartRun();
        }

        private static bool WasTapPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private void OnStateChanged(GameState state)
        {
            isVisible = state == GameState.MainMenu;

            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        public void StartRun()
        {
            runSessionController.StartRun();
        }

        public void OpenShop()
        {
            if (shopPresenter != null)
            {
                shopPresenter.Open();
            }
        }
    }
}
