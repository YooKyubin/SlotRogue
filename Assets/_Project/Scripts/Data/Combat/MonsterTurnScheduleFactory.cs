using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;

namespace SlotRogue.Data.Combat
{
    public static class MonsterTurnScheduleFactory
    {
        public static MonsterTurnSchedule FromPattern(MonsterTurnPatternDefinition pattern)
        {
            if (pattern == null)
            {
                return CreateDefaultFallback();
            }

            if (pattern.turns == null || pattern.turns.Length == 0)
            {
                return new MonsterTurnSchedule(Array.Empty<CombatEffect[]>());
            }

            return FromTurnSteps(pattern.turns);
        }

        public static MonsterTurnSchedule FromSteps(IReadOnlyList<IReadOnlyList<CombatEffectStep>> turns)
        {
            if (turns == null || turns.Count == 0)
            {
                return CreateDefaultFallback();
            }

            var turnSets = new CombatEffect[turns.Count][];
            for (int turnIndex = 0; turnIndex < turns.Count; turnIndex++)
            {
                turnSets[turnIndex] = ToCombatEffects(turns[turnIndex]);
            }

            return new MonsterTurnSchedule(turnSets);
        }

        public static MonsterTurnSchedule FromSteps(params CombatEffectStep[][] turns)
        {
            if (turns == null || turns.Length == 0)
            {
                return CreateDefaultFallback();
            }

            var turnSets = new CombatEffect[turns.Length][];
            for (int turnIndex = 0; turnIndex < turns.Length; turnIndex++)
            {
                turnSets[turnIndex] = ToCombatEffects(turns[turnIndex]);
            }

            return new MonsterTurnSchedule(turnSets);
        }

        public static MonsterTurnSchedule FromTurnSteps(MonsterTurnStepDefinition[] turns)
        {
            if (turns == null || turns.Length == 0)
            {
                return new MonsterTurnSchedule(Array.Empty<CombatEffect[]>());
            }

            var turnSets = new CombatEffect[turns.Length][];
            for (int turnIndex = 0; turnIndex < turns.Length; turnIndex++)
            {
                turnSets[turnIndex] = ToCombatEffects(turns[turnIndex].actions);
            }

            return new MonsterTurnSchedule(turnSets);
        }

        private static CombatEffect[] ToCombatEffects(IReadOnlyList<CombatEffectStep> steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return Array.Empty<CombatEffect>();
            }

            var effects = new CombatEffect[steps.Count];
            for (int index = 0; index < steps.Count; index++)
            {
                effects[index] = steps[index].ToCombatEffect();
            }

            return effects;
        }

        private static CombatEffect[] ToCombatEffects(CombatEffectStep[] steps)
        {
            if (steps == null || steps.Length == 0)
            {
                return Array.Empty<CombatEffect>();
            }

            var effects = new CombatEffect[steps.Length];
            for (int index = 0; index < steps.Length; index++)
            {
                effects[index] = steps[index].ToCombatEffect();
            }

            return effects;
        }

        private static MonsterTurnSchedule CreateDefaultFallback()
        {
            return new MonsterTurnSchedule(
                new[] { new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy) });
        }
    }
}
