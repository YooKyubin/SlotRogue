using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.Relics.Pool;
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
        private UniTaskCompletionSource _attackTcs;
        private UniTask _pendingSwapAnimation = UniTask.CompletedTask;
        private SlotTurnResult _pendingSlotTurnResult;
        private bool _battleCompleted;
        private bool _turnRunning;
        private bool _spinInputBlocked;
        private bool _swappedThisTurn;
        private bool _swappedThisBattle;
        private int _spinTurnIndex;

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
            _screenController.AttackRequested += HandleAttackRequested;
            _screenController.SlotSwapRequested += HandleSlotSwapRequested;
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
            _swappedThisBattle = false;
            _spinTurnIndex = 0;

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

        /// <summary>
        /// v29 유물 실행 엔진(<see cref="RelicSpecRunner"/>)을 이번 스핀에 적용한 결과를 계산한다.
        /// 보유 유물이 없으면 null(적용 없음).
        /// </summary>
        private RelicSpecResolveResult ResolveSpecResult(IReadOnlyList<SlotPatternMatch> patternMatches)
        {
            IReadOnlyList<RelicSpec> ownedSpecs = BuildOwnedSpecs(_context.OwnedRelics);
            if (ownedSpecs.Count == 0)
            {
                return null;
            }

            var runtimeContext = new RelicRuntimeContext(
                _swappedThisTurn,
                _swappedThisBattle,
                GameFlowSession.RunCoins,
                _spinTurnIndex,
                _spinTurnIndex == 1,
                patternMatches?.Count ?? 0);

            return RelicSpecRunner.ResolveDamageTurn(
                ownedSpecs,
                runtimeContext,
                BuildRelicPatternViews(patternMatches, _swappedThisTurn));
        }

        /// <summary>엔진이 만든 가산 피해(FlatDamage)/회복(Heal)/기여를 기존 유물 결과에 합류시킨다.</summary>
        private static RelicResolveResult MergeSpecResult(
            RelicResolveResult baseResult,
            RelicSpecResolveResult specResult)
        {
            if (specResult == null ||
                (specResult.FlatDamage == 0 &&
                 specResult.Heal == 0 &&
                 specResult.StatusRequests.Count == 0))
            {
                return baseResult;
            }

            var contributions = new List<RelicContributionDelta>(baseResult.Contributions);
            for (int index = 0; index < specResult.Contributions.Count; index++)
            {
                RelicSpecContribution contribution = specResult.Contributions[index];
                contributions.Add(new RelicContributionDelta(
                    contribution.RelicId,
                    contribution.RelicName,
                    contribution.FlatDamage,
                    block: 0,
                    heal: contribution.Heal,
                    triggerPatternIndex: -1));
            }

            return new RelicResolveResult(
                baseResult.AdditionalDamage + specResult.FlatDamage,
                baseResult.AdditionalBlock,
                baseResult.HealAmount + specResult.Heal,
                MergeStatusRequests(baseResult.StatusEffectsToApply, specResult.StatusRequests),
                baseResult.ActivationSummary,
                contributions,
                baseResult.DerivedHeals);
        }

        /// <summary>엔진 상태이상 요청을 전투 상태이상 요청으로 변환해 기존 목록에 합친다.</summary>
        private static IReadOnlyList<StatusEffectRequest> MergeStatusRequests(
            IReadOnlyList<StatusEffectRequest> baseStatuses,
            IReadOnlyList<RelicSpecStatusRequest> specStatuses)
        {
            if (specStatuses == null || specStatuses.Count == 0)
            {
                return baseStatuses;
            }

            var merged = new List<StatusEffectRequest>(baseStatuses);
            for (int index = 0; index < specStatuses.Count; index++)
            {
                RelicSpecStatusRequest request = specStatuses[index];
                CombatTargetMode target = request.Kind == RelicEffectKind.GainThorns
                    ? CombatTargetMode.Self
                    : CombatTargetMode.SelectedEnemy;
                merged.Add(new StatusEffectRequest(MapStatusKind(request.Kind), request.Amount, target));
            }

            return merged;
        }

        private static StatusEffectKind MapStatusKind(RelicEffectKind kind)
        {
            switch (kind)
            {
                case RelicEffectKind.ApplyBurn:
                    return StatusEffectKind.Burn;
                case RelicEffectKind.ApplyInfection:
                    return StatusEffectKind.Infection;
                case RelicEffectKind.ApplyVulnerable:
                    return StatusEffectKind.Vulnerable;
                case RelicEffectKind.ApplyWeaken:
                    return StatusEffectKind.Weaken;
                case RelicEffectKind.GainThorns:
                    return StatusEffectKind.Thorns;
                default:
                    return StatusEffectKind.None;
            }
        }

        private RelicRuntimeContext EventContext() => new(
            _swappedThisTurn,
            _swappedThisBattle,
            GameFlowSession.RunCoins,
            _spinTurnIndex,
            _spinTurnIndex == 1,
            0);

        private static IReadOnlyList<RelicSpec> BuildOwnedSpecs(IReadOnlyList<RelicDefinition> owned)
        {
            var specs = new List<RelicSpec>();
            if (owned != null)
            {
                for (int index = 0; index < owned.Count; index++)
                {
                    RelicDefinition relic = owned[index];
                    if (relic == null)
                    {
                        continue;
                    }

                    RelicSpec spec = RelicSpecCatalog.GetById(relic.Id);
                    if (spec != null)
                    {
                        specs.Add(spec);
                    }
                }
            }

            // 제안(처치 보상)으로 획득한 영구 엔진 효과도 유물과 함께 적용한다.
            IReadOnlyList<RelicSpec> proposalSpecs = GameFlowSession.ProposalSpecs;
            for (int index = 0; index < proposalSpecs.Count; index++)
            {
                specs.Add(proposalSpecs[index]);
            }

            return specs;
        }

        private static IReadOnlyList<RelicPatternView> BuildRelicPatternViews(
            IReadOnlyList<SlotPatternMatch> matches,
            bool swappedThisSpin)
        {
            var views = new List<RelicPatternView>();
            if (matches == null)
            {
                return views;
            }

            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                if (match?.MatchedCells == null)
                {
                    continue;
                }

                int size = match.MatchedCells.Count;
                // madeBySwap 근사: 이번 스핀에 스왑을 썼으면 그 스핀 족보를 swap-made로 본다(정밀 추적은 추후).
                // wholeLineSameSymbol은 5칸 매치로 근사. CalculatedValue = 이 족보의 기본 피해(배율 모델이 곱함).
                views.Add(new RelicPatternView(
                    match.Symbol, size, swappedThisSpin, size >= 5, match.CalculatedValue));
            }

            return views;
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
            _screenController.AttackRequested -= HandleAttackRequested;
            _screenController.SlotSwapRequested -= HandleSlotSwapRequested;
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

        private void HandleAttackRequested()
        {
            _attackTcs?.TrySetResult();
        }

        private void HandleSlotSwapRequested(int firstIndex, int secondIndex)
        {
            HandleSlotSwapRequestedAsync(firstIndex, secondIndex).Forget();
        }

        private async UniTaskVoid HandleSlotSwapRequestedAsync(int firstIndex, int secondIndex)
        {
            if (_pendingSlotTurnResult == null)
            {
                return;
            }

            if (!_slotTurnController.TrySwapCurrentSpinResult(
                    firstIndex,
                    secondIndex,
                    out SlotTurnResult swappedTurnResult))
            {
                return;
            }

            // 스왑을 사용하면 이번 턴 별조각 보상은 지급하지 않는다.
            _swappedThisTurn = true;
            _pendingSlotTurnResult = swappedTurnResult;

            // 자리 교체 연출을 재생한다. 공격 정산이 이 연출 완료를 기다리도록 태스크를 보관한다.
            // 필드에 저장 후 나중에 await하므로 Preserve로 재활용/중복 await를 방지한다.
            UniTask animation = _slotTurnController
                .PlaySwapPresentationAsync(firstIndex, secondIndex)
                .Preserve();
            _pendingSwapAnimation = animation;

            // 연출이 끝나 보드가 정착한 뒤에 새 족보 하이라이트를 그린다.
            // 연출 중에 다시 그리면 이동하는 고스트 뒤로 하이라이트/심볼 잔상이 비친다.
            await animation.AttachExternalCancellation(_presentationCancellationToken);
            _screenController.UpdateSwapDecisionResult(_pendingSlotTurnResult);
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
            _spinTurnIndex++;

            try
            {
                SlotTurnResult slotTurnResult = await _slotTurnController.SpinAsync(
                    new SlotTurnRequest(_presentationCancellationToken));
                // 별조각(런 코인)은 공격 확정 시점에 지급한다. 스왑을 넛지로 쓰면 이번 턴 보상은 0.
                _swappedThisTurn = false;
                _pendingSwapAnimation = UniTask.CompletedTask;
                _pendingSlotTurnResult = slotTurnResult;
                _attackTcs = new UniTaskCompletionSource();
                _screenController.BeginSwapDecision(
                    slotTurnResult,
                    GameFlowSession.SwapCountPerPlayerTurn);
                TutorialSignalRaised?.Invoke(BattleTutorialSignal.SlotDecisionStarted);
                await _attackTcs.Task.AttachExternalCancellation(_presentationCancellationToken);
                // 스왑 연출이 진행 중이면 보드가 최종 상태로 정착할 때까지 기다린 뒤 정산한다.
                await _pendingSwapAnimation.AttachExternalCancellation(_presentationCancellationToken);
                _pendingSwapAnimation = UniTask.CompletedTask;
                int spinCoinReward = _swappedThisTurn ? 0 : GameFlowSession.AddSpinCoins();
                slotTurnResult = _slotTurnController.ResolveCurrentSpinResult(
                    spinCoinReward,
                    GameFlowSession.RunCoins);
                _screenController.EndSwapDecision();
                _swappedThisBattle |= _swappedThisTurn;

                CombatParticipantId selectedTargetId = _screenController.SelectedEnemyId;
                RelicResolveResult relicResult = _relicTurnResolver.Resolve(
                    _context.OwnedRelics,
                    slotTurnResult.PatternMatches,
                    RelicTurnContext.FromBattle(_battle, selectedTargetId));
                RelicSpecResolveResult specResult = ResolveSpecResult(slotTurnResult.PatternMatches);
                relicResult = MergeSpecResult(relicResult, specResult);
                SlotCombatRequest baseCombatRequest = slotTurnResult.BaseCombatRequest;
                if (specResult != null)
                {
                    // 배율 유물이 있으면 족보별로 곱해진 기본 피해로 교체(P2). 배율이 없으면 원래 값과 같다.
                    baseCombatRequest = new SlotCombatRequest(
                        specResult.MultipliedBaseDamage,
                        baseCombatRequest.Defense,
                        baseCombatRequest.AttackCount,
                        baseCombatRequest.HealAmount,
                        baseCombatRequest.IsCritical,
                        baseCombatRequest.PatternName);
                }

                RunCombatRequestResult requestResult = _combatTurnRequestBuilder.Build(
                    baseCombatRequest,
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
                _attackTcs = null;
                _pendingSlotTurnResult = null;
                _slotTurnController.ResetImmediate();
                _screenController.EndSwapDecision();
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

            if (combatEvent.Kind == CombatEventKind.PhaseChanged &&
                combatEvent.Phase == BattlePhase.PlayerTurn)
            {
                _screenController.RefreshVisibleIntentsFromBattle();
                TutorialSignalRaised?.Invoke(BattleTutorialSignal.EnemyTurnCompleted);
            }
        }

        private void CompleteBattleIfNeeded()
        {
            if (_battle.CurrentPhase != BattlePhase.Ended || _battleCompleted)
            {
                return;
            }

            _battleCompleted = true;

            if (_battle.EndReason == BattleEndReason.Victory)
            {
                int killCoins = RelicSpecRunner.ResolveEventCoins(
                    BuildOwnedSpecs(_context.OwnedRelics), RelicTrigger.OnKill, EventContext());
                if (killCoins != 0)
                {
                    GameFlowSession.AddRunCoins(killCoins);
                }
            }

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
