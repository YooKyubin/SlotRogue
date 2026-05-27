# ADR-0001: 전투 턴 결과는 이벤트 로그(`TurnResult`)로 UI에 전달한다

**Status**: accepted  
**Date**: 2026-05-27  
**Supersedes**: none  
**Superseded by**: none  
**Related design-docs**: [`docs/design-docs/combat-core.md`](../design-docs/combat-core.md) (C10)

---

## Context

`feature-combat-core` Phase B에서 `BattlePresenter`가 스핀 전·후 `BattleState` 스냅샷을 diff해 `PlayerHpChanged` 등을 발행했다. 이 방식은 HP 바 동기화에는 충분하지만, **턴 내부에서 발생한 사건**(피해 후 회복으로 최종 HP가 동일한 경우, 플레이어 연출 후 몬스터 연출 순서 등)을 표현·재생할 수 없다. UI 타임라인은 “무슨 일이 있었는가”의 순서가 필요하다.

## Decision

스핀당 `BattleResolver`가 **`TurnResult`**(순서 있는 `CombatEvent` 목록 + 턴 종료 `BattleStateSnapshot`)를 생성하고, `BattlePresenter`는 이 로그를 UI 소비용 신호로 **번역**한다. **canonical 전달 모델은 이벤트 로그**이며, 최종 스냅샷은 HP 바 등 상태 동기화용 보조 데이터다. 연출 타임라인(대기·순차 재생)은 **UI 오케스트레이터**가 담당한다.

## Alternatives considered

- **Option A — 스냅샷 diff 유지** — `BattlePresenter`가 HP·`PatternIndex`만 비교해 이벤트 발행. 구현이 단순하나 턴 내부 사건·순차 연출을 표현할 수 없어 거절.
- **Option B — 하이브리드( diff + 일부 이벤트 )** — Resolver는 최소 이벤트만, Presenter가 diff와 혼합. 규칙이 이중화되어 디버깅이 어려워 거절.
- **Option C — 이벤트 로그 + 최종 스냅샷 (채택)** — Resolver가 턴 처리 중 기록, Presenter/UI가 로그 순서대로 연출하고 스냅샷으로 UI 상태를 맞춤.

## Consequences

- **이득**: 피해·회복·행동·승패를 턴 순서대로 UI에 전달 가능. 슬롯↔전투 경계(C10) 유지.
- **비용**: `BattleResolver`·`BattlePresenter`·EditMode 테스트 리팩터 필요. 기존 diff 기반 Presenter 동작은 제거.
- **후속**: [`feature-combat-turn-events`](../exec-plans/completed/feature-combat-turn-events.md) 구현 완료. `combat-core.md` C10 갱신됨.

## Notes

- 구현 전 문서(`combat-core.md`, exec-plan)를 먼저 확정한다. 코드는 해당 plan 체크리스트에 따른다.
- 레포에 타입 스케치(`CombatEvent`, `TurnResult` 등)가 있을 수 있으나, Resolver/Presenter 연동은 plan 완료 기준까지 **미완**일 수 있다.
