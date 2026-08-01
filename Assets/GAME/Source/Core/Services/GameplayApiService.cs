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

        // After an ad closes the browser emits a trailing focus/blur burst around the WebGL canvas.
        // Ignoring focus events for a short window after the ad keeps that burst from leaving the
        // platform convinced the player never came back. Mirrors WebGLFocusHandler.
        private const float AdSettleSeconds = 0.5f;

        private bool _stateIsGameplay;
        private bool _hasFocus = true;
        private bool _isActive;
        private bool _adActive;
        private bool _popupActive;
        private float _ignoreFocusUntil;

        private void OnEnable()
        {
            PauseService.ReasonsChanged += OnPauseReasonsChanged;
        }

        private void OnDisable()
        {
            PauseService.ReasonsChanged -= OnPauseReasonsChanged;

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
            HandleFocus(hasFocus);
        }

        private void OnApplicationPause(bool isPaused)
        {
            HandleFocus(!isPaused);
        }

        private void HandleFocus(bool hasFocus)
        {
            // An ad already forces the gameplay flag off, and the focus noise it produces is
            // meaningless — letting it through would strand _hasFocus at false and keep the
            // platform showing its promo blocks over a running game.
            if (_adActive || Time.realtimeSinceStartup < _ignoreFocusUntil)
            {
                return;
            }

            _hasFocus = hasFocus;
            UpdateActivity();
        }

        private void OnPauseReasonsChanged()
        {
            bool adActive = PauseService.HasReason(PauseReason.Ad);
            bool popupActive = PauseService.HasReason(PauseReason.Popup);

            if (adActive == _adActive && popupActive == _popupActive)
            {
                return;
            }

            if (adActive != _adActive && _adActive)
            {
                _ignoreFocusUntil = Time.realtimeSinceStartup + AdSettleSeconds;
            }

            _adActive = adActive;
            _popupActive = popupActive;
            UpdateActivity();
        }

        private void UpdateActivity()
        {
            // The platform must never count ad time as gameplay: Yandex expects GameplayAPI.stop()
            // for the whole duration of an ad, whatever state the game was in when it started. The
            // same holds for a modal window opened over a run — the game is frozen behind it.
            bool shouldBeActive = _stateIsGameplay && _hasFocus && !_adActive && !_popupActive;
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
