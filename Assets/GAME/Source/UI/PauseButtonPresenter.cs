using JumpRing.Game.Core.State;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// The only pause affordance inside a run: the menu icon bar lives under the main-menu canvas,
    /// which is faded out and non-interactive the moment the run starts. Yandex asks for one
    /// (publishing requirement 6.3), and the platform panel's stop button is not it — that one is
    /// outside the game.
    /// </summary>
    /// <remarks>
    /// The button opens the settings window, and the window is what freezes the game:
    /// <see cref="PopupTracker"/> takes <c>PauseReason.Popup</c> while it is up, which stops the
    /// world, mutes nothing the player is toggling, and tells the platform the run is not running.
    /// </remarks>
    public sealed class PauseButtonPresenter : MonoBehaviour
    {
        // Lives on the HUD, not on the button: it deactivates the button object, and doing that to
        // its own object would unsubscribe it from the state machine for good.
        [SerializeField] private Button _button;

        [SerializeField] private GameStateMachine _gameStateMachine;

        [SerializeField] private SettingsPopupOpener _settingsPopupOpener;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
            _gameStateMachine.StateChanged += OnStateChanged;
            OnStateChanged(_gameStateMachine.CurrentState);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
            _gameStateMachine.StateChanged -= OnStateChanged;
        }

        private void OnClicked()
        {
            _settingsPopupOpener.Open();
        }

        private void OnStateChanged(GameState state)
        {
            // Ready is the armed-but-not-moving state at the start of a run, so the button is up
            // from the first frame the player can act. It stays out of the menu (the icon bar
            // already carries the same window there) and out of Paused/GameOver, where the
            // second-chance and game-over panels own the screen.
            bool isVisible = state == GameState.Gameplay || state == GameState.Ready;
            _button.gameObject.SetActive(isVisible);
        }
    }
}
