using System;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 진행 중인 런을 디스크에 저장하기 위한 직렬화 DTO입니다(JsonUtility 호환: public 필드만).
    /// 전투 내부 상태는 담지 않습니다. GameFlowSession은 전투 경계에서만 갱신되므로
    /// 이 스냅샷은 항상 "현재 전투 시작" 시점의 일관된 상태를 나타냅니다.
    /// </summary>
    [Serializable]
    public sealed class RunSaveData
    {
        public int version;
        public bool isTutorialRun;
        public bool isInfiniteMode;
        public int playerMaxHp;
        public int playerCurrentHp;
        public int battleIndex;
        public int currentBattleNumber;
        public int runSeed;
        public int victories;
        public int rewardsClaimed;
        public int damageBonus;
        public int defenseBonus;
        public int runCoins;
        public int relicSlotCapacity;
        public bool hasRevivedThisRun;
        public string[] relicIds;
        public int[] symbolTypes;
        public int[] symbolBaseDamageBonuses;

        // 호환을 위해 필드명은 유지하지만 값의 의미는 심볼별 한 칸 출현 확률값이다.
        public int[] symbolCounts;
    }
}
