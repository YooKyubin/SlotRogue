using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;
using UnityEngine;

namespace SlotRogue.UI.Combat
{
    public sealed class CombatEventConsoleLogger
    {
        public int CaptureEventCursor(BattleSystem battle) => battle.Events.Count;

        public void LogEventsSince(BattleSystem battle, int eventCursor, SlotCombatRequest request = null)
        {
            if (request != null)
            {
                Debug.Log(FormatRequest(request));
            }

            for (int index = eventCursor; index < battle.Events.Count; index++)
            {
                LogEvent(battle.Events[index]);
            }

            LogSnapshot(battle);
        }

        private static void LogEvent(CombatEvent combatEvent)
        {
            switch (combatEvent.Kind)
            {
                case CombatEventKind.PhaseChanged:
                    Debug.Log($"[Combat] Phase -> {combatEvent.Phase}");
                    break;

                case CombatEventKind.EffectApplied:
                    Debug.Log(FormatEffectApplied(combatEvent));
                    break;

                case CombatEventKind.ShieldReset:
                    Debug.Log(
                        $"[Combat] Shield reset ({ParticipantLabel(combatEvent.IsPlayerParticipant)}) " +
                        $"phase={combatEvent.Phase}");
                    break;

                case CombatEventKind.BattleEnded:
                    Debug.Log($"[Combat] Battle ended: {combatEvent.EndReason}");
                    break;
            }
        }

        private static string FormatEffectApplied(CombatEvent combatEvent)
        {
            CombatEffect effect = combatEvent.Effect;
            EffectApplyResult result = combatEvent.ApplyResult;

            CombatParticipantSnapshot before = combatEvent.TargetBefore;
            CombatParticipantSnapshot after = combatEvent.TargetAfter;

            return
                $"[Combat] Effect {effect.Kind} amount={effect.Amount} target={effect.Target} " +
                $"on={ParticipantLabel(combatEvent.IsPlayerParticipant)} phase={combatEvent.Phase} " +
                $"hp {before.Hp}->{after.Hp} shield {before.Shield}->{after.Shield} " +
                $"dmg={result.DamageDealt} shieldConsumed={result.ShieldConsumed} " +
                $"shieldGained={result.ShieldGained} heal={result.HealApplied}";
        }

        private static string FormatRequest(SlotCombatRequest request)
        {
            return
                $"[Combat] Request pattern={request.PatternName} crit={request.IsCritical} " +
                $"dmg={request.Damage} def={request.Defense} hits={request.AttackCount} heal={request.HealAmount}";
        }

        private static void LogSnapshot(BattleSystem battle)
        {
            CombatParticipant player = battle.Player;
            CombatParticipant monster = battle.Monster;

            Debug.Log(
                $"[Combat] Snapshot | Phase={battle.CurrentPhase} EndReason={battle.EndReason} | " +
                $"Player HP {player.CurrentHp}/{player.MaxHp} Shield {player.Shield} | " +
                $"Monster HP {monster.CurrentHp}/{monster.MaxHp} Shield {monster.Shield}");
        }

        private static string ParticipantLabel(bool isPlayerParticipant) =>
            isPlayerParticipant ? "Player" : "Monster";
    }
}
