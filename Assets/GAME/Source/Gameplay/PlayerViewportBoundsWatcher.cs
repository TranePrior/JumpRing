using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class PlayerViewportBoundsWatcher : MonoBehaviour
    {
        [SerializeField]
        private Camera gameplayCamera;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField, Min(0f)]
        private float viewportPadding = 0.05f;

        private void Update()
        {
            if (!runSessionController.CanControlPlayer)
            {
                return;
            }

            var viewportPosition = gameplayCamera.WorldToViewportPoint(transform.position);
            if (viewportPosition.z < 0f)
            {
                runSessionController.FinishRun();
                return;
            }

            var min = -viewportPadding;
            var max = 1f + viewportPadding;
            if (viewportPosition.x < min || viewportPosition.x > max || viewportPosition.y < min || viewportPosition.y > max)
            {
                runSessionController.FinishRun();
            }
        }
    }
}
