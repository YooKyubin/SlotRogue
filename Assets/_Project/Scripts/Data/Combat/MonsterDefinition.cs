using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [CreateAssetMenu(menuName = "SlotRogue/Combat/Monster Definition")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        [SerializeField] private MonsterVisualDefinition _visual;

        public int maxHp = 10;

        public MonsterTurnPatternDefinition turnPattern = null!;

        public MonsterVisualDefinition Visual => _visual;
    }
}
