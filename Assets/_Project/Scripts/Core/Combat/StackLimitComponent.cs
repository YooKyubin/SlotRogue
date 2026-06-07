namespace SlotRogue.Core.Combat
{
    public sealed class StackLimitComponent : StatusEffectComponent
    {
        private readonly int _maxStack;

        public StackLimitComponent(int maxStack)
        {
            _maxStack = maxStack;
        }

        public override void OnApplied(StatusEffectContext context)
        {
            if (_maxStack > 0 && context.Instance.StackCount > _maxStack)
            {
                context.Instance.StackCount = _maxStack;
            }
        }
    }
}
