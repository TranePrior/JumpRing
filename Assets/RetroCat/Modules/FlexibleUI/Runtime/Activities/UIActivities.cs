using System;
using System.Collections.Generic;
using System.Linq;
using RetroCat.Modules.Core.UI.Activities.Popups.Core;
using RetroCat.Modules.FlexibleUI.Runtime.Screens;
using RetroCat.Modules.FlexibleUI.Runtime.TransitionCurtain;
using RetroCat.Modules.Tools.Runtime.Source;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RetroCat.Modules.FlexibleUI.Runtime.Activities
{
    public class UIActivities : MonoSingleton<UIActivities>
    {
        [SerializeField] private RectTransform _curtainsParent;
        [SerializeField] private RectTransform _screensParent;
        [SerializeField] private RectTransform _popupsParent;
        [SerializeField] private RectTransform _fallbackParent;
    
        [SerializeField] private List<ActivityBase> _activities;

        private IActivityFactory _activityFactory = new DefaultActivityFactory();

        public void SetActivityFactory(IActivityFactory activityFactory)
        {
            _activityFactory = activityFactory ?? throw new ArgumentNullException($"{nameof(activityFactory)} is null");
        }
        
        public void ShowActivity<T>(Scene executedFromScene, Action<T> activityCreated = null) where T : ActivityBase
        {
            void ActivityCreated(ActivityBase activity)
            {
                activityCreated?.Invoke((T)activity);
            }
            
            ShowActivity(typeof(T), executedFromScene, ActivityCreated);
        }

        public void ShowActivity(string activityTypeName, Scene executedFromScene, Action<ActivityBase> activityCreated = null)
        {
            if (string.IsNullOrWhiteSpace(activityTypeName))
                throw new ArgumentException("Activity type name is null or empty", nameof(activityTypeName));

            Type resolvedType = ResolveActivityType(activityTypeName);
            if (resolvedType == null)
                throw new InvalidOperationException("Failed to show activity by name: " + activityTypeName);

            ShowActivity(resolvedType, executedFromScene, activityCreated);
        }

        public void ShowActivity(Type activityType, Scene executedFromScene, Action<ActivityBase> activityCreated = null)
        {
            if (activityType == null)
                throw new ArgumentNullException(nameof(activityType));

            if (!typeof(ActivityBase).IsAssignableFrom(activityType))
                throw new ArgumentException("Type must inherit from ActivityBase: " + activityType.FullName, nameof(activityType));

            ActivityBase prefab = FindPrefabByType(activityType);
            
            if (prefab == null)
                throw new InvalidOperationException("Failed to show activity of type " + activityType.FullName);

            RectTransform parent = GetParentForType(activityType);
            ActivityBase activity = Instantiate(prefab, parent, executedFromScene);
            activityCreated?.Invoke(activity);
            activity.Open();
        }

        private Type ResolveActivityType(string activityTypeName)
        {
            Type resolved = Type.GetType(activityTypeName);
            if (resolved != null)
                return resolved;

            ActivityBase matched = _activities.FirstOrDefault(a =>
                string.Equals(a.GetType().FullName, activityTypeName, StringComparison.Ordinal) ||
                string.Equals(a.GetType().Name, activityTypeName, StringComparison.Ordinal));

            return matched?.GetType();
        }

        private ActivityBase FindPrefabByType(Type activityType)
        {
            return _activities.FirstOrDefault(a => a.GetType() == activityType);
        }

        private RectTransform GetParentForType(Type activityType)
        {
            if (typeof(PopupBase).IsAssignableFrom(activityType))
                return _popupsParent;
            
            if (typeof(ScreenBase).IsAssignableFrom(activityType)) 
                return _screensParent;
            
            if (typeof(TransitionCurtainBase).IsAssignableFrom(activityType))
                return _curtainsParent;
            
            return _fallbackParent;
        }

        private ActivityBase Instantiate(ActivityBase prefab, RectTransform parent, Scene executedFromScene)
        {
            ActivityBase instance = _activityFactory.CreateActivity(prefab, parent, executedFromScene);
            return instance;
        }
    }
}