using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [CreateAssetMenu(menuName = "SlotRogue/Combat/Monster Definition")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        public int maxHp = 10;

        public Sprite portrait;

        public MonsterTurnPatternDefinition turnPattern = null!;
    }
}
