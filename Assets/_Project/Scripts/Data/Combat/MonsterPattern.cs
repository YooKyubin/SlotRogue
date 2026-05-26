using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [CreateAssetMenu(menuName = "SlotRogue/Combat/Monster Pattern", fileName = "NewMonsterPattern")]
    public sealed class MonsterPattern : ScriptableObject
    {
        public PatternStep[] Steps = System.Array.Empty<PatternStep>();

        public bool Loop = true;
    }
}
