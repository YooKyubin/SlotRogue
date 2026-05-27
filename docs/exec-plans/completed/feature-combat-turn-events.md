# 전투 턴 이벤트 로그 (Combat Turn Events)

**Status**: completed  
**Started**: 2026-05-27  
**Finished**: 2026-05-27  
**Owner**: _(전투 담당 — GitHub id 기입)_  
**Contributors**: _(UI 담당 — 타임라인·연출 순서)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md) (C10), [`ADR-0001`](../../adr/0001-combat-turn-event-log.md)

## Goal

`BattleResolver`가 스핀 처리 중 발생한 사건을 **순서 있는 `CombatEvent` 목록**으로 기록하고, `TurnResult`(이벤트 + 턴 종료 스냅샷)를 `BattlePresenter`가 UI로 전달한다. 스냅샷 diff만으로는 표현되지 않던 **턴 내부 피해·회복·행동**과 **연출 순서**를 지원한다.

## 배경

- 완료: [`feature-combat-core`](./feature-combat-core.md) — Resolver 규칙(C1–C9), diff 기반 Presenter 스텁.
- 문제: 최종 HP가 같으면 diff 이벤트가 누락됨. UI가 플레이어→몬스터 순차 연출을 하려면 **사건 로그**가 필요함.
- 결정: [`ADR-0001`](../../adr/0001-combat-turn-event-log.md).

## Checklist

### 문서

- [x] `combat-core.md` C10 — `TurnResult` / `CombatEvent` / 역할 분리 반영
- [x] `ADR-0001` — 이벤트 로그 채택
- [x] 본 exec-plan + `STATUS.md` 등록

### 코드

- [x] `BattleResolver` — `ProcessSpin`에서 `CombatEvent` 기록 후 `TurnResult` 반환(return)
- [x] `BattleResolver` — 이벤트 발생 순서 고정: PlayerPhase 피해 → Monster 행동(`MonsterActionExecuted`) → 부가 피해/승패
- [x] `BattlePresenter` — 스냅샷 diff 제거; `Consume(TurnResult)`에서 이벤트 스트림 발행 + `TurnCompleted`
- [x] `BattleDebugLogListener` — 턴 이벤트 로그 출력으로 갱신
- [x] EditMode: 이벤트 종류·순서·승패 검증 (`BattleResolverTests`)
- [x] EditMode: Presenter 단위 테스트(`BattlePresenterTests`)로 `TurnResult` 순서 전달 검증
- [x] Play Mode: `BattleTest` — Console에서 턴 이벤트 순서 확인

### UI (별도 plan)

- [ ] `CombatTimelineController`(가칭) — 이벤트 큐·순차 연출·입력 잠금 (follow-up)

## Notes

- B/B 선택 반영: Resolver는 이벤트를 broadcast하지 않고 `TurnResult`를 호출자에 반환, 호출자가 Presenter에 전달.
- Defend 중복 이벤트는 제거하고 `MonsterActionExecuted(Kind=Defend)`만 사용.
- `dotnet build SlotRogue.Core.csproj` 검증은 Unity가 생성한 csproj 포함 목록이 최신이 아니어서 신규 타입 미포함 오류가 발생할 수 있음. Unity 프로젝트 재생성/에디터 컴파일 기준으로 최종 확인 필요.
- 수동 검증 완료: Unity Test Runner(EditMode) + `BattleTest` Play Mode에서 이벤트 순서 로그 확인.

## Completion

- **Finished**: 2026-05-27
- **Outcome**: 전투 코어가 스핀당 `TurnResult`를 반환하고, Presenter가 `CombatEvent` 스트림을 전달하는 구조로 전환 완료. 기존 diff 기반 Presenter 경로 제거.
- **Follow-ups**:
  - UI 타임라인 컨트롤러(`CombatTimelineController`) 구현 plan 분리.
  - 입력 잠금 정책(연출 중 스핀 방지) 확정.
