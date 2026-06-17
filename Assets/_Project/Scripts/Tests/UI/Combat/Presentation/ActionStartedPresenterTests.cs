using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;

namespace SlotRogue.UI.Tests.Combat.Presentation
{
    public sealed class ActionStartedPresenterTests
    {
        [Test]
        public void PresentAsync_EnemyActionStartedRequestsEnemyAttackOnce()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new RecordingCommands();
                var presenter = new ActionStartedPresenter(new CombatPresentationHost(hostObject, commands));
                var sourceParticipantId = new CombatParticipantId(101);
                var combatEvent = new CombatEvent(
                    CombatEventKind.ActionStarted,
                    BattlePhase.EnemyTurn,
                    sourceParticipantId: sourceParticipantId);

                presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                Assert.That(commands.EnemyAttackCallCount, Is.EqualTo(1));
                Assert.That(commands.LastEnemyAttackParticipantId.Value, Is.EqualTo(sourceParticipantId.Value));
                Assert.That(commands.OtherCommandCallCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void PresentAsync_NonActionStartedEventDoesNotRequestEnemyAttack()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new RecordingCommands();
                var presenter = new ActionStartedPresenter(new CombatPresentationHost(hostObject, commands));
                var combatEvent = new CombatEvent(
                    CombatEventKind.ActionCompleted,
                    BattlePhase.EnemyTurn,
                    sourceParticipantId: new CombatParticipantId(101));

                presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                Assert.That(commands.EnemyAttackCallCount, Is.EqualTo(0));
                Assert.That(commands.OtherCommandCallCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        private sealed class RecordingCommands : ICombatPresentationCommands
        {
            public int EnemyAttackCallCount { get; private set; }

            public int OtherCommandCallCount { get; private set; }

            public CombatParticipantId LastEnemyAttackParticipantId { get; private set; }

            public UniTask PlayEnemyAttackAsync(
                CombatParticipantId participantId,
                CancellationToken cancellationToken)
            {
                EnemyAttackCallCount++;
                LastEnemyAttackParticipantId = participantId;
                return UniTask.CompletedTask;
            }

            public UniTask ShowFloatingDamageAsync(
                FloatingDamageRequest request,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask ShowShieldGainAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask ShowShieldHitAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask ShowShieldBreakAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask ShowShieldExpireAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask ShowTurnBannerAsync(
                string message,
                float duration,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }
        }
    }
}
