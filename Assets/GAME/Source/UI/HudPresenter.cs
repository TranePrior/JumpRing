using TMPro;
using UnityEngine;
using JumpRing.Game.Core.Services;

namespace JumpRing.Game.UI
{
    public sealed class HudPresenter : MonoBehaviour
    {
        private const string ScoreFormat = "Score: {0}";
        private const string BestScoreFormat = "BEST SCORE: {0}";
        private const string CoinsFormat = "COINS: {0}";

        [SerializeField]
        private TMP_Text scoreLabel;

        [SerializeField]
        private TMP_Text bestScoreLabel;

        [SerializeField]
        private TMP_Text diamondsLabel;

        private IScoreService scoreService;
        private ICurrencyService currencyService;
        private bool isConstructed;

        public void Construct(IScoreService score, ICurrencyService currency)
        {
            if (isConstructed)
            {
                return;
            }

            scoreService = score;
            currencyService = currency;

            scoreService.ScoreChanged += OnScoreChanged;
            currencyService.BalanceChanged += OnBalanceChanged;

            OnScoreChanged(scoreService.CurrentScore);
            OnBalanceChanged(currencyService.Balance);
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
