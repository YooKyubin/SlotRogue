using System;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public abstract class EnemyEffectDefinition
    {
    }

    [Serializable]
    public sealed class StatusEffectDefinition : EnemyEffectDefinition
    {
        [SerializeField] private StatusEffectKind _statusKind;
        [SerializeField] private int _amount;
        [SerializeField] private CombatEffectTargetDefinition _target;

        public StatusEffectDefinition()
        {
            _target = new CombatEffectTargetDefinition(CombatTargetMode.SelectedEnemy);
        }

        public StatusEffectDefinition(
            StatusEffectKind statusKind,
            int amount,
            CombatEffectTargetDefinition target)
        {
            _statusKind = statusKind;
            _amount = amount;
            _target = target;
        }

        public StatusEffectKind StatusKind => _statusKind;

        public int Amount => _amount;

        public CombatEffectTargetDefinition Target => _target;
    }
}
