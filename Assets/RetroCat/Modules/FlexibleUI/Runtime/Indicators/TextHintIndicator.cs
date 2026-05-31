using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace RetroCat.Modules.Core.UI.Controls.Indicators
{
    public class TextHintIndicator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private TMP_Text _hintLabel;

        [Header("Hints")]
        [SerializeField] private string[] _hints;

        [Header("Animation")]
        [SerializeField, Min(0.1f)] private float _switchIntervalSeconds = 3f;
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.25f;

        private Coroutine _routine;
        private int _current;

        private void OnEnable()
        {
            if (_hints == null || _hints.Length == 0 || _hintLabel == null)
                return;

            _routine = StartCoroutine(RunHintsLoop());
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _hintLabel.DOKill();
        }

        private IEnumerator RunHintsLoop()
        {
            _current = 0;

            while (true)
            {
                string hint = _hints[_current];
                if (!string.IsNullOrEmpty(hint))
                {
                    yield return _hintLabel.DOFade(0f, _fadeDuration).WaitForCompletion();
                    _hintLabel.text = hint;
                    yield return _hintLabel.DOFade(1f, _fadeDuration).WaitForCompletion();
                }

                yield return new WaitForSeconds(_switchIntervalSeconds);

                _current = (_current + 1) % _hints.Length;
            }
        }
    }
}
