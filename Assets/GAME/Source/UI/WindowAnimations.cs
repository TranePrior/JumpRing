using DG.Tweening;
using UnityEngine;

namespace JumpRing.Game.UI
{
    public static class WindowAnimations
    {
        private const float OpenDuration = 0.3f;
        private const float CloseDuration = 0.2f;
        private const float FadeDuration = 0.2f;
        private const float StartScale = 0.85f;

        public static Sequence AnimateOpen(CanvasGroup canvasGroup, Transform target)
        {
            canvasGroup.alpha = 0f;
            target.localScale = Vector3.one * StartScale;

            var seq = DOTween.Sequence();
            seq.Join(canvasGroup.DOFade(1f, FadeDuration).SetEase(Ease.OutQuad));
            seq.Join(target.DOScale(1f, OpenDuration).SetEase(Ease.OutBack));
            seq.SetUpdate(true);
            return seq;
        }

        public static Sequence AnimateClose(CanvasGroup canvasGroup, Transform target, GameObject root = null)
        {
            var seq = DOTween.Sequence();
            seq.Join(canvasGroup.DOFade(0f, CloseDuration * 0.75f).SetEase(Ease.InQuad));
            seq.Join(target.DOScale(StartScale, CloseDuration).SetEase(Ease.InBack));
            seq.SetUpdate(true);

            seq.OnComplete(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                if (root != null) root.SetActive(false);
            });

            return seq;
        }

        public static Sequence Heartbeat(Transform target)
        {
            target.localScale = Vector3.one;

            var seq = DOTween.Sequence();
            seq.Append(target.DOScale(1.12f, 0.14f).SetEase(Ease.OutQuad));
            seq.Append(target.DOScale(1f, 0.14f).SetEase(Ease.InQuad));
            seq.Append(target.DOScale(1.08f, 0.12f).SetEase(Ease.OutQuad));
            seq.Append(target.DOScale(1f, 0.12f).SetEase(Ease.InQuad));
            seq.AppendInterval(0.5f);
            seq.SetLoops(-1);
            seq.SetUpdate(true);
            return seq;
        }

        public static Tween FadeIn(CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 0f;
            return canvasGroup.DOFade(1f, FadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public static Tween FadeOut(CanvasGroup canvasGroup, GameObject root = null)
        {
            var tween = canvasGroup.DOFade(0f, FadeDuration).SetEase(Ease.InQuad).SetUpdate(true);

            if (root != null)
            {
                tween.OnComplete(() => root.SetActive(false));
            }

            return tween;
        }
    }
}
