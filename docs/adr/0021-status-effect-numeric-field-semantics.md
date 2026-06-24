# ADR-0021: 상태 효과 수치 필드의 의미를 계층별로 구분한다

**Status**: accepted
**Date**: 2026-06-24
**Supersedes**: none
**Superseded by**: none
**Related design-docs**: [`docs/design-docs/attribute-status-interference.md`](../design-docs/attribute-status-interference.md), [`docs/design-docs/combat-core.md`](../design-docs/combat-core.md)

---

## Context

공통 상태 요청과 런타임 인스턴스는 `Amount`, `Magnitude`, `StackCount`, `RemainingTurns` 수치를 사용한다. 상태마다 전달하는 값의 의미가 달라 한 필드 이름만 보고 화상 강도, 감염 누적량, 남은 적용 횟수, 가시 반사 피해를 같은 개념으로 오해할 수 있다.

현재 구현된 취약·약화·흡혈은 `StackCount`를 남은 적용 횟수로 사용하고, 가시는 `Magnitude`를 반사 피해량으로 사용한다. 향후 구현할 감염은 피해 후 1 감소하는 현재 감염량을 `StackCount`로 표현할 예정이다. 화상과 감염의 세부 실행 타이밍은 아직 구현 대상이며 이 ADR에서 확정하지 않는다.

기존 유물 계층에는 최종 예상 피해를 기준으로 회복량을 미리 계산하는 `RelicDerivedHeal` 흡혈 경로도 존재한다. 이는 실제 HP 피해 뒤 타격별로 회복하는 Core `Lifesteal` 상태와 계산 기준 및 실행 시점이 다르다.

## Decision

상태 효과 수치 필드의 의미를 데이터가 이동하는 계층에 따라 다음과 같이 구분한다.

- `StatusEffectRequest.Amount`는 유물·행동 요청 계층이 전달하는 **상태별 범용 입력값**이다. 구체적인 의미는 `StatusEffectKind`가 결정한다.
- `StatusEffectSpec.Magnitude`와 `StatusEffectInstance.Magnitude`는 피해량처럼 상태가 발생시키는 **효과 강도**를 표현한다.
- `StatusEffectInstance.StackCount`는 중첩으로 증가하거나 사용·틱으로 감소할 수 있는 **현재 상태량**을 표현한다. 상태에 따라 누적량 또는 남은 적용 횟수로 해석한다.
- `StatusEffectInstance.RemainingTurns`는 일반적인 지속 턴 수에만 사용한다. 적용 횟수나 효과 강도를 대신 표현하지 않는다.

상태별 `Amount` 계약은 다음과 같다.

| 상태 | `Amount`의 의미 | 런타임 저장 | `Amount = 3`의 의미 |
|------|-----------------|-------------|---------------------|
| `Burn` | 화상 피해 강도 | `Magnitude` | 즉시 3 피해, 대응 턴 종료 시 3 피해 |
| `Infection` | 추가 감염량 | `StackCount` | 기존 감염에 3을 추가하고, 턴 종료 피해 후 현재 감염량을 1 감소 |
| `Lifesteal` | 남은 적용 행동 횟수 | `StackCount` | 실제 HP 피해 기반 20% 흡혈을 다음 3번의 유효 행동에 적용 |
| `Vulnerable` | 남은 피해 증폭 적용 횟수 | `StackCount` | 다음 3번의 유효 피해 정산에 취약 적용 |
| `Weaken` | 남은 피해 감소 적용 횟수 | `StackCount` | 다음 3번의 유효 공격 정산에 약화 적용 |
| `Thorns` | 가시 반사 피해량 | `Magnitude` | 직접 공격 피격 시 확률 성공하면 공격자에게 3 피해 반사 |

`StackCount`의 “누적량”과 “남은 횟수”는 모두 현재 유효한 상태량이며, 각 상태 컴포넌트가 구체적인 감소 조건을 소유한다. 공통 UI와 디버깅 코드는 `StackCount`를 무조건 “중첩 횟수”로 표시하지 않고 `StatusEffectKind`별 표시 규칙을 사용해야 한다.

`Magnitude`만 사용하는 `Burn`과 `Thorns`는 의미 없는 중첩 수치를 만들지 않도록 `StackCount`를 0으로 유지한다. 현재 공통 `Stack` 정책은 새 `Magnitude`로 교체하므로, 두 상태의 `Stack`은 피해량 합산이나 지속 횟수 증가를 의미하지 않는다.

다음 항목은 이번 결정에서 보류한다.

- `Magnitude`만 사용하는 상태의 `StatusStackMode.Stack`을 금지할지, `Magnitude`를 합산할지 여부는 나머지 속성 구현 후 공통 Stack/Refresh 정책 리팩터링에서 결정한다. 결정 전에는 새 `Magnitude`로 교체하고 `StackCount`를 0으로 유지한다.
- 기존 `RelicDerivedHeal` 흡혈을 Core `Lifesteal` 상태로 교체하거나 병행할지 여부는 기획자와 회복 기준 및 실행 시점을 확정한 뒤 결정한다.

## Alternatives considered

- **모든 상태의 `Amount`를 무조건 `Magnitude`로 유지** — 취약·약화·흡혈의 값은 효과 강도가 아니라 남은 적용 횟수이므로 런타임 의미와 맞지 않아 거절한다.
- **누적량과 남은 횟수에 별도 필드를 추가** — 현재 상태 컴포넌트는 둘을 동시에 사용하지 않으며 둘 다 감소 가능한 현재 상태량으로 처리할 수 있다. 필드와 Snapshot·Revision 복잡도만 늘어나므로 현 단계에서는 거절한다.
- **`Amount`를 `Stacks`로 명명** — 화상 강도와 가시 반사 피해량은 스택이 아니므로 요청 계층의 범용 이름으로 부적합해 거절한다.
- **흡혈 비율을 `Amount`로 전달** — Core 흡혈 비율은 20%로 고정되고 상태 수치는 남은 적용 행동 횟수다. 기존 유물의 가변 비율 흡혈은 별도 규칙이므로 거절한다.

## Consequences

- 요청 계층은 하나의 `Amount` 필드로 상태를 전달하되, 변환 계층에서 상태별로 `Magnitude` 또는 `StackCount`에 매핑한다.
- 상태 Factory와 컴포넌트가 상태별 수치 의미를 명시적으로 소유한다.
- 감염 구현 시 피해량은 감소 전 `StackCount`, 피해 후 감소량은 1, 0 도달 시 기존 만료 경로를 사용한다.
- 상태 UI는 `Magnitude`, `StackCount`, `RemainingTurns` 중 무엇을 표시할지 상태별 정책이 필요하다.
- `Magnitude` 전용 상태의 Stack 계산 정책과 흡혈 경로 통합은 열린 결정으로 남아 있으며, 후속 결정 전에는 현재 동작을 조용히 일반화하지 않는다.

## Notes

- 현재 Core enum에는 아직 `Infection`이 없고 기존 `Poison`이 임시 구현으로 남아 있다. 표의 `Infection` 행은 향후 공식 구현 계약이다.
- 화상·감염 실행 타이밍과 이벤트 순서는 [`attribute-status-interference.md`](../design-docs/attribute-status-interference.md)의 구현 작업에서 별도로 확정한다.
