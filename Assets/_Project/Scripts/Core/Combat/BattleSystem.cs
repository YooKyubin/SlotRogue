using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleSystem
    {
        private readonly EffectApplicator _effectApplicator;
        private readonly List<CombatEvent> _events = new();
        private CombatParticipant _player = null!;
        private CombatParticipant _monster = null!;
        private IReadOnlyList<CombatEffect> _monsterTurnActions = Array.Empty<CombatEffect>();

        public BattleSystem()
            : this(new EffectApplicator())
        {
        }

        public BattleSystem(EffectApplicator effectApplicator)
        {
            _effectApplicator = effectApplicator ?? new EffectApplicator();
        }

        public BattlePhase CurrentPhase { get; private set; } = BattlePhase.NotInBattle;

        public BattleEndReason EndReason { get; private set; } = BattleEndReason.None;

        public CombatParticipant Player => _player;

        public CombatParticipant Monster => _monster;

        public IReadOnlyList<CombatEvent> Events => _events;

        public IReadOnlyList<CombatEffect> UpcomingEnemyActions => _monsterTurnActions;

        public bool CanApplyPlayerTurn => CurrentPhase == BattlePhase.PlayerTurn;

        public void StartBattle(
            CombatParticipant player,
            CombatParticipant monster,
            IReadOnlyList<CombatEffect> monsterTurnActions)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _monster = monster ?? throw new ArgumentNullException(nameof(monster));
            _monsterTurnActions = monsterTurnActions ?? Array.Empty<CombatEffect>();
            EndReason = BattleEndReason.None;
            _events.Clear();

            SetPhase(BattlePhase.PlayerTurn);
        }

        public BattleApplyResult ApplyPlayerTurn(IReadOnlyList<CombatEffect> playerEffects)
        {
            if (CurrentPhase != BattlePhase.PlayerTurn)
            {
                return BattleApplyResult.Rejected(CurrentPhase);
            }

            SetPhase(BattlePhase.Resolving);

            ApplyEffects(playerEffects ?? Array.Empty<CombatEffect>(), _player, _monster);

            if (TryEndBattleAfterPlayerTurn())
            {
                return AcceptedResult();
            }

            ResetShield(_monster, isPlayerParticipant: false);

            if (TryEndBattleAfterPlayerTurn())
            {
                return AcceptedResult();
            }

            RunEnemyTurn();

            return AcceptedResult();
        }

        private void RunEnemyTurn()
        {
            SetPhase(BattlePhase.EnemyTurn);

            ApplyEffects(_monsterTurnActions, _monster, _player);

            if (TryEndBattle())
            {
                return;
            }

            ResetShield(_player, isPlayerParticipant: true);

            if (TryEndBattle())
            {
                return;
            }

            SetPhase(BattlePhase.PlayerTurn);
        }

        private void ApplyEffects(
            IReadOnlyList<CombatEffect> effects,
            CombatParticipant source,
            CombatParticipant opponent)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                CombatEffect effect = effects[index];
                EffectApplyResult applyResult = _effectApplicator.Apply(effect, source, opponent);
                bool isPlayerTarget = ResolveTargetParticipant(effect.Target, source, opponent) == _player;

                _events.Add(new CombatEvent(
                    CombatEventKind.EffectApplied,
                    CurrentPhase,
                    effect,
                    applyResult,
                    isPlayerParticipant: isPlayerTarget));

                if (TryEndBattle())
                {
                    return;
                }
            }
        }

        private bool TryEndBattleAfterPlayerTurn()
        {
            if (_monster.IsDead)
            {
                EndBattle(BattleEndReason.Victory);
                return true;
            }

            if (_player.IsDead)
            {
                EndBattle(BattleEndReason.Defeat);
                return true;
            }

            return false;
        }

        private bool TryEndBattle()
        {
            if (_player.IsDead)
            {
                EndBattle(BattleEndReason.Defeat);
                return true;
            }

            if (_monster.IsDead)
            {
                EndBattle(BattleEndReason.Victory);
                return true;
            }

            return false;
        }

        private void EndBattle(BattleEndReason endReason)
        {
            EndReason = endReason;
            SetPhase(BattlePhase.Ended);
            _events.Add(new CombatEvent(
                CombatEventKind.BattleEnded,
                BattlePhase.Ended,
                endReason: endReason));
        }

        private void ResetShield(CombatParticipant participant, bool isPlayerParticipant)
        {
            participant.Shield = 0;
            _events.Add(new CombatEvent(
                CombatEventKind.ShieldReset,
                CurrentPhase,
                isPlayerParticipant: isPlayerParticipant));
        }

        private void SetPhase(BattlePhase phase)
        {
            CurrentPhase = phase;
            _events.Add(new CombatEvent(CombatEventKind.PhaseChanged, phase));
        }

        private BattleApplyResult AcceptedResult() =>
            new(accepted: true, CurrentPhase, EndReason);

        private static CombatParticipant ResolveTargetParticipant(
            CombatEffectTarget target,
            CombatParticipant source,
            CombatParticipant opponent)
        {
            return target == CombatEffectTarget.Self ? source : opponent;
        }
    }
}
