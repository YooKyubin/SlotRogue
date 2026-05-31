using System.Text;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunCombatRequestResolver
    {
        public RunCombatRequestResult Resolve(
            SlotPatternResult patternResult,
            SlotCombatRequest baseRequest,
            StarterArtifactDefinition starterArtifact,
            int runDamageBonus,
            int runDefenseBonus)
        {
            SlotCombatRequest normalizedRequest = NormalizeBlankTurn(baseRequest);
            StarterArtifactActivation artifactActivation =
                TryApplyStarterArtifact(patternResult, starterArtifact, normalizedRequest, out SlotCombatRequest artifactRequest);
            string runBonusSummary = BuildRunBonusSummary(runDamageBonus, runDefenseBonus);

            SlotCombatRequest finalRequest = new(
                artifactRequest.Damage + (artifactRequest.Damage > 0 ? runDamageBonus : 0),
                artifactRequest.Defense + runDefenseBonus,
                artifactRequest.AttackCount,
                artifactRequest.HealAmount,
                artifactRequest.IsCritical,
                artifactRequest.PatternName);

            return new RunCombatRequestResult(
                normalizedRequest,
                finalRequest,
                artifactActivation,
                runBonusSummary);
        }

        private static SlotCombatRequest NormalizeBlankTurn(SlotCombatRequest request)
        {
            if (request == null || HasNoEffects(request))
            {
                return new SlotCombatRequest(
                    SlotCombatRequest.BaseAttackDamage,
                    0,
                    SlotCombatRequest.BaseAttackCount,
                    0,
                    false,
                    SlotCombatRequest.BaseAttackName);
            }

            return request;
        }

        private static bool HasNoEffects(SlotCombatRequest request) =>
            request.Damage <= 0 &&
            request.Defense <= 0 &&
            request.HealAmount <= 0;

        private static StarterArtifactActivation TryApplyStarterArtifact(
            SlotPatternResult patternResult,
            StarterArtifactDefinition artifact,
            SlotCombatRequest request,
            out SlotCombatRequest artifactRequest)
        {
            artifactRequest = request;

            if (artifact == null ||
                artifact.Id == StarterArtifactId.None ||
                patternResult == null ||
                !patternResult.HasMatch ||
                patternResult.Symbol != artifact.TargetSymbol ||
                patternResult.MatchLength < artifact.MinimumMatchLength)
            {
                return StarterArtifactActivation.None;
            }

            artifactRequest = new SlotCombatRequest(
                request.Damage + artifact.BonusDamage,
                request.Defense + artifact.BonusDefense,
                request.AttackCount,
                request.HealAmount + artifact.BonusHeal,
                request.IsCritical,
                request.PatternName);

            return new StarterArtifactActivation(true, artifact.DisplayName, artifact.Description);
        }

        private static string BuildRunBonusSummary(int runDamageBonus, int runDefenseBonus)
        {
            var builder = new StringBuilder();

            if (runDamageBonus > 0)
            {
                builder.Append($"damage +{runDamageBonus}");
            }

            if (runDefenseBonus > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append($"defense +{runDefenseBonus}");
            }

            return builder.ToString();
        }
    }
}
