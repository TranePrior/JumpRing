using DG.Tweening;
using JumpRing.Game.Core.State;
using TMPro;
using UnityEngine;
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

        [SerializeField]
        private TapHandAnimator tapHandAnimator;

        [Header("Label Pulse")]
        [SerializeField, Range(0f, 1f)]
        private float alphaMin = 0.7f;

        [SerializeField, Range(0f, 1f)]
        private float alphaMax = 1f;

        [SerializeField, Min(0.01f)]
        private float scaleMin = 0.95f;

        [SerializeField, Min(0.01f)]
        private float scaleMax = 1.05f;

        private bool isVisible;
        private Tween fadeTween;

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
            if (tapToStartLabel == null || tapHandAnimator == null) return;

            float t = tapHandAnimator.PulseT;
            float alpha = Mathf.Lerp(alphaMin, alphaMax, t);
            float scale = Mathf.Lerp(scaleMin, scaleMax, t);

            var c = tapToStartLabel.color;
            c.a = alpha;
            tapToStartLabel.color = c;
            tapToStartLabel.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void DetectTapToStart()
        {
            if (ShopPresenter.IsOpen) return;
            if (!WasTapPressed()) return;
            if (UIInputHelper.IsTapOverInteractableUI()) return;

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
            fadeTween?.Kill();

            if (isVisible)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                fadeTween = WindowAnimations.FadeIn(canvasGroup);
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
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
