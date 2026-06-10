using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class PhaseChangedPresenter : CombatPresenterBase
    {
        private const float BannerDuration = 1f;

        public PhaseChangedPresenter(CombatPresentationHost host)
            : base(host)
        {
        }

        public override async UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.PhaseChanged)
            {
                return;
            }

            string message = combatEvent.Phase switch
            {
                BattlePhase.Resolving => "플레이어(전투) 턴 시작",
                BattlePhase.EnemyTurn => "몬스터 턴 시작",
                BattlePhase.PlayerTurn => "플레이어(룰렛) 턴 시작",
                _ => null,
            };

            if (message == null)
            {
                return;
            }

            await Host.Commands.ShowTurnBannerAsync(message, BannerDuration, cancellationToken);
        }
    }
}
