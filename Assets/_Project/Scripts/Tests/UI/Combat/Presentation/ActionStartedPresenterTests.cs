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
        public void PresentAsync_EnemyActionStartedRequestsEnemyActionOnce()
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
                    sourceParticipantId: sourceParticipantId,
                    actionName: "Defend");

                presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                Assert.That(commands.EnemyActionCallCount, Is.EqualTo(1));
                Assert.That(commands.LastEnemyActionParticipantId.Value, Is.EqualTo(sourceParticipantId.Value));
                Assert.That(commands.LastEnemyActionName, Is.EqualTo("Defend"));
                Assert.That(commands.OtherCommandCallCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void PresentAsync_NonActionStartedEventDoesNotRequestEnemyAction()
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

                Assert.That(commands.EnemyActionCallCount, Is.EqualTo(0));
                Assert.That(commands.OtherCommandCallCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        private sealed class RecordingCommands : ICombatPresentationCommands
        {
            public int EnemyActionCallCount { get; private set; }

            public int OtherCommandCallCount { get; private set; }

            public CombatParticipantId LastEnemyActionParticipantId { get; private set; }

            public string LastEnemyActionName { get; private set; }

            public UniTask PlayEnemyActionAsync(
                CombatParticipantId participantId,
                string actionName,
                CancellationToken cancellationToken)
            {
                EnemyActionCallCount++;
                LastEnemyActionParticipantId = participantId;
                LastEnemyActionName = actionName;
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
