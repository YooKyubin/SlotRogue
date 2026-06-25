# 속성 전투 연결

**Status**: active  
**Started**: 2026-06-05  
**Owner**: _(전투 담당)_  
**Contributors**: Codex (문서 정리)
**Related design-docs**: [`attribute-status-interference.md`](../../design-docs/attribute-status-interference.md), [`combat-core.md`](../../design-docs/combat-core.md), [`relic-system.md`](../../design-docs/relic-system.md), [`game-flow.md`](../../design-docs/game-flow.md)
**Related ADRs**: [`ADR-0021`](../../adr/0021-status-effect-numeric-field-semantics.md)

## Goal

유물과 몬스터 행동이 v6 기준 공통 6속성(`화상`, `감염`, `흡혈`, `취약`, `약화`, `가시`)을 전투 코어에 안정적으로 전달하고, 플레이어/몬스터 양쪽에서 같은 상태 언어와 정산 단위를 사용하게 만든다. 이번 plan의 범위는 공통 6속성까지이며, 보스 전용 슬롯 방해의 실제 슬롯 적용은 별도 후속 plan으로 분리한다.

## Scope

### 포함

- 기존 `StatusEffectEngine` 컴포넌트 구조를 v6 속성 계약에 맞게 확장한다.
- 기존 독 임시 매핑을 `Infection` 계약으로 정리한다.
- `Burn`, `Infection`, `Vulnerable`, `Weaken`, `Lifesteal`, `Thorns`를 전투 코어에서 표현한다.
- 턴 종료 피해, 피해 정산 전/후 보정, 피격 후 반응, 실제 피해 기반 회복, 라운드 종료 처리를 추가한다.
- 유물 실행 경로에서 상태/흡혈/가시 요청을 생성하고, 구현된 유물만 보상풀에 단계적으로 편입한다.
- EditMode 테스트로 핵심 결정론 로직을 고정한다.

### 제외

- 보스 슬롯 가리기, 슬롯 잠금 실제 적용, 라인 약화, 저주 심볼 임시 추가.
- 스핀 후 선택 기능(릴 홀드, 제한 리롤 등).
- Rare/Legendary/Curse의 복합 패시브 전체 구현.

## Current baseline

2026-06-22 기준 코드는 이전 속성 연결 작업의 중간 상태다.

- `CombatEffectKind.ApplyStatus`, `StatusEffectSpec`, `StatusEffectEngine`, `StatusEffectInstance`, `IStatusEffectComponent` 기반은 구현되어 있다.
- 작업 시작 당시 공식 상태 종류는 `Burn`, `Freeze`, 기존 독 상태였다.
- `Burn`은 턴 시작 고정 피해 + duration 감소 구조다.
- 기존 독은 턴 시작 stack 피해 + 최대 5스택 + 감소 없음 구조였다.
- `Freeze`는 행동 스킵 상태로 존재하지만 v6 1차 핵심에서는 제외한다.
- `RelicEffectRunner`는 상태 계열 유물을 만나면 경고만 출력하고 실행하지 않는다.
- 작업 시작 당시 `RelicTurnResolver`는 감염을 기존 독 보유 여부로 임시 판정했다.
- 상태 아이콘 UI와 상태 이벤트 일부(`StatusApplied`, `StatusTicked`, `StatusExpired`, `ActionSkipped`)는 이미 연결되어 있다.

## Checklist

