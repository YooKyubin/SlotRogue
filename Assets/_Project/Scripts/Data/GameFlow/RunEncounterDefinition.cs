using System;
using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [CreateAssetMenu(menuName = "SlotRogue/Game Flow/Run Encounter Definition")]
    public sealed class RunEncounterDefinition : ScriptableObject
    {
        public RunEncounterEntry[] entries = Array.Empty<RunEncounterEntry>();

        public int EntryCount => entries == null ? 0 : entries.Length;

        public bool HasEntries => EntryCount > 0;
    }
}
