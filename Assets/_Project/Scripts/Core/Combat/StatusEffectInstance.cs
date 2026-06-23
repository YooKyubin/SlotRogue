using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class StatusEffectInstance
    {
        private int _remainingTurns;
        private int _magnitude;
        private int _stackCount;

        internal StatusEffectInstance(
            StatusEffectKind kind,
            int remainingTurns,
            int magnitude,
            int stackCount,
            IReadOnlyList<IStatusEffectComponent> components)
        {
            Kind = kind;
            _remainingTurns = remainingTurns;
            _magnitude = magnitude;
            _stackCount = stackCount;
            Components = components;
        }

        public StatusEffectKind Kind { get; }

        public int RemainingTurns
        {
            get => _remainingTurns;
            internal set
            {
                if (_remainingTurns == value)
                {
                    return;
                }

                _remainingTurns = value;
                Revision++;
            }
        }

        public int Magnitude
        {
            get => _magnitude;
            internal set
            {
                if (_magnitude == value)
                {
                    return;
                }

                _magnitude = value;
                Revision++;
            }
        }

        public int StackCount
        {
            get => _stackCount;
            internal set
            {
                if (_stackCount == value)
                {
                    return;
                }

                _stackCount = value;
                Revision++;
            }
        }

        internal int Revision { get; private set; }

        internal IReadOnlyList<IStatusEffectComponent> Components { get; }
    }
}
