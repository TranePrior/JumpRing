using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.FlexibleUI.Runtime.Activities
{
    public class ActivityCloseButton : MonoBehaviour
    {
        private ActivityBase _activity;
        [SerializeField] private Button _button;

#if UNITY_EDITOR
        private void Reset()
        {
            _button = GetComponent<Button>();
        }
#endif

        private void Awake()
        {
            _activity = GetComponentInParent<ActivityBase>();

            if (_activity == null)
            {
                Debug.Log("Not found activity for close button");
            }
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(_activity.Close);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(_activity.Close);
        }
        
        private void OnClick()
        {
            _activity.Close();
        }
    }
}
