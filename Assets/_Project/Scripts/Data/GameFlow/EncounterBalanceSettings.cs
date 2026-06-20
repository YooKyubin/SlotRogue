using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [CreateAssetMenu(
        fileName = "EncounterBalanceSettings",
        menuName = "SlotRogue/GameFlow/Encounter Balance Settings")]
    public sealed class EncounterBalanceSettings : ScriptableObject
    {
        [SerializeField] private float _hpIncreasePerBattle = 0.05f;
        [SerializeField] private float _hpIncreasePerCycle = 0.25f;
        [SerializeField] private float _normalTierHpMultiplier = 1f;
        [SerializeField] private float _eliteTierHpMultiplier = 1.35f;
        [SerializeField] private float _bossTierHpMultiplier = 1.8f;

        public EncounterBalanceConfig CreateConfig()
        {
            return new EncounterBalanceConfig(
                _hpIncreasePerBattle,
                _hpIncreasePerCycle,
                _normalTierHpMultiplier,
                _eliteTierHpMultiplier,
                _bossTierHpMultiplier);
        }
    }
}
