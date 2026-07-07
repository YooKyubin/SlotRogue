using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.UI.Combat.Presentation;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class BattleScreenController : IDisposable
    {
        private readonly RunBattleScreenView _view;
        private readonly RunBattleScreenViewModel _viewModel = new();
        private readonly RunBattleScreenStateUpdater _stateUpdater;
        private readonly EnemyVisibleIntentState _enemyVisibleIntentState = new();
        private readonly RelicShopModel _shop = new();
        private readonly ShopDescriptionView _shopDescriptionView;

        private BattleSystem _battle;
        private CombatViewModel _combatViewModel;
        private RunEncounterRoster _encounterRoster;
        private BattleTargetSelectionController _targetSelectionController;
        private Func<bool> _isPresentationBusy;
        private Func<bool> _isTurnRunning;
        private string _encounterTitle = string.Empty;
        private int _runDamageBonus;
        private int _runDefenseBonus;
        private bool _battleCompleted;
        private bool _spinInputBlocked;
        private bool _targetSelectionBlocked;
        private bool _hasSlotResult;
        private bool _swapDecisionActive;
        private bool _shopOpen;
        private int _swapsRemaining;
        private int _selectedSwapCellIndex = -1;
        private bool _isBound;
        private IDisposable _stateSubscription;

        internal BattleScreenController(
            RunBattleScreenView view,
            ShopDescriptionView shopDescriptionView = null)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _shopDescriptionView = shopDescriptionView;
            _stateUpdater = new RunBattleScreenStateUpdater(_viewModel);
        }

        internal event Action SpinRequested;

        internal event Action AttackRequested;

        internal event Action<int, int> SlotSwapRequested;

        internal CombatParticipantId SelectedEnemyId =>
            _targetSelectionController != null
                ? _targetSelectionController.ResolveSelectedEnemyId()
                : default;

        private CombatParticipantId DisplayedSelectedEnemyId =>
            _targetSelectionController != null
                ? _targetSelectionController.PeekSelectedEnemyId()
                : default;

        internal void BeginBattle(
            BattleSystem battle,
            CombatViewModel combatViewModel,
            RunEncounterRoster encounterRoster,
            string encounterTitle,
            int runDamageBonus,
            int runDefenseBonus,
            Func<bool> isPresentationBusy,
            Func<bool> isTurnRunning)
        {
            Unbind();

            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            _combatViewModel = combatViewModel ?? throw new ArgumentNullException(nameof(combatViewModel));
            _encounterRoster = encounterRoster ?? throw new ArgumentNullException(nameof(encounterRoster));
            _encounterTitle = encounterTitle ?? string.Empty;
            _runDamageBonus = runDamageBonus;
            _runDefenseBonus = runDefenseBonus;
            _isPresentationBusy = isPresentationBusy ?? throw new ArgumentNullException(nameof(isPresentationBusy));
            _isTurnRunning = isTurnRunning ?? throw new ArgumentNullException(nameof(isTurnRunning));
            _battleCompleted = false;
            _spinInputBlocked = false;
            _targetSelectionBlocked = false;
            _hasSlotResult = false;
            _shopOpen = false;
            _shopDescriptionView?.Hide();
            ResetSwapDecision();
            ApplyBattleStartRelicEffects();
            _shop.Roll();
            _enemyVisibleIntentState.Clear();

            _targetSelectionController = new BattleTargetSelectionController(
                _battle,
                _view,
                _encounterRoster,
                _isPresentationBusy,
                () => _isTurnRunning() || _targetSelectionBlocked,
                Refresh);
            _targetSelectionController.ResolveSelectedEnemyId();
            _enemyVisibleIntentState.RefreshFromBattle(_battle, _battle.Enemies, _encounterRoster);
            BindEnemyCombatVisualPrefabs();

            Bind();
            _targetSelectionController.Bind();
            _view.Render(_viewModel.State.CurrentValue);
            _viewModel.SetActionMode(RunBattleActionMode.Spin, spinInteractable: true);
            Refresh();
            UpdateSlotResult(null, null);
            UpdateSwapState(false);
            UpdateRelicShopState();
        }

        internal void SetSpinInteractable(bool interactable)
        {
            _viewModel.SetSpinInteractable(interactable);
            if (!interactable)
            {
                UpdateSwapState(false);
                UpdateRelicShopState(false);
            }
        }

        internal void SetSpinInputBlocked(bool blocked)
        {
            _spinInputBlocked = blocked;
            if (_battle != null && _battle.CurrentPhase != BattlePhase.NotInBattle)
            {
                UpdateSpinButtonState();
                UpdateRelicShopState();
            }
        }

        internal void SetTargetSelectionBlocked(bool blocked)
        {
            _targetSelectionBlocked = blocked;
            Refresh();
        }

        internal void UpdateTurnResult(
            SlotTurnResult slotTurnResult,
            RunCombatRequestResult combatRequestResult)
        {
            if (slotTurnResult != null)
            {
                _hasSlotResult = true;
                _stateUpdater.UpdateSlotCells(slotTurnResult.SpinResult);
            }

            UpdateSlotResult(slotTurnResult, combatRequestResult);
            UpdateSwapState(_swapDecisionActive);
        }

        internal void BeginSwapDecision(SlotTurnResult slotTurnResult, int swapsAvailable)
        {
            if (slotTurnResult == null)
            {
                return;
            }

            _swapDecisionActive = true;
            _swapsRemaining = Math.Max(0, swapsAvailable);
            _selectedSwapCellIndex = -1;
            _hasSlotResult = true;
            _viewModel.Batch(() =>
            {
                _stateUpdater.UpdateSlotCells(slotTurnResult.SpinResult);
                UpdateSwapPreview(slotTurnResult);
                _viewModel.SetActionMode(RunBattleActionMode.Attack, spinInteractable: true);
                _viewModel.SetSwapState(
                    interactable: _swapsRemaining > 0,
                    _swapsRemaining,
                    _selectedSwapCellIndex);
                _viewModel.SetRelicShop(BuildRelicShopState(canUseShop: false));
            });
        }

        internal void UpdateSwapDecisionResult(SlotTurnResult slotTurnResult)
        {
            if (!_swapDecisionActive || slotTurnResult == null)
            {
                return;
            }

            _viewModel.Batch(() =>
            {
                _stateUpdater.UpdateSlotCells(slotTurnResult.SpinResult);
                UpdateSwapPreview(slotTurnResult);
                _viewModel.SetSwapState(
                    interactable: _swapsRemaining > 0,
                    _swapsRemaining,
                    _selectedSwapCellIndex);
            });
        }

        internal void EndSwapDecision()
        {
            if (!_swapDecisionActive && _swapsRemaining == 0 && _selectedSwapCellIndex < 0)
            {
                return;
            }

            ResetSwapDecision();
            UpdateSwapState(false);
            UpdateSpinButtonState();
            UpdateRelicShopState();
        }

        internal void CompleteBattle()
        {
            _battleCompleted = true;
            _enemyVisibleIntentState.Clear();
            _viewModel.SetSpinInteractable(false);
            UpdateSwapState(false);
            UpdateRelicShopState(false);
            Refresh();
        }

        internal void ResumeBattle()
        {
            _battleCompleted = false;
            _enemyVisibleIntentState.RefreshFromBattle(
                _battle,
                _battle.Enemies,
                _encounterRoster);
            Refresh();
        }

        internal void RefreshVisibleIntentsFromBattle()
        {
            if (_battle == null)
            {
                _enemyVisibleIntentState.Clear();
                return;
            }

            _enemyVisibleIntentState.RefreshFromBattle(_battle, _battle.Enemies, _encounterRoster);
            Refresh();
        }

        internal void ConsumeEnemyVisibleIntentAction(CombatParticipantId enemyId)
        {
            _enemyVisibleIntentState.ConsumeFirstAction(enemyId);
            Refresh();
        }

        internal void Refresh()
        {
            if (_battle == null || _battle.CurrentPhase == BattlePhase.NotInBattle)
            {
                return;
            }

            CombatParticipantId selectedTargetId = DisplayedSelectedEnemyId;

            _viewModel.Batch(() =>
            {
                _stateUpdater.UpdatePlayerHud(_battle.Player, _combatViewModel);
                _stateUpdater.UpdateEnemySlots(
                    _battle,
                    _combatViewModel,
                    _enemyVisibleIntentState,
                    _encounterTitle,
                    _view.EnemySlotCount,
                    _encounterRoster,
                    selectedTargetId,
                    _isPresentationBusy(),
                    _isTurnRunning() || _targetSelectionBlocked);
                _viewModel.SetRunCoins(GameFlowSession.RunCoins);
            });

            UpdateSpinButtonState();
        }

        internal void ResolveSelectedEnemyAfterPresentation()
        {
            if (_targetSelectionController == null)
            {
                return;
            }

            _targetSelectionController.ResolveSelectedEnemyId();
            Refresh();
        }

        public void Dispose()
        {
            Unbind();
            _battle = null;
            _combatViewModel = null;
            _encounterRoster = null;
            _enemyVisibleIntentState.Clear();
            _isPresentationBusy = null;
            _isTurnRunning = null;
        }

        private void Bind()
        {
            if (_isBound)
            {
                return;
            }

            _stateSubscription = _viewModel.State.Subscribe(HandleStateChanged);
            _combatViewModel.Changed += Refresh;
            _view.SpinRequested += HandleSpinRequested;
            _view.SlotCellSelected += HandleSlotCellSelected;
            _view.SlotCellsDragged += HandleSlotCellsDragged;
            _view.RelicShopPurchaseRequested += HandleRelicShopPurchaseRequested;
            _view.RelicShopRerollRequested += HandleRelicShopRerollRequested;
            _view.RelicShopToggleRequested += HandleRelicShopToggleRequested;
            _view.ShopOfferSelected += HandleShopOfferSelected;
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            _view.SpinRequested -= HandleSpinRequested;
            _view.SlotCellSelected -= HandleSlotCellSelected;
            _view.SlotCellsDragged -= HandleSlotCellsDragged;
            _view.RelicShopPurchaseRequested -= HandleRelicShopPurchaseRequested;
            _view.RelicShopRerollRequested -= HandleRelicShopRerollRequested;
            _view.RelicShopToggleRequested -= HandleRelicShopToggleRequested;
            _view.ShopOfferSelected -= HandleShopOfferSelected;
            _stateSubscription?.Dispose();
            _stateSubscription = null;
            if (_combatViewModel != null)
            {
                _combatViewModel.Changed -= Refresh;
            }

            _targetSelectionController?.Unbind();
            _targetSelectionController = null;
            _isBound = false;
        }

        private void HandleStateChanged(RunBattleScreenState state)
        {
            _view.Render(state);
        }

        private void HandleSpinRequested()
        {
            if (_shopOpen)
            {
                return;
            }

            if (_swapDecisionActive)
            {
                AttackRequested?.Invoke();
                return;
            }

            SpinRequested?.Invoke();
        }

        private void HandleSlotCellSelected(int cellIndex)
        {
            if (!_swapDecisionActive ||
                _swapsRemaining <= 0 ||
                !SlotSpinResult.IsValidIndex(cellIndex))
            {
                return;
            }

            if (_selectedSwapCellIndex < 0)
            {
                _selectedSwapCellIndex = cellIndex;
                UpdateSwapState(true);
                return;
            }

            if (_selectedSwapCellIndex == cellIndex)
            {
                _selectedSwapCellIndex = -1;
                UpdateSwapState(true);
                return;
            }

            if (!SlotSpinResult.AreAdjacent(_selectedSwapCellIndex, cellIndex))
            {
                _selectedSwapCellIndex = cellIndex;
                UpdateSwapState(true);
                return;
            }

            int firstIndex = _selectedSwapCellIndex;
            TryRequestSwap(firstIndex, cellIndex);
        }

        private void HandleSlotCellsDragged(int firstIndex, int secondIndex)
        {
            TryRequestSwap(firstIndex, secondIndex);
        }

        private bool TryRequestSwap(int firstIndex, int secondIndex)
        {
            if (!_swapDecisionActive ||
                _swapsRemaining <= 0 ||
                !SlotSpinResult.AreAdjacent(firstIndex, secondIndex))
            {
                UpdateSwapState(_swapDecisionActive);
                return false;
            }

            _selectedSwapCellIndex = -1;
            _swapsRemaining = Math.Max(0, _swapsRemaining - 1);
            SlotSwapRequested?.Invoke(firstIndex, secondIndex);
            UpdateSwapState(_swapDecisionActive);
            return true;
        }

        private void HandleRelicShopPurchaseRequested(int offerIndex)
        {
            if (!CanUseRelicShop() || !_shop.TryPurchase(offerIndex))
            {
                UpdateRelicShopState();
                return;
            }

            UpdateRelicShopState();
            Refresh();
            _shopDescriptionView?.Hide();
        }

        private void HandleRelicShopRerollRequested()
        {
            if (!CanUseRelicShop() || !_shop.TryReroll())
            {
                UpdateRelicShopState();
                return;
            }

            UpdateRelicShopState();
            Refresh();
            _shopDescriptionView?.Hide();
        }

        private void HandleRelicShopToggleRequested()
        {
            if (!_shop.HasAnyOffer())
            {
                _shop.Roll();
            }

            _shopOpen = !_shopOpen;
            UpdateSpinButtonState();
            if (!_shopOpen)
            {
                _shopDescriptionView?.Hide();
            }
        }

        private void HandleShopOfferSelected(RunBattleRelicShopOfferState offer)
        {
            _shopDescriptionView?.Show(offer);
        }

        private void BindEnemyCombatVisualPrefabs()
        {
            _view.ClearEnemyCombatVisualPrefabs();
            _view.ClearEnemyPortraitSprites();

            for (int rosterIndex = 0; rosterIndex < _encounterRoster.Enemies.Count; rosterIndex++)
            {
                EnemyEncounterUnit unit = _encounterRoster.Enemies[rosterIndex];
                MonsterVisualDefinition visual = ResolveMonsterVisual(unit, rosterIndex);
                _view.SetEnemyPortraitSprite(
                    unit.FormationSlot,
                    visual != null ? visual.Portrait : null);

                GameObject combatVisualPrefab = ResolveCombatVisualPrefab(visual);
                _view.SetEnemyCombatVisualPrefab(unit.FormationSlot, combatVisualPrefab);
            }
        }

        private static void ApplyBattleStartRelicEffects()
        {
            GameFlowSession.ApplyBattleStartRelicCoins();
        }

        private static MonsterVisualDefinition ResolveMonsterVisual(EnemyEncounterUnit unit, int rosterIndex)
        {
            MonsterDefinition definition = unit.Definition;
            if (definition == null)
            {
                Debug.LogError(
                    $"[BattleScreenController] Enemy roster index {rosterIndex} has no MonsterDefinition.");
                return null;
            }

            MonsterVisualDefinition visual = definition.Visual;
            if (visual == null)
            {
                Debug.LogError(
                    $"[BattleScreenController] MonsterDefinition '{definition.name}' has no visual definition.");
                return null;
            }

            return visual;
        }

        private static GameObject ResolveCombatVisualPrefab(MonsterVisualDefinition visual)
        {
            if (visual == null)
            {
                return null;
            }

            GameObject combatVisualPrefab = visual.CombatVisualPrefab;
            if (combatVisualPrefab == null)
            {
                Debug.LogError(
                    $"[BattleScreenController] Monster visual definition '{visual.name}' has no combat visual prefab.");
            }

            return combatVisualPrefab;
        }

        private void UpdateSlotResult(
            SlotTurnResult slotTurnResult,
            RunCombatRequestResult combatRequestResult)
        {
            _stateUpdater.UpdateSlotResult(
                combatRequestResult,
                slotTurnResult);
        }

        private void UpdateSpinButtonState()
        {
            if (_battleCompleted)
            {
                _viewModel.SetActionMode(RunBattleActionMode.Spin, spinInteractable: false);
                UpdateSwapState(false);
                return;
            }

            if (_swapDecisionActive)
            {
                _viewModel.SetActionMode(RunBattleActionMode.Attack, spinInteractable: true);
                UpdateSwapState(true);
                UpdateRelicShopState(false);
                return;
            }

            bool canSpin = _battle.CurrentPhase != BattlePhase.NotInBattle
                && _battle.CanApplyPlayerTurn
                && !_isPresentationBusy()
                && !_isTurnRunning()
                && !_spinInputBlocked
                && !_shopOpen;

            _viewModel.SetActionMode(RunBattleActionMode.Spin, canSpin);
            UpdateSwapState(false);
            UpdateRelicShopState(CanUseRelicShop());
        }

        private void UpdateSwapState(bool interactable)
        {
            bool canInteract = interactable &&
                _swapDecisionActive &&
                _hasSlotResult &&
                _swapsRemaining > 0;
            _viewModel.SetSwapState(
                canInteract,
                _swapDecisionActive ? _swapsRemaining : 0,
                _swapDecisionActive ? _selectedSwapCellIndex : -1);
        }

        private void UpdateSwapPreview(SlotTurnResult slotTurnResult)
        {
            SlotPatternResult patternResult = slotTurnResult?.PatternResult;
            bool hasPattern = patternResult != null && patternResult.HasMatch;
            string resultText = hasPattern
                ? $"SWAP PHASE\nMATCH READY\n{patternResult.PatternName}"
                : "SWAP PHASE\nNO PATTERN YET";

            resultText += "\n" + RunBattleScreenStateUpdater.FormatSpinEconomy(slotTurnResult);
            resultText += $"\nSWAP {_swapsRemaining}";

            _viewModel.SetBattleText(resultText, string.Empty);
            _viewModel.SetSlotOutcome(
                hasPattern,
                patternResult != null ? patternResult.Row : -1,
                patternResult != null ? patternResult.StartColumn : -1,
                patternResult != null ? patternResult.MatchLength : 0,
                RunBattleScreenStateUpdater.CollectHighlightedCellIndices(slotTurnResult?.PatternMatches),
                RunBattleScreenStateUpdater.CollectHighlightedCellSymbols(slotTurnResult?.PatternMatches));
        }

        private void ResetSwapDecision()
        {
            _swapDecisionActive = false;
            _swapsRemaining = 0;
            _selectedSwapCellIndex = -1;
        }

        private void UpdateRelicShopState()
        {
            UpdateRelicShopState(CanUseRelicShop());
        }

        private void UpdateRelicShopState(bool canUseShop)
        {
            _viewModel.SetRelicShop(BuildRelicShopState(canUseShop));
        }

        private RunBattleRelicShopState BuildRelicShopState(bool canUseShop)
        {
            bool canOpen = _shop.HasAnyOffer();
            bool visible = canOpen && _shopOpen;
            var offers = new RunBattleRelicShopOfferState[_shop.Count];
            for (int index = 0; index < offers.Length; index++)
            {
                RelicDefinition relic = _shop.OfferAt(index);
                int cost = _shop.CostOf(index);
                bool purchased = _shop.IsPurchased(index);
                bool canPurchase = visible &&
                    canUseShop &&
                    relic != null &&
                    !purchased &&
                    GameFlowSession.RunCoins >= cost &&
                    _shop.CanPurchase(index);
                offers[index] = new RunBattleRelicShopOfferState(
                    relic?.Id,
                    relic?.Name,
                    relic != null ? RelicDisplay.GradeKorean(relic.Grade) : string.Empty,
                    relic != null ? RewardRarityMap.FromGrade(relic.Grade) : RewardRarity.Common,
                    relic != null ? RelicDisplay.BuildDescription(relic) : string.Empty,
                    relic?.IconKey,
                    cost,
                    purchased,
                    canPurchase);
            }

            return new RunBattleRelicShopState(
                visible,
                offers,
                GameFlowSession.RunCoins,
                RelicShopModel.RerollCost,
                visible && canUseShop && GameFlowSession.RunCoins >= RelicShopModel.RerollCost,
                visible && canUseShop,
                canOpen || visible);
        }

        private bool CanUseRelicShop()
        {
            return _battle != null
                && _isPresentationBusy != null
                && _isTurnRunning != null
                && !_battleCompleted
                && !_swapDecisionActive
                && _battle.CurrentPhase != BattlePhase.NotInBattle
                && _battle.CanApplyPlayerTurn
                && !_isPresentationBusy()
                && !_isTurnRunning()
                && !_spinInputBlocked;
        }
    }
}
