using UnityEngine;
using UnityEngine.SceneManagement;

namespace RetroCat.Modules.FlexibleUI.Runtime.Activities
{
    public class DefaultActivityFactory : IActivityFactory
    {
        public ActivityBase CreateActivity(ActivityBase prefab, Transform parent, Scene executedFromScene)
        {
            return Object.Instantiate(prefab, parent);
        }
    }
}