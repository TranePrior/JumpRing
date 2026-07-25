using DG.Tweening;
using TMPro;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Counts a label from one integer to another. Runs on unscaled time so it keeps
    /// ticking while the game is paused behind a popup.
    /// </summary>
    public static class NumberTween
    {
        public static Tween Play(TMP_Text label, int from, int to, float duration, string format)
        {
            int current = from;
            label.text = string.Format(format, from);

            return DOTween.To(() => current, value =>
                {
                    current = value;
                    label.text = string.Format(format, value);
                }, to, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }
    }
}
