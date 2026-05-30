namespace SlotRogue.Core.Combat
{
    public sealed class BattleApplyResult
    {
        public static BattleApplyResult Rejected(BattlePhase phase) =>
            new(accepted: false, phase, BattleEndReason.None);

        public BattleApplyResult(bool accepted, BattlePhase phase, BattleEndReason endReason)
        {
            Accepted = accepted;
            Phase = phase;
            EndReason = endReason;
        }

        public bool Accepted { get; }

        public BattlePhase Phase { get; }

        public BattleEndReason EndReason { get; }
    }
}
