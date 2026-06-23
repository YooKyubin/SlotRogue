using System;

namespace SlotRogue.Core.Combat
{
    public readonly struct EncounterBalanceConfig
    {
        public float HpIncreasePerBattle { get; }
        public float HpIncreasePerThemeSection { get; }
        public float NormalTierHpMultiplier { get; }
        public float EliteTierHpMultiplier { get; }
        public float BossTierHpMultiplier { get; }

        public EncounterBalanceConfig(
            float hpIncreasePerBattle,
            float hpIncreasePerThemeSection,
            float normalTierHpMultiplier,
            float eliteTierHpMultiplier,
            float bossTierHpMultiplier)
        {
            if (hpIncreasePerBattle < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hpIncreasePerBattle));
            }

            if (hpIncreasePerThemeSection < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hpIncreasePerThemeSection));
            }

            if (normalTierHpMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(normalTierHpMultiplier));
            }

            if (eliteTierHpMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(eliteTierHpMultiplier));
            }

            if (bossTierHpMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bossTierHpMultiplier));
            }

            HpIncreasePerBattle = hpIncreasePerBattle;
            HpIncreasePerThemeSection = hpIncreasePerThemeSection;
            NormalTierHpMultiplier = normalTierHpMultiplier;
            EliteTierHpMultiplier = eliteTierHpMultiplier;
            BossTierHpMultiplier = bossTierHpMultiplier;
        }
    }
}
