using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 유물이 "이번 턴 전투에 부여하고 싶은 상태이상"을 표현하는 값 객체.
    /// 유물 시스템은 이 값만 만들고, 실제 부여/처리는 기존 전투 시스템이 담당한다.
    /// Phase 1에서는 기존 전투가 지원하는 상태이상(Burn/Poison/Freeze)만 사용한다.
    /// </summary>
    public readonly struct StatusEffectRequest
    {
        public StatusEffectRequest(StatusEffectKind kind, int stacks)
        {
            Kind = kind;
            Stacks = stacks > 0 ? stacks : 1;
        }

        public StatusEffectKind Kind { get; }

        public int Stacks { get; }
    }
}
