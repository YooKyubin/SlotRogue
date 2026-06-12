# 몬스터 Intent Action UI

**Status**: completed  
**Started**: 2026-06-12  
**Owner**: _(전투/UI 담당)_  
**Related design-docs**: [`../../design-docs/game-flow.md`](../../design-docs/game-flow.md), [`../../design-docs/combat-core.md`](../../design-docs/combat-core.md)

## Goal

몬스터별 다음 행동을 Core 전투 Schedule에서 조회해 RunGame 전투 화면에 Intent 아이콘으로 표시한다. UI 표시 상태는 BattleSystem Schedule과 분리해, 플레이어 턴 시작 시 공개된 Intent만 화면에 유지하고 적 행동 실행에 맞춰 아이콘을 하나씩 소비한다.

## Checklist

- [x] Core 전투 로직에서 몬스터별 다음 행동 조회 API 추가
- [x] `CombatEffect` 목록을 UI 표시용 `EnemyUpcomingActionViewData`로 변환
- [x] `RunBattleEnemySlotState`에 몬스터별 upcoming action 목록 전달
- [x] `EnemyIntentIconView` 추가로 action 1개를 icon 1개로 렌더링
- [x] `EnemyFormationSlotView`에서 Intent icon 인스턴스 재사용 및 가시성 관리
- [x] `EnemyFormationView`가 slot state의 `UpcomingActions`를 slot view로 전달
- [x] `EnemyVisibleIntentState`로 BattleSystem Schedule과 UI 표시 상태 분리
- [x] 적 행동 1개 완료 시 현재 표시 중인 Intent icon 1개 소비
- [x] 플레이어 턴 시작 시점에만 다음 Intent 목록 재공개
- [x] Unity Editor에서 필요한 Intent icon prefab/root/Sprite wiring 항목 문서화

## Notes

- 2026-06-11: 몬스터 다음 행동 표시 준비로 `EnemyUpcomingActionViewData`를 추가했다. `RunBattleScreenStateUpdater`가 `BattleSystem.TryGetUpcomingEnemyTurn()` 결과를 몬스터별 ViewData 배열로 변환해 `RunBattleEnemySlotState`까지 전달하며, 실제 `EnemyFormationSlotView` 아이콘 렌더링은 다음 단계로 남겼다.
- 2026-06-12: 몬스터 Intent 아이콘 렌더링 View를 추가했다. `EnemyFormationView`가 `RunBattleEnemySlotState.UpcomingActions`를 슬롯 View로 전달하고, `EnemyFormationSlotView`는 아이콘 인스턴스를 재사용해 행동 1개당 아이콘 1개를 표시한다. 실제 prefab 배치와 Sprite 연결은 Unity Editor에서 수동 wiring한다.
- 2026-06-12: 몬스터 Intent 표시 상태를 `EnemyVisibleIntentState`로 분리했다. `RunBattleScreenStateUpdater`는 더 이상 전투 Schedule을 직접 조회하지 않고 표시 상태만 읽으며, 전투 이벤트의 enemy `ActionCompleted`마다 아이콘 1개를 소비하고 `PlayerTurn` phase 표시 시점에만 다음 Intent를 다시 공개한다.

## Completion

- **Finished**: 2026-06-12
- **Outcome**: Core의 몬스터별 다음 행동 조회 결과를 RunGame 전투 화면의 Intent icon 표시 상태로 연결했다. `EnemyVisibleIntentState`가 공개된 Intent 목록을 보관하고, 적 행동 완료마다 icon 1개를 소비하며, 다음 플레이어 턴 시작 시에만 Schedule을 다시 읽도록 정리했다.
- **Follow-ups**: Unity Editor에서 Intent icon prefab/root/Sprite를 실제 slot prefab에 wiring하고 RunGame에서 수동 확인한다.
