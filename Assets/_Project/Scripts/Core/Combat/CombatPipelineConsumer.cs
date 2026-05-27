namespace SlotRogue.Core.Combat
{
    /// <summary>
    /// Adapter that keeps slot-side contract simple while preserving
    /// resolver-return + presenter-consume pipeline internally.
    /// </summary>
    public sealed class CombatPipelineConsumer : ISpinCombatConsumer
    {
        private readonly BattleResolver _resolver;
        private readonly BattlePresenter _presenter;

        public CombatPipelineConsumer(BattleResolver resolver, BattlePresenter presenter)
        {
            _resolver = resolver;
            _presenter = presenter;
        }

        public void OnSpinResolved(CombatSpinOutcome outcome)
        {
            if (_resolver == null || _presenter == null)
            {
                return;
            }

            TurnResult result = _resolver.ProcessSpin(outcome);
            _presenter.Consume(result);
        }
    }
}
