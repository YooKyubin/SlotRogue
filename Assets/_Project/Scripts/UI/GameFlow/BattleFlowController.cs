using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.Slot.Data;
using SlotRogue.UI.Combat;
using SlotRogue.UI.Combat.Presentation;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// Coordinates one battle from slot spin through combat event replay.
    /// Screen state, target input, scene assembly, and run-session mutation live outside this type.
    /// </summary>
    public sealed class BattleFlowController : IDisposable
    {
        private readonly BattleSystem _battle;
        private readonly SlotCombatRequestToCombatEffectsConverter _converter;
        private readonly RelicTurnResolver _relicTurnResolver;
        private readonly CombatTurnRequestBuilder _combatTurnRequestBuilder;
        private readonly SlotTurnController _slotTurnController;
        private readonly BattlePresentationController _battlePresentationController;
        private readonly CombatViewModel _combatViewModel;
        private readonly BattleScreenController _screenController;
        private readonly CancellationToken _presentationCancellationToken;
        private readonly RelicContributionAccumulator _relicContributions = new();
        private readonly SlotSymbolContributionAccumulator _slotSymbolContributions = new();

        private BattleFlowContext _context;
        private bool _battleCompleted;
        private bool _turnRunning;
        private bool _spinInputBlocked;

        internal BattleFlowController(
            BattleSystem battle,
            SlotCombatRequestToCombatEffectsConverter converter,
            RelicTurnResolver relicTurnResolver,
            CombatTurnRequestBuilder combatTurnRequestBuilder,
            SlotTurnController slotTurnController,
            BattlePresentationController battlePresentationController,
            CombatViewModel combatViewModel,
            BattleScreenController screenController,
            CancellationToken presentationCancellationToken)
        {
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _relicTurnResolver = relicTurnResolver ?? throw new ArgumentNullException(nameof(relicTurnResolver));
            _combatTurnRequestBuilder = combatTurnRequestBuilder ?? throw new ArgumentNullException(nameof(combatTurnRequestBuilder));
            _slotTurnController = slotTurnController ?? throw new ArgumentNullException(nameof(slotTurnController));
            _battlePresentationController = battlePresentationController ?? throw new ArgumentNullException(nameof(battlePresentationController));
            _combatViewModel = combatViewModel ?? throw new ArgumentNullException(nameof(combatViewModel));
            _screenController = screenController ?? throw new ArgumentNullException(nameof(screenController));
            _presentationCancellationToken = presentationCancellationToken;
            _screenController.SpinRequested += HandleSpinRequested;
        }

        public event Action<BattleFlowResult> BattleCompleted;

        internal event Action<BattleTutorialSignal> TutorialSignalRaised;

        public void BeginBattle(BattleFlowContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _battleCompleted = false;
            _turnRunning = false;
            _spinInputBlocked = false;
            _relicContributions.Clear();
            _slotSymbolContributions.Clear();

            _slotTurnController.SetupImmediate();
            _battle.StartBattle(_context.Player, ExtractEnemyCombatants(_context.EncounterRoster));
            _combatViewModel.SyncFrom(_battle);
            _screenController.BeginBattle(
                _battle,
                _combatViewModel,
                _context.EncounterRoster,
                _context.EncounterTitle,
                _context.RunDamageBonus,
                _context.RunDefenseBonus,
                () => _battlePresentationController.IsBusy,
                () => _turnRunning);
            TutorialSignalRaised?.Invoke(BattleTutorialSignal.BattleStarted);
        }

        public void DevApplyRelicStatusTurn(
            StatusEffectKind statusEffectKind,
            int amount,
            CombatTargetMode targetMode)
        {
            DevApplyRelicStatusTurnAsync(
                statusEffectKind,
                amount,
                targetMode).Forget();
        }

        public void Dispose()
        {
            _screenController.SpinRequested -= HandleSpinRequested;
            _screenController.Dispose();
        }

        public bool TryRevivePlayer(int currentHp)
        {
            if (!_battleCompleted ||
                _turnRunning ||
                !_battle.TryRevivePlayer(currentHp))
            {
                return false;
            }

            _battleCompleted = false;
            _relicContributions.Clear();
            _slotSymbolContributions.Clear();
            _combatViewModel.SyncFrom(_battle);
            _slotTurnController.SetupImmediate();
            _screenController.ResumeBattle();
            return true;
        }

        internal void SetSpinInputBlocked(bool blocked)
        {
            _spinInputBlocked = blocked;
            _screenController.SetSpinInputBlocked(blocked);
        }

        internal void SetTargetSelectionBlocked(bool blocked)
        {
            _screenController.SetTargetSelectionBlocked(blocked);
        }

        private void HandleSpinRequested()
        {
            RunSpinTurnAsync().Forget();
        }

        private async UniTaskVoid RunSpinTurnAsync()
        {
            if (!CanStartTurn())
            {
                _screenController.Refresh();
                return;
            }

            _screenController.SetSpinInteractable(false);
            _turnRunning = true;

            try
            {
                SlotTurnResult slotTurnResult = await _slotTurnController.SpinAsync(
                    new SlotTurnRequest(_presentationCancellationToken));
                CombatParticipantId selectedTargetId = _screenController.SelectedEnemyId;
                RelicResolveResult relicResult = _relicTurnResolver.Resolve(
                    _context.OwnedRelics,
                    slotTurnResult.PatternMatches,
                    RelicTurnContext.FromBattle(_battle, selectedTargetId));
                RunCombatRequestResult requestResult = _combatTurnRequestBuilder.Build(
                    slotTurnResult.BaseCombatRequest,
                    relicResult,
                    _context.RunDamageBonus,
                    _context.RunDefenseBonus);
                _relicContributions.RecordTurn(
                    relicResult.Contributions,
                    requestResult.FinalRequest.AttackCount);
                // 흡혈/방어전환 회복은 최종 피해·방어 확정 후 계산되므로 별도로 집계한다.
                // 회복 델타라 공격 횟수 배수와 무관하다(공격력 기여는 0).
                _relicContributions.RecordTurn(
                    requestResult.DerivedHealContributions,
                    attackCount: 1);
                _slotSymbolContributions.RecordTurn(
                    slotTurnResult.PatternMatches,
                    requestResult.BaseRequest,
                    relicResult.Contributions,
                    requestResult.FinalRequest.AttackCount);

                _screenController.UpdateTurnResult(slotTurnResult, requestResult);

                SlotCombatRequest request = requestResult.FinalRequest;
                CombatEffect[] playerEffects = _converter.Convert(
                    request,
                    selectedTargetId,
                    requestResult.StatusEffectsToApply);
                var presentationContext =
                    new PresentationContext(request.IsCritical, request.PatternName);
                int eventCursor = _battle.Events.Count;

                await _slotTurnController.PlayPresentationAsync(
                    slotTurnResult,
                    relicResult,
                    requestResult,
                    _presentationCancellationToken);
                TutorialSignalRaised?.Invoke(BattleTutorialSignal.SlotPresentationCompleted);

                BattleApplyResult result = _battle.ApplyPlayerTurn(playerEffects, selectedTargetId);
                if (result.Accepted)
                {
                    await _battlePresentationController.PresentEventsAsync(
                        _battle,
                        eventCursor,
                        presentationContext,
                        _presentationCancellationToken,
                        BeforeBattleEventPresentedAsync,
                        AfterBattleEventPresentedAsync);
                }
            }
            finally
            {
                _slotTurnController.ResetImmediate();
                _turnRunning = false;
                CompleteBattleIfNeeded();
                _screenController.Refresh();
            }
        }

        private async UniTaskVoid DevApplyRelicStatusTurnAsync(
            StatusEffectKind statusEffectKind,
            int amount,
            CombatTargetMode targetMode)
        {
            if (statusEffectKind == StatusEffectKind.None || !CanStartTurn())
            {
                _screenController.Refresh();
                return;
            }

            CombatParticipantId selectedTargetId = _screenController.SelectedEnemyId;
            if (targetMode == CombatTargetMode.SelectedEnemy && !selectedTargetId.IsValid)
            {
                return;
            }

            _screenController.SetSpinInteractable(false);
            _turnRunning = true;

            try
            {
                var request = new SlotCombatRequest(
                    damage: 0,
                    defense: 0,
                    attackCount: 1,
                    healAmount: 0,
                    isCritical: false,
                    patternName: $"DEV {statusEffectKind}");
                StatusEffectRequest[] statusRequests =
                {
                    new(
                        statusEffectKind,
                        Math.Max(1, amount),
                        targetMode),
                };
                var requestResult = new RunCombatRequestResult(
                    baseRequest: request,
                    finalRequest: request,
                    relicActivationSummary: $"DEV RELIC: {statusEffectKind} {Math.Max(1, amount)}",
                    runBonusSummary: string.Empty,
                    statusEffectsToApply: CombatTurnRequestBuilder.BuildStatusEffectSpecs(statusRequests));
                _screenController.UpdateTurnResult(null, requestResult);

                CombatEffect[] playerEffects = _converter.Convert(
                    requestResult.FinalRequest,
                    selectedTargetId,
                    requestResult.StatusEffectsToApply);
                var presentationContext =
                    new PresentationContext(isCritical: false, requestResult.FinalRequest.PatternName);
                int eventCursor = _battle.Events.Count;
                BattleApplyResult result = _battle.ApplyPlayerTurn(playerEffects, selectedTargetId);

                if (result.Accepted)
                {
                    await _battlePresentationController.PresentEventsAsync(
                        _battle,
                        eventCursor,
                        presentationContext,
                        _presentationCancellationToken,
                        BeforeBattleEventPresentedAsync,
                        AfterBattleEventPresentedAsync);
                }
            }
            finally
            {
                _turnRunning = false;
                CompleteBattleIfNeeded();
                _screenController.Refresh();
            }
        }

        private bool CanStartTurn()
        {
            return !_battleCompleted
                && !_turnRunning
                && !_spinInputBlocked
                && _battle.CanApplyPlayerTurn
                && !_battlePresentationController.IsBusy;
        }

        private async UniTask BeforeBattleEventPresentedAsync(
            CombatEvent combatEvent,
            int eventIndex,
            System.Collections.Generic.IReadOnlyList<CombatEvent> events)
        {
            await _slotTurnController.BeforeBattleEventPresentedAsync(combatEvent, eventIndex, events);
        }

        private async UniTask AfterBattleEventPresentedAsync(
            CombatEvent combatEvent,
            int eventIndex,
            System.Collections.Generic.IReadOnlyList<CombatEvent> events)
        {
            await _slotTurnController.AfterBattleEventPresentedAsync(combatEvent, eventIndex, events);

            if (combatEvent.Kind == CombatEventKind.ActionCompleted &&
                combatEvent.Phase == BattlePhase.EnemyTurn &&
                combatEvent.SourceParticipantId.IsValid)
            {
                _screenController.ConsumeEnemyVisibleIntentAction(combatEvent.SourceParticipantId);
                return;
            }

            if (combatEvent.Kind == CombatEventKind.EffectApplied &&
                combatEvent.Phase == BattlePhase.EnemyTurn &&
                combatEvent.IsPlayerParticipant &&
                combatEvent.Effect.Kind == CombatEffectKind.Damage)
            {
                TutorialSignalRaised?.Invoke(BattleTutorialSignal.EnemyAttackReceived);
                return;
            }

            if (ShouldResolveSelectionAfterDeathPresentation(combatEvent))
            {
                _screenController.ResolveSelectedEnemyAfterPresentation();
                return;
            }

            if (combatEvent.Kind == CombatEventKind.PhaseChanged &&
                combatEvent.Phase == BattlePhase.PlayerTurn)
            {
                _screenController.RefreshVisibleIntentsFromBattle();
                TutorialSignalRaised?.Invoke(BattleTutorialSignal.EnemyTurnCompleted);
            }
        }

        private static bool ShouldResolveSelectionAfterDeathPresentation(CombatEvent combatEvent)
        {
            return (combatEvent.Kind == CombatEventKind.EffectApplied ||
                    combatEvent.Kind == CombatEventKind.StatusTicked) &&
                !combatEvent.IsPlayerParticipant &&
                combatEvent.Effect.Kind == CombatEffectKind.Damage &&
                combatEvent.TargetBefore.Hp > 0 &&
                combatEvent.TargetAfter.Hp <= 0;
        }

        private void CompleteBattleIfNeeded()
        {
            if (_battle.CurrentPhase != BattlePhase.Ended || _battleCompleted)
            {
                return;
            }

            _battleCompleted = true;
            _screenController.CompleteBattle();
            BattleCompleted?.Invoke(new BattleFlowResult(
                _battle.EndReason,
                _battle.Player.CurrentHp,
                _relicContributions.Snapshot(),
                _slotSymbolContributions.Snapshot()));
        }

        private static EnemyCombatant[] ExtractEnemyCombatants(RunEncounterRoster roster)
        {
            var combatants = new EnemyCombatant[roster.Enemies.Count];
            for (int index = 0; index < roster.Enemies.Count; index++)
            {
                combatants[index] = roster.Enemies[index].Combatant;
            }

            return combatants;
        }
    }
}
