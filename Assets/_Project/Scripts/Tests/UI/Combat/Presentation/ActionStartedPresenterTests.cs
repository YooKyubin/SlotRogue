using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;

namespace SlotRogue.UI.Tests.Combat.Presentation
{
    public sealed class DamagePresenterTests
    {
        [Test]
        public async Task PresentAsync_DamageWaitsForFloatingDamageAndHealthBar()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new DamageRecordingCommands();
                var statusCommands = new DamageStatusRecordingCommands();
                var presenter = new DamagePresenter(
                    new CombatPresentationHost(hostObject, commands, statusCommands));
                var targetParticipantId = new CombatParticipantId(101);
                var viewModel = new CombatViewModel();
                var combatEvent = new CombatEvent(
                    CombatEventKind.EffectApplied,
                    effect: new CombatEffect(
                        CombatEffectKind.Damage,
                        amount: 4,
                        CombatEffectTarget.Enemy),
                    applyResult: new EffectApplyResult(
                        damageDealt: 4,
                        shieldConsumed: 0,
                        shieldGained: 0,
                        healApplied: 0),
                    isPlayerParticipant: false,
                    targetParticipantId: targetParticipantId,
                    targetAfter: new CombatParticipantSnapshot(hp: 6, shield: 0),
                    appliedStatusModifiers: new[]
                    {
                        new AppliedStatusModifier(
                            targetParticipantId,
                            StatusEffectKind.Vulnerable,
                            CombatTeam.Enemy),
                    });

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        viewModel,
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                await Task.Yield();
                Assert.That(presentTask.IsCompleted, Is.False);

                commands.CompleteFloatingDamage();
                await Task.Yield();
                Assert.That(presentTask.IsCompleted, Is.False);

                commands.CompleteHealthBar();
                await Task.Yield();
                Assert.That(presentTask.IsCompleted, Is.False);

                statusCommands.CompleteActivation();
                await presentTask;

                Assert.That(commands.FloatingDamageCallCount, Is.EqualTo(1));
                Assert.That(commands.HealthBarCallCount, Is.EqualTo(1));
                Assert.That(commands.LastHealthBarParticipantId.Value, Is.EqualTo(targetParticipantId.Value));
                Assert.That(commands.LastHealthBarIsPlayerTarget, Is.False);
                Assert.That(commands.EnemyDeathCallCount, Is.EqualTo(0));
                Assert.That(statusCommands.ActivationCallCount, Is.EqualTo(1));
                Assert.That(statusCommands.LastKind, Is.EqualTo(StatusEffectKind.Vulnerable));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public async Task PresentAsync_PlayerDirectResolvingEnemyDamageRequestsDamageVFX()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new DamageRecordingCommands();
                var presenter = new DamagePresenter(new CombatPresentationHost(hostObject, commands));
                var targetParticipantId = new CombatParticipantId(101);
                var combatEvent = new CombatEvent(
                    CombatEventKind.EffectApplied,
                    BattlePhase.Resolving,
                    effect: new CombatEffect(
                        CombatEffectKind.Damage,
                        amount: 4,
                        CombatEffectTarget.Enemy),
                    applyResult: new EffectApplyResult(
                        damageDealt: 4,
                        shieldConsumed: 0,
                        shieldGained: 0,
                        healApplied: 0),
                    isPlayerParticipant: false,
                    targetParticipantId: targetParticipantId,
                    targetAfter: new CombatParticipantSnapshot(hp: 6, shield: 0));

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                await Task.Yield();
                Assert.That(commands.DamageVFXCallCount, Is.EqualTo(1));
                Assert.That(commands.LastDamageVFXRequest.Profile, Is.EqualTo(CombatDamageVFXProfile.PlayerDirectDamage));
                Assert.That(commands.LastDamageVFXRequest.TargetParticipantId.Value, Is.EqualTo(targetParticipantId.Value));
                Assert.That(commands.LastDamageVFXRequest.DamageAmount, Is.EqualTo(4));

