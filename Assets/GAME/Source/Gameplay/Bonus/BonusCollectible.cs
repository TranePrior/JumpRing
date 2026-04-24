using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class BonusCollectible : MonoBehaviour
    {
        [SerializeField]
        private BonusType bonusType;

        private BonusEffectManager effectManager;
        private RunSessionController runSessionController;
        private bool isCollected;

        public BonusType BonusType => bonusType;

        public void Construct(BonusEffectManager effectManager, RunSessionController runSession, BonusType type)
        {
            this.effectManager = effectManager;
            runSessionController = runSession;
            bonusType = type;
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

            // SecondChance can always be picked up (stacks up to max)
            if (bonusType == BonusType.SecondChance)
            {
                if (effectManager.SecondChanceCount >= effectManager.MaxSecondChances)
                {
                    return;
                }
            }
            else if (effectManager.HasActiveBonus)
            {
                return;
            }

            isCollected = true;
            effectManager.ActivateBonus(bonusType);
            Destroy(gameObject);
        }
    }
}
