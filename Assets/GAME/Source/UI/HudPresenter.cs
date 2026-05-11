using TMPro;
using UnityEngine;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;

namespace JumpRing.Game.UI
{
    public sealed class HudPresenter : MonoBehaviour
    {
        private const string ScoreFormat = "SCORE: {0}";
        private const string BestScoreFormat = "BEST SCORE: {0}";
        private const string CoinsFormat = "COIN: {0}";

        [SerializeField]
        private TMP_Text scoreLabel;

        [SerializeField]
        private TMP_Text bestScoreLabel;

        [SerializeField]
        private TMP_Text diamondsLabel;

        [SerializeField]
        private MonoBehaviour gameStateMachineComponent;

        private IScoreService scoreService;
        private ICurrencyService currencyService;
        private IGameStateMachine gameStateMachine;
        private bool isConstructed;

        public void Construct(IScoreService score, ICurrencyService currency)
        {
            if (isConstructed)
            {
                return;
            }

            scoreService = score;
            currencyService = currency;

            if (gameStateMachineComponent != null)
            {
                gameStateMachine = (IGameStateMachine)gameStateMachineComponent;
                gameStateMachine.StateChanged += OnStateChanged;
            }

            scoreService.ScoreChanged += OnScoreChanged;
            currencyService.BalanceChanged += OnBalanceChanged;

            OnScoreChanged(scoreService.CurrentScore);
            OnBalanceChanged(currencyService.Balance);

            if (gameStateMachine != null)
            {
                OnStateChanged(gameStateMachine.CurrentState);
            }

            isConstructed = true;
        }

        private void OnDestroy()
        {
            if (!isConstructed)
            {
                return;
            }

            scoreService.ScoreChanged -= OnScoreChanged;
            currencyService.BalanceChanged -= OnBalanceChanged;

            if (gameStateMachine != null)
            {
                gameStateMachine.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(GameState state)
        {
            var showScores = state != GameState.MainMenu;

            if (scoreLabel != null)
            {
                scoreLabel.gameObject.SetActive(showScores);
            }

            if (bestScoreLabel != null)
            {
                bestScoreLabel.gameObject.SetActive(showScores);
            }
        }

        private void OnScoreChanged(int score)
        {
            scoreLabel.text = string.Format(ScoreFormat, score);
            bestScoreLabel.text = string.Format(BestScoreFormat, scoreService.BestScore);
        }

        private void OnBalanceChanged(int balance)
        {
            diamondsLabel.text = string.Format(CoinsFormat, balance);
        }
    }
}
