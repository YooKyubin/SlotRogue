using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.Combat.Presentation
{
    public static class CombatPresentationTweens
    {
        public static async UniTask TweenIntAsync(
            int from,
            int to,
            float duration,
            Action<int> onValueChanged,
            GameObject linkTarget,
            CancellationToken cancellationToken)
        {
            if (duration <= 0f || from == to)
            {
                onValueChanged(to);
                return;
            }

#if DOTWEEN
            float value = from;
            Tween tween = DOTween.To(
                () => value,
                x =>
                {
                    value = x;
                    onValueChanged(Mathf.RoundToInt(x));
                },
                to,
                duration)
                .SetEase(Ease.OutQuad)
                .SetLink(linkTarget);

            await AwaitTweenAsync(tween, cancellationToken);
#else
            await TweenIntUniTaskAsync(from, to, duration, onValueChanged, linkTarget, cancellationToken);
#endif
        }

        public static UniTask DelayAsync(
            float seconds,
            GameObject linkTarget,
            CancellationToken cancellationToken)
        {
            if (seconds <= 0f)
            {
                return UniTask.CompletedTask;
            }

#if DOTWEEN
            Tween tween = DOTween.To(() => 0f, _ => { }, 1f, seconds).SetLink(linkTarget);
            return AwaitTweenAsync(tween, cancellationToken);
#else
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken);
#endif
        }

#if DOTWEEN
        public static async UniTask AwaitTweenAsync(Tween tween, CancellationToken cancellationToken)
        {
            if (tween == null || !tween.active)
            {
                return;
            }

            UniTaskCompletionSource completionSource = new();
            CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                if (tween.active)
                {
                    tween.Kill();
                }
            });

            tween.onComplete += OnComplete;
            tween.onKill += OnComplete;

            try
            {
                await completionSource.Task;
            }
            finally
            {
                tween.onComplete -= OnComplete;
                tween.onKill -= OnComplete;
                registration.Dispose();
            }

            cancellationToken.ThrowIfCancellationRequested();

            void OnComplete()
            {
                completionSource.TrySetResult();
            }
        }
#endif

        private static async UniTask TweenIntUniTaskAsync(
            int from,
            int to,
            float duration,
            Action<int> onValueChanged,
            GameObject linkTarget,
            CancellationToken cancellationToken)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (linkTarget == null)
                {
                    break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onValueChanged(Mathf.RoundToInt(Mathf.Lerp(from, to, t)));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            onValueChanged(to);
        }
    }
}
