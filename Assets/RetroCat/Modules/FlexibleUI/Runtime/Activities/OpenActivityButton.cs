using RetroCat.Modules.Attributes;
using RetroCat.Modules.FlexibleUI.Runtime.Activities;
using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.Core.UI.Activities.Core
{
    public class OpenActivityButton : MonoBehaviour
    {
        [SerializeField, SubclassSelector(typeof(ActivityBase))] private string _activityType;
        [SerializeField] private Button _button;

        
#if UNITY_EDITOR
        private void Reset()
        {
            _button = GetComponent<Button>();
        }
#endif

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);        
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            UIActivities.Instance.ShowActivity(_activityType, gameObject.scene);
        }
    }
}