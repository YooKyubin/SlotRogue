using SlotRogue.Data.Combat;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleState
    {
        public int PlayerHp { get; internal set; }

        public int PlayerMaxHp { get; internal set; }

        public int MonsterHp { get; internal set; }

        public int MonsterMaxHp { get; internal set; }

        public int PatternIndex { get; internal set; }

        public int PendingMonsterDefense { get; internal set; }

        public BattleEndReason EndReason { get; internal set; }

        public bool IsBattleOver => EndReason != BattleEndReason.None;

        public static BattleState Begin(MonsterDefinition monster, int playerMaxHp)
        {
            return new BattleState
            {
                PlayerHp = playerMaxHp,
                PlayerMaxHp = playerMaxHp,
                MonsterHp = monster.MaxHp,
                MonsterMaxHp = monster.MaxHp,
                PatternIndex = 0,
                PendingMonsterDefense = 0,
                EndReason = BattleEndReason.None
            };
        }
    }
}
