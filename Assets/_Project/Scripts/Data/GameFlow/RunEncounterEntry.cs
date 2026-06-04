using System;
using SlotRogue.Data.Combat;
using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [Serializable]
    public sealed class RunEncounterEntry
    {
        public MonsterDefinition monster = null!;

        [Tooltip("HUD slot index: 0=left, 1=center, 2=right.")]
        public int formationSlot;

        public MonsterTurnPatternDefinition turnPatternOverride = null!;
    }
}
