using TMPro;
using UnityEngine;
using JumpRing.Game.Core.Localization;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;

namespace JumpRing.Game.UI
{
    public sealed class HudPresenter : MonoBehaviour
    {
        private const string NumberFormat = "{0}";

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

        // The localized word changes only when the player switches language, so the format string
        // is rebuilt then rather than on every score change. TMP.SetText formats the number into
        // its own buffer, which keeps the per-tap path free of string allocations.
        private string cachedScoreWord;
        private string cachedScoreFormat = NumberFormat;

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
        }

        private void OnScoreChanged(int score)
        {
            if (scoreLabel != null)
            {
                string scoreWord = LocalizationService.Instance != null
                    ? LocalizationService.Instance.GetText(LocalizationKey.Score)
                    : "SCORE";

                if (!ReferenceEquals(scoreWord, cachedScoreWord))
                {
                    cachedScoreWord = scoreWord;
                    cachedScoreFormat = scoreWord + ": " + NumberFormat;
                }

                scoreLabel.SetText(cachedScoreFormat, score);
            }

            if (bestScoreLabel != null)
            {
                bestScoreLabel.SetText(NumberFormat, scoreService.BestScore);
            }
        }

        private void OnBalanceChanged(int balance)
        {
            if (diamondsLabel != null)
            {
                diamondsLabel.SetText(NumberFormat, balance);
            }
        }
    }
}
