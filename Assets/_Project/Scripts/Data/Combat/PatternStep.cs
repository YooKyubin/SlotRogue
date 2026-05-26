using System;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public sealed class PatternStep
    {
        public MonsterActionDefinition Action;

        [Tooltip("When set, replaces RawAttack for Attack steps only.")]
        public bool OverrideRawAttack;

        public int OverrideRawAttackValue;
    }
}
