using System;
using UnityEngine;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.UI.Combat.Presentation;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class BattleScreenController : IDisposable
    {
        private readonly RunBattleScreenView _view;
        private readonly RunBattleScreenViewModel _viewModel = new();
        private readonly RunBattleScreenStateUpdater _stateUpdater;
        private readonly EnemyVisibleIntentState _enemyVisibleIntentState = new();

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
        private bool _isBound;

        internal BattleScreenController(RunBattleScreenView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _stateUpdater = new RunBattleScreenStateUpdater(_viewModel);
        }

        internal event Action SpinRequested;

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
            _view.Render(_viewModel.State);
            _viewModel.SetActionMode(RunBattleActionMode.Spin, spinInteractable: true);
            Refresh();
            UpdateSlotResult(null, null);
        }

        internal void SetSpinInteractable(bool interactable)
        {
            _viewModel.SetSpinInteractable(interactable);
        }

        internal void SetSpinInputBlocked(bool blocked)
        {
            _spinInputBlocked = blocked;
            if (_battle != null && _battle.CurrentPhase != BattlePhase.NotInBattle)
            {
                UpdateSpinButtonState();
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
                _stateUpdater.UpdateSlotCells(slotTurnResult.SpinResult);
            }

            UpdateSlotResult(slotTurnResult, combatRequestResult);
        }

        internal void CompleteBattle()
        {
            _battleCompleted = true;
            _enemyVisibleIntentState.Clear();
            _viewModel.SetSpinInteractable(false);
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

            _viewModel.Changed += HandleStateChanged;
            _combatViewModel.Changed += Refresh;
            _view.SpinRequested += HandleSpinRequested;
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            _view.SpinRequested -= HandleSpinRequested;
            _viewModel.Changed -= HandleStateChanged;
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
            SpinRequested?.Invoke();
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
                slotTurnResult?.PatternResult);
        }

        private void UpdateSpinButtonState()
        {
            if (_battleCompleted)
            {
                return;
            }

            bool canSpin = _battle.CurrentPhase != BattlePhase.NotInBattle
                && _battle.CanApplyPlayerTurn
                && !_isPresentationBusy()
                && !_isTurnRunning()
                && !_spinInputBlocked;

            _viewModel.SetActionMode(RunBattleActionMode.Spin, canSpin);
        }
    }
}
