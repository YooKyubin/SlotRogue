using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class RunBattleEnemySelectionBinder
    {
        private readonly BattleSystem _battle;
        private readonly RunBattleScreenView _view;
        private readonly RunEncounterRoster _encounterRoster;
        private readonly Func<bool> _isBusy;
        private readonly Func<bool> _isSpinRunning;
        private readonly Action _refreshStatusText;

        private CombatParticipantId _selectedEnemyId;

        internal RunBattleEnemySelectionBinder(
            BattleSystem battle,
            RunBattleScreenView view,
            RunEncounterRoster encounterRoster,
            Func<bool> isBusy,
            Func<bool> isSpinRunning,
            Action refreshStatusText)
        {
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _encounterRoster = encounterRoster ?? throw new ArgumentNullException(nameof(encounterRoster));
            _isBusy = isBusy ?? throw new ArgumentNullException(nameof(isBusy));
            _isSpinRunning = isSpinRunning ?? throw new ArgumentNullException(nameof(isSpinRunning));
            _refreshStatusText = refreshStatusText ?? throw new ArgumentNullException(nameof(refreshStatusText));
        }

        internal void Bind()
        {
            int slotCount = _view.EnemySlotCount;
            for (int index = 0; index < slotCount; index++)
            {
                _view.SetEnemySlotClickHandler(index, null);
                _view.SetEnemyPortrait(index, null);
            }

            int bindCount = Mathf.Min(slotCount, _battle.Enemies.Count);
            var usedFormationSlots = new HashSet<int>();
            for (int rosterIndex = 0; rosterIndex < bindCount; rosterIndex++)
            {
                CombatParticipant enemy = _battle.Enemies[rosterIndex];
                CombatParticipantId enemyId = enemy.Id;
                int slotIndex = RunBattleScreenStateUpdater.ResolveHudSlotIndex(
                    _encounterRoster, rosterIndex, slotCount, usedFormationSlots);
                _view.SetEnemySlotClickHandler(slotIndex, () => HandleEnemySelected(enemyId));
            }

            if (_battle.Enemies.Count > slotCount)
            {
                Debug.LogWarning(
                    $"[RunBattleCompositionRoot] Enemy count {_battle.Enemies.Count} exceeds configured slots {slotCount}.");
            }
        }

        internal CombatParticipantId ResolveSelectedEnemyId()
        {
            if (_selectedEnemyId.IsValid)
            {
                for (int index = 0; index < _battle.Enemies.Count; index++)
                {
                    CombatParticipant enemy = _battle.Enemies[index];
                    if (enemy.Id.Value == _selectedEnemyId.Value && !enemy.IsDead)
                    {
                        return _selectedEnemyId;
                    }
                }
            }

            for (int index = 0; index < _battle.Enemies.Count; index++)
            {
                CombatParticipant enemy = _battle.Enemies[index];
                if (!enemy.IsDead)
                {
                    _selectedEnemyId = enemy.Id;
                    return _selectedEnemyId;
                }
            }

            _selectedEnemyId = default;
            return default;
        }

        private void HandleEnemySelected(CombatParticipantId enemyId)
        {
            if (_isBusy() || _isSpinRunning())
            {
                return;
            }

            for (int index = 0; index < _battle.Enemies.Count; index++)
            {
                CombatParticipant enemy = _battle.Enemies[index];
                if (enemy.Id.Value == enemyId.Value && !enemy.IsDead)
                {
                    _selectedEnemyId = enemyId;
                    _refreshStatusText();
                    return;
                }
            }
        }
    }
}
