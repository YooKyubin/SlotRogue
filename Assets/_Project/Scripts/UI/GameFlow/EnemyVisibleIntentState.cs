using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyVisibleIntentState
    {
        private static readonly EnemyUpcomingActionViewData[] EmptyActions = Array.Empty<EnemyUpcomingActionViewData>();

        private readonly Dictionary<int, List<EnemyUpcomingActionViewData>> _actionsByEnemyId = new();

        public void RefreshFromBattle(BattleSystem battle, IReadOnlyList<CombatParticipant> enemies)
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

                if (!battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn))
                {
                    _actionsByEnemyId[enemy.Id.Value] = new List<EnemyUpcomingActionViewData>();
                    continue;
                }

                IReadOnlyList<CombatEffect> plannedEffects = upcomingTurn.Plan.Effects;
                if (plannedEffects.Count == 0)
                {
                    _actionsByEnemyId[enemy.Id.Value] = new List<EnemyUpcomingActionViewData>();
                    continue;
                }

                _actionsByEnemyId[enemy.Id.Value] = BuildActions(plannedEffects);
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

        private static List<EnemyUpcomingActionViewData> BuildActions(IReadOnlyList<CombatEffect> effects)
        {
            var actions = new List<EnemyUpcomingActionViewData>(effects.Count);
            for (int index = 0; index < effects.Count; index++)
            {
                CombatEffect effect = effects[index];
                actions.Add(new EnemyUpcomingActionViewData(ToUpcomingActionKind(effect.Kind), effect.Amount));
            }

            return actions;
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
