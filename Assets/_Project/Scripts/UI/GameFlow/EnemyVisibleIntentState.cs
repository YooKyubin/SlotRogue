using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyVisibleIntentState
    {
        private static readonly EnemyUpcomingActionViewData[] EmptyActions = Array.Empty<EnemyUpcomingActionViewData>();

        private readonly Dictionary<int, List<EnemyUpcomingActionViewData>> _actionsByEnemyId = new();

        public void RefreshFromBattle(
            BattleSystem battle,
            IReadOnlyList<CombatParticipant> enemies,
            RunEncounterRoster encounterRoster)
        {
            _actionsByEnemyId.Clear();
            if (battle == null || enemies == null)
            {
                return;
            }

            for (int index = 0; index < enemies.Count; index++)
            {
                CombatParticipant enemy = enemies[index];
                if (enemy == null || !enemy.Id.IsValid)
                {
                    continue;
                }

                EnemyActionPresentationMap presentationMap =
                    ResolvePresentationMap(encounterRoster, enemy.Id);
                if (!battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn))
                {
                    _actionsByEnemyId[enemy.Id.Value] = new List<EnemyUpcomingActionViewData>();
                    continue;
                }

                IReadOnlyList<EnemyPlannedAction> plannedActions = upcomingTurn.Plan.Actions;
                if (plannedActions.Count == 0)
                {
                    _actionsByEnemyId[enemy.Id.Value] = new List<EnemyUpcomingActionViewData>();
                    continue;
                }

                _actionsByEnemyId[enemy.Id.Value] = BuildActions(plannedActions, presentationMap);
            }
        }

        public void ConsumeFirstAction(CombatParticipantId enemyId)
        {
            if (!enemyId.IsValid ||
                !_actionsByEnemyId.TryGetValue(enemyId.Value, out List<EnemyUpcomingActionViewData> actions) ||
                actions.Count == 0)
            {
                return;
            }

            actions.RemoveAt(0);
        }

        public IReadOnlyList<EnemyUpcomingActionViewData> GetActions(CombatParticipantId enemyId)
        {
            if (!enemyId.IsValid ||
                !_actionsByEnemyId.TryGetValue(enemyId.Value, out List<EnemyUpcomingActionViewData> actions) ||
                actions.Count == 0)
            {
                return EmptyActions;
            }

            return actions;
        }

        public void Clear()
        {
            _actionsByEnemyId.Clear();
        }

        private static List<EnemyUpcomingActionViewData> BuildActions(
            IReadOnlyList<EnemyPlannedAction> actions,
            EnemyActionPresentationMap presentationMap)
        {
            var viewData = new List<EnemyUpcomingActionViewData>(actions.Count);
            for (int index = 0; index < actions.Count; index++)
            {
                EnemyPlannedAction action = actions[index];
                bool hasCombatEffect = TryFindRepresentativeCombatEffect(action, out CombatEffect representativeEffect);
                EnemyUpcomingActionKind kind = hasCombatEffect
                    ? ToUpcomingActionKind(representativeEffect.Kind)
                    : EnemyUpcomingActionKind.Special;
                int amount = hasCombatEffect ? representativeEffect.Amount : 0;
                string displayName = string.Empty;
                UnityEngine.Sprite intentIcon = null;

                if (presentationMap != null &&
                    presentationMap.TryGet(action.ActionKey, out EnemyActionPresentation presentation))
                {
                    displayName = presentation.DisplayName;
                    intentIcon = presentation.IntentIcon;
                }

                viewData.Add(new EnemyUpcomingActionViewData(kind, amount, displayName, intentIcon));
            }

            return viewData;
        }

        private static EnemyActionPresentationMap ResolvePresentationMap(
            RunEncounterRoster encounterRoster,
            CombatParticipantId enemyId)
        {
            if (encounterRoster == null || !enemyId.IsValid)
            {
                return EnemyActionPresentationMap.Empty;
            }

            for (int index = 0; index < encounterRoster.Enemies.Count; index++)
            {
                EnemyEncounterUnit unit = encounterRoster.Enemies[index];
                if (unit.Combatant.Participant.Id.Value == enemyId.Value)
                {
                    return unit.PresentationMap;
                }
            }

            return EnemyActionPresentationMap.Empty;
        }

        private static bool TryFindRepresentativeCombatEffect(
            EnemyPlannedAction action,
            out CombatEffect combatEffect)
        {
            if (action == null)
            {
                combatEffect = default;
                return false;
            }

            IReadOnlyList<EnemyActionEffect> effects = action.Effects;
            for (int index = 0; index < effects.Count; index++)
            {
                EnemyActionEffect effect = effects[index];
                if (effect.Kind == EnemyActionEffectKind.Combat)
                {
                    combatEffect = effect.CombatEffect;
                    return true;
                }
            }

            combatEffect = default;
            return false;
        }

        private static EnemyUpcomingActionKind ToUpcomingActionKind(CombatEffectKind kind)
        {
            switch (kind)
            {
                case CombatEffectKind.Damage:
                    return EnemyUpcomingActionKind.Damage;
                case CombatEffectKind.Shield:
                    return EnemyUpcomingActionKind.Shield;
                case CombatEffectKind.Heal:
                    return EnemyUpcomingActionKind.Heal;
                case CombatEffectKind.ApplyStatus:
                    return EnemyUpcomingActionKind.ApplyStatus;
                default:
                    return EnemyUpcomingActionKind.Special;
            }
        }
    }
}
