using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatPresentationHost
    {
        public CombatPresentationHost(
            GameObject linkTarget,
            ICombatPresentationCommands commands)
            : this(linkTarget, commands, null)
        {
        }

        public CombatPresentationHost(
            GameObject linkTarget,
            ICombatPresentationCommands commands,
            ICombatStatusPresentationCommands statusCommands)
        {
            LinkTarget = linkTarget;
            Commands = commands ?? NullCombatPresentationCommands.Instance;
            StatusCommands = statusCommands;
        }

        public GameObject LinkTarget { get; }

        public ICombatPresentationCommands Commands { get; }

        public ICombatStatusPresentationCommands StatusCommands { get; }
    }
}
