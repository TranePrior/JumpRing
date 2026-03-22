using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class ObstacleCollisionHandler : MonoBehaviour
    {
        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private LayerMask obstacleLayers;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other.gameObject);
        }

        private void HandleCollision(GameObject target)
        {
            if (!runSessionController.CanControlPlayer)
            {
                return;
            }

            if (!IsObstacle(target.layer))
            {
                return;
            }

            runSessionController.FinishRun();
        }

        private bool IsObstacle(int layer)
        {
            return (obstacleLayers.value & (1 << layer)) != 0;
        }
    }
}
