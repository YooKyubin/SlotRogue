using System;

namespace SlotRogue.Core.Combat
{
    public sealed class EncounterScaling
    {
        private readonly EncounterBalanceConfig _config;

        public EncounterScaling(EncounterBalanceConfig config)
        {
            _config = config;
        }

        public EncounterScaleResult Scale(EncounterScaleRequest request)
        {
            float battleGrowth = (request.BattleNumber - 1) * _config.HpIncreasePerBattle;
            float themeSectionGrowth = request.ThemeSectionIndex * _config.HpIncreasePerThemeSection;
            float growthMultiplier = 1f + battleGrowth + themeSectionGrowth;
            float scaledHp = request.BaseMaxHp * growthMultiplier * request.TierHpMultiplier;
            int maxHp = Math.Max(1, (int)Math.Round(scaledHp, MidpointRounding.AwayFromZero));
            return new EncounterScaleResult(maxHp);
        }
    }
}
