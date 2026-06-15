using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public readonly struct EnemyActionPresentation
    {
        public EnemyActionPresentation(EnemyActionKey actionKey, string displayName, Sprite intentIcon)
        {
            ActionKey = actionKey;
            DisplayName = displayName ?? string.Empty;
            IntentIcon = intentIcon;
        }

        public EnemyActionKey ActionKey { get; }

        public string DisplayName { get; }

        public Sprite IntentIcon { get; }
    }
}
