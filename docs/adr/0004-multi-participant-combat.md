# ADR-0004: 전투는 ParticipantId 기반 다인전 구조로 확장한다

**Status**: accepted  
**Date**: 2026-06-02  
**Supersedes**: none  
**Superseded by**: none  
**Related design-docs**: [`docs/design-docs/combat-core.md`](../design-docs/combat-core.md)

---

## Context

현재 전투 코어는 ADR-0001에 따라 1스핀 = 1턴, Effect 목록 파이프라인, `CombatEvent` 로그를 사용한다. ADR-0003은 이 로그를 UI Presentation Timeline으로 순차 Replay하는 방식을 채택했다. 이 구조 덕분에 Core는 Unity UI·UniTask·DOTween에 의존하지 않고, 연출 레이어는 `EffectApplied` 스냅샷만으로 HUD를 재생할 수 있다.

그러나 현재 구현은 `BattleSystem`의 `_player` / `_monster`, `CombatEvent.IsPlayerParticipant`, `CombatViewModel.PlayerHp` / `MonsterHp`, `RunBattleView`의 단일 몬스터 HUD처럼 **1 플레이어 + 1 몬스터** 전제를 여러 계층에 가지고 있다. 앞으로 전투는 **플레이어 + 몬스터 2명 이상** 구성을 지원해야 하므로, 1:1 bool 분기를 유지한 채 몬스터 필드만 늘리면 타겟팅, 적 턴 순서, HUD 연출, 테스트가 빠르게 분기 폭발한다.

## Decision

- 전투 참가자는 플레이어/몬스터 전용 필드가 아니라 **안정적인 `CombatParticipantId`를 가진 Participant 목록**으로 표현한다.
- 각 Participant는 `CombatTeam` 또는 동등한 팀/진영 정보를 가진다. 승패 판정, 팀 단위 shield reset, 타겟 후보 필터링은 이 팀 정보를 기준으로 한다.
- `BattleSystem`은 단일 `_player` / `_monster` 대신 encounter 또는 roster 구조를 소유한다. 기존 1:1 전투는 이 roster의 특수 케이스로 취급한다.
- `CombatEvent.IsPlayerParticipant` 같은 bool 대상 표시는 **`TargetParticipantId`** 로 대체한다. 필요한 경우 `SourceParticipantId`, affected team, affected participant list를 이벤트에 추가한다.
- `EffectApplied` 이벤트의 before/after `CombatParticipantSnapshot` 원칙은 유지한다. Presenter는 Core Participant를 직접 폴링하지 않고 이벤트 스냅샷과 participant id로만 연출한다.
- UI 연출은 ADR-0003의 **Effect 종류별 Presenter + Replay 파이프라인**을 유지한다. Player/Monster별 Presenter를 복제하지 않고, `DamagePresenter`, `ShieldPresenter`, `HealPresenter`가 `TargetParticipantId`로 HUD binding을 찾아 갱신한다.
- HUD 표시 상태는 `CombatViewModel`의 player/monster 고정 필드에서 **participant id 기반 상태 맵**으로 확장한다. View는 참가자 수에 맞춰 HUD widget을 바인딩하거나 생성한다.
- Victory/Defeat는 단일 몬스터 HP가 아니라 팀 생존 상태로 판단한다. 기본 규칙은 Enemy team 전멸 시 Victory, Player team 전멸 시 Defeat다.
- 플레이어 공격 기본 타겟은 자동 선택이 아니라 **직접 선택(SelectedEnemy)** 이다.
- 타겟 모델은 `TargetMode` + `TargetParticipantId` 조합을 사용한다. MVP `TargetMode`는 `Self`, `SelectedEnemy`, `AllEnemies`, `RandomEnemy`다.
- `SelectedEnemy`는 UI가 선택한 대상의 `TargetParticipantId`를 Effect에 명시한다. Core는 object handle 참조가 아니라 id 기반으로 대상 해석을 수행한다.
- 멀티히트 기본 규칙은 **한 대상 반복 타격**이다. 단, 연속 타격 중 대상이 사망하면 남은 타수는 다른 생존 Enemy로 자동 전환한다.
- 적 턴 적용 순서는 speed/initiative 없이 **왼쪽→오른쪽 고정 순차**로 처리한다.
- 몬스터 사망 시 남은 몬스터의 schedule index는 **유지**하고, 사망한 몬스터만 스킵한다.
- 이번 범위의 player team은 **1명만 지원**한다. 플레이어 파티(2인 이상)는 후속 ADR/plan에서 다룬다.

