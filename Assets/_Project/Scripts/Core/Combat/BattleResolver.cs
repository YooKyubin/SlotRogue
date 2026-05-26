using SlotRogue.Data.Combat;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleResolver
    {
        private readonly MonsterDefinition _monsterDefinition;

        public BattleResolver(MonsterDefinition monsterDefinition, int playerMaxHp)
        {
            _monsterDefinition = monsterDefinition;
            State = BattleState.Begin(monsterDefinition, playerMaxHp);
        }

        public BattleState State { get; private set; }

        public void ProcessSpin(CombatSpinOutcome outcome)
        {
            if (State.IsBattleOver)
            {
                return;
            }

            RunPlayerPhase(outcome);

            if (TryEndBattleAfterPlayerPhase())
            {
                return;
            }

            RunMonsterPhase(outcome);
            TryEndBattleFinal();
        }

        private void RunPlayerPhase(CombatSpinOutcome outcome)
        {
            if (outcome.Attack <= 0)
            {
                return;
            }

            int damage = CombatDamage.Apply(outcome.Attack, State.PendingMonsterDefense);
            State.MonsterHp -= damage;
            State.PendingMonsterDefense = 0;
        }

        private bool TryEndBattleAfterPlayerPhase()
        {
            if (State.PlayerHp <= 0)
            {
                State.EndReason = BattleEndReason.Defeat;
                return true;
            }

            return false;
        }

        private void RunMonsterPhase(CombatSpinOutcome outcome)
        {
            MonsterPattern pattern = _monsterDefinition.Pattern;
            if (pattern == null || pattern.Steps == null || pattern.Steps.Length == 0)
            {
                return;
            }

            int stepIndex = State.PatternIndex;
            if (stepIndex < 0 || stepIndex >= pattern.Steps.Length)
            {
                stepIndex = 0;
            }

            PatternStep step = pattern.Steps[stepIndex];
            if (step?.Action != null)
            {
                ApplyMonsterAction(PatternActionResolver.Resolve(step), outcome);
            }

            AdvancePatternIndex(pattern);
        }

        private void ApplyMonsterAction(MonsterAction action, CombatSpinOutcome outcome)
        {
            switch (action.Kind)
            {
                case MonsterActionKind.Attack:
                    int damageToPlayer = CombatDamage.Apply(action.RawAttack, outcome.Defense);
                    State.PlayerHp -= damageToPlayer;
                    break;
                case MonsterActionKind.Defend:
                    State.PendingMonsterDefense = action.DefendValue;
                    break;
                case MonsterActionKind.Buff:
                case MonsterActionKind.Special:
                    break;
            }
        }

        private void AdvancePatternIndex(MonsterPattern pattern)
        {
            PatternStep[] steps = pattern.Steps;
            int nextIndex = State.PatternIndex + 1;

            if (nextIndex >= steps.Length)
            {
                nextIndex = pattern.Loop ? 0 : steps.Length - 1;
            }

            State.PatternIndex = nextIndex;
        }

        private void TryEndBattleFinal()
        {
            if (State.PlayerHp <= 0)
            {
                State.EndReason = BattleEndReason.Defeat;
                return;
            }

            if (State.MonsterHp <= 0)
            {
                State.EndReason = BattleEndReason.Victory;
            }
        }
    }
}
