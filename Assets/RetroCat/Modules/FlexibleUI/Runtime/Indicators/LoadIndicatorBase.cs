using UnityEngine;

namespace RetroCat.Modules.FlexibleUI.Runtime.Indicators
{
    public abstract class LoadIndicatorBase : MonoBehaviour
    {
        public abstract void StartLoading();
        public abstract void SetProgress(float progress);
        public abstract void StopLoading();
    }
}