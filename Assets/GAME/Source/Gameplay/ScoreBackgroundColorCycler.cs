using JumpRing.Game.Core.Services;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class ScoreBackgroundColorCycler : MonoBehaviour
    {
        [SerializeField]
        private Camera gameplayCamera;

        [SerializeField]
        private MonoBehaviour scoreServiceComponent;

        [SerializeField, Min(1)]
        private int clicksPerColorStep = 25;

        [SerializeField, Min(0.01f)]
        private float transitionDuration = 0.4f;

        [SerializeField]
        private Color baseBackgroundColor = new(0.939f, 0.875f, 0.845f, 1f);

        private int appliedColorStep;
        private Color transitionStartColor;
        private Color transitionEndColor;
        private float transitionTime;
        private bool isTransitionRunning;

        private IScoreService ScoreService => (IScoreService)scoreServiceComponent;

        private void OnEnable()
        {
            ScoreService.ScoreChanged += OnScoreChanged;
        }

        private void OnDisable()
        {
            ScoreService.ScoreChanged -= OnScoreChanged;
        }

        private void Start()
        {
            gameplayCamera.backgroundColor = baseBackgroundColor;
            OnScoreChanged(ScoreService.CurrentScore);
        }

        private void Update()
        {
            if (!isTransitionRunning)
            {
                return;
            }

            transitionTime += Time.deltaTime;
            var progress = Mathf.Clamp01(transitionTime / transitionDuration);
            gameplayCamera.backgroundColor = Color.Lerp(transitionStartColor, transitionEndColor, progress);

            if (progress >= 1f)
            {
                isTransitionRunning = false;
            }
        }

        private void OnScoreChanged(int score)
        {
            if (score <= 0)
            {
                appliedColorStep = 0;
                StartTransition(baseBackgroundColor);
                return;
            }

            var colorStep = score / clicksPerColorStep;
            if (colorStep <= appliedColorStep)
            {
                return;
            }

            appliedColorStep = colorStep;
            StartTransition(GenerateMutedColor());
        }

        private void StartTransition(Color targetColor)
        {
            transitionStartColor = gameplayCamera.backgroundColor;
            transitionEndColor = targetColor;
            transitionTime = 0f;
            isTransitionRunning = true;
        }

        private static Color GenerateMutedColor()
        {
            return Random.ColorHSV(
                0.02f,
                0.12f,
                0.08f,
                0.25f,
                0.75f,
                0.95f,
                1f,
                1f);
        }
    }
}
