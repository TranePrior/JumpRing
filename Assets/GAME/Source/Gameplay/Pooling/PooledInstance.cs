using UnityEngine;

namespace JumpRing.Game.Gameplay.Pooling
{
    /// <summary>
    /// Handle a pooled object uses to hand itself back instead of being destroyed.
    /// </summary>
    public sealed class PooledInstance : MonoBehaviour
    {
        private GameObjectPool owner;

        public void BindTo(GameObjectPool pool)
        {
            owner = pool;
        }

        public void Release()
        {
            owner.Return(this);
        }
    }
}
