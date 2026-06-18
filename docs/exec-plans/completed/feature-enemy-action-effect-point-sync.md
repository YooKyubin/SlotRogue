# 적 행동 EffectPoint 동기화

**Status**: completed  
**Started**: 2026-06-17  
**Owner**: Codex  
**Related design-docs**: [`../../design-docs/combat-core.md`](../../design-docs/combat-core.md), [`../../adr/0003-combat-presentation-replay.md`](../../adr/0003-combat-presentation-replay.md)

## Goal

적 행동 애니메이션의 `EffectPoint`와 기존 `EffectApplied` 연출을 CombatEvent Replay 순서 안에서 동기화한다. `ActionStarted`는 행동 애니메이션 시작 후 `EffectPoint`까지 대기하고, `ActionCompleted`는 행동 애니메이션 종료까지 대기한다.

## Checklist

- [x] Core 적 행동 종료 시 `ActionCompleted` 이벤트 순서 보장
- [x] Presentation command를 EffectPoint 대기와 행동 종료 대기로 분리
- [x] `IEnemyCombatVisual`을 행동 일반 비동기 API로 변경
- [x] `MoonRabitCombatVisual` Animation Event 수신과 playback 상태 관리 구현
- [x] `ActionCompletedPresenter` 추가 및 pipeline 등록
- [x] Core/UI 테스트 추가 및 기존 테스트 갱신
- [x] 빌드/테스트와 asmdef 참조 영향 확인

## Notes

- Unity Editor 자산 편집은 이번 범위에서 하지 않았다. Animation Clip Event, Animator Transition, prefab/Inspector 작업은 완료 보고에 수동 작업으로 남긴다.
- 하나의 Action이 하나의 Effect만 가지는 현재 설계를 기준으로 구현했다.
- `dotnet build`가 Unity-generated `.csproj`에 새 파일을 자동 포함하지 않아, `ActionCompletedPresenter`는 `ActionStartedPresenter.cs`에, `EnemyActionPlaybackState`는 `MoonRabitCombatVisual.cs`에 같은 namespace 내부 타입으로 배치했다. asmdef 참조는 추가하지 않았다.

## Completion

- **Finished**: 2026-06-17
- **Outcome**: 적 `ActionStarted`는 combat visual 행동 애니메이션을 시작하고 `OnEffectPoint` Animation Event까지 대기한 뒤 `EffectApplied` 연출로 진행한다. 적 `ActionCompleted`는 `OnActionAnimationCompleted` Animation Event까지 대기한 뒤 다음 CombatEvent로 진행한다. 적 행동 Effect로 전투가 끝나도 `ActionCompleted`가 `BattleEnded`보다 먼저 기록된다.
- **Verification**: `dotnet build SlotRogue.sln --no-restore` 통과. 기존 `Assembly-CSharp-firstpass.csproj`의 `System.Net.Http` 버전 충돌 경고 1개만 남았다. `dotnet test SlotRogue.sln --no-build`는 종료 코드 0으로 통과했다.
- **Follow-ups**: Unity Editor에서 행동 AnimationClip별 `OnEffectPoint`, `OnActionAnimationCompleted` Animation Event를 추가하고 Animator State/Transition 설정을 수동 검증한다.
