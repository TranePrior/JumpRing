using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class PlayerRunResetter : MonoBehaviour
    {
        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private Rigidbody2D playerRigidbody;

        [SerializeField]
        private Transform spawnPoint;

        [SerializeField]
        private LinePathGenerator linePathGenerator;

        [SerializeField]
        private Transform hitTop;

        [SerializeField]
        private Transform hitBottom;


        private void OnEnable()
        {
            runSessionController.RunStarted += OnRunStarted;
        }

        private void OnDisable()
        {
            runSessionController.RunStarted -= OnRunStarted;
        }

        private void Start()
        {
            SnapPlayerToLine();
        }

        private void OnRunStarted()
        {
            SnapPlayerToLine();
        }

        private void SnapPlayerToLine()
        {
            var px = spawnPoint.position.x;
            var lineY = linePathGenerator.EvaluateHeightAtX(px);
            var holeCenterLocalY = (hitTop.localPosition.y + hitBottom.localPosition.y) * 0.5f;
            var targetY = lineY - holeCenterLocalY;

            playerRigidbody.transform.SetPositionAndRotation(
                new Vector3(px, targetY, 0f), Quaternion.identity);
            playerRigidbody.position = new Vector2(px, targetY);
            playerRigidbody.rotation = 0f;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
    }
}
