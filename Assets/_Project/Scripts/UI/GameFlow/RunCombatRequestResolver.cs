using System.Text;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunCombatRequestResolver
    {
        public RunCombatRequestResult Resolve(
            SlotPatternResult patternResult,
            SlotCombatRequest baseRequest,
            ArtifactDefinitionSO artifact,
            int runDamageBonus,
            int runDefenseBonus)
        {
            SlotCombatRequest normalizedRequest = NormalizeBlankTurn(baseRequest);
            StarterArtifactActivation artifactActivation = TryApplyArtifact(
                patternResult, artifact, normalizedRequest,
                out SlotCombatRequest artifactRequest);

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

        private static StarterArtifactActivation TryApplyArtifact(
            SlotPatternResult patternResult,
            ArtifactDefinitionSO artifact,
            SlotCombatRequest request,
            out SlotCombatRequest artifactRequest)
        {
            artifactRequest = request;

            if (artifact == null ||
                patternResult == null ||
                !patternResult.HasMatch ||
                patternResult.Symbol != artifact.TargetSymbol ||
                patternResult.MatchLength < artifact.MinimumMatchLength)
            {
                return StarterArtifactActivation.None;
            }

            switch (artifact.EffectKind)
            {
                case ArtifactEffectKind.BonusDamage:
                    artifactRequest = new SlotCombatRequest(
                        request.Damage + artifact.BonusAmount,
                        request.Defense,
                        request.AttackCount,
                        request.HealAmount,
                        request.IsCritical,
                        request.PatternName);
                    break;
                case ArtifactEffectKind.BonusDefense:
                    artifactRequest = new SlotCombatRequest(
                        request.Damage,
                        request.Defense + artifact.BonusAmount,
                        request.AttackCount,
                        request.HealAmount,
                        request.IsCritical,
                        request.PatternName);
                    break;
                case ArtifactEffectKind.BonusHeal:
                    artifactRequest = new SlotCombatRequest(
                        request.Damage,
                        request.Defense,
                        request.AttackCount,
                        request.HealAmount + artifact.BonusAmount,
                        request.IsCritical,
                        request.PatternName);
                    break;
                // ApplyBurn / ApplyFreeze / ApplyPoison:
                // 연출 발동(StarterArtifactActivation)만 처리하고
                // 실제 상태이상 적용은 전투 담당이 구현 예정.
            }

            return new StarterArtifactActivation(true, artifact.DisplayName, artifact.Description);
        }

        private static string BuildRunBonusSummary(int runDamageBonus, int runDefenseBonus)
        {
            var builder = new StringBuilder();

            if (runDamageBonus > 0)
            {
                builder.Append($"피해 +{runDamageBonus}");
            }

            if (runDefenseBonus > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append($"방어 +{runDefenseBonus}");
            }

            return builder.ToString();
        }
    }
}
