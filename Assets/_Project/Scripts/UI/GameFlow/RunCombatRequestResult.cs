using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunCombatRequestResult
    {
        public RunCombatRequestResult(
            SlotCombatRequest baseRequest,
            SlotCombatRequest finalRequest,
            StarterArtifactActivation starterArtifactActivation,
            string runBonusSummary)
        {
            BaseRequest = baseRequest;
            FinalRequest = finalRequest;
            StarterArtifactActivation = starterArtifactActivation;
            RunBonusSummary = runBonusSummary;
        }

        public SlotCombatRequest BaseRequest { get; }

        public SlotCombatRequest FinalRequest { get; }

        public StarterArtifactActivation StarterArtifactActivation { get; }

        public string RunBonusSummary { get; }
    }
}
