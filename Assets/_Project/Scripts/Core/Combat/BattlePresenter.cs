using System;
using SlotRogue.Data.Combat;

namespace SlotRogue.Core.Combat
{
    public sealed class BattlePresenter
    {
        private readonly BattleResolver _resolver;
        private readonly MonsterDefinition _monsterDefinition;
        private BattleSnapshot _lastSnapshot;

        public BattlePresenter(BattleResolver resolver, MonsterDefinition monsterDefinition)
        {
            _resolver = resolver;
            _monsterDefinition = monsterDefinition;
            _lastSnapshot = BattleSnapshot.From(resolver.State);
            _resolver.SpinProcessed += OnSpinProcessed;
        }

        public event Action<int, int> PlayerHpChanged;

        public event Action<int, int> MonsterHpChanged;

        public event Action<MonsterAction> MonsterActionExecuted;

        public event Action<BattleEndReason> BattleEnded;

        public void Unsubscribe()
        {
            _resolver.SpinProcessed -= OnSpinProcessed;
        }

        private void OnSpinProcessed()
        {
            BattleState state = _resolver.State;
            BattleSnapshot current = BattleSnapshot.From(state);

            if (current.PlayerHp != _lastSnapshot.PlayerHp)
            {
                PlayerHpChanged?.Invoke(current.PlayerHp, state.PlayerMaxHp);
            }

            if (current.MonsterHp != _lastSnapshot.MonsterHp)
            {
                MonsterHpChanged?.Invoke(current.MonsterHp, state.MonsterMaxHp);
            }

            PublishMonsterActionIfExecuted(current);

            if (state.IsBattleOver && !_lastSnapshot.WasBattleOver)
            {
                BattleEnded?.Invoke(state.EndReason);
            }

            _lastSnapshot = current;
        }

        private void PublishMonsterActionIfExecuted(BattleSnapshot current)
        {
            if (current.PatternIndex == _lastSnapshot.PatternIndex)
            {
                return;
            }

            MonsterPattern pattern = _monsterDefinition?.Pattern;
            if (pattern?.Steps == null || pattern.Steps.Length == 0)
            {
                return;
            }

            int executedIndex = _lastSnapshot.PatternIndex;
            if (executedIndex < 0 || executedIndex >= pattern.Steps.Length)
            {
                return;
            }

            PatternStep step = pattern.Steps[executedIndex];
            if (step?.Action == null)
            {
                return;
            }

            MonsterAction action = PatternActionResolver.Resolve(step);
            if (action.Kind is MonsterActionKind.Attack or MonsterActionKind.Defend)
            {
                MonsterActionExecuted?.Invoke(action);
            }
        }

        private readonly struct BattleSnapshot
        {
            public BattleSnapshot(int playerHp, int monsterHp, int patternIndex, bool wasBattleOver)
            {
                PlayerHp = playerHp;
                MonsterHp = monsterHp;
                PatternIndex = patternIndex;
                WasBattleOver = wasBattleOver;
            }

            public int PlayerHp { get; }

            public int MonsterHp { get; }

            public int PatternIndex { get; }

            public bool WasBattleOver { get; }

            public static BattleSnapshot From(BattleState state)
            {
                return new BattleSnapshot(
                    state.PlayerHp,
                    state.MonsterHp,
                    state.PatternIndex,
                    state.IsBattleOver);
            }
        }
    }
}
