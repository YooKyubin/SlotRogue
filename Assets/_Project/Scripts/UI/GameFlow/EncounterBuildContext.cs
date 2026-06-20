using System;
using SlotRogue.Core.Combat;
using SlotRogue.Data.GameFlow;

namespace SlotRogue.UI.GameFlow
{
    public readonly struct EncounterBuildContext
    {
        public EncounterBuildContext(
            EncounterTier tier,
            int battleNumber,
            int cycle)
        {
            if (battleNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(battleNumber));
            }

            if (cycle < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cycle));
            }

            Tier = tier;
            BattleNumber = battleNumber;
            Cycle = cycle;
        }

        public EncounterTier Tier { get; }

        public int BattleNumber { get; }

        public int Cycle { get; }

        public float ResolveTierHpMultiplier(EncounterBalanceConfig config)
        {
            return Tier switch
            {
                EncounterTier.Elite => config.EliteTierHpMultiplier,
                EncounterTier.Boss => config.BossTierHpMultiplier,
                _ => config.NormalTierHpMultiplier,
            };
        }
    }
}
