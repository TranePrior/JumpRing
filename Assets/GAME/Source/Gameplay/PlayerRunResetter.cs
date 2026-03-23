using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class PlayerRunResetter : MonoBehaviour
    {
        private static readonly Vector3 OriginPosition = Vector3.zero;

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

        [SerializeField, Min(0.0001f)]
        private float alignmentTolerance = 0.001f;

        [SerializeField, Min(0f)]
        private float lineBoundsPadding = 0.001f;

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
            ResetPlayerToOrigin();
            AlignLineToSpawnPoint();
        }

        private void OnRunStarted()
        {
            ResetPlayerToOrigin();
            AlignLineToSpawnPoint();
        }

        private void ResetPlayerToOrigin()
        {
            playerRigidbody.transform.SetPositionAndRotation(OriginPosition, Quaternion.identity);
            playerRigidbody.position = Vector2.zero;
            playerRigidbody.rotation = 0f;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        private void AlignLineToSpawnPoint()
        {
            var lineAnchorPoint = (Vector2)spawnPoint.position;
            linePathGenerator.AlignAndRebuildToPoint(lineAnchorPoint);

            if (!CanStartRunWithCurrentLine(lineAnchorPoint))
            {
                runSessionController.OpenMainMenu();
                Debug.LogError("Line start validation failed. Run was blocked.");
            }
        }

        private bool CanStartRunWithCurrentLine(Vector2 lineAnchorPoint)
        {
            if (!linePathGenerator.IsAlignedWithPoint(lineAnchorPoint, alignmentTolerance))
            {
                return false;
            }

            var lineYAtRing = linePathGenerator.EvaluateHeightAtX(playerRigidbody.position.x);
            var minAllowedY = Mathf.Min(hitBottom.position.y, hitTop.position.y) + lineBoundsPadding;
            var maxAllowedY = Mathf.Max(hitBottom.position.y, hitTop.position.y) - lineBoundsPadding;
            return lineYAtRing > minAllowedY && lineYAtRing < maxAllowedY;
        }
    }
}
