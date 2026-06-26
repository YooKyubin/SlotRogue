namespace SlotRogue.Core.Combat
{
    public sealed class LifestealComponent : StatusEffectComponent, IAfterHealthDamageDealt
    {
        private const int LifestealPercent = 20;
        private const int PercentScale = 100;

        public bool OnAfterHealthDamageDealt(AfterHealthDamageContext context)
        {
            if (context.DamageOrigin != DamageOrigin.DirectAction ||
                context.HealthDamage <= 0)
            {
                return false;
            }

            int healAmount =
                (context.HealthDamage * LifestealPercent + PercentScale - 1) /
                PercentScale;
            context.HealAttacker(healAmount);
            return true;
        }
    }
}
