using System.Collections.Generic;
using UnityEngine;

namespace JumpRing.Game.Gameplay.Pooling
{
    /// <summary>
    /// Reuses instances of a single prefab. A run spawns coins and bonuses without pause, and on
    /// WebGL every discarded instance is garbage the player eventually feels as a collection hitch.
    /// </summary>
    public sealed class GameObjectPool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Stack<PooledInstance> idle = new Stack<PooledInstance>();

        public GameObjectPool(GameObject prefab, Transform parent, int prewarmCount)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (var i = 0; i < prewarmCount; i++)
            {
                var instance = Create();
                instance.gameObject.SetActive(false);
                idle.Push(instance);
            }
        }

        public PooledInstance Rent(Vector3 position)
        {
            var instance = idle.Count > 0 ? idle.Pop() : Create();
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Return(PooledInstance instance)
        {
            instance.gameObject.SetActive(false);
            idle.Push(instance);
        }

        private PooledInstance Create()
        {
            var instance = Object.Instantiate(prefab, parent).GetComponent<PooledInstance>();
            instance.BindTo(this);
            return instance;
        }
    }
}
