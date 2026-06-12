using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Relics.Pool;

namespace SlotRogue.UI.GameFlow
{
    public sealed class BattleFlowContext
    {
        public BattleFlowContext(
            CombatParticipant player,
            RunEncounterRoster encounterRoster,
            IReadOnlyList<RelicDefinition> ownedRelics,
            int runDamageBonus,
            int runDefenseBonus,
            string encounterTitle)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            EncounterRoster = encounterRoster ?? throw new ArgumentNullException(nameof(encounterRoster));
            OwnedRelics = ownedRelics ?? Array.Empty<RelicDefinition>();
            RunDamageBonus = runDamageBonus;
            RunDefenseBonus = runDefenseBonus;
            EncounterTitle = encounterTitle ?? string.Empty;
        }

        public CombatParticipant Player { get; }

        public RunEncounterRoster EncounterRoster { get; }

        public IReadOnlyList<RelicDefinition> OwnedRelics { get; }

        public int RunDamageBonus { get; }

        public int RunDefenseBonus { get; }

        public string EncounterTitle { get; }
    }

    public readonly struct BattleFlowResult
    {
        public BattleFlowResult(BattleEndReason endReason, int remainingPlayerHp)
        {
            EndReason = endReason;
            RemainingPlayerHp = remainingPlayerHp;
        }

        public BattleEndReason EndReason { get; }

        public int RemainingPlayerHp { get; }
    }
}
