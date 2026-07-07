using System.Collections.Generic;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// 보유 v29 유물(<see cref="RelicSpec"/>)을 트리거별로 실행하는 엔진. 데이터(부품)를 읽어 값만 만들고
    /// 전투/경제 코어는 건드리지 않는다(순수 함수). 조건 판정은 <see cref="RelicConditionEvaluator"/>에 위임.
    ///
    /// P1 슬라이스: <see cref="RelicTrigger.OnDamageResolve"/>만 처리한다. 나머지 트리거
    /// (전투 시작/처치/별조각/치명/스핀 생성/규칙)와 특수 규칙(<see cref="RelicEffectKind.SpecialRule"/>)은
    /// 후속 슬라이스에서 각 훅에 배선한다.
    /// </summary>
    public static class RelicSpecRunner
    {
        /// <summary>
        /// 이번 스핀(피해 정산 시점)에 보유 유물을 적용해 델타·배율을 계산한다.
        /// </summary>
        /// <param name="owned">보유 유물 명세(순서 무관, null 허용).</param>
        /// <param name="context">스왑/별조각/턴수/첫 스핀/족보 수 등 런타임 상태.</param>
        /// <param name="patterns">이번 스핀에 발동한 족보 요약(족보 조건·다중 발동 판정용).</param>
        public static RelicSpecResolveResult ResolveDamageTurn(
            IReadOnlyList<RelicSpec> owned,
            in RelicRuntimeContext context,
            IReadOnlyList<RelicPatternView> patterns)
        {
            if (owned == null || owned.Count == 0)
            {
                return RelicSpecResolveResult.Empty;
            }

            int flatDamage = 0;
            int heal = 0;
            float comboMultAdd = 0f;
            float specialMult = 1f;
            float finalMult = 1f;
            float incomingDamageMul = 1f;
            var contributions = new List<RelicSpecContribution>();
            var statusRequests = new List<RelicSpecStatusRequest>();

            for (int index = 0; index < owned.Count; index++)
            {
                RelicSpec spec = owned[index];
                if (spec == null || spec.Trigger != RelicTrigger.OnDamageResolve)
                {
                    continue;
                }

                int applyCount = ApplyCount(spec, context, patterns);
                if (applyCount <= 0)
                {
                    continue;
                }

                int specFlat = 0;
                int specHeal = 0;
                IReadOnlyList<RelicEffect> effects = spec.Effects;
                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    RelicEffect effect = effects[effectIndex];
                    switch (effect.Kind)
                    {
                        case RelicEffectKind.FlatDamageAdd:
                            specFlat += (int)effect.Value1 * applyCount;
                            break;
                        case RelicEffectKind.Heal:
                            specHeal += (int)effect.Value1 * applyCount;
                            break;
                        case RelicEffectKind.ComboMultAdd:
                            comboMultAdd += effect.Value1 * applyCount;
                            break;
                        case RelicEffectKind.SpecialMultTimes:
                            specialMult *= Pow(effect.Value1, applyCount);
                            break;
                        case RelicEffectKind.FinalMultTimes:
                            finalMult *= Pow(effect.Value1, applyCount);
                            break;
                        case RelicEffectKind.IncomingDamageMul:
                            incomingDamageMul *= Pow(effect.Value1, applyCount);
                            break;
                        case RelicEffectKind.ApplyBurn:
                        case RelicEffectKind.ApplyInfection:
                        case RelicEffectKind.ApplyVulnerable:
                        case RelicEffectKind.ApplyWeaken:
                        case RelicEffectKind.GainThorns:
                            // 상태이상은 족보 수와 무관하게 이번 턴 1회 요청한다(양 = Value1).
                            statusRequests.Add(new RelicSpecStatusRequest(effect.Kind, (int)effect.Value1));
                            break;
                        default:
                            // GainCoins/PayHp/SymbolBaseDelta/AddAgainMark/SwapCountDelta/Shop*/
                            // SurviveLethal/Retrigger*/SpecialRule 등은 다른 트리거/후속 슬라이스에서 처리.
                            break;
                    }
                }

                flatDamage += specFlat;
                heal += specHeal;
                if (specFlat != 0 || specHeal != 0)
                {
                    contributions.Add(new RelicSpecContribution(
                        spec.Id, spec.DisplayName, specFlat, specHeal));
                }
            }

            int multipliedBaseDamage = ComputeMultipliedBaseDamage(owned, context, patterns);
            return new RelicSpecResolveResult(
                multipliedBaseDamage, flatDamage, heal, comboMultAdd, specialMult, finalMult,
                incomingDamageMul, contributions, statusRequests);
        }

        /// <summary>
        /// 이벤트 트리거(전투 시작/처치/별조각 등)에서 보유 유물이 주는 별조각 증감 합을 계산한다.
        /// 이벤트 유물은 족보 조건이 없어 런타임 상태(스왑/별조각 등)만 평가한다.
        /// </summary>
        public static int ResolveEventCoins(
            IReadOnlyList<RelicSpec> owned,
            RelicTrigger trigger,
            in RelicRuntimeContext context)
        {
            if (owned == null || owned.Count == 0)
            {
                return 0;
            }

            int coins = 0;
            for (int index = 0; index < owned.Count; index++)
            {
                RelicSpec spec = owned[index];
                if (spec == null || spec.Trigger != trigger)
                {
                    continue;
                }

                if (!RelicConditionEvaluator.Passes(spec.Conditions, context, RelicPatternView.None))
                {
                    continue;
                }

                IReadOnlyList<RelicEffect> effects = spec.Effects;
                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    if (effects[effectIndex].Kind == RelicEffectKind.GainCoins)
                    {
                        coins += (int)effects[effectIndex].Value1;
                    }
                }
            }

            return coins;
        }

        /// <summary>
        /// 이벤트 트리거(처치 등)에서 보유 유물이 주는 회복량 합(R-33 등). 런타임 조건만 평가한다.
        /// </summary>
        public static int ResolveEventHeal(
            IReadOnlyList<RelicSpec> owned,
            RelicTrigger trigger,
            in RelicRuntimeContext context)
        {
            if (owned == null || owned.Count == 0)
            {
                return 0;
            }

            int heal = 0;
            for (int index = 0; index < owned.Count; index++)
            {
                RelicSpec spec = owned[index];
                if (spec == null || spec.Trigger != trigger)
                {
                    continue;
                }

                if (!RelicConditionEvaluator.Passes(spec.Conditions, context, RelicPatternView.None))
                {
                    continue;
                }

                IReadOnlyList<RelicEffect> effects = spec.Effects;
                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    if (effects[effectIndex].Kind == RelicEffectKind.Heal)
                    {
                        heal += (int)effects[effectIndex].Value1;
                    }
                }
            }

            return heal;
        }

        /// <summary>
        /// 상시 규칙 유물(<see cref="RelicTrigger.RuleModifier"/>)이 주는 정수 규칙 증감 합
        /// (스왑 횟수·상점 선택지 등). 상시 규칙이라 조건은 평가하지 않는다.
        /// </summary>
        public static int ResolveRuleModifier(IReadOnlyList<RelicSpec> owned, RelicEffectKind kind)
        {
            if (owned == null || owned.Count == 0)
            {
                return 0;
            }

            int sum = 0;
            for (int index = 0; index < owned.Count; index++)
            {
                RelicSpec spec = owned[index];
                if (spec == null || spec.Trigger != RelicTrigger.RuleModifier)
                {
                    continue;
                }

                IReadOnlyList<RelicEffect> effects = spec.Effects;
                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    if (effects[effectIndex].Kind == kind)
                    {
                        sum += (int)effects[effectIndex].Value1;
                    }
                }
            }

            return sum;
        }

        /// <summary>
        /// 배율 피해 모델(P2). 족보별로 base × (1 + ΣComboMultAdd) × ΠSpecialMultTimes 를 계산해 합산하고,
        /// 전체에 최종 배율(FinalMultTimes)을 곱한다. 각 배율은 "그 족보 조건을 만족하는 유물"만 적용하므로
        /// 애그리게이트(전 족보 일괄 곱) 붕괴를 피한다. 배율 유물이 없으면 원래 기본 피해와 같다.
        /// </summary>
        private static int ComputeMultipliedBaseDamage(
            IReadOnlyList<RelicSpec> owned,
            in RelicRuntimeContext context,
            IReadOnlyList<RelicPatternView> patterns)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return 0;
            }

            float sumRaw = 0f;
            float maxRaw = 0f;
            for (int patternIndex = 0; patternIndex < patterns.Count; patternIndex++)
            {
                RelicPatternView pattern = patterns[patternIndex];
                float comboAdd = 0f;
                float specialMult = 1f;

                for (int relicIndex = 0; relicIndex < owned.Count; relicIndex++)
                {
                    RelicSpec spec = owned[relicIndex];
                    if (spec == null ||
                        spec.Trigger != RelicTrigger.OnDamageResolve ||
                        !RelicConditionEvaluator.Passes(spec.Conditions, context, pattern))
                    {
                        continue;
                    }

                    IReadOnlyList<RelicEffect> effects = spec.Effects;
                    for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                    {
                        switch (effects[effectIndex].Kind)
                        {
                            case RelicEffectKind.ComboMultAdd:
                                comboAdd += effects[effectIndex].Value1;
                                break;
                            case RelicEffectKind.SpecialMultTimes:
                                specialMult *= effects[effectIndex].Value1;
                                break;
                        }
                    }
                }

                float patternDamage = pattern.BaseDamage * (1f + comboAdd) * specialMult;
                sumRaw += patternDamage;
                if (patternDamage > maxRaw)
                {
                    maxRaw = patternDamage;
                }
            }

            float finalMult = ComputeGlobalFinalMult(owned, context, patterns);
            float baseTotal = sumRaw * finalMult;
            float highest = maxRaw * finalMult;

            float retriggerBonus = ComputeRetriggerBonus(owned, context, patterns, baseTotal, highest);
            return (int)System.Math.Round(baseTotal + retriggerBonus);
        }

        /// <summary>
        /// 재발동(RetriggerHighestPattern/RetriggerAllPatterns) 유물이 이번 스핀 피해에 더하는 보너스.
        /// "전투당 1회"(OncePerBattle) 재발동은 자동으로 전투 첫 스핀에 발동한다(상태 추적 없이 결정론적).
        /// </summary>
        private static float ComputeRetriggerBonus(
            IReadOnlyList<RelicSpec> owned,
            in RelicRuntimeContext context,
            IReadOnlyList<RelicPatternView> patterns,
            float baseTotal,
            float highest)
        {
            float bonus = 0f;
            for (int relicIndex = 0; relicIndex < owned.Count; relicIndex++)
            {
                RelicSpec spec = owned[relicIndex];
                if (spec == null || spec.Trigger != RelicTrigger.OnDamageResolve)
                {
                    continue;
                }

                bool retriggerAll = HasEffect(spec, RelicEffectKind.RetriggerAllPatterns);
                bool retriggerHighest = HasEffect(spec, RelicEffectKind.RetriggerHighestPattern);
                if (!retriggerAll && !retriggerHighest)
                {
                    continue;
                }

                if (spec.Lifetime.Kind == RelicLifetimeKind.OncePerBattle &&
                    !context.IsFirstSpinOfBattle)
                {
                    continue;
                }

                if (!SpecApplies(spec, context, patterns))
                {
                    continue;
                }

                bonus += retriggerAll ? baseTotal : highest;
            }

            return bonus;
        }

        private static bool HasEffect(RelicSpec spec, RelicEffectKind kind)
        {
            IReadOnlyList<RelicEffect> effects = spec.Effects;
            for (int index = 0; index < effects.Count; index++)
            {
                if (effects[index].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>최종 배율(FinalMultTimes)은 조건이 충족되면 전체 피해에 1회 곱한다(R-13 한 줄 대박 등).</summary>
        private static float ComputeGlobalFinalMult(
            IReadOnlyList<RelicSpec> owned,
            in RelicRuntimeContext context,
            IReadOnlyList<RelicPatternView> patterns)
        {
            float finalMult = 1f;
            for (int relicIndex = 0; relicIndex < owned.Count; relicIndex++)
            {
                RelicSpec spec = owned[relicIndex];
                if (spec == null ||
                    spec.Trigger != RelicTrigger.OnDamageResolve ||
                    !SpecApplies(spec, context, patterns))
                {
                    continue;
                }

                IReadOnlyList<RelicEffect> effects = spec.Effects;
                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    if (effects[effectIndex].Kind == RelicEffectKind.FinalMultTimes)
                    {
                        finalMult *= effects[effectIndex].Value1;
                    }
                }
            }

            return finalMult;
        }

        /// <summary>유물이 이번 스핀에 적용되는지 — 족보 조건이면 어떤 족보라도 충족, 런타임 조건만이면 컨텍스트로.</summary>
        private static bool SpecApplies(
            RelicSpec spec,
            in RelicRuntimeContext context,
            IReadOnlyList<RelicPatternView> patterns)
        {
            if (patterns != null)
            {
                for (int patternIndex = 0; patternIndex < patterns.Count; patternIndex++)
                {
                    if (RelicConditionEvaluator.Passes(spec.Conditions, context, patterns[patternIndex]))
                    {
                        return true;
                    }
                }
            }

            return RelicConditionEvaluator.Passes(spec.Conditions, context, RelicPatternView.None);
        }

        /// <summary>
        /// 이 유물이 이번 턴 몇 번 적용되는지. 족보 조건이 있으면 조건을 만족하는 족보 수(가로/세로/대각 다중
        /// 발동), 런타임 조건만 있으면(또는 조건 없음) 0/1.
        /// </summary>
        private static int ApplyCount(
            RelicSpec spec,
            in RelicRuntimeContext context,
            IReadOnlyList<RelicPatternView> patterns)
        {
            IReadOnlyList<RelicCondition> conditions = spec.Conditions;
            if (!HasPatternScopedCondition(conditions))
            {
                return RelicConditionEvaluator.Passes(conditions, context, RelicPatternView.None) ? 1 : 0;
            }

            if (patterns == null || patterns.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < patterns.Count; index++)
            {
                if (RelicConditionEvaluator.Passes(conditions, context, patterns[index]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasPatternScopedCondition(IReadOnlyList<RelicCondition> conditions)
        {
            if (conditions == null)
            {
                return false;
            }

            for (int index = 0; index < conditions.Count; index++)
            {
                switch (conditions[index].Kind)
                {
                    case RelicConditionKind.PatternContainsSymbol:
                    case RelicConditionKind.PatternSizeEquals:
                    case RelicConditionKind.PatternSizeAtLeast:
                    case RelicConditionKind.WholeLineSameSymbol:
                    case RelicConditionKind.PatternMadeBySwap:
                        return true;
                }
            }

            return false;
        }

        private static float Pow(float value, int count)
        {
            float result = 1f;
            for (int index = 0; index < count; index++)
            {
                result *= value;
            }

            return result;
        }
    }
}
