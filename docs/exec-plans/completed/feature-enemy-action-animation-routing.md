# 적 행동별 애니메이션 라우팅

**Status**: completed  
**Started**: 2026-06-17  
**Owner**: Codex  
**Related design-docs**: [`../../design-docs/combat-core.md`](../../design-docs/combat-core.md), [`../../adr/0003-combat-presentation-replay.md`](../../adr/0003-combat-presentation-replay.md), [`../../adr/0004-multi-participant-combat.md`](../../adr/0004-multi-participant-combat.md), [`../../adr/0008-ui-strict-mvvm-boundary.md`](../../adr/0008-ui-strict-mvvm-boundary.md)

## Goal

적 `ActionStarted` 연출이 항상 Attack 애니메이션을 호출하던 경로를 `EnemyActionDefinition.ActionName` 기반 범용 행동 애니메이션 요청으로 바꾼다. `ActionName`은 사용자 표시 이름과 Animator State 이름을 함께 담당하며, 중간 계층은 이 문자열을 해석하지 않고 전달만 한다.

## Checklist

- [x] `EnemyActionDefinition.DisplayName`을 `ActionName`으로 대체
- [x] `EnemyPlannedAction`과 `CombatEvent`에 `ActionName` 추가
- [x] `BattleSystem.ApplyPlannedActions()`에서 `ActionStarted`에 현재 행동 이름 전달
- [x] `PlayEnemyAttackAsync`/`PlayEnemyCombatVisualAttack`/`PlayCombatVisualAttack` 경로를 Action 범용 API로 변경
- [x] `IEnemyCombatVisual.PlayAttack()`을 `PlayAction(string actionName)`으로 변경
- [x] `MoonRabitCombatVisual`이 전달받은 Animator State 이름을 그대로 재생하도록 변경
- [x] `MoonRabbitPattern.asset`의 행동 이름을 Animator State 기준 `Attack`/`Defend`/`Heal`로 직접 갱신
- [x] Core, Presenter, View 전달 테스트 갱신

## Notes

- `ActionType` enum은 추가하지 않는다.
- `CombatEffectKind` 또는 `EnemyActionKey`로 애니메이션을 추론하지 않는다.
- 이름이 비어 있을 때 임의로 Attack fallback을 재생하지 않는다.
- Defend/Heal AnimationClip과 Animator State 추가는 Unity Editor 수동 작업으로 남긴다.

## Handoff

### 구현 의도

이번 작업의 핵심 의도는 적 행동 애니메이션을 `CombatEffectKind`나 `EnemyActionKey`에서 추론하지 않고, 데이터에 적힌 `EnemyActionDefinition.ActionName`을 단일 출처로 쓰는 것이다. `ActionName`은 사용자에게 보이는 행동 이름이면서 Animator State 이름이므로, 값과 Animator State 이름은 대소문자까지 정확히 같아야 한다.

### 구현된 흐름

`EnemyActionDefinition.ActionName`은 `EnemyActionPlannerFactory`에서 `EnemyPlannedAction.ActionName`으로 복사된다. `BattleSystem.ApplyPlannedActions()`는 적 행동 시작 시 `CombatEventKind.ActionStarted` 이벤트에 `SourceParticipantId`와 `ActionName`을 함께 기록한다. `ActionStartedPresenter`는 이 값을 `ICombatPresentationCommands.PlayEnemyActionAsync(participantId, actionName)`으로 넘기고, View 계층은 문자열을 해석하지 않은 채 `EnemyFormationSlotView`까지 전달한다. 최종적으로 `IEnemyCombatVisual.PlayAction(actionName)`이 호출되고, `MoonRabitCombatVisual`은 `Animator.StringToHash(actionName)` 후 `Animator.Play()`를 호출한다.

### 아직 못한 작업

- MoonRabbit Animator Controller에는 현재 `Idle`, `Attack` State만 있다. `MoonRabbitPattern.asset`에는 `Defend`, `Heal` 행동도 들어가므로, 해당 행동이 실행되면 Unity가 `Animator.GotoState: State could not be found` 경고를 낸다.
- `Defend`, `Heal` AnimationClip은 아직 만들지 않았다. 임시로 같은 clip을 써도 되지만, 최소한 Animator Controller에 `Defend`, `Heal` State는 추가해야 한다.
- 다른 몬스터 combat visual prefab이 같은 `ActionName` 정책을 따르는지 Unity Editor에서 확인하지 않았다.
- `ActionName` 오타를 Editor/빌드 타임에 검출하는 검증 도구는 아직 없다. 현재는 런타임에서 Animator State가 없을 때 Unity 경고로 드러난다.

### 왜 남겼나

이번 범위는 코드 전달 경로를 만드는 작업이었다. Animator Controller State 추가와 clip 배정은 Unity Editor 자산 편집이며, 실제 방어/회복 연출 클립이 아직 확정되지 않았다. 그래서 코드에서 임의 fallback을 만들지 않고, `ActionName`과 Animator State 불일치가 드러나도록 그대로 두었다. 이 방식이 데이터 오류를 조용히 Attack으로 숨기지 않아 후속 자산 작업에서 문제를 빨리 발견할 수 있다.

### 다음 작업 제안

- `MoonRabbitAnimController.controller`에 `Defend`, `Heal` State 추가.
- 임시 상태라도 `Defend`, `Heal`이 각각 재생되는지 RunGame 전투에서 수동 확인.
- 몬스터 패턴 SO의 `ActionName`과 combat visual Animator State 목록을 비교하는 Editor 검증 메뉴 또는 테스트 추가.
- 몬스터별 공통 State 정책이 필요해지면 `docs/design-docs/combat-core.md`에 State naming 규칙을 보강.

## Completion

- **Finished**: 2026-06-17
- **Outcome**: `EnemyActionDefinition.ActionName`이 `EnemyPlannedAction.ActionName` → `CombatEvent.ActionName` → `ActionStartedPresenter` → presentation command → battle view → `IEnemyCombatVisual.PlayAction(actionName)`으로 전달된다. `MoonRabitCombatVisual`은 `Animator.Play(Animator.StringToHash(actionName))`만 수행한다.
- **Verification**: `dotnet build SlotRogue.sln --no-restore`로 검증.
- **Follow-ups**: MoonRabbit Animator Controller에 `Defend`, `Heal` State 추가, 몬스터별 Animator State 검증 도구 추가 검토.
