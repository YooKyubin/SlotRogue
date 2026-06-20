using System;

namespace SlotRogue.Core.Combat
{
    public readonly struct EncounterScaleRequest
    {
        public EncounterScaleRequest(
            int baseMaxHp,
            int battleNumber,
            int cycle,
            float tierHpMultiplier)
        {
            if (baseMaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseMaxHp));
            }

            if (battleNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(battleNumber));
            }

            if (cycle < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cycle));
            }

            if (tierHpMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tierHpMultiplier));
            }

            BaseMaxHp = baseMaxHp;
            BattleNumber = battleNumber;
            Cycle = cycle;
            TierHpMultiplier = tierHpMultiplier;
        }

        public int BaseMaxHp { get; }

        public int BattleNumber { get; }

        public int Cycle { get; }

        public float TierHpMultiplier { get; }
    }
}
