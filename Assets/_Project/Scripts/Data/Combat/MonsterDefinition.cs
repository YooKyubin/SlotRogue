using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [CreateAssetMenu(menuName = "SlotRogue/Combat/Monster Definition", fileName = "NewMonster")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        public int MaxHp = 50;

        public MonsterPattern Pattern;
    }
}
