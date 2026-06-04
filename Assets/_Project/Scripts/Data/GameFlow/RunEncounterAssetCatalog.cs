using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [CreateAssetMenu(menuName = "SlotRogue/Game Flow/Run Encounter Asset Catalog")]
    public sealed class RunEncounterAssetCatalog : ScriptableObject
    {
        public RunEncounterDefinition encounterDuo2A = null!;
        public RunEncounterDefinition encounterEliteTrio2B = null!;
    }
}
