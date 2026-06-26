namespace SlotRogue.Core.Combat
{
    public interface ICombatRandom
    {
        bool RollPercent(int successPercent);

        int NextIndex(int count);
    }
}
