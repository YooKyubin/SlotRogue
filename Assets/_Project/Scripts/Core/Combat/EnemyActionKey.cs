using System;

namespace SlotRogue.Core.Combat
{
    public readonly struct EnemyActionKey : IEquatable<EnemyActionKey>
    {
        public EnemyActionKey(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public bool IsValid => Value > 0;

        public bool Equals(EnemyActionKey other) => Value == other.Value;

        public override bool Equals(object obj) => obj is EnemyActionKey other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(EnemyActionKey left, EnemyActionKey right) => left.Equals(right);

        public static bool operator !=(EnemyActionKey left, EnemyActionKey right) => !left.Equals(right);
    }
}
