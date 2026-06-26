using System;

namespace SlotRogue.Core.Combat
{
    public sealed class SystemCombatRandom : ICombatRandom
    {
        private const int PercentScale = 100;
        private readonly Random _random;

        public SystemCombatRandom()
            : this(new Random())
        {
        }

        public SystemCombatRandom(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool RollPercent(int successPercent)
        {
            if (successPercent <= 0)
            {
                return false;
            }

            if (successPercent >= PercentScale)
            {
                return true;
            }

            return _random.Next(PercentScale) < successPercent;
        }

        public int NextIndex(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return _random.Next(count);
        }
    }
}
