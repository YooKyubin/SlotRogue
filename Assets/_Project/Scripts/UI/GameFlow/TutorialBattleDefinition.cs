using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    internal static class TutorialBattleDefinition
    {
        internal const int PlayerMaxHp = 20;

        internal const int LeftMonsterRosterIndex = 0;
        internal const int LeftMonsterFormationSlot = 0;
        internal const int LeftMonsterMaxHp = 6;
        internal const int LeftMonsterAttack = 4;

        internal const int RightMonsterRosterIndex = 1;
        internal const int RightMonsterFormationSlot = 2;
        internal const int RightMonsterMaxHp = 8;
        internal const int RightMonsterShield = 3;
        internal const int RightMonsterAttack = 4;

        internal const int TrainingBatteryRequiredCount = 3;
        internal const int TrainingBatteryDamage = 3;
        internal const int TrainingBatteryBlock = 4;
        internal const string TrainingBatteryRelicId = "TUTORIAL-TRAINING-BATTERY";
        internal const string TrainingBatteryRelicName = "훈련용 배터리";
        internal const string TrainingBatteryDescription =
            "체리 3개 이상: 추가 피해 +3\n레몬 3개 이상: 보호막 +4";

        internal static RelicDefinition TrainingBatteryRelic { get; } =
            new(
                TrainingBatteryRelicId,
                RelicGrade.Starter,
                TrainingBatteryRelicName,
                RelicIconKeys.Slot06,
                RelicRole.Utility,
                RelicTriggerType.Passive,
                RelicEffectType.Special,
                triggerSymbol: null,
                triggerTag: null,
                requiredCount: TrainingBatteryRequiredCount,
                effectValue: TrainingBatteryDamage,
                effectValue2: TrainingBatteryBlock,
                enemyHpBelowPercent: 0,
                playerHpBelowPercentForBonus: 0,
                enemyStatusRequirement: EnemyStatusRequirement.None,
                isStarter: false,
                phase1: true,
                description: TrainingBatteryDescription,
                intent: "체리/레몬 족보에 반응하는 튜토리얼 전용 유물",
                qaRisk: "튜토리얼 런에서만 지급");

        internal static bool TryGetRelicDescription(string relicId, out string description)
        {
            if (relicId == TrainingBatteryRelicId)
            {
                description = TrainingBatteryDescription;
                return true;
            }

            description = string.Empty;
            return false;
        }
    }
}
