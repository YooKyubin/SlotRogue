using System;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public sealed class LockSlotEffectDefinition : EnemyEffectDefinition
    {
        [SerializeField] private int _lockCount;
        [SerializeField] private int _durationTurns;

        public LockSlotEffectDefinition()
        {
        }

        public LockSlotEffectDefinition(int lockCount, int durationTurns)
        {
            _lockCount = lockCount;
            _durationTurns = durationTurns;
        }

        public int LockCount => _lockCount;

        public int DurationTurns => _durationTurns;
    }
}
