using TMPro;
using UnityEngine;
using JumpRing.Game.Core.Services;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Drives the persistent HUD counters: best score and the coin balance.
    /// </summary>
    /// <remarks>
    /// The live run score is not shown here — it is rendered in world space by
    /// <see cref="WorldTapCounterPresenter"/>. This class used to carry a `scoreLabel` field, the
    /// localized "SCORE: {0}" format caching around it, and a state subscription whose only job was
    /// to hide that label outside the main menu. Nothing was ever assigned to the field, so all of
    /// it was dead weight that read like a missing reference.
    /// </remarks>
    public sealed class HudPresenter : MonoBehaviour
    {
        private const string NumberFormat = "{0}";

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
            bestScoreLabel.SetText(NumberFormat, scoreService.BestScore);
        }

        private void OnBalanceChanged(int balance)
        {
            diamondsLabel.SetText(NumberFormat, balance);
        }
    }
}
