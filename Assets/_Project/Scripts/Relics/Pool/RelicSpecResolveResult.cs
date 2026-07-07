using System.Collections.Generic;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// v29 유물(<see cref="RelicSpec"/>)을 이번 스핀 족보/런타임 상태에 적용해 "이번 턴 전투에 넘길 값"만
    /// 계산한 결과. 전투를 직접 실행하지 않고 델타·배율만 만든다(상위 레이어가 소비).
    ///
    /// P1 슬라이스: OnDamageResolve 트리거의 즉시 적용 효과(<see cref="FlatDamage"/>/<see cref="Heal"/>)와
    /// 배율 데이터(<see cref="ComboMultAdd"/>/<see cref="SpecialMult"/>/<see cref="FinalMult"/>/
    /// <see cref="IncomingDamageMul"/>)를 산출한다. 배율의 실제 피해 적용은 P2(배율 피해 모델)에서 소비한다.
    /// </summary>
    public sealed class RelicSpecResolveResult
    {
        public static readonly RelicSpecResolveResult Empty = new(
            0, 0, 0, 0f, 1f, 1f, 1f,
            System.Array.Empty<RelicSpecContribution>(),
            System.Array.Empty<RelicSpecStatusRequest>());

        public RelicSpecResolveResult(
            int multipliedBaseDamage,
            int flatDamage,
            int heal,
            float comboMultAdd,
            float specialMult,
            float finalMult,
            float incomingDamageMul,
            IReadOnlyList<RelicSpecContribution> contributions,
            IReadOnlyList<RelicSpecStatusRequest> statusRequests)
        {
            MultipliedBaseDamage = multipliedBaseDamage;
            FlatDamage = flatDamage;
            Heal = heal;
            ComboMultAdd = comboMultAdd;
            SpecialMult = specialMult;
            FinalMult = finalMult;
            IncomingDamageMul = incomingDamageMul;
            Contributions = contributions ?? System.Array.Empty<RelicSpecContribution>();
            StatusRequests = statusRequests ?? System.Array.Empty<RelicSpecStatusRequest>();
        }

        /// <summary>
        /// 족보별 기본 피해에 콤보/특수/최종 배율을 족보 단위로 적용해 합산한 최종 기본 피해.
        /// 배율 유물이 없으면 원래 기본 피해와 같다. 상위 레이어가 base 피해를 이 값으로 교체한다.
        /// </summary>
        public int MultipliedBaseDamage { get; }

        /// <summary>이번 턴 합산 피해에 더할 정수 가산(FlatDamageAdd 합).</summary>
        public int FlatDamage { get; }

        /// <summary>이번 턴 회복량(Heal 합).</summary>
        public int Heal { get; }

        /// <summary>콤보배율 가산 합(P2에서 base×(1+ComboMultAdd) 등으로 소비).</summary>
        public float ComboMultAdd { get; }

        /// <summary>특수배율 곱(1 시작, SpecialMultTimes 누적곱).</summary>
        public float SpecialMult { get; }

        /// <summary>최종배율 곱(1 시작, FinalMultTimes 누적곱).</summary>
        public float FinalMult { get; }

        /// <summary>받는 피해 배율 곱(1 시작, 유리 대포 등). P2/방어 처리에서 소비.</summary>
        public float IncomingDamageMul { get; }

        /// <summary>발동 유물별 기여(표시/집계용).</summary>
        public IReadOnlyList<RelicSpecContribution> Contributions { get; }

        /// <summary>이번 턴 적(또는 자신)에게 부여할 상태이상 요청. 상위 레이어가 전투 파이프라인에 전달한다.</summary>
        public IReadOnlyList<RelicSpecStatusRequest> StatusRequests { get; }
    }

    /// <summary>엔진이 요청하는 상태이상 하나. 종류는 <see cref="RelicEffectKind"/>(ApplyBurn 등), 양은 스택/지속.</summary>
    public readonly struct RelicSpecStatusRequest
    {
        public RelicSpecStatusRequest(RelicEffectKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }

        public RelicEffectKind Kind { get; }
        public int Amount { get; }
    }

    /// <summary>유물 하나가 이번 턴에 만든 즉시 델타(표시/집계용).</summary>
    public readonly struct RelicSpecContribution
    {
        public RelicSpecContribution(string relicId, string relicName, int flatDamage, int heal)
        {
            RelicId = relicId ?? string.Empty;
            RelicName = relicName ?? string.Empty;
            FlatDamage = flatDamage;
            Heal = heal;
        }

        public string RelicId { get; }
        public string RelicName { get; }
        public int FlatDamage { get; }
        public int Heal { get; }
    }
}
