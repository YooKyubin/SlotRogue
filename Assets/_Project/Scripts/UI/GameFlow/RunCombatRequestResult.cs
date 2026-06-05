using SlotRogue.Slot.Data;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunCombatRequestResult
    {
        public RunCombatRequestResult(
            SlotCombatRequest baseRequest,
            SlotCombatRequest finalRequest,
            StarterArtifactActivation starterArtifactActivation,
            string runBonusSummary,
            StatusEffectSpec statusEffectToApply = default)
        {
            BaseRequest = baseRequest;
            FinalRequest = finalRequest;
            StarterArtifactActivation = starterArtifactActivation;
            RunBonusSummary = runBonusSummary;
            StatusEffectToApply = statusEffectToApply;
        }

        public SlotCombatRequest BaseRequest { get; }

        public SlotCombatRequest FinalRequest { get; }

        public StarterArtifactActivation StarterArtifactActivation { get; }

        public string RunBonusSummary { get; }

        public StatusEffectSpec StatusEffectToApply { get; }
    }
}
