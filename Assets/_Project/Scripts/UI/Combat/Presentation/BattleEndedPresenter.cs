using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class BattleEndedPresenter : CombatPresenterBase
    {
        private const float PauseDuration = 0.3f;

        public BattleEndedPresenter(CombatPresentationHost host)
            : base(host)
        {
        }

        public override async UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.BattleEnded)
            {
                return;
            }

            Debug.Log($"[Presentation] Battle ended: {combatEvent.EndReason}");
            await CombatPresentationTweens.DelayAsync(PauseDuration, Host.LinkTarget, cancellationToken);
        }
    }
}
