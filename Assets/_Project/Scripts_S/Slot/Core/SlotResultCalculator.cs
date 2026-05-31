using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Core
{
    public sealed class SlotResultCalculator
    {
        public SlotCalculationResult Calculate(SlotPatternResult patternResult)
        {
            if (patternResult == null || !patternResult.HasMatch)
            {
                return new SlotCalculationResult(
                    SlotCombatRequest.BaseAttackDamage,
                    0,
                    SlotCombatRequest.BaseAttackCount,
                    0,
                    false);
            }

            int matchLength = patternResult.MatchLength;
            int damage = 0;
            int defense = 0;
            int attackCount = 0;
            int healAmount = 0;
            bool isCritical = false;

            switch (patternResult.Symbol)
            {
                case SlotSymbolType.Sword:
                    damage = matchLength * 6;
                    attackCount = 1 + (matchLength - 3);
                    isCritical = matchLength >= 5;
                    break;
                case SlotSymbolType.Shield:
                    damage = matchLength * 2;
                    defense = matchLength * 5;
                    attackCount = 1;
                    break;
                case SlotSymbolType.Heart:
                    damage = matchLength * 2;
                    healAmount = matchLength * 4;
                    attackCount = 1;
                    break;
                case SlotSymbolType.Coin:
                    damage = matchLength * 3;
                    defense = matchLength;
                    attackCount = 1;
                    break;
                case SlotSymbolType.Gem:
                    damage = matchLength * 5;
                    attackCount = 1;
                    isCritical = matchLength >= 4;
                    break;
                case SlotSymbolType.Skull:
                    damage = matchLength * 4;
                    attackCount = matchLength - 2;
                    isCritical = matchLength >= 5;
                    break;
            }

            if (isCritical)
            {
                damage *= 2;
            }

            return new SlotCalculationResult(damage, defense, attackCount, healAmount, isCritical);
        }
    }
}
