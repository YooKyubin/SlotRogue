namespace SlotRogue.Core.Combat
{
    public sealed class PeriodicDamageComponent : StatusEffectComponent
    {
        private readonly StatusDamageMode _damageMode;

        public PeriodicDamageComponent(StatusDamageMode damageMode)
        {
            _damageMode = damageMode;
        }

        public override void OnTurnStart(StatusEffectContext context)
        {
            int damage = _damageMode == StatusDamageMode.StackCount
                ? context.Instance.StackCount
                : context.Instance.Magnitude;

            if (damage > 0)
            {
                context.DealDamage(damage);
            }
        }
    }
}
