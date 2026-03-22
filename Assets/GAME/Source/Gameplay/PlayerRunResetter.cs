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

        [SerializeField, Min(0.0001f)]
        private float alignmentTolerance = 0.001f;

        private void OnEnable()
        {
            runSessionController.RunStarted += OnRunStarted;
        }

        private void OnDisable()
        {
            runSessionController.RunStarted -= OnRunStarted;
        }

        private void OnRunStarted()
        {
            playerRigidbody.position = spawnPoint.position;
            playerRigidbody.rotation = spawnPoint.eulerAngles.z;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;

            linePathGenerator.AlignAndRebuildToPoint(playerRigidbody.position);

            if (!linePathGenerator.IsAlignedWithPoint(playerRigidbody.position, alignmentTolerance))
            {
                runSessionController.OpenMainMenu();
                Debug.LogError("Line start alignment failed. Run was blocked.");
            }
        }
    }
}
