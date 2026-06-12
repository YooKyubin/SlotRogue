using System.Collections.Generic;
using System.Text;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class RunBattleScreenStateUpdater
    {
        private readonly RunBattleScreenViewModel _vm;

        internal RunBattleScreenStateUpdater(RunBattleScreenViewModel vm)
        {
            _vm = vm;
        }

        internal void UpdateSlotCells(SlotSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return;
            }

            string[] cells = new string[RunBattleScreenViewModel.DefaultSlotCellCount];
            int cellCount = Mathf.Min(cells.Length, spinResult.Symbols.Count);
            for (int index = 0; index < cellCount; index++)
            {
                cells[index] = FormatSlotSymbol(spinResult.Symbols[index]);
            }

            _vm.SetSlotCells(cells);
        }

        internal void UpdateSlotResult(
            RunCombatRequestResult lastRequestResult,
            SlotPatternResult patternResult,
            string upcomingEnemyActionText)
        {
            if (lastRequestResult == null)
            {
                _vm.Batch(() =>
                {
                    _vm.SetBattleText(
                        _vm.State.StatusText,
                        "NEXT ATTACK\n" + upcomingEnemyActionText,
                        "ATK 0",
                        _vm.State.EnemyIntentText);
                    _vm.SetSlotOutcome(hasPattern: false, row: -1, startColumn: -1, matchLength: 0);
                });
                return;
            }

            SlotCombatRequest request = lastRequestResult.FinalRequest;
            bool hasPattern = patternResult != null && patternResult.HasMatch;
            var builder = new StringBuilder();
            if (hasPattern)
            {
                builder.AppendLine("PATTERN HIT!");
                builder.AppendLine(patternResult.PatternName);
            }
            else
            {
                builder.AppendLine(SlotCombatRequest.BaseAttackName.ToUpperInvariant());
                builder.AppendLine("No pattern matched");
            }

            builder.AppendLine(FormatRequest(request));

            if (!string.IsNullOrEmpty(lastRequestResult.RelicActivationSummary))
            {
                builder.AppendLine(lastRequestResult.RelicActivationSummary);
            }

            if (!string.IsNullOrEmpty(lastRequestResult.RunBonusSummary))
            {
                builder.AppendLine(lastRequestResult.RunBonusSummary);
            }

            _vm.Batch(() =>
            {
                _vm.SetBattleText(
                    _vm.State.StatusText,
                    builder.ToString(),
                    $"ATK {lastRequestResult.AttackPower}",
                    _vm.State.EnemyIntentText);
                _vm.SetSlotOutcome(
                    hasPattern,
                    patternResult != null ? patternResult.Row : -1,
                    patternResult != null ? patternResult.StartColumn : -1,
                    patternResult != null ? patternResult.MatchLength : 0);
            });
        }

        internal void UpdatePlayerHud(CombatParticipant player, CombatViewModel combatViewModel)
        {
            _vm.SetPlayerHud(
                $"{combatViewModel.PlayerHp}/{player.MaxHp}",
                combatViewModel.PlayerHp,
                player.MaxHp,
                combatViewModel.PlayerShield,
                Mathf.Max(1, player.MaxHp));
        }

        internal void UpdateBattleTextMeta(string statusText, string enemyIntentText)
        {
            _vm.SetBattleText(
                statusText,
                _vm.State.SlotResultText,
                _vm.State.AttackResultText,
                enemyIntentText);
        }

        internal int[] UpdateEnemySlots(
            BattleSystem battle,
            CombatViewModel combatViewModel,
            EnemyVisibleIntentState enemyVisibleIntentState,
            string encounterDisplayName,
            int viewSlotCount,
            RunEncounterRoster encounterRoster,
            CombatParticipantId selectedEnemyId,
            bool isBusy,
            bool isSpinRunning)
        {
            int enemyCount = battle.Enemies.Count;
            var computedSlotIndices = new int[enemyCount];
            var usedFormationSlots = new HashSet<int>();

            for (int index = 0; index < enemyCount; index++)
            {
                computedSlotIndices[index] = ResolveHudSlotIndex(
                    encounterRoster, index, viewSlotCount, usedFormationSlots);
            }

            _vm.Batch(() =>
            {
                _vm.ClearEnemySlots();
                for (int index = 0; index < enemyCount; index++)
                {
                    CombatParticipant enemy = battle.Enemies[index];
                    int slotIndex = computedSlotIndices[index];
                    CombatParticipantSnapshot snapshot = combatViewModel.TryGetParticipantSnapshot(enemy.Id, out CombatParticipantSnapshot s)
                                                       ? s
                                                       : new CombatParticipantSnapshot(enemy.CurrentHp, enemy.Shield);
                    bool selected = selectedEnemyId.IsValid && selectedEnemyId.Value == enemy.Id.Value;
                    string deadSuffix = enemy.IsDead ? " [DOWN]" : string.Empty;
                    IReadOnlyList<EnemyUpcomingActionViewData> upcomingActions =
                        enemyVisibleIntentState?.GetActions(enemy.Id) ?? System.Array.Empty<EnemyUpcomingActionViewData>();
                    _vm.SetEnemySlot(
                        slotIndex,
                        enemy.Id,
                        $"{encounterDisplayName} #{index + 1}{deadSuffix}\n{snapshot.Hp}/{enemy.MaxHp}",
                        snapshot.Hp,
                        enemy.MaxHp,
                        snapshot.Shield,
                        selected,
                        !enemy.IsDead && !isBusy && !isSpinRunning,
                        BuildStatusViewData(enemy.StatusEffects),
                        upcomingActions);
                }
            });

            return computedSlotIndices;
        }

        private static StatusEffectViewData[] BuildStatusViewData(
            IReadOnlyList<StatusEffectInstance> statusEffects)
        {
            if (statusEffects == null || statusEffects.Count == 0)
            {
                return System.Array.Empty<StatusEffectViewData>();
            }

            var statuses = new StatusEffectViewData[statusEffects.Count];
            for (int index = 0; index < statusEffects.Count; index++)
            {
                StatusEffectInstance status = statusEffects[index];
                statuses[index] = new StatusEffectViewData(
                    status.Kind,
                    status.RemainingTurns,
                    status.Magnitude,
                    status.StackCount);
            }

            return statuses;
        }

        internal static int ResolveHudSlotIndex(
            RunEncounterRoster encounterRoster,
            int rosterIndex,
            int slotCount,
            HashSet<int> usedFormationSlots)
        {
            int slotIndex = RunEncounterRosterBuilder.ResolveFormationSlot(encounterRoster, rosterIndex, slotCount);
            if (!usedFormationSlots.Add(slotIndex))
            {
                Debug.LogWarning(
                    $"[RunBattleScreenStateUpdater] Duplicate formation slot {slotIndex} for roster index {rosterIndex}; using roster index.");
                slotIndex = Mathf.Clamp(rosterIndex, 0, slotCount - 1);
                usedFormationSlots.Add(slotIndex);
            }

            return slotIndex;
        }

        internal static string FormatVisibleEnemyAction(
            EnemyVisibleIntentState enemyVisibleIntentState,
            CombatParticipantId participantId)
        {
            IReadOnlyList<EnemyUpcomingActionViewData> actions =
                enemyVisibleIntentState?.GetActions(participantId) ??
                System.Array.Empty<EnemyUpcomingActionViewData>();
            if (actions.Count == 0)
            {
                return "none";
            }

            EnemyUpcomingActionViewData action = actions[0];
            return $"{action.Kind} {action.Amount}";
        }

        internal static string FormatRequest(SlotCombatRequest request)
        {
            return $"DMG {request.Damage} / DEF {request.Defense}\nHIT {request.AttackCount} / HEAL {request.HealAmount}";
        }

        internal static string FormatSlotSymbol(SlotSymbolType symbol)
        {
            switch (symbol)
            {
                case SlotSymbolType.Cherry: return "CHERRY";
                case SlotSymbolType.Seven: return "SEVEN";
                case SlotSymbolType.Diamond: return "DIAMOND";
                case SlotSymbolType.Bell: return "BELL";
                case SlotSymbolType.Clover: return "CLOVER";
                case SlotSymbolType.Lemon: return "LEMON";
                default: return symbol.ToString().ToUpperInvariant();
            }
        }
    }
}
