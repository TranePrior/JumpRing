using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Gameplay;
using JumpRing.Game.Core.State;

namespace JumpRing.Game.UI
{
    public sealed class HeartsHudPresenter : MonoBehaviour
    {
        [SerializeField]
        private GameStateMachine gameStateMachine;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private BonusEffectManager bonusEffectManager;

        [SerializeField]
        private Image[] heartImages;

        [SerializeField]
        private Color activeColor = new Color(1f, 0.1f, 0.2f, 1f);

        [SerializeField]
        private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        private void OnEnable()
        {
            bonusEffectManager.SecondChanceCountChanged += UpdateHearts;
            UpdateHearts(bonusEffectManager.SecondChanceCount);

            gameStateMachine.StateChanged += OnStateChanged;
            OnStateChanged(gameStateMachine.CurrentState);
        }

        private void OnDisable()
        {
            bonusEffectManager.SecondChanceCountChanged -= UpdateHearts;
            gameStateMachine.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            var isVisible = state == GameState.Gameplay || state == GameState.Paused || state == GameState.Ready;
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        private void UpdateHearts(int count)
        {
            for (var i = 0; i < heartImages.Length; i++)
            {
                heartImages[i].color = i < count ? activeColor : inactiveColor;
            }
        }
    }
}