- [x] 기존 상태이상 컴포넌트 기반 구조 파악 — Codex
- [x] v6 속성/방해 기획서를 design-doc으로 재작성 — Codex
- [x] Core/Combat: `StatusEffectKind`를 v6 6속성 기준으로 정리하고 기존 독 제거 및 `Freeze` 호환 경로 유지 — Codex
- [x] Core/Combat: 팀 턴 종료 상태 반응 훅 추가 — Codex
- [x] Core/Combat: `Burn`을 부여 즉시 피해 + 대상 팀 턴 종료 1회 피해 후 제거로 변경 — Codex
- [x] Core/Combat: `Infection`을 턴 종료 피해 후 수치 1 감소, 총 스택 상한 없음으로 구현 — Codex
- [ ] Core/Combat: 피해 정산 전/후 훅 추가 — _(전투 담당)_
- [x] Core/Combat: `Vulnerable`을 다음 N번 피해 정산 증가 + 적용 횟수 감소로 구현 — Codex
- [x] Core/Combat: `Weaken`을 다음 N번 공격/스핀 정산 피해 20% 감소 + 적용 횟수 감소로 구현 — Codex
- [x] Core/Combat: 실제 HP 피해량 기반 후처리 훅 추가 — Codex
- [x] Core/Combat: `Lifesteal`을 실제 HP 피해 20% 올림 회복 + 실제 발동 행동당 1회 감소로 구현 — Codex
- [x] Core/Combat: 직접 공격 피격 후 반응 훅과 테스트 가능한 전투 RNG 추가 — Codex
- [x] Core/Combat: `Thorns`를 타격별 50% 고정 반사 피해로 구현 — Codex
- [x] Core/Combat: 팀 턴 종료 알림과 `IExpireOnOpponentTeamTurnEnd` 컴포넌트 정책으로 상대 팀 가시 제거 — Codex
- [x] Core/Combat: 반사 피해가 취약·약화·흡혈·가시를 발동하지 않도록 `Reflection` 경계 적용 — Codex
- [ ] Core/Combat: 상태 적용/틱/소모/반사/흡혈 회복 이벤트를 UI가 구분 가능하게 보강 — _(전투 담당)_
- [ ] UI/GameFlow: `StatusEffectRequest`와 `CombatTurnRequestBuilder`를 v6 상태 요청으로 갱신 — _(전투 담당)_
- [ ] UI/GameFlow: `RelicTurnResolver`의 상태 조회를 `Burn`/`Infection`/`Vulnerable`/`Weaken` 구분으로 갱신 — _(전투 담당)_
- [ ] UI/GameFlow: `RelicEffectRunner`에서 `ApplyBurn`, `ApplyInfect`, `ApplyVulnerable`, `ApplyWeak`, `GainThorns`, `Lifesteal` 요청 생성 — _(전투 담당)_
- [ ] Relics: 구현 완료된 상태 유물만 `Phase1=true`로 단계적 전환 — _(전투 담당)_
- [ ] Data/Combat: 몬스터 행동 데이터가 상태 부여/흡혈 공격/가시 태세를 표현할 수 있게 효과 정의 추가 — _(전투 담당)_
- [ ] UI/GameFlow: 적 행동 planner factory에서 새 몬스터 효과 정의를 Core 효과로 변환 — _(전투 담당)_
- [ ] UI/GameFlow: 상태 아이콘/표시 텍스트를 v6 6속성 기준으로 갱신 — _(전투 담당)_
- [x] Tests/Core: Burn 즉시 피해 + 팀 턴 종료 피해 + 만료 테스트 추가/갱신 — Codex
- [x] Tests/Core: Infection 누적, 턴 종료 피해, 1 감소, 상한 없음 테스트 추가 — Codex
- [x] Tests/Core: Vulnerable/Weaken 정산 단위와 소모 테스트 추가 — Codex
- [x] Tests/Core: Lifesteal 실제 HP 피해 기반 회복, 행동당 소모, Snapshot/Revision, 이벤트 순서 테스트 추가 — Codex
- [x] Tests/Core: Thorns 확률, 다단히트·다중 대상, 방어막, 반사 재귀 방지, 이벤트 순서, 팀 턴 종료 제거 테스트 추가 — Codex
- [ ] Tests/UI: 유물 resolver → status request → combat effect 변환 테스트 갱신 — _(전투 담당)_
- [x] `dotnet build SlotRogue.slnx` 통과 — Codex
- [ ] Unity Editor에서 EditMode 테스트 결과 확인 — _(전투 담당)_

## Implementation notes

