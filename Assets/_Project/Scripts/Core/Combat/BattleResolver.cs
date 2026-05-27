using System;
using System.Collections.Generic;
using SlotRogue.Data.Combat;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleResolver
    {
        private readonly MonsterDefinition _monsterDefinition;
        private readonly List<CombatEvent> _turnEvents = new();

        public BattleState State { get; private set; }

        public BattleResolver(MonsterDefinition monsterDefinition, int playerMaxHp)
        {
            _monsterDefinition = monsterDefinition;
            State = BattleState.Begin(monsterDefinition, playerMaxHp);
        }

        public TurnResult ProcessSpin(CombatSpinOutcome outcome)
        {
            _turnEvents.Clear();

            if (State.IsBattleOver)
            {
                return BuildTurnResult();
            }

            RunPlayerPhase(outcome);

            if (TryEndBattleAfterPlayerPhase())
            {
                return CompleteTurn();
            }

            RunMonsterPhase(outcome);
            TryEndBattleFinal();
            return CompleteTurn();
        }

        public void ApplyPlayerHeal(int amount)
        {
            if (State.IsBattleOver || amount <= 0)
            {
                return;
            }

            int before = State.PlayerHp;
            State.PlayerHp = Math.Min(State.PlayerHp + amount, State.PlayerMaxHp);
            int healed = State.PlayerHp - before;
            if (healed > 0)
            {
                _turnEvents.Add(CombatEvent.PlayerHealed(healed));
            }
        }

        public void ApplyMonsterHeal(int amount)
        {
            if (State.IsBattleOver || amount <= 0)
            {
                return;
            }

            int before = State.MonsterHp;
            State.MonsterHp = Math.Min(State.MonsterHp + amount, State.MonsterMaxHp);
            int healed = State.MonsterHp - before;
            if (healed > 0)
            {
                _turnEvents.Add(CombatEvent.MonsterHealed(healed));
            }
        }

        private TurnResult CompleteTurn() => BuildTurnResult();

        private TurnResult BuildTurnResult()
        {
            return new TurnResult(_turnEvents.ToArray(), BattleStateSnapshot.From(State));
        }

        private void RunPlayerPhase(CombatSpinOutcome outcome)
        {
            if (outcome.Attack <= 0)
            {
                return;
            }

            int damage = CombatDamage.Apply(outcome.Attack, State.PendingMonsterDefense);
            if (damage > 0)
            {
                State.MonsterHp -= damage;
                _turnEvents.Add(CombatEvent.PlayerDamageToMonster(damage));
            }

            State.PendingMonsterDefense = 0;
        }

        private bool TryEndBattleAfterPlayerPhase()
        {
            if (State.PlayerHp <= 0)
            {
                State.EndReason = BattleEndReason.Defeat;
                _turnEvents.Add(CombatEvent.BattleEnded(State.EndReason));
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
                MonsterAction action = PatternActionResolver.Resolve(step);
                PublishMonsterAction(action);
                ApplyMonsterAction(action, outcome);
            }

            AdvancePatternIndex(pattern);
        }

        private void PublishMonsterAction(MonsterAction action)
        {
            if (action.Kind is MonsterActionKind.Attack or MonsterActionKind.Defend)
            {
                _turnEvents.Add(CombatEvent.MonsterActionExecuted(action));
            }
        }

        private void ApplyMonsterAction(MonsterAction action, CombatSpinOutcome outcome)
        {
            switch (action.Kind)
            {
                case MonsterActionKind.Attack:
                    int damageToPlayer = CombatDamage.Apply(action.RawAttack, outcome.Defense);
                    if (damageToPlayer > 0)
                    {
                        State.PlayerHp -= damageToPlayer;
                        _turnEvents.Add(CombatEvent.MonsterDamageToPlayer(damageToPlayer));
                    }

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
            }
            else if (State.MonsterHp <= 0)
            {
                State.EndReason = BattleEndReason.Victory;
            }

            if (State.IsBattleOver)
            {
                _turnEvents.Add(CombatEvent.BattleEnded(State.EndReason));
            }
        }
    }
}
