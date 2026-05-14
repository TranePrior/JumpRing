using UnityEngine;

namespace JumpRing.Visual
{
    public sealed class SpinEffect : MonoBehaviour
    {
        [SerializeField] private float _degreesPerSecond = 90f;

        private void Update()
        {
            transform.Rotate(0f, 0f, _degreesPerSecond * Time.deltaTime);
        }
    }
}