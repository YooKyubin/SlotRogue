using System.Collections.Generic;
using System.Text;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunCombatRequestResolver
    {
        // 유물 조건은 대표 패턴 하나가 아니라 ResolveAll 결과 전체(CurrentPatternMatches)를 기준으로 본다.
        public RunCombatRequestResult Resolve(
            IReadOnlyList<SlotPatternMatch> patternMatches,
            SlotCombatRequest baseRequest,
            ArtifactDefinitionSO artifact,
            int runDamageBonus,
            int runDefenseBonus)
        {
            SlotCombatRequest normalizedRequest = NormalizeBlankTurn(baseRequest);
            StarterArtifactActivation artifactActivation = TryApplyArtifact(
                patternMatches, artifact, normalizedRequest,
                out SlotCombatRequest artifactRequest,
                out StatusEffectSpec statusEffectToApply);

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
                runBonusSummary,
                statusEffectToApply);
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
            IReadOnlyList<SlotPatternMatch> patternMatches,
            ArtifactDefinitionSO artifact,
            SlotCombatRequest request,
            out SlotCombatRequest artifactRequest,
            out StatusEffectSpec statusEffectToApply)
        {
            artifactRequest = request;
            statusEffectToApply = StatusEffectSpec.None;

            if (artifact == null || !AnyMatchSatisfiesArtifact(patternMatches, artifact))
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
                case ArtifactEffectKind.ApplyBurn:
                    statusEffectToApply = new StatusEffectSpec(
                        StatusEffectKind.Burn,
                        artifact.StatusDuration,
                        artifact.StatusMagnitude,
                        ToStatusStackMode(artifact.StatusStackBehavior));
                    break;
                case ArtifactEffectKind.ApplyFreeze:
                    statusEffectToApply = new StatusEffectSpec(
                        StatusEffectKind.Freeze,
                        artifact.StatusDuration,
                        artifact.StatusMagnitude,
                        ToStatusStackMode(artifact.StatusStackBehavior));
                    break;
                case ArtifactEffectKind.ApplyPoison:
                    statusEffectToApply = new StatusEffectSpec(
                        StatusEffectKind.Poison,
                        artifact.StatusDuration,
                        artifact.StatusMagnitude,
                        ToStatusStackMode(artifact.StatusStackBehavior));
                    break;
            }

            return new StarterArtifactActivation(true, artifact.DisplayName, artifact.Description);
        }

        // 전체 패턴 목록 중 하나라도 유물의 대상 심볼 + 최소 매치 길이를 만족하면 발동한다.
        private static bool AnyMatchSatisfiesArtifact(
            IReadOnlyList<SlotPatternMatch> patternMatches,
            ArtifactDefinitionSO artifact)
        {
            if (patternMatches == null)
            {
                return false;
            }

            for (int index = 0; index < patternMatches.Count; index++)
            {
                SlotPatternMatch match = patternMatches[index];
                if (match == null || match.MatchedCells == null)
                {
                    continue;
                }

                if (match.Symbol == artifact.TargetSymbol &&
                    match.MatchedCells.Count >= artifact.MinimumMatchLength)
                {
                    return true;
                }
            }

            return false;
        }

        private static StatusStackMode ToStatusStackMode(StatusStackBehavior behavior) =>
            behavior == StatusStackBehavior.Stack ? StatusStackMode.Stack : StatusStackMode.Refresh;

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
