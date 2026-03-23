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
        private bool isCollected;

        public void Construct(ICurrencyService currency, RunSessionController runSession)
        {
            currencyService = currency;
            runSessionController = runSession;
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
            currencyService.Add(amount);
            Destroy(gameObject);
        }
    }
}
