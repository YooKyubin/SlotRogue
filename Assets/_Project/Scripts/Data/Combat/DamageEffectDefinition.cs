using System;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public sealed class DamageEffectDefinition : EnemyEffectDefinition
    {
        [SerializeField] private int _amount;
        [SerializeField] private CombatEffectTargetDefinition _target;

        public DamageEffectDefinition()
        {
            _target = new CombatEffectTargetDefinition(CombatTargetMode.SelectedEnemy);
        }

        public DamageEffectDefinition(int amount, CombatEffectTargetDefinition target)
        {
            _amount = amount;
            _target = target;
        }

        public int Amount => _amount;

        public CombatEffectTargetDefinition Target => _target;
    }
}
