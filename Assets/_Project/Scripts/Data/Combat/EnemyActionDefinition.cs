using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public sealed class EnemyActionDefinition
    {
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private Sprite _intentIcon;
        [SerializeReference] private EnemyEffectDefinition[] _effects = Array.Empty<EnemyEffectDefinition>();

        public EnemyActionDefinition()
        {
        }

        public EnemyActionDefinition(
            string displayName,
            Sprite intentIcon,
            EnemyEffectDefinition[] effects)
        {
            _displayName = displayName ?? string.Empty;
            _intentIcon = intentIcon;
            _effects = effects ?? Array.Empty<EnemyEffectDefinition>();
        }

        public string DisplayName => _displayName ?? string.Empty;

        public Sprite IntentIcon => _intentIcon;

        public IReadOnlyList<EnemyEffectDefinition> Effects => Clone(_effects);

        private static EnemyEffectDefinition[] Clone(IReadOnlyList<EnemyEffectDefinition> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return Array.Empty<EnemyEffectDefinition>();
            }

            var copy = new EnemyEffectDefinition[effects.Count];
            for (int index = 0; index < effects.Count; index++)
            {
                copy[index] = effects[index];
            }

            return copy;
        }
    }
}
