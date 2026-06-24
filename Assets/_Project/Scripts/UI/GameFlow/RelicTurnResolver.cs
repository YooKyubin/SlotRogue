using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RelicTurnResolver
    {
        private readonly RelicEffectRunner _effectRunner;

        public RelicTurnResolver()
            : this(new RelicEffectRunner())
        {
        }

        public RelicTurnResolver(RelicEffectRunner effectRunner)
        {
            _effectRunner = effectRunner ?? new RelicEffectRunner();
        }

        public RelicResolveResult Resolve(
            IReadOnlyList<RelicDefinition> ownedRelics,
            IReadOnlyList<SlotPatternMatch> patternMatches,
            RelicTurnContext context)
        {
            return _effectRunner.Resolve(patternMatches, ownedRelics, context.BattleContext);
        }
    }

    public readonly struct RelicTurnContext
    {
        public RelicTurnContext(RelicBattleContext battleContext)
        {
            BattleContext = battleContext;
        }

        public RelicBattleContext BattleContext { get; }

        public static RelicTurnContext FromBattle(
            BattleSystem battle,
            CombatParticipantId selectedTargetId)
        {
            CombatParticipant player = battle?.Player;
            CombatParticipant enemy = FindEnemy(battle, selectedTargetId);
            return new RelicTurnContext(new RelicBattleContext(
                player?.CurrentHp ?? 0,
                player?.MaxHp ?? 0,
                enemy?.CurrentHp ?? 0,
                enemy?.MaxHp ?? 0,
                enemy != null && enemy.StatusEffects.Count > 0,
                HasStatus(enemy, StatusEffectKind.Burn),
                HasStatus(enemy, StatusEffectKind.Infection)));
        }

        private static bool HasStatus(CombatParticipant participant, StatusEffectKind kind)
        {
            if (participant == null)
            {
                return false;
            }

            for (int index = 0; index < participant.StatusEffects.Count; index++)
            {
                if (participant.StatusEffects[index].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static CombatParticipant FindEnemy(
            BattleSystem battle,
            CombatParticipantId selectedTargetId)
        {
            if (battle == null)
            {
                return null;
            }

            for (int index = 0; index < battle.Enemies.Count; index++)
            {
                CombatParticipant enemy = battle.Enemies[index];
                if (selectedTargetId.IsValid && enemy.Id.Value == selectedTargetId.Value)
                {
                    return enemy;
                }
            }

            for (int index = 0; index < battle.Enemies.Count; index++)
            {
                CombatParticipant enemy = battle.Enemies[index];
                if (!enemy.IsDead)
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