- 취약/약화의 핵심 위험은 “유물 발동 건별 소모”다. v6 기준 정산 1회는 플레이어 스핀 1회의 합산 피해, 몬스터 공격 행동 1회다.
- 현재 `CombatEffect[]`는 개별 효과 순서로 적용되므로, 취약/약화를 단순히 `Damage` effect마다 소모하면 기획과 달라질 수 있다. 정산 묶음 또는 action 단위 context가 필요하다.
- 감염은 총 스택 상한이 없다. 기존 독 전용 5스택 제한 코드는 제거한다.
- 가시는 자신의 팀 턴 종료에 제거하지 않는다. `BattleSystem`은 종료된 팀만 `StatusEffectEngine`에 알리고, 엔진이 상대 팀 참가자의 `IExpireOnOpponentTeamTurnEnd` 상태를 기존 `StatusExpired` 이벤트 경로로 제거한다.
- 화상은 `StatusApplied` 이벤트 후 즉시 `DamageOrigin.Status` 피해를 주고, 보유자 팀 턴 종료 알림에서 같은 피해를 한 번 더 준 뒤 기존 `StatusExpired` 경로로 제거한다.
- 감염은 부여 직후 피해 없이 `StackCount`에 `Amount`를 저장한다. 보유자 팀 턴 종료에 감소 전 수치만큼 `DamageOrigin.Status` 피해를 준 뒤 1 감소시키고, 0이면 기존 만료 경로를 요청한다. 기존 독 전용 턴 시작 피해와 5스택 제한 컴포넌트는 제거한다.
- 화상과 가시는 `Magnitude`만 사용하므로 의미 없는 `StackCount`를 증가시키지 않고 0으로 유지한다. `Stack` 시 피해량 합산 여부는 후속 공통 정책 결정 전까지 기존처럼 새 `Magnitude`로 교체한다.
- 흡혈은 요청 피해량이나 `EffectApplyResult.DamageDealt`가 아니라 타격 전후 HP Snapshot 차이로 계산한 실제 HP 피해량 기준이다. shield로 막힌 피해와 overkill 초과량은 회복량에 포함하지 않는다.
- 흡혈 회복은 피해 `EffectApplied` 직후 같은 타격 안에서 기존 Heal `EffectApplied`로 기록한다. 최대 HP여도 실제 HP 피해를 줬다면 발동 및 행동당 횟수 소모로 본다.
- 반사 피해는 `DamageOrigin.Reflection`으로 직접 적용해 가시·흡혈·취약·약화 경로를 모두 우회한다.
- 가시 판정은 직접 피해 `EffectApplied`와 흡혈 Heal `EffectApplied` 다음에 수행하며, 반사 처리 후 기존 승패 판정을 실행한다.
- 유물 상태 요청은 `CombatTargetMode`로 대상을 반드시 명시한다. `CombatTurnRequestBuilder`는 `(StatusEffectKind, CombatTargetMode)`가 같은 요청만 병합하고, `SlotCombatRequestToCombatEffectsConverter`가 최종 `CombatEffectTarget`으로 변환한다.
- 상태 종류와 대상 enum을 해석하는 변환 경계는 지원하지 않는 값을 기본 상태나 선택 적 대상으로 대체하지 않고 `ArgumentOutOfRangeException`으로 계약 위반을 드러낸다.
- `Freeze`는 debug/test 경로에 남길 수 있지만, 공식 유물/몬스터 데이터의 1차 속성에는 포함하지 않는다.

## Open questions

| ID | 질문 | 비고 |
|----|------|------|
| Q1 | 취약 기본 배율은 20%와 25% 중 무엇을 쓸 것인가? | v6 상세표는 25%, 일부 기존 유물 설명은 20%다. |
| ~~Q2~~ | ~~약화 기본 감소율은 30%와 25% 중 무엇을 쓸 것인가?~~ | **닫음 (2026-06-24).** 직접 공격 피해를 20% 감소시키며, `ceil(기본 피해 × 0.8)`을 사용한다. |
| Q3 | 플레이어 정산 1회를 Core에서 어떻게 표현할 것인가? | `CombatEffect[]` 전체, damage group, action context 중 선택 필요. |
| Q4 | 몬스터의 `Weak`는 모든 행동 피해 합산에 한 번 적용할 것인가, 행동 내부 damage effect마다 적용할 것인가? | v6은 공격 행동 1회를 기준으로 한다. |
| Q5 | 상태 아이콘 수치 표시는 `Magnitude`, `StackCount`, `RemainingTurns` 중 무엇을 우선 표시할 것인가? | 감염/취약/약화/가시마다 UI 규칙이 다를 수 있다. |
| Q6 | `Magnitude` 전용 상태의 `StatusStackMode.Stack`은 금지와 피해량 합산 중 무엇으로 정의할 것인가? | 현재는 새 `Magnitude`로 교체하고 `StackCount`를 0으로 유지한다. 전체 속성 구현 후 공통 정책 리팩터링에서 결정한다. |
| Q7 | 기존 `RelicDerivedHeal` 흡혈을 Core `Lifesteal` 상태로 교체할 것인가? | 기획자와 예상 총피해 선계산 및 실제 HP 피해 후 타격별 회복 중 공식 규칙을 확정한 뒤 결정한다. |

## Completion

_(completed/로 옮길 때 채움.)_

- **Finished**:
- **Outcome**:
- **Follow-ups**:
