using System.Collections.Generic;
using UnityEngine;

namespace JumpRing.Game.Gameplay.Pooling
{
    /// <summary>
    /// One pool per prefab under a shared parent. Spawners deal in prefabs — coin skins swap with
    /// the theme, bonuses come in several kinds — so the pool they need is chosen per spawn.
    /// </summary>
    public sealed class PrefabPoolRegistry
    {
        private readonly Transform parent;
        private readonly int prewarmCount;
        private readonly Dictionary<GameObject, GameObjectPool> pools = new Dictionary<GameObject, GameObjectPool>();

        public PrefabPoolRegistry(Transform parent, int prewarmCount)
        {
            this.parent = parent;
            this.prewarmCount = prewarmCount;
        }

        public PooledInstance Rent(GameObject prefab, Vector3 position)
        {
            if (!pools.TryGetValue(prefab, out var pool))
            {
                pool = new GameObjectPool(prefab, parent, prewarmCount);
                pools.Add(prefab, pool);
            }

            return pool.Rent(position);
        }
    }
}
