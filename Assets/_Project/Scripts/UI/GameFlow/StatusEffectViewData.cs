using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public readonly struct StatusEffectViewData
    {
        public StatusEffectViewData(
            StatusEffectKind kind,
            int remainingTurns,
            int magnitude,
            int stackCount)
        {
            Kind = kind;
            RemainingTurns = remainingTurns;
            Magnitude = magnitude;
            StackCount = stackCount;
        }

        public StatusEffectKind Kind { get; }

        public int RemainingTurns { get; }

        public int Magnitude { get; }

        public int StackCount { get; }

    }
}
