namespace SlotRogue.Core.Combat
{
    public sealed class EnemyPlannedAction
    {
        private readonly EnemyActionEffect _effect;

        public EnemyPlannedAction(
            EnemyActionKey actionKey,
            string actionName,
            EnemyActionEffect? effect = null)
        {
            ActionKey = actionKey;
            ActionName = actionName ?? string.Empty;
            if (effect.HasValue)
            {
                _effect = effect.Value;
                HasEffect = true;
            }
        }

        public EnemyActionKey ActionKey { get; }

        public string ActionName { get; }

        public bool HasEffect { get; }

        public EnemyActionEffect Effect => _effect;
    }
}