                commands.CompleteFloatingDamage();
                commands.CompleteHealthBar();
                await presentTask;
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public async Task PresentAsync_LethalEnemyDamageWaitsThenPlaysEnemyDeath()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new DamageRecordingCommands();
                var presenter = new DamagePresenter(new CombatPresentationHost(hostObject, commands));
                var targetParticipantId = new CombatParticipantId(101);
                var combatEvent = new CombatEvent(
                    CombatEventKind.EffectApplied,
                    effect: new CombatEffect(
                        CombatEffectKind.Damage,
                        amount: 6,
                        CombatEffectTarget.Enemy),
                    applyResult: new EffectApplyResult(
                        damageDealt: 6,
                        shieldConsumed: 0,
                        shieldGained: 0,
                        healApplied: 0),
                    isPlayerParticipant: false,
                    targetParticipantId: targetParticipantId,
                    targetBefore: new CombatParticipantSnapshot(hp: 6, shield: 0),
                    targetAfter: new CombatParticipantSnapshot(hp: 0, shield: 0));

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                await Task.Yield();
                Assert.That(presentTask.IsCompleted, Is.False);
                Assert.That(commands.EnemyDeathCallCount, Is.EqualTo(0));

                commands.CompleteFloatingDamage();
                commands.CompleteHealthBar();
                await presentTask;

                Assert.That(commands.EnemyDeathCallCount, Is.EqualTo(1));
                Assert.That(commands.LastEnemyDeathParticipantId.Value, Is.EqualTo(targetParticipantId.Value));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public async Task PresentAsync_PlayerLethalDamageDoesNotPlayEnemyDeath()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new DamageRecordingCommands();
                var presenter = new DamagePresenter(new CombatPresentationHost(hostObject, commands));
                var combatEvent = new CombatEvent(
                    CombatEventKind.EffectApplied,
                    effect: new CombatEffect(
                        CombatEffectKind.Damage,
                        amount: 6,
                        CombatEffectTarget.Self),
                    applyResult: new EffectApplyResult(
                        damageDealt: 6,
                        shieldConsumed: 0,
                        shieldGained: 0,
                        healApplied: 0),
                    isPlayerParticipant: true,
                    targetParticipantId: new CombatParticipantId(1),
                    targetBefore: new CombatParticipantSnapshot(hp: 6, shield: 0),
                    targetAfter: new CombatParticipantSnapshot(hp: 0, shield: 0));

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                commands.CompleteFloatingDamage();
                commands.CompleteHealthBar();
                await presentTask;

