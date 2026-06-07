namespace SlotRogue.Core.Combat
{
    public sealed class SkipActionComponent : StatusEffectComponent
    {
        public override bool ShouldSkipAction(StatusEffectContext context) => true;
    }
}
