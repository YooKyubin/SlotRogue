using System;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public sealed class EnemyActionDefinition
    {
        [SerializeField] private string _actionName = "Common";
        [SerializeField] private Sprite _intentIcon;
        [SerializeReference] private EnemyEffectDefinition _effect;

        public string ActionName => _actionName ?? string.Empty;
        public Sprite IntentIcon => _intentIcon;
        public EnemyEffectDefinition Effect => _effect;

        public EnemyActionDefinition()
        {
        }

        public EnemyActionDefinition(
            string actionName,
            Sprite intentIcon,
            EnemyEffectDefinition effect)
        {
            _actionName = actionName ?? string.Empty;
            _intentIcon = intentIcon;
            _effect = effect;
        }
    }
}
