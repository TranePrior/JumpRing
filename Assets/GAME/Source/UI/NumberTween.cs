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
        /// <param name="format">
        /// A TMP format string such as "{0}" or "+{0}". It is passed straight to
        /// <see cref="TMP_Text.SetText(string, float)"/>, which formats the number into TMP's own
        /// buffer — string.Format here allocated a string on every frame of the count-up, on the
        /// Game Over screen where punch, scale and blur animations are already running.
        /// </param>
        public static Tween Play(TMP_Text label, int from, int to, float duration, string format)
        {
            int current = from;
            label.SetText(format, from);

            return DOTween.To(() => current, value =>
                {
                    // The tween ticks every frame but the rounded value only changes a handful of
                    // times. Skipping the repeats keeps TMP from regenerating an identical mesh
                    // ~60 times a second on the frame budget the open animation already eats.
                    if (value == current)
                    {
                        return;
                    }

                    current = value;
                    label.SetText(format, value);
                }, to, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                // Gives the tween a Unity target, so it can be killed via DOTween.Kill(label) and
                // safe mode can tie it to the label's lifetime. Without one it is unreachable.
                .SetTarget(label);
        }
    }
}
