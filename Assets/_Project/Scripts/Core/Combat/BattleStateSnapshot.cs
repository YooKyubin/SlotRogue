namespace SlotRogue.Core.Combat
{
    public readonly struct BattleStateSnapshot
    {
        public BattleStateSnapshot(
            int playerHp,
            int playerMaxHp,
            int monsterHp,
            int monsterMaxHp,
            int patternIndex,
            BattleEndReason endReason)
        {
            PlayerHp = playerHp;
            PlayerMaxHp = playerMaxHp;
            MonsterHp = monsterHp;
            MonsterMaxHp = monsterMaxHp;
            PatternIndex = patternIndex;
            EndReason = endReason;
        }

        public int PlayerHp { get; }

        public int PlayerMaxHp { get; }

        public int MonsterHp { get; }

        public int MonsterMaxHp { get; }

        public int PatternIndex { get; }

        public BattleEndReason EndReason { get; }

        public bool IsBattleOver => EndReason != BattleEndReason.None;

        public static BattleStateSnapshot From(BattleState state)
        {
            return new BattleStateSnapshot(
                state.PlayerHp,
                state.PlayerMaxHp,
                state.MonsterHp,
                state.MonsterMaxHp,
                state.PatternIndex,
                state.EndReason);
        }
    }
}
