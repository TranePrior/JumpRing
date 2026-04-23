using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class CameraFollowTarget : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField, Min(0.01f), Tooltip("Smooth time for vertical camera follow")]
        private float verticalSmoothTime = 0.12f;

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
            transform.position = new Vector3(targetPosition.x, smoothedY, cameraZ);
        }
    }
}