## Alternatives considered

- **1:1 전투 모델 유지 + 몬스터 슬롯만 추가** — 기존 코드 변경량은 작아 보이나 `_monster1`, `_monster2`, `IsPlayerParticipant`, `MonsterHp` 같은 1:1 분기가 여러 계층으로 퍼진다. 타겟팅·연출·테스트가 참가자 수에 비례해 중복되므로 거절.
- **Player/Monster별 Presenter/ViewModel/View를 각각 둔다** — 참가자별 UI ownership은 직관적이지만, 데미지·실드·힐 연출 로직이 캐릭터 수만큼 복제된다. ADR-0003의 이벤트 Replay 모델에서는 Effect 종류별 Presenter를 유지하고 participant id로 HUD binding을 찾는 편이 중복이 적어 거절.
- **전투 전체를 ECS/DOT 구조로 재작성한다** — 다수 참가자 처리에는 유리할 수 있으나 현재 규모에서는 Entity storage, system query, authoring bridge 비용이 크다. Core 순수 로직과 Replay 이벤트 구조를 유지한 채 roster/id 모델로 확장하면 충분하므로 거절.
- **Core에 UI HUD slot 개념을 넣는다** — 타겟 표시와 HUD 위치를 쉽게 맞출 수 있지만 Core가 Presentation 배치에 의존하게 된다. Core는 participant id와 team만 제공하고, UI 계층이 id를 widget/anchor에 매핑하도록 분리한다.

## Consequences

- `CombatParticipant`, `BattleSystem`, `CombatEvent`, `CombatEffectTarget` 또는 동등한 타겟 모델에 breaking change가 필요하다.
- 기존 1:1 테스트는 roster 기반 전투의 특수 케이스로 유지하고, 다수 몬스터, 사망한 몬스터 스킵, 팀 단위 승패, 팀 단위 shield reset 테스트를 추가해야 한다.
- `MonsterTurnSchedule`은 단일 몬스터 전역 schedule에서 participant별 schedule 또는 enemy action provider 구조로 확장해야 한다.
- `SlotCombatRequestToCombatEffectsConverter`는 "항상 단일 Enemy" 전제를 제거하고, `TargetMode`/`TargetParticipantId`를 포함한 타겟 정보를 생산하거나 별도 resolver와 협력해야 한다.
- `TargetMode.SelectedEnemy` 경로에는 UI 선택 상태와 `TargetParticipantId` 유효성 검증(생존 여부, Enemy team 여부)이 필요하다.
- 멀티히트 처리 시 "중간 사망 -> 남은 타수 재타겟" 규칙과 "대체 대상 없음 -> 잔여타 소멸" 규칙을 테스트로 고정해야 한다.
- 적 턴은 왼쪽→오른쪽 순차 적용과 "사망 participant schedule skip, 생존 participant index 유지" 동작을 테스트로 고정해야 한다.
- 당시 `CombatViewModel`, `RunBattleView`, `BattleDevHarness`, `RunBattleController`, `CombatEventConsoleLogger`의 player/monster 고정 HUD 의존을 제거했다. 이후 구 View/Controller/DevHarness는 본편 구조 전환 과정에서 삭제됐다.
- 구현 기록: [`feature-multi-participant-combat`](../exec-plans/completed/feature-multi-participant-combat.md) (2026-06-03 완료).

## Notes

- 선행 결정: [ADR-0001](./0001-combat-turn-effect-pipeline.md), [ADR-0003](./0003-combat-presentation-replay.md).
