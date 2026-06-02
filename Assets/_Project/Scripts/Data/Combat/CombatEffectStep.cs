using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public struct CombatEffectStep
    {
        public CombatEffectKind kind;

        public int amount;

        public CombatTargetMode targetMode;

        public int targetParticipantId;

        public CombatEffect ToCombatEffect() => new(kind, amount, BuildTarget());

        private CombatEffectTarget BuildTarget()
        {
            if (targetMode == CombatTargetMode.Self)
            {
                return CombatEffectTarget.Self;
            }

            if (targetParticipantId > 0)
            {
                return CombatEffectTarget.SelectedEnemy(new CombatParticipantId(targetParticipantId));
            }

            return targetMode switch
            {
                CombatTargetMode.AllEnemies => new CombatEffectTarget(CombatTargetMode.AllEnemies),
                CombatTargetMode.RandomEnemy => new CombatEffectTarget(CombatTargetMode.RandomEnemy),
                _ => CombatEffectTarget.Enemy,
            };
        }
    }
}
