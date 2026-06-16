using System;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public sealed class ShieldEffectDefinition : EnemyEffectDefinition
    {
        [SerializeField] private int _amount;
        [SerializeField] private CombatEffectTargetDefinition _target;

        public ShieldEffectDefinition()
        {
            _target = new CombatEffectTargetDefinition(CombatTargetMode.Self);
        }

        public ShieldEffectDefinition(int amount, CombatEffectTargetDefinition target)
        {
            _amount = amount;
            _target = target;
        }

        public int Amount => _amount;

        public CombatEffectTargetDefinition Target => _target;
    }
}
