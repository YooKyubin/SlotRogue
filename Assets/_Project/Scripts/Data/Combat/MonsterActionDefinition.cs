using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [CreateAssetMenu(menuName = "SlotRogue/Combat/Monster Action", fileName = "NewMonsterAction")]
    public sealed class MonsterActionDefinition : ScriptableObject
    {
        public MonsterActionKind Kind;

        [Header("Attack")]
        public int RawAttack;

        [Header("Defend (C7)")]
        public int DefendValue;

        [Header("Buff / Special")]
        public string BuffId;
    }
}
