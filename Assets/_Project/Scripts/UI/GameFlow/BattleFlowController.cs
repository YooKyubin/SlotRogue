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

        private BattleFlowContext _context;
        private bool _battleCompleted;
        private bool _turnRunning;

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

        public void BeginBattle(BattleFlowContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _battleCompleted = false;
            _turnRunning = false;

            _slotTurnController.SetupImmediate();
            _battle.StartBattle(
                _context.Player,
                _context.EncounterRoster.EnemyCombatants);
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
        }

        public void DevApplyStatusTurn(
            StatusEffectKind statusEffectKind,
            int duration,
            int magnitude,
            StatusStackMode stackMode,
            bool includeDamage,
            int damage,
            int attackCount)
        {
            DevApplyStatusTurnAsync(
                statusEffectKind,
                duration,
                magnitude,
                stackMode,
                includeDamage,
                damage,
                attackCount).Forget();
        }

        public void Dispose()
        {
            _screenController.SpinRequested -= HandleSpinRequested;
            _screenController.Dispose();
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
                    requestResult,
                    _presentationCancellationToken);

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

        private async UniTaskVoid DevApplyStatusTurnAsync(
            StatusEffectKind statusEffectKind,
            int duration,
            int magnitude,
            StatusStackMode stackMode,
            bool includeDamage,
            int damage,
            int attackCount)
        {
            if (statusEffectKind == StatusEffectKind.None || !CanStartTurn())
            {
                _screenController.Refresh();
                return;
            }

            CombatParticipantId selectedTargetId = _screenController.SelectedEnemyId;
            if (!selectedTargetId.IsValid)
            {
                return;
            }

            _screenController.SetSpinInteractable(false);
            _turnRunning = true;

            try
            {
                var statusEffect = new StatusEffectSpec(
                    statusEffectKind,
                    Math.Max(0, duration),
                    Math.Max(0, magnitude),
                    stackMode);
                var request = new SlotCombatRequest(
                    includeDamage ? Math.Max(0, damage) : 0,
                    0,
                    Math.Max(1, attackCount),
                    0,
                    false,
                    $"DEV {statusEffectKind}");
                var requestResult = new RunCombatRequestResult(
                    request,
                    request,
                    string.Empty,
                    $"DEV {statusEffectKind}",
                    new[] { statusEffect });
                _screenController.UpdateTurnResult(null, requestResult);

                CombatEffect[] playerEffects = _converter.Convert(request, selectedTargetId, statusEffect);
                var presentationContext =
                    new PresentationContext(isCritical: false, request.PatternName);
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

            if (combatEvent.Kind == CombatEventKind.PhaseChanged &&
                combatEvent.Phase == BattlePhase.PlayerTurn)
            {
                _screenController.RefreshVisibleIntentsFromBattle();
            }
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
                _battle.Player.CurrentHp));
        }
    }
}
