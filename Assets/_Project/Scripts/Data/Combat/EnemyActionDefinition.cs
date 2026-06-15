using System;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public sealed class EnemyActionDefinition
    {
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private Sprite _intentIcon;
        [SerializeReference] private EnemyEffectDefinition _effect;

        public string DisplayName => _displayName ?? string.Empty;
        public Sprite IntentIcon => _intentIcon;
        public EnemyEffectDefinition Effect => _effect;

        public EnemyActionDefinition()
        {
        }

        public EnemyActionDefinition(
            string displayName,
            Sprite intentIcon,
            EnemyEffectDefinition effect)
        {
            _displayName = displayName ?? string.Empty;
            _intentIcon = intentIcon;
            _effect = effect;
        }
    }
}
