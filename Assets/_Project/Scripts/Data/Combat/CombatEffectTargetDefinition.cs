using System;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public struct CombatEffectTargetDefinition
    {
        [SerializeField] private CombatTargetMode _targetMode;
        [SerializeField] private int _targetParticipantId;

        public CombatEffectTargetDefinition(CombatTargetMode targetMode, int targetParticipantId = 0)
        {
            _targetMode = targetMode;
            _targetParticipantId = targetParticipantId;
        }

        public CombatTargetMode TargetMode => _targetMode;

        public int TargetParticipantId => _targetParticipantId;
    }
}
