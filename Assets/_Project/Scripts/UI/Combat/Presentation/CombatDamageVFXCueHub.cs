using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// 하나의 Damage VFX 요청 내부에서 animation cue를 구독자에게 전달한다.
    /// </summary>
    public sealed class CombatDamageVFXCueHub : IDisposable
    {
        private event Func<CombatDamageVFXCue, CancellationToken, UniTask> Impact;

        public IDisposable SubscribeImpact(Func<CombatDamageVFXCue, CancellationToken, UniTask> handler)
        {
            Impact += handler;
            return new ImpactSubscription(this, handler);
        }

        public UniTask PublishImpactAsync(Vector3 worldPosition, CancellationToken cancellationToken)
        {
            if (Impact == null)
            {
                return UniTask.CompletedTask;
            }

            Delegate[] handlers = Impact.GetInvocationList();
            var tasks = new List<UniTask>(handlers.Length);
            CombatDamageVFXCue cue = new(worldPosition);
            for (int index = 0; index < handlers.Length; index++)
            {
                var handler = (Func<CombatDamageVFXCue, CancellationToken, UniTask>)handlers[index];
                tasks.Add(handler(cue, cancellationToken));
            }

            return UniTask.WhenAll(tasks);
        }

        public void Dispose()
        {
            Impact = null;
        }

        private void UnsubscribeImpact(Func<CombatDamageVFXCue, CancellationToken, UniTask> handler)
        {
            Impact -= handler;
        }

        private sealed class ImpactSubscription : IDisposable
        {
            private CombatDamageVFXCueHub _hub;
            private Func<CombatDamageVFXCue, CancellationToken, UniTask> _handler;

            public ImpactSubscription(
                CombatDamageVFXCueHub hub,
                Func<CombatDamageVFXCue, CancellationToken, UniTask> handler)
            {
                _hub = hub;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_hub == null)
                {
                    return;
                }

                _hub.UnsubscribeImpact(_handler);
                _hub = null;
                _handler = null;
            }
        }
    }

    public readonly struct CombatDamageVFXCue
    {
        public CombatDamageVFXCue(Vector3 worldPosition)
        {
            WorldPosition = worldPosition;
        }

        public Vector3 WorldPosition { get; }
    }
}
