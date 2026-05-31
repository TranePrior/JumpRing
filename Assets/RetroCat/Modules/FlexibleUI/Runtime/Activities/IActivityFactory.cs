using UnityEngine;
using UnityEngine.SceneManagement;

namespace RetroCat.Modules.FlexibleUI.Runtime.Activities
{
    public interface IActivityFactory
    {
        public ActivityBase CreateActivity(ActivityBase prefab, Transform parent, Scene executedFromScene);
    }
}