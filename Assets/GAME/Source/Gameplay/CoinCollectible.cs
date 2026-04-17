using JumpRing.Game.Core.Services;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class CoinCollectible : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int amount = 1;

        private ICurrencyService currencyService;
        private RunSessionController runSessionController;
        private RiskRewardSystem riskRewardSystem;
        private MicroEventSystem microEventSystem;
        private bool isCollected;

        public void Construct(
            ICurrencyService currency,
            RunSessionController runSession,
            RiskRewardSystem riskReward,
            MicroEventSystem microEvent)
        {
            currencyService = currency;
            runSessionController = runSession;
            riskRewardSystem = riskReward;
            microEventSystem = microEvent;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryCollect(collision.gameObject);
        }

        private void TryCollect(GameObject target)
        {
            if (isCollected)
            {
                return;
            }

            if (!runSessionController.CanControlPlayer)
            {
                return;
            }

            if (target.GetComponentInParent<CoinCollectorZone>() == null)
            {
                return;
            }

            isCollected = true;

            var coinMultiplier = 1f;

            if (riskRewardSystem != null)
            {
                coinMultiplier *= riskRewardSystem.CoinValueMultiplier;
            }

            if (microEventSystem != null)
            {
                coinMultiplier *= microEventSystem.EventCoinMultiplier;
            }

            var finalAmount = Mathf.Max(1, Mathf.RoundToInt(amount * coinMultiplier));
            currencyService.Add(finalAmount);
            Destroy(gameObject);
        }
    }
}
