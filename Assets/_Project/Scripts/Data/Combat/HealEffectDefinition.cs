using System;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public sealed class HealEffectDefinition : EnemyEffectDefinition
    {
        [SerializeField] private int _amount;
        [SerializeField] private CombatEffectTargetDefinition _target;

        public HealEffectDefinition()
        {
            _target = new CombatEffectTargetDefinition(CombatTargetMode.Self);
        }

        public HealEffectDefinition(int amount, CombatEffectTargetDefinition target)
        {
            _amount = amount;
            _target = target;
        }

        public int Amount => _amount;

        public CombatEffectTargetDefinition Target => _target;
    }
}
