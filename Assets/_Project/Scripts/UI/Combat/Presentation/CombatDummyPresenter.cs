using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatDummyPresenter : ICombatEventPresenter
    {
        public UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            viewModel.ApplySnapshot(combatEvent);

            if (combatEvent.Kind == CombatEventKind.EffectApplied)
            {
                Debug.Log(
                    $"[Presentation] Dummy EffectApplied {combatEvent.Effect.Kind} " +
                    $"crit={context.IsCritical} pattern={context.PatternName}");
            }
            else
            {
                Debug.Log($"[Presentation] Dummy {combatEvent.Kind}");
            }

            return UniTask.CompletedTask;
        }
    }
}
