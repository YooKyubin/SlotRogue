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
                    remainingTurns: 0,
                    spec.Magnitude,
                    stackCount: 0,
                    new IStatusEffectComponent[]
                    {
                        new BurnOnAppliedDamageComponent(),
                        new BurnOnTeamTurnEndedComponent(),
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
                StatusEffectKind.Infection => new StatusEffectInstance(
                    spec.Kind,
                    remainingTurns: 0,
                    magnitude: 0,
                    stackCount: spec.Magnitude > 0 ? spec.Magnitude : 1,
                    new IStatusEffectComponent[]
                    {
                        new InfectionOnTeamTurnEndedComponent(),
                    }),
                StatusEffectKind.Vulnerable => new StatusEffectInstance(
                    spec.Kind,
                    remainingTurns: 0,
                    magnitude: 0,
                    stackCount: spec.Magnitude > 0 ? spec.Magnitude : 1,
                    new IStatusEffectComponent[]
                    {
                        new VulnerableDamageModifier(),
                        new VulnerableUsageHandler(),
                    }),
                StatusEffectKind.Weaken => new StatusEffectInstance(
                    spec.Kind,
                    remainingTurns: 0,
                    magnitude: 0,
                    stackCount: spec.Magnitude > 0 ? spec.Magnitude : 1,
                    new IStatusEffectComponent[]
                    {
                        new WeakenDamageModifier(),
                        new WeakenUsageHandler(),
                    }),
                StatusEffectKind.Lifesteal => new StatusEffectInstance(
                    spec.Kind,
                    remainingTurns: 0,
                    magnitude: 0,
                    stackCount: spec.Magnitude > 0 ? spec.Magnitude : 1,
                    new IStatusEffectComponent[]
                    {
                        new LifestealComponent(),
                        new LifestealUsageHandler(),
                    }),
                StatusEffectKind.Thorns => new StatusEffectInstance(
                    spec.Kind,
                    remainingTurns: 0,
                    magnitude: spec.Magnitude,
                    stackCount: 0,
                    new IStatusEffectComponent[]
                    {
                        new ThornsComponent(),
                    }),
                _ => null,
            };
        }
    }
}
