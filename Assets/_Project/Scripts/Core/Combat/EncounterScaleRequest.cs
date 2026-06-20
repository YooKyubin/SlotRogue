using System;

namespace SlotRogue.Core.Combat
{
    public readonly struct EncounterScaleRequest
    {
        public int BaseMaxHp { get; }
        public int BattleNumber { get; }
        public int ThemeSectionIndex { get; }
        public float TierHpMultiplier { get; }

        public EncounterScaleRequest(
            int baseMaxHp,
            int battleNumber,
            int themeSectionIndex,
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

            if (themeSectionIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(themeSectionIndex));
            }

            if (tierHpMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tierHpMultiplier));
            }

            BaseMaxHp = baseMaxHp;
            BattleNumber = battleNumber;
            ThemeSectionIndex = themeSectionIndex;
            TierHpMultiplier = tierHpMultiplier;
        }
    }
}
