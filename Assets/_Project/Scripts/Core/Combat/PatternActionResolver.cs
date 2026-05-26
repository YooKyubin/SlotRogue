using SlotRogue.Data.Combat;

namespace SlotRogue.Core.Combat
{
    public static class PatternActionResolver
    {
        public static MonsterAction Resolve(PatternStep step)
        {
            var definition = step?.Action;
            if (definition == null)
            {
                throw new System.ArgumentException("Pattern step action is missing.", nameof(step));
            }
            var rawAttack = definition.RawAttack;

            if (step.OverrideRawAttack && definition.Kind == MonsterActionKind.Attack)
            {
                rawAttack = step.OverrideRawAttackValue;
            }

            return new MonsterAction(
                definition.Kind,
                rawAttack,
                definition.DefendValue,
                definition.BuffId);
        }
    }
}
