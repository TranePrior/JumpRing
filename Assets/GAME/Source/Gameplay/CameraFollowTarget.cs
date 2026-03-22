using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class CameraFollowTarget : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        private float cameraZ;

        private void Awake()
        {
            cameraZ = transform.position.z;
        }

        private void LateUpdate()
        {
            var targetPosition = target.position;
            transform.position = new Vector3(targetPosition.x, targetPosition.y, cameraZ);
        }
    }
}
