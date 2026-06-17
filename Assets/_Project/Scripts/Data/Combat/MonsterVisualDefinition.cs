using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [CreateAssetMenu(
        menuName = "SlotRogue/Combat/Monster Visual Definition",
        fileName = "NewMonsterVisualDefinition")]
    public sealed class MonsterVisualDefinition : ScriptableObject
    {
        [SerializeField] private Sprite _portrait;
        [SerializeField] private GameObject _combatVisualPrefab;

        public Sprite Portrait => _portrait;

        public GameObject CombatVisualPrefab => _combatVisualPrefab;
    }
}
