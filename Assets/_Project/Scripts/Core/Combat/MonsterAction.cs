using SlotRogue.Data.Combat;

namespace SlotRogue.Core.Combat
{
    public readonly struct MonsterAction
    {
        public MonsterAction(MonsterActionKind kind, int rawAttack, int defendValue, string buffId)
        {
            Kind = kind;
            RawAttack = rawAttack;
            DefendValue = defendValue;
            BuffId = buffId;
        }

        public MonsterActionKind Kind { get; }

        public int RawAttack { get; }

        public int DefendValue { get; }

        public string BuffId { get; }
    }
}
