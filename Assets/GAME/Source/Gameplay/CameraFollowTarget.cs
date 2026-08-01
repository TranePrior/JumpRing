using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    /// <summary>
    /// What a revive needs from the camera: a cut to the player's new position, so the smoothing
    /// doesn't sweep across the level after a teleport.
    /// </summary>
    public interface ICameraFollow
    {
        void SnapImmediate();
    }

    public sealed class CameraFollowTarget : MonoBehaviour, ICameraFollow
    {
        [SerializeField]
        private Transform target;

        [SerializeField, Min(0.01f), Tooltip("Smooth time for vertical camera follow")]
        private float verticalSmoothTime = 0.12f;

        [SerializeField, Tooltip("Horizontal offset so the ring appears left of screen center")]
        private float horizontalOffset = 3f;

        private float cameraZ;
        private float yVelocity;

        private void Awake()
        {
            cameraZ = transform.position.z;
        }

        private void LateUpdate()
        {
            var targetPosition = target.position;
            var smoothedY = Mathf.SmoothDamp(transform.position.y, targetPosition.y, ref yVelocity, verticalSmoothTime);
            transform.position = new Vector3(targetPosition.x + horizontalOffset, smoothedY, cameraZ);
        }

        public void SnapImmediate()
        {
            var targetPosition = target.position;
            yVelocity = 0f;
            transform.position = new Vector3(targetPosition.x + horizontalOffset, targetPosition.y, cameraZ);
        }
    }
}
