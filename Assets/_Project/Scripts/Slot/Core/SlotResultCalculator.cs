using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Core
{
    /// <summary>
    /// 슬롯 기본 전투 계산기.
    /// 기본 피해는 SO 족보가 잡아낸 패턴들의 심볼별 공격력 합으로 결정된다.
    /// <para>
    /// 회복 / 방어 / 상태이상 / 치명타 / 다단히트처럼 "심볼에 의미를 부여하는" 효과는
    /// 여기서 만들지 않는다. 그런 효과는 유물(Relic) / 아티팩트가 패턴 조건에 반응해 추가한다.
    /// </para>
    /// </summary>
    public sealed class SlotResultCalculator
    {
        /// <summary>
        /// ResolveAll 결과(모든 패턴)를 받아 심볼별 기본 피해를 누적 산출한다.
        /// 패턴이 하나도 없으면 공격을 만들지 않는다.
        /// </summary>
        public SlotCalculationResult Calculate(IReadOnlyList<SlotPatternMatch> matches)
        {
            int damage = 0;

            if (matches != null)
            {
                for (int index = 0; index < matches.Count; index++)
                {
                    SlotPatternMatch match = matches[index];
                    if (match == null)
                    {
                        continue;
                    }

                    damage += match.CalculatedValue;
                }
            }

            if (damage <= 0)
            {
                return SlotCalculationResult.Empty;
            }

            // 기본 계산은 단일 타격 피해만 만든다. 방어/회복/치명타/다단히트/상태이상은 유물 몫.
            return new SlotCalculationResult(
                damage,
                defense: 0,
                attackCount: 1,
                healAmount: 0,
                isCritical: false);
        }
    }
}
