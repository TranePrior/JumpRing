using System;
using UnityEngine;

namespace RetroCat.Modules.Tools.Runtime.Source
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static bool IsInitialized => _instance != null;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                    
                    if (_instance == null)
                        throw new InvalidOperationException("No instance of " + typeof(T).FullName + " found in the scene");
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}


