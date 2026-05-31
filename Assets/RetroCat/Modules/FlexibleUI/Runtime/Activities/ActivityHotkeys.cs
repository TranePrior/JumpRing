using System;
using RetroCat.Modules.Attributes;
using UnityEngine;

namespace RetroCat.Modules.FlexibleUI.Runtime.Activities
{
    public class ActivityHotkeys : MonoBehaviour
    {
        [SerializeField] private ActivityHotkey _activityHotkeys;
    
        [Serializable]
        private class ActivityHotkey
        {
            [SerializeField, SubclassSelector(typeof(ActivityBase))] private string _activityType;
            [SerializeField] private KeyCode _keyCode;
        
            public string ActivityType => _activityType;
            public KeyCode KeyCode => _keyCode;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_activityHotkeys.KeyCode))
                UIActivities.Instance.ShowActivity(_activityHotkeys.ActivityType, gameObject.scene);
        }
    }
}