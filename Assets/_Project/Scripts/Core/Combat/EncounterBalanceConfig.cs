using System;

namespace SlotRogue.Core.Combat
{
    public readonly struct EncounterBalanceConfig
    {
        public EncounterBalanceConfig(
            float hpIncreasePerBattle,
            float hpIncreasePerCycle,
            float normalTierHpMultiplier,
            float eliteTierHpMultiplier,
            float bossTierHpMultiplier)
        {
            if (hpIncreasePerBattle < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hpIncreasePerBattle));
            }

            if (hpIncreasePerCycle < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hpIncreasePerCycle));
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
            HpIncreasePerCycle = hpIncreasePerCycle;
            NormalTierHpMultiplier = normalTierHpMultiplier;
            EliteTierHpMultiplier = eliteTierHpMultiplier;
            BossTierHpMultiplier = bossTierHpMultiplier;
        }

        public float HpIncreasePerBattle { get; }

        public float HpIncreasePerCycle { get; }

        public float NormalTierHpMultiplier { get; }

        public float EliteTierHpMultiplier { get; }

        public float BossTierHpMultiplier { get; }
    }
}
