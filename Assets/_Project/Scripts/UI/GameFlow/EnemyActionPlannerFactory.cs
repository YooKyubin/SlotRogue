using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyActionPlannerFactory
    {
        public IEnemyActionPlanner Create(MonsterTurnPatternDefinition pattern)
        {
            return Build(pattern).Planner;
        }

        public EnemyActionPlannerBuildResult Build(MonsterTurnPatternDefinition pattern)
        {
            if (pattern == null)
            {
                return BuildDefaultFallback();
            }

            if (pattern.turns == null || pattern.turns.Length == 0)
            {
                return BuildEmptyPlanner();
            }

            var plans = new EnemyActionPlan[pattern.turns.Length];
            var presentations = new List<EnemyActionPresentation>();
            int nextActionKey = 1;
            for (int turnIndex = 0; turnIndex < pattern.turns.Length; turnIndex++)
            {
                plans[turnIndex] = ToActionPlan(
                    pattern.turns[turnIndex].actions,
                    presentations,
                    ref nextActionKey);
            }

            return new EnemyActionPlannerBuildResult(
                new FixedSequenceEnemyActionPlanner(plans),
                new EnemyActionPresentationMap(presentations));
        }

        public IEnemyActionPlanner Create(IReadOnlyList<IReadOnlyList<CombatEffect>> turnEffects)
        {
            if (turnEffects == null || turnEffects.Count == 0)
            {
                return CreateDefaultFallback();
            }

            var plans = new EnemyActionPlan[turnEffects.Count];
            for (int turnIndex = 0; turnIndex < turnEffects.Count; turnIndex++)
            {
                plans[turnIndex] = new EnemyActionPlan(turnEffects[turnIndex]);
            }

            return new FixedSequenceEnemyActionPlanner(plans);
        }

        private static EnemyActionPlannerBuildResult BuildDefaultFallback()
        {
            return new EnemyActionPlannerBuildResult(
                CreateDefaultFallback(),
                EnemyActionPresentationMap.Empty);
        }

        private static EnemyActionPlannerBuildResult BuildEmptyPlanner()
        {
            return new EnemyActionPlannerBuildResult(
                CreateEmptyPlanner(),
                EnemyActionPresentationMap.Empty);
        }

        private static IEnemyActionPlanner CreateDefaultFallback()
        {
            return new FixedSequenceEnemyActionPlanner(new[]
            {
                new EnemyActionPlan(new[]
                {
                    new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
                }),
            });
        }

        private static IEnemyActionPlanner CreateEmptyPlanner()
        {
            return new FixedSequenceEnemyActionPlanner(new[]
            {
                new EnemyActionPlan(null),
            });
        }

        private static EnemyActionPlan ToActionPlan(
            IReadOnlyList<EnemyActionDefinition> actions,
            List<EnemyActionPresentation> presentations,
            ref int nextActionKey)
        {
            if (actions == null || actions.Count == 0)
            {
                return EnemyActionPlan.FromActions(null);
            }

            var plannedActions = new EnemyPlannedAction[actions.Count];
            for (int index = 0; index < actions.Count; index++)
            {
                EnemyActionDefinition action = actions[index];
                EnemyActionKey actionKey = new(nextActionKey++);
                if (action == null)
                {
                    plannedActions[index] = new EnemyPlannedAction(actionKey, null);
                    continue;
                }

                presentations.Add(new EnemyActionPresentation(
                    actionKey,
                    action.DisplayName,
                    action.IntentIcon));
                plannedActions[index] = new EnemyPlannedAction(
                    actionKey,
                    ToActionEffects(action.Effect));
            }

            return EnemyActionPlan.FromActions(plannedActions);
        }

        private static EnemyActionEffect[] ToActionEffects(EnemyEffectDefinition definition)
        {
            if (definition == null)
            {
                return Array.Empty<EnemyActionEffect>();
            }

            return TryToActionEffect(definition, out EnemyActionEffect effect)
                ? new[] { effect }
                : Array.Empty<EnemyActionEffect>();
        }

        private static bool TryToActionEffect(
            EnemyEffectDefinition definition,
            out EnemyActionEffect actionEffect)
        {
            switch (definition)
            {
                case DamageEffectDefinition damage:
                    actionEffect = EnemyActionEffect.FromCombatEffect(new CombatEffect(
                        CombatEffectKind.Damage,
                        damage.Amount,
                        ToCombatEffectTarget(damage.Target)));
                    return true;
                case ShieldEffectDefinition shield:
                    actionEffect = EnemyActionEffect.FromCombatEffect(new CombatEffect(
                        CombatEffectKind.Shield,
                        shield.Amount,
                        ToCombatEffectTarget(shield.Target)));
                    return true;
                case HealEffectDefinition heal:
                    actionEffect = EnemyActionEffect.FromCombatEffect(new CombatEffect(
                        CombatEffectKind.Heal,
                        heal.Amount,
                        ToCombatEffectTarget(heal.Target)));
                    return true;
                case LockSlotEffectDefinition lockSlot:
                    actionEffect = EnemyActionEffect.LockSlot(lockSlot.LockCount, lockSlot.DurationTurns);
                    return true;
                default:
                    actionEffect = default;
                    return false;
            }
        }

        private static CombatEffectTarget ToCombatEffectTarget(CombatEffectTargetDefinition target)
        {
            if (target.TargetMode == CombatTargetMode.Self)
            {
                return CombatEffectTarget.Self;
            }

            if (target.TargetParticipantId > 0)
            {
                return CombatEffectTarget.SelectedEnemy(new CombatParticipantId(target.TargetParticipantId));
            }

            return target.TargetMode switch
            {
                CombatTargetMode.AllEnemies => new CombatEffectTarget(CombatTargetMode.AllEnemies),
                CombatTargetMode.RandomEnemy => new CombatEffectTarget(CombatTargetMode.RandomEnemy),
                _ => CombatEffectTarget.Enemy,
            };
        }

    }
}
