using UnityEngine;

namespace JumpRing.Game.Core.Spawning
{
    public abstract class TimedSpawner : MonoBehaviour, ISpawner
    {
        [SerializeField, Min(0.05f)]
        private float spawnInterval = 0.5f;

        private float elapsedTime;

        public bool IsRunning { get; private set; }

        public void StartSpawning()
        {
            elapsedTime = 0f;
            IsRunning = true;
        }

        public void StopSpawning()
        {
            IsRunning = false;
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            elapsedTime += Time.deltaTime;
            if (elapsedTime < spawnInterval)
            {
                return;
            }

            elapsedTime -= spawnInterval;
            Spawn();
        }

        protected abstract void Spawn();
    }
}
