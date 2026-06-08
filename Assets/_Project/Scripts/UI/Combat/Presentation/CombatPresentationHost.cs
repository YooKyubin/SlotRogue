using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatPresentationHost
    {
        public CombatPresentationHost(
            GameObject linkTarget,
            ICombatPresentationCommands commands)
        {
            LinkTarget = linkTarget;
            Commands = commands ?? NullCombatPresentationCommands.Instance;
        }

        public GameObject LinkTarget { get; }

        public ICombatPresentationCommands Commands { get; }
    }
}
