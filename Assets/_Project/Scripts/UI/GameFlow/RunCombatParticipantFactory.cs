using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public static class RunCombatParticipantFactory
    {
        private const int PlayerIdValue = 1;
        private const int EnemyIdBaseValue = 100;

        public static CombatParticipant CreatePlayer(int maxHp, int currentHp)
        {
            return new CombatParticipant(
                maxHp,
                currentHp,
                shield: 0,
                id: new CombatParticipantId(PlayerIdValue),
                team: CombatTeam.Player);
        }

        public static CombatParticipant CreateEnemy(int rosterIndex, int maxHp)
        {
            return new CombatParticipant(
                maxHp,
                currentHp: maxHp,
                shield: 0,
                id: new CombatParticipantId(EnemyIdBaseValue + rosterIndex),
                team: CombatTeam.Enemy);
        }
    }
}
