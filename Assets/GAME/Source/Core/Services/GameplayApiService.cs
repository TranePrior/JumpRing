using System.Runtime.InteropServices;
using JumpRing.Game.Core.State;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class GameplayApiService : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void JumpRing_GameplayApiStart();

        [DllImport("__Internal")]
        private static extern void JumpRing_GameplayApiStop();
#endif

        private bool _stateIsGameplay;
        private bool _hasFocus = true;
        private bool _isActive;

        private void OnDisable()
        {
            // Make sure we never leave the platform thinking gameplay is active
            // when this object is torn down (e.g. scene reload).
            if (_isActive)
            {
                _isActive = false;
                StopGameplay();
            }
        }

        public void OnStateChanged(GameState state)
        {
            _stateIsGameplay = state == GameState.Gameplay;
            UpdateActivity();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
            UpdateActivity();
        }

        private void OnApplicationPause(bool isPaused)
        {
            _hasFocus = !isPaused;
            UpdateActivity();
        }

        private void UpdateActivity()
        {
            bool shouldBeActive = _stateIsGameplay && _hasFocus;
            if (shouldBeActive == _isActive)
            {
                return;
            }

            _isActive = shouldBeActive;

            if (_isActive)
            {
                StartGameplay();
            }
            else
            {
                StopGameplay();
            }
        }

        private void StartGameplay()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JumpRing_GameplayApiStart();
#endif
        }

        private void StopGameplay()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JumpRing_GameplayApiStop();
#endif
        }
    }
}
