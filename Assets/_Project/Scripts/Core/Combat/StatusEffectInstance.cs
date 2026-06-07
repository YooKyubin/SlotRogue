using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class StatusEffectInstance
    {
        internal StatusEffectInstance(
            StatusEffectKind kind,
            int remainingTurns,
            int magnitude,
            int stackCount,
            IReadOnlyList<IStatusEffectComponent> components)
        {
            Kind = kind;
            RemainingTurns = remainingTurns;
            Magnitude = magnitude;
            StackCount = stackCount;
            Components = components;
        }

        public StatusEffectKind Kind { get; }

        public int RemainingTurns { get; internal set; }

        public int Magnitude { get; internal set; }

        public int StackCount { get; internal set; }

        internal IReadOnlyList<IStatusEffectComponent> Components { get; }
    }
}
