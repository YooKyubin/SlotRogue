using System;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class BattleScreenController : IDisposable
    {
        private readonly RunBattleScreenView _view;
        private readonly RunBattleScreenViewModel _viewModel = new();
        private readonly RunBattleScreenStateUpdater _stateUpdater;

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

            _targetSelectionController = new BattleTargetSelectionController(
                _battle,
                _view,
                _encounterRoster,
                _isPresentationBusy,
                _isTurnRunning,
                Refresh);
            _targetSelectionController.ResolveSelectedEnemyId();

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
            _viewModel.SetSpinInteractable(false);
            Refresh();
        }

        internal void Refresh()
        {
            if (_battle == null || _battle.CurrentPhase == BattlePhase.NotInBattle)
            {
                return;
            }

            CombatParticipantId selectedTargetId = SelectedEnemyId;
            string statusText =
                $"{_battle.CurrentPhase}\n" +
                $"Turn {_battle.UpcomingMonsterTurnIndex}\n" +
                $"Enemies {_battle.Enemies.Count}\n" +
                $"Bonus D+{_runDamageBonus} / S+{_runDefenseBonus}";
            string enemyIntentText =
                $"ENEMY INTENT: {RunBattleScreenStateUpdater.FormatUpcomingEnemyAction(_battle)}\n" +
                $"TARGET: {selectedTargetId}";

            _viewModel.Batch(() =>
            {
                _stateUpdater.UpdatePlayerHud(_battle.Player, _combatViewModel);
                _stateUpdater.UpdateEnemySlots(
                    _battle,
                    _combatViewModel,
                    _encounterTitle,
                    _view.EnemySlotCount,
                    _encounterRoster,
                    selectedTargetId,
                    _isPresentationBusy(),
                    _isTurnRunning());
                _stateUpdater.UpdateBattleTextMeta(statusText, enemyIntentText);
            });

            UpdateSpinButtonState();
        }

        public void Dispose()
        {
            Unbind();
            _battle = null;
            _combatViewModel = null;
            _encounterRoster = null;
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

        private void UpdateSlotResult(
            SlotTurnResult slotTurnResult,
            RunCombatRequestResult combatRequestResult)
        {
            string enemyActionText = _battle != null
                ? RunBattleScreenStateUpdater.FormatUpcomingEnemyAction(_battle)
                : "none";
            _stateUpdater.UpdateSlotResult(
                combatRequestResult,
                slotTurnResult?.PatternResult,
                enemyActionText);
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
                && !_isTurnRunning();

            _viewModel.SetActionMode(RunBattleActionMode.Spin, canSpin);
        }
    }
}
