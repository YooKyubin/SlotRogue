using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public abstract class CombatPresenterBase : ICombatEventPresenter
    {
        protected CombatPresenterBase(CombatPresentationHost host)
        {
            Host = host;
        }

        protected CombatPresentationHost Host { get; }

        public abstract UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken);

        protected void RefreshHud() => Host.RefreshStatusText();

        protected UniTask TweenTargetHpAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            float duration,
            CancellationToken cancellationToken)
        {
            CombatParticipantSnapshot before = combatEvent.TargetBefore;
            CombatParticipantSnapshot after = combatEvent.TargetAfter;
            bool isPlayer = combatEvent.IsPlayerParticipant;

            if (isPlayer)
            {
                viewModel.SetPlayerHp(before.Hp);
            }
            else
            {
                viewModel.SetMonsterHp(before.Hp);
            }

            RefreshHud();

            return CombatPresentationTweens.TweenIntAsync(
                before.Hp,
                after.Hp,
                duration,
                value =>
                {
                    if (isPlayer)
                    {
                        viewModel.SetPlayerHp(value);
                    }
                    else
                    {
                        viewModel.SetMonsterHp(value);
                    }

                    RefreshHud();
                },
                Host.LinkTarget,
                cancellationToken);
        }

        protected UniTask TweenTargetShieldAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            float duration,
            CancellationToken cancellationToken)
        {
            CombatParticipantSnapshot before = combatEvent.TargetBefore;
            CombatParticipantSnapshot after = combatEvent.TargetAfter;
            bool isPlayer = combatEvent.IsPlayerParticipant;

            if (isPlayer)
            {
                viewModel.SetPlayerShield(before.Shield);
            }
            else
            {
                viewModel.SetMonsterShield(before.Shield);
            }

            RefreshHud();

            return CombatPresentationTweens.TweenIntAsync(
                before.Shield,
                after.Shield,
                duration,
                value =>
                {
                    if (isPlayer)
                    {
                        viewModel.SetPlayerShield(value);
                    }
                    else
                    {
                        viewModel.SetMonsterShield(value);
                    }

                    RefreshHud();
                },
                Host.LinkTarget,
                cancellationToken);
        }

        protected static UniTask EffectStubDelayAsync(
            float seconds,
            CombatPresentationHost host,
            CancellationToken cancellationToken) =>
            CombatPresentationTweens.DelayAsync(seconds, host.LinkTarget, cancellationToken);
    }
}
