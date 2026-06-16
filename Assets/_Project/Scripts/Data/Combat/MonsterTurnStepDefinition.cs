using System;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public struct MonsterTurnStepDefinition
    {
        public EnemyActionDefinition[] actions;
    }
}