                Assert.That(commands.EnemyDeathCallCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public async Task PresentAsync_PlayerReflectionDamagePlaysThornsStatusActivation()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new DamageRecordingCommands();
                var statusCommands = new DamageStatusRecordingCommands();
                var presenter = new DamagePresenter(
                    new CombatPresentationHost(hostObject, commands, statusCommands));
                var thornOwnerId = new CombatParticipantId(101);
                var combatEvent = new CombatEvent(
                    CombatEventKind.EffectApplied,
                    effect: new CombatEffect(
                        CombatEffectKind.Damage,
                        amount: 4,
                        CombatEffectTarget.Self,
                        DamageOrigin.Reflection),
                    applyResult: new EffectApplyResult(
                        damageDealt: 4,
                        shieldConsumed: 0,
                        shieldGained: 0,
                        healApplied: 0),
                    isPlayerParticipant: true,
                    targetParticipantId: new CombatParticipantId(1),
                    targetAfter: new CombatParticipantSnapshot(hp: 6, shield: 0),
                    sourceParticipantId: thornOwnerId);

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                await Task.Yield();
                Assert.That(presentTask.IsCompleted, Is.False);

                commands.CompleteFloatingDamage();
                commands.CompleteHealthBar();
                statusCommands.CompleteActivation();
                await presentTask;

                Assert.That(statusCommands.ActivationCallCount, Is.EqualTo(1));
                Assert.That(statusCommands.LastParticipantId.Value, Is.EqualTo(thornOwnerId.Value));
                Assert.That(statusCommands.LastKind, Is.EqualTo(StatusEffectKind.Thorns));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public async Task PresentAsync_ShieldDamageWithoutBreakWaitsForShieldHit()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new DamageRecordingCommands();
                var presenter = new DamagePresenter(new CombatPresentationHost(hostObject, commands));
                var targetParticipantId = new CombatParticipantId(101);
                var combatEvent = new CombatEvent(
                    CombatEventKind.EffectApplied,
                    effect: new CombatEffect(
                        CombatEffectKind.Damage,
                        amount: 3,
                        CombatEffectTarget.Enemy),
                    applyResult: new EffectApplyResult(
                        damageDealt: 0,
                        shieldConsumed: 3,
                        shieldGained: 0,
                        healApplied: 0),
                    isPlayerParticipant: false,
                    targetParticipantId: targetParticipantId,
                    targetBefore: new CombatParticipantSnapshot(hp: 10, shield: 5),
                    targetAfter: new CombatParticipantSnapshot(hp: 10, shield: 2));

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                await Task.Yield();

                Assert.That(commands.ShieldHitCallCount, Is.EqualTo(1));
                Assert.That(commands.ShieldBreakCallCount, Is.EqualTo(0));
                Assert.That(presentTask.IsCompleted, Is.False);

                commands.CompleteShieldHit();
                commands.CompleteFloatingDamage();
                commands.CompleteHealthBar();
                await presentTask;

                Assert.That(commands.DamageVFXCallCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public async Task PresentAsync_ShieldBreakingDamageWaitsForShieldBreakWithoutSeparateHit()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new DamageRecordingCommands();
                var presenter = new DamagePresenter(new CombatPresentationHost(hostObject, commands));
                var targetParticipantId = new CombatParticipantId(101);
                var combatEvent = new CombatEvent(
                    CombatEventKind.EffectApplied,
                    effect: new CombatEffect(
                        CombatEffectKind.Damage,
                        amount: 3,
                        CombatEffectTarget.Enemy),
                    applyResult: new EffectApplyResult(
                        damageDealt: 0,
                        shieldConsumed: 3,
                        shieldGained: 0,
                        healApplied: 0),
                    isPlayerParticipant: false,
                    targetParticipantId: targetParticipantId,
                    targetBefore: new CombatParticipantSnapshot(hp: 10, shield: 3),
                    targetAfter: new CombatParticipantSnapshot(hp: 10, shield: 0));

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                await Task.Yield();

                Assert.That(commands.ShieldHitCallCount, Is.EqualTo(0));
                Assert.That(commands.ShieldBreakCallCount, Is.EqualTo(1));
                Assert.That(presentTask.IsCompleted, Is.False);

                commands.CompleteShieldBreak();
                commands.CompleteFloatingDamage();
                commands.CompleteHealthBar();
                await presentTask;
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        private sealed class DamageStatusRecordingCommands : ICombatStatusPresentationCommands
        {
            private readonly UniTaskCompletionSource _activationCompletion = new();

            public int ActivationCallCount { get; private set; }

            public StatusEffectKind LastKind { get; private set; }

            public CombatParticipantId LastParticipantId { get; private set; }

            public void CompleteActivation()
            {
                _activationCompletion.TrySetResult();
            }

            public UniTask AddEnemyStatusAsync(
                CombatParticipantId participantId,
                StatusEffectViewData status,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask UpdateEnemyStatusValueAsync(
                CombatParticipantId participantId,
                StatusEffectViewData status,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask PlayEnemyStatusActivationAsync(
                CombatParticipantId participantId,
                StatusEffectKind kind,
                CancellationToken cancellationToken)
            {
                ActivationCallCount++;
                LastParticipantId = participantId;
                LastKind = kind;
                return WaitAsync(_activationCompletion, cancellationToken);
            }

            public UniTask PlayEnemyStatusModifierActivationAsync(
                CombatParticipantId ownerParticipantId,
                StatusEffectKind kind,
                CancellationToken cancellationToken)
            {
                ActivationCallCount++;
                LastParticipantId = ownerParticipantId;
                LastKind = kind;
                return WaitAsync(_activationCompletion, cancellationToken);
            }

            public UniTask RemoveEnemyStatusAsync(
                CombatParticipantId participantId,
                StatusEffectKind kind,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            private static async UniTask WaitAsync(
                UniTaskCompletionSource completion,
                CancellationToken cancellationToken)
            {
                using CancellationTokenRegistration registration =
                    cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
                await completion.Task;
            }
        }

        private sealed class DamageRecordingCommands : ICombatPresentationCommands
        {
            private readonly UniTaskCompletionSource _floatingDamageCompletion = new();
            private readonly UniTaskCompletionSource _healthBarCompletion = new();
            private readonly UniTaskCompletionSource _shieldHitCompletion = new();
            private readonly UniTaskCompletionSource _shieldBreakCompletion = new();

            public int FloatingDamageCallCount { get; private set; }

            public int HealthBarCallCount { get; private set; }

            public int ShieldHitCallCount { get; private set; }

            public int ShieldBreakCallCount { get; private set; }

            public int DamageVFXCallCount { get; private set; }

            public CombatDamageVFXRequest LastDamageVFXRequest { get; private set; }

            public CombatParticipantId LastHealthBarParticipantId { get; private set; }

            public bool LastHealthBarIsPlayerTarget { get; private set; }

            public int EnemyDeathCallCount { get; private set; }

            public CombatParticipantId LastEnemyDeathParticipantId { get; private set; }

            public void CompleteFloatingDamage()
            {
                _floatingDamageCompletion.TrySetResult();
            }

            public void CompleteHealthBar()
            {
                _healthBarCompletion.TrySetResult();
            }

            public void CompleteShieldHit()
            {
                _shieldHitCompletion.TrySetResult();
            }

            public void CompleteShieldBreak()
            {
                _shieldBreakCompletion.TrySetResult();
            }

            public UniTask PlayEnemyActionUntilEffectPointAsync(
                CombatParticipantId participantId,
                string actionName,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask WaitEnemyActionCompletedAsync(
                CombatParticipantId participantId,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask PlayEnemyDeathAsync(
                CombatParticipantId participantId,
                CancellationToken cancellationToken)
            {
                EnemyDeathCallCount++;
                LastEnemyDeathParticipantId = participantId;
                return UniTask.CompletedTask;
            }

            public UniTask ShowFloatingCombatTextAsync(
                FloatingCombatTextRequest request,
                CancellationToken cancellationToken)
            {
                FloatingDamageCallCount++;
                return WaitAsync(_floatingDamageCompletion, cancellationToken);
            }

            public UniTask ShowCombatDamageVFXAsync(
                CombatDamageVFXRequest request,
                CancellationToken cancellationToken)
            {
                DamageVFXCallCount++;
                LastDamageVFXRequest = request;
                return UniTask.CompletedTask;
            }

            public UniTask WaitHealthBarAsync(
                CombatParticipantId participantId,
                bool isPlayerTarget,
                CancellationToken cancellationToken)
            {
                HealthBarCallCount++;
                LastHealthBarParticipantId = participantId;
                LastHealthBarIsPlayerTarget = isPlayerTarget;
                return WaitAsync(_healthBarCompletion, cancellationToken);
            }

            public UniTask ShowShieldGainAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ShowShieldHitAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                ShieldHitCallCount++;
                return WaitAsync(_shieldHitCompletion, cancellationToken);
            }

            public UniTask ShowShieldBreakAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                ShieldBreakCallCount++;
                return WaitAsync(_shieldBreakCompletion, cancellationToken);
            }

            public UniTask ShowShieldExpireAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ShowTurnBannerAsync(
                string message,
                float duration,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            private static async UniTask WaitAsync(
                UniTaskCompletionSource completion,
                CancellationToken cancellationToken)
            {
                using CancellationTokenRegistration registration =
                    cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
                await completion.Task;
            }
        }
    }

    public sealed class ActionStartedPresenterTests
    {
        [Test]
        public async Task PresentAsync_EnemyActionStartedWaitsUntilEffectPoint()
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

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                await Task.Yield();
                Assert.That(presentTask.IsCompleted, Is.False);

                commands.CompleteEffectPoint();
                await presentTask;

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
        public void PresentAsync_EnemyActionStartedCancelsWhileWaiting()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new RecordingCommands();
                var presenter = new ActionStartedPresenter(new CombatPresentationHost(hostObject, commands));
                using var cancellationTokenSource = new CancellationTokenSource();
                var combatEvent = new CombatEvent(
                    CombatEventKind.ActionStarted,
                    BattlePhase.EnemyTurn,
                    sourceParticipantId: new CombatParticipantId(101),
                    actionName: "Attack");

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        cancellationTokenSource.Token)
                    .AsTask();
                cancellationTokenSource.Cancel();

                Assert.ThrowsAsync<System.OperationCanceledException>(async () => await presentTask);
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

            private readonly UniTaskCompletionSource _effectPointCompletion = new();

            private readonly UniTaskCompletionSource _actionCompletedCompletion = new();

            public void CompleteEffectPoint()
            {
                _effectPointCompletion.TrySetResult();
            }

            public UniTask PlayEnemyActionUntilEffectPointAsync(
                CombatParticipantId participantId,
                string actionName,
                CancellationToken cancellationToken)
            {
                EnemyActionCallCount++;
                LastEnemyActionParticipantId = participantId;
                LastEnemyActionName = actionName;
                return WaitAsync(_effectPointCompletion, cancellationToken);
            }

            public UniTask WaitEnemyActionCompletedAsync(
                CombatParticipantId participantId,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return WaitAsync(_actionCompletedCompletion, cancellationToken);
            }

            public UniTask PlayEnemyDeathAsync(
                CombatParticipantId participantId,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask ShowFloatingCombatTextAsync(
                FloatingCombatTextRequest request,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask ShowCombatDamageVFXAsync(
                CombatDamageVFXRequest request,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask WaitHealthBarAsync(
                CombatParticipantId participantId,
                bool isPlayerTarget,
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

            private static async UniTask WaitAsync(
                UniTaskCompletionSource completion,
                CancellationToken cancellationToken)
            {
                using CancellationTokenRegistration registration =
                    cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
                await completion.Task;
            }
        }
    }

    public sealed class HealPresenterTests
    {
        [Test]
        public async Task PresentAsync_EnemyLifestealHealPlaysStatusActivation()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new HealRecordingCommands();
                var statusCommands = new HealStatusRecordingCommands();
                var presenter = new HealPresenter(
                    new CombatPresentationHost(hostObject, commands, statusCommands));
                var participantId = new CombatParticipantId(101);
                var combatEvent = new CombatEvent(
                    CombatEventKind.EffectApplied,
                    effect: new CombatEffect(
                        CombatEffectKind.Heal,
                        amount: 3,
                        CombatEffectTarget.Self),
                    applyResult: new EffectApplyResult(
                        damageDealt: 0,
                        shieldConsumed: 0,
                        shieldGained: 0,
                        healApplied: 3),
                    isPlayerParticipant: false,
                    targetParticipantId: participantId,
                    targetAfter: new CombatParticipantSnapshot(hp: 8, shield: 0),
                    statusEffectKind: StatusEffectKind.Lifesteal);

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                await Task.Yield();
                Assert.That(presentTask.IsCompleted, Is.False);

                commands.CompleteHealthBar();
                statusCommands.CompleteActivation();
                await presentTask;

                Assert.That(commands.HealthBarCallCount, Is.EqualTo(1));
                Assert.That(statusCommands.ActivationCallCount, Is.EqualTo(1));
                Assert.That(statusCommands.LastParticipantId.Value, Is.EqualTo(participantId.Value));
                Assert.That(statusCommands.LastKind, Is.EqualTo(StatusEffectKind.Lifesteal));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        private sealed class HealStatusRecordingCommands : ICombatStatusPresentationCommands
        {
            private readonly UniTaskCompletionSource _activationCompletion = new();

            public int ActivationCallCount { get; private set; }

            public StatusEffectKind LastKind { get; private set; }

            public CombatParticipantId LastParticipantId { get; private set; }

            public void CompleteActivation()
            {
                _activationCompletion.TrySetResult();
            }

            public UniTask AddEnemyStatusAsync(
                CombatParticipantId participantId,
                StatusEffectViewData status,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask UpdateEnemyStatusValueAsync(
                CombatParticipantId participantId,
                StatusEffectViewData status,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask PlayEnemyStatusActivationAsync(
                CombatParticipantId participantId,
                StatusEffectKind kind,
                CancellationToken cancellationToken)
            {
                ActivationCallCount++;
                LastParticipantId = participantId;
                LastKind = kind;
                return WaitAsync(_activationCompletion, cancellationToken);
            }

            public UniTask PlayEnemyStatusModifierActivationAsync(
                CombatParticipantId ownerParticipantId,
                StatusEffectKind kind,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask RemoveEnemyStatusAsync(
                CombatParticipantId participantId,
                StatusEffectKind kind,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            private static async UniTask WaitAsync(
                UniTaskCompletionSource completion,
                CancellationToken cancellationToken)
            {
                using CancellationTokenRegistration registration =
                    cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
                await completion.Task;
            }
        }

        private sealed class HealRecordingCommands : ICombatPresentationCommands
        {
            private readonly UniTaskCompletionSource _healthBarCompletion = new();

            public int HealthBarCallCount { get; private set; }

            public void CompleteHealthBar()
            {
                _healthBarCompletion.TrySetResult();
            }

            public UniTask PlayEnemyActionUntilEffectPointAsync(
                CombatParticipantId participantId,
                string actionName,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask WaitEnemyActionCompletedAsync(
                CombatParticipantId participantId,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask PlayEnemyDeathAsync(
                CombatParticipantId participantId,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ShowFloatingCombatTextAsync(
                FloatingCombatTextRequest request,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ShowCombatDamageVFXAsync(
                CombatDamageVFXRequest request,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask WaitHealthBarAsync(
                CombatParticipantId participantId,
                bool isPlayerTarget,
                CancellationToken cancellationToken)
            {
                HealthBarCallCount++;
                return WaitAsync(_healthBarCompletion, cancellationToken);
            }

            public UniTask ShowShieldGainAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ShowShieldHitAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ShowShieldBreakAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ShowShieldExpireAsync(
                ShieldPresentationRequest request,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ShowTurnBannerAsync(
                string message,
                float duration,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            private static async UniTask WaitAsync(
                UniTaskCompletionSource completion,
                CancellationToken cancellationToken)
            {
                using CancellationTokenRegistration registration =
                    cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
                await completion.Task;
            }
        }
    }

    public sealed class ActionCompletedPresenterTests
    {
        [Test]
        public async Task PresentAsync_EnemyActionCompletedWaitsUntilAnimationCompleted()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new ActionCompletedRecordingCommands();
                var presenter = new ActionCompletedPresenter(new CombatPresentationHost(hostObject, commands));
                var sourceParticipantId = new CombatParticipantId(101);
                var combatEvent = new CombatEvent(
                    CombatEventKind.ActionCompleted,
                    BattlePhase.EnemyTurn,
                    sourceParticipantId: sourceParticipantId,
                    actionName: "Attack");

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .AsTask();

                await Task.Yield();
                Assert.That(presentTask.IsCompleted, Is.False);

                commands.CompleteAction();
                await presentTask;

                Assert.That(commands.ActionCompletedCallCount, Is.EqualTo(1));
                Assert.That(commands.LastCompletedParticipantId.Value, Is.EqualTo(sourceParticipantId.Value));
                Assert.That(commands.OtherCommandCallCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void PresentAsync_EnemyActionCompletedCancelsWhileWaiting()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new ActionCompletedRecordingCommands();
                var presenter = new ActionCompletedPresenter(new CombatPresentationHost(hostObject, commands));
                using var cancellationTokenSource = new CancellationTokenSource();
                var combatEvent = new CombatEvent(
                    CombatEventKind.ActionCompleted,
                    BattlePhase.EnemyTurn,
                    sourceParticipantId: new CombatParticipantId(101));

                Task presentTask = presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        cancellationTokenSource.Token)
                    .AsTask();
                cancellationTokenSource.Cancel();

                Assert.ThrowsAsync<System.OperationCanceledException>(async () => await presentTask);
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void PresentAsync_NonActionCompletedEventDoesNotRequestCompletionWait()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new ActionCompletedRecordingCommands();
                var presenter = new ActionCompletedPresenter(new CombatPresentationHost(hostObject, commands));
                var combatEvent = new CombatEvent(
                    CombatEventKind.ActionStarted,
                    BattlePhase.EnemyTurn,
                    sourceParticipantId: new CombatParticipantId(101));

                presenter.PresentAsync(
                        combatEvent,
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                Assert.That(commands.ActionCompletedCallCount, Is.EqualTo(0));
                Assert.That(commands.OtherCommandCallCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        private sealed class ActionCompletedRecordingCommands : ICombatPresentationCommands
        {
            private readonly UniTaskCompletionSource _effectPointCompletion = new();
            private readonly UniTaskCompletionSource _actionCompletedCompletion = new();

            public int ActionCompletedCallCount { get; private set; }

            public int OtherCommandCallCount { get; private set; }

            public CombatParticipantId LastCompletedParticipantId { get; private set; }

            public void CompleteAction()
            {
                _actionCompletedCompletion.TrySetResult();
            }

            public UniTask PlayEnemyActionUntilEffectPointAsync(
                CombatParticipantId participantId,
                string actionName,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return WaitAsync(_effectPointCompletion, cancellationToken);
            }

            public UniTask WaitEnemyActionCompletedAsync(
                CombatParticipantId participantId,
                CancellationToken cancellationToken)
            {
                ActionCompletedCallCount++;
                LastCompletedParticipantId = participantId;
                return WaitAsync(_actionCompletedCompletion, cancellationToken);
            }

            public UniTask PlayEnemyDeathAsync(
                CombatParticipantId participantId,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask ShowFloatingCombatTextAsync(
                FloatingCombatTextRequest request,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask ShowCombatDamageVFXAsync(
                CombatDamageVFXRequest request,
                CancellationToken cancellationToken)
            {
                OtherCommandCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask WaitHealthBarAsync(
                CombatParticipantId participantId,
                bool isPlayerTarget,
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

            private static async UniTask WaitAsync(
                UniTaskCompletionSource completion,
                CancellationToken cancellationToken)
            {
                using CancellationTokenRegistration registration =
                    cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
                await completion.Task;
            }
        }
    }

    public sealed class StatusEffectPresenterTests
    {
        [Test]
        public void PresentAsync_AppliedChangedAndExpired_UpdateStateAndCommandsInOrder()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new StatusRecordingCommands();
                var host = new CombatPresentationHost(
                    hostObject,
                    NullCombatPresentationCommands.Instance,
                    commands);
                var presenter = new StatusEffectPresenter(host);
                var viewModel = new CombatViewModel();
                var participantId = new CombatParticipantId(101);
                var context = new PresentationContext(isCritical: false, patternName: string.Empty);

                presenter.PresentAsync(
                        StatusEvent(
                            CombatEventKind.StatusApplied,
                            participantId,
                            StatusEffectKind.Infection,
                            stackCount: 3),
                        viewModel,
                        context,
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                presenter.PresentAsync(
                        StatusEvent(
                            CombatEventKind.StatusValueChanged,
                            participantId,
                            StatusEffectKind.Infection,
                            stackCount: 2),
                        viewModel,
                        context,
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                presenter.PresentAsync(
                        StatusEvent(
                            CombatEventKind.StatusExpired,
                            participantId,
                            StatusEffectKind.Infection,
                            stackCount: 0),
                        viewModel,
                        context,
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                Assert.That(commands.Calls, Is.EqualTo(new[] { "Add:3", "Update:2", "Remove" }));
                Assert.That(viewModel.GetStatuses(participantId), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void PresentAsync_LifestealValueChanged_UpdatesWithoutActivation()
        {
            var hostObject = new GameObject("Presentation Host");
            try
            {
                var commands = new StatusRecordingCommands();
                var host = new CombatPresentationHost(
                    hostObject,
                    NullCombatPresentationCommands.Instance,
                    commands);
                var presenter = new StatusEffectPresenter(host);
                var participantId = new CombatParticipantId(101);

                presenter.PresentAsync(
                        StatusEvent(
                            CombatEventKind.StatusValueChanged,
                            participantId,
                            StatusEffectKind.Lifesteal,
                            stackCount: 1),
                        new CombatViewModel(),
                        new PresentationContext(isCritical: false, patternName: string.Empty),
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                Assert.That(commands.Calls, Is.EqualTo(new[] { "Update:1" }));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        private static CombatEvent StatusEvent(
            CombatEventKind kind,
            CombatParticipantId participantId,
            StatusEffectKind statusEffectKind,
            int stackCount)
        {
            return new CombatEvent(
                kind,
                isPlayerParticipant: false,
                targetParticipantId: participantId,
                statusEffectKind: statusEffectKind,
                statusStackCount: stackCount);
        }

        private sealed class StatusRecordingCommands : ICombatStatusPresentationCommands
        {
            public System.Collections.Generic.List<string> Calls { get; } = new();

            public UniTask AddEnemyStatusAsync(
                CombatParticipantId participantId,
                StatusEffectViewData status,
                CancellationToken cancellationToken)
            {
                Calls.Add($"Add:{status.DisplayValue}");
                return UniTask.CompletedTask;
            }

            public UniTask UpdateEnemyStatusValueAsync(
                CombatParticipantId participantId,
                StatusEffectViewData status,
                CancellationToken cancellationToken)
            {
                Calls.Add($"Update:{status.DisplayValue}");
                return UniTask.CompletedTask;
            }

            public UniTask PlayEnemyStatusActivationAsync(
                CombatParticipantId participantId,
                StatusEffectKind kind,
                CancellationToken cancellationToken)
            {
                Calls.Add("Activate");
                return UniTask.CompletedTask;
            }

            public UniTask PlayEnemyStatusModifierActivationAsync(
                CombatParticipantId ownerParticipantId,
                StatusEffectKind kind,
                CancellationToken cancellationToken)
            {
                Calls.Add("ModifierActivate");
                return UniTask.CompletedTask;
            }

            public UniTask RemoveEnemyStatusAsync(
                CombatParticipantId participantId,
                StatusEffectKind kind,
                CancellationToken cancellationToken)
            {
                Calls.Add("Remove");
                return UniTask.CompletedTask;
            }
        }
    }
}
