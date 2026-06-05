namespace SlotRogue.Core.Combat
{
    public sealed class StatusEffectFactory
    {
        public StatusEffectInstance Create(StatusEffectSpec spec)
        {
            return spec.Kind switch
            {
                StatusEffectKind.Burn => new StatusEffectInstance(
                    spec.Kind,
                    spec.Duration,
                    spec.Magnitude,
                    stackCount: 1,
                    new IStatusEffectComponent[]
                    {
                        new PeriodicDamageComponent(StatusDamageMode.FixedMagnitude),
                        new DurationComponent(),
                    }),
                StatusEffectKind.Freeze => new StatusEffectInstance(
                    spec.Kind,
                    spec.Duration,
                    spec.Magnitude,
                    stackCount: 1,
                    new IStatusEffectComponent[]
                    {
                        new SkipActionComponent(),
                        new DurationComponent(),
                    }),
                StatusEffectKind.Poison => new StatusEffectInstance(
                    spec.Kind,
                    remainingTurns: 0,
                    magnitude: 1,
                    stackCount: spec.Magnitude > 0 ? spec.Magnitude : 1,
                    new IStatusEffectComponent[]
                    {
                        new StackLimitComponent(5),
                        new PeriodicDamageComponent(StatusDamageMode.StackCount),
                    }),
                _ => null,
            };
        }
    }
}
