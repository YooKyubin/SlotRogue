using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 유물이 "이번 턴 전투에 부여하고 싶은 상태이상"을 표현하는 값 객체.
    /// 유물 시스템은 이 값만 만들고, 실제 부여/처리는 기존 전투 시스템이 담당한다.
    /// 상태별 Amount 의미는 ADR-0021의 요청 계층 계약을 따른다.
    /// </summary>
    public readonly struct StatusEffectRequest
    {
        public StatusEffectRequest(StatusEffectKind kind, int amount, CombatTargetMode targetMode)
        {
            Kind = kind;
            Amount = amount > 0 ? amount : 1;
            TargetMode = targetMode;
        }

        public StatusEffectKind Kind { get; }

        public int Amount { get; }

        public CombatTargetMode TargetMode { get; }
    }
}
