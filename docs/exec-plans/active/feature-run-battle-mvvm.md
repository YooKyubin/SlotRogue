# RunBattle MVVM 정리

**Status**: active  
**Started**: 2026-06-05  
**Owner**: _(슬롯 담당)_  
**Related design-docs**: [`../../design-docs/game-flow.md`](../../design-docs/game-flow.md), [`../../design-docs/combat-core.md`](../../design-docs/combat-core.md)

## Goal

`RunBattle` 씬의 하이어라키, Inspector, View/ViewModel 경계를 Unity식 MVVM 구조로 정리한다. `ViewModel`은 Unity 타입을 모르고 화면 상태만 보유한다. `View`는 입력 이벤트와 렌더링만 담당한다. 전투 진행과 씬 조립은 `RunBattleCompositionRoot`가 맡고, 씬은 큰 단일 오브젝트가 아니라 작은 View 단위로 분리한다.

## Checklist

- [x] 기존 `RunBattleView` / `RunBattleController` 책임 분리 지점 확인
- [x] 순수 C# `RunBattleScreenViewModel` 추가
- [x] `RunBattleView.Render(state)` 경로 추가로 기존 화면을 ViewModel 갱신 중심으로 조정
- [x] `RunBattleScreenView`, `RunBattlePlayerHudView`, `RunBattleStatusView`, `RunBattleSlotBoardView`, `RunBattleActionView`, `RunBattlePresentationOverlayView`, `RunBattleWorldView` 분리
- [x] `EnemyFormationView` / `EnemyFormationSlotView`로 몬스터 formation slot View 분리
- [x] `RunBattleCompositionRoot` 추가, 새 구조에서 `RunBattleController` 없이 전투 흐름 시작
- [x] Builder의 RunBattle 생성 기준을 기존 화면 배치 유지 + `RunBattleCompositionRoot` wiring 방식으로 조정
- [x] Builder가 `EnemyFormationSlot.prefab`과 `FloatingDamageTextView.prefab`을 생성하도록 준비
- [x] 새 `RunBattleScreenView` / `RunBattleCompositionRoot` 전용 Inspector 추가
- [x] ViewModel EditMode 테스트 추가
- [x] `dotnet build SlotRogue.slnx` 검증
- [x] Builder 기본 rebuild 경로를 기존 prefab/scene 보존형 migration으로 전환
- [x] 기존 `EnemyFormationSlotView`를 `EnemyFormationView` adapter로 감싸 strict MVVM View 렌더 경로에 연결
- [x] 보존형 migration이 `RunBattleScreenView` / 하위 View / `RunBattleCompositionRoot`를 wiring하고 legacy `RunBattleController` / `RunBattleView`를 제거하도록 전환
- [x] 플레이어 HP fill 바인딩 복구 및 플레이어/몬스터 HP 바 감소 방향 통일
- [x] Sprite 없는 몬스터 HP 바 표시와 최종 공격력 ATK HUD 연결
- [x] 전투 연출 1차 MVVM 정리 — `DamagePresenter`의 View 생성/HP tween 제거, floating text 명령 View 분리, HP fill 보간을 View로 이동
- [x] 전투 연출 후속 정리 — `TurnBannerView` 분리, presentation command에서 enemy damage anchor 등록 제거, anchor resolve를 View registry 흐름으로 이동
- [x] 전투 연출 View 자동 wiring 제거 — `FloatingCombatTextLayerView` / `TurnBannerView` / fallback anchor를 RunGame 씬 Inspector 수동 연결 대상으로 전환
- [x] damage anchor fallback 제거 — 적 anchor 조회 실패 시 대체 anchor를 쓰지 않고 에러로 노출
- [x] 몬스터 shield 표시/생명주기 연출을 `ShieldGaugeView`와 presentation command 경로로 분리
- [x] Unity Editor에서 `SlotRogue > Game Flow > Migrate Run Battle Hierarchy In Place (Preserve UI)` 실행해 prefab/scene strict MVVM 적용과 기존 배치 리소스 유지 확인
- [ ] RunBattle 수동 플레이테스트로 스핀, 타겟 선택, 승리/패배 전환 확인

## Notes

- 2026-06-05: strict MVVM 적용 후 더 이상 쓰지 않는 legacy `RunBattleController` / 구 `RunBattleView` 스크립트와 `.meta`를 삭제했다. `GameFlowScenePrefabBuilder`에서는 임시 `Patch` / `Repair` 메뉴와 `Danger/Rebuild` 덮어쓰기 경로를 제거했고, 남은 RunBattle builder 경로는 기존 prefab/scene을 보존하는 migration 방식으로만 동작한다.
- 2026-06-05: `Ingame_Slot_ani` 3프레임을 사용하는 `SlotMachineFrameView`를 추가했다. 스핀 중에는 2/3번 프레임을 반복하고, 슬롯 presentation 종료 시 2번에서 1번으로 복귀한다. 기존 scene에 컴포넌트가 없어도 런타임에 `Slot Machine Panel` Image를 찾아 임시 연결하고, safe migration 실행 시에는 영구 wiring된다.
- 2026-06-06: 플레이어 HP 이미지는 오브젝트 이름 대신 `battle/player-hp-fill` 슬롯 ID로 복구하도록 변경했다. 플레이어/몬스터 HP 바는 `Image.Type.Filled`를 사용해 세로 게이지는 아래에서 위로, 가로 게이지는 왼쪽에서 오른쪽으로 채워지도록 통일했다.
- 2026-06-06: Sprite가 없는 몬스터 HP 이미지는 `fillAmount`가 적용되지 않아 왼쪽 pivot 고정 + 캐시된 최대 폭 조절 방식으로 변경했다. `Attack Power Text`는 런타임과 migration에서 자동 연결하고 최종 `Damage × AttackCount`를 `ATK`로 표시한다.
- 2026-06-08: 전투 연출 Presenter를 MVVM에 더 가깝게 정리했다. `DamagePresenter`는 최종 스냅샷 반영과 floating damage 요청만 수행하고, `FloatingCombatTextLayerView`가 prefab 생성·anchor 배치·턴 배너 표시를 담당한다. `CombatViewModel.Changed`를 화면 갱신 트리거로 연결하고, 플레이어/몬스터 HP fill 보간은 각 View가 처리한다.
- 2026-06-08: `FloatingCombatTextLayerView`에서 turn banner 생성을 제거하고 `TurnBannerView`로 분리했다. `ICombatPresentationCommands`는 floating damage와 turn banner 요청만 남겼고, enemy damage anchor resolve는 `ICombatDamageAnchorRegistry` / `RunBattleScreenView` / `EnemyFormationView` 흐름으로 옮겼다.
- 2026-06-08: 사용자가 RunGame 씬을 수동 wiring하기로 결정해, `RunBattleCompositionRoot`의 런타임 `AddComponent`와 임시 damage anchor 생성 코드를 제거했다. 이제 전투 연출 View 참조와 floating text prefab/root/anchor registry는 Inspector에서 명시적으로 연결해야 한다.
- 2026-06-09: damage anchor fallback을 제거했다. 적 participantId에 대응하는 slot anchor를 찾지 못하면 floating damage를 표시하지 않고 에러 로그로 세팅 문제를 드러낸다.
- 2026-06-09: `MonsterView` fallback 경로를 제거하고 enemy 렌더링을 `EnemyFormationSlotView`로 통일했다. `RunBattleWorldView`는 formation slot child만 바인딩하고, `EnemyFormationView`는 slot view 렌더링만 담당한다. formation slot / damage anchor 누락은 로그로 드러내고, enemy damage anchor 매핑 실패 시 대체 anchor 없이 `null`을 반환한다.
- 2026-06-09~10: 몬스터 shield 표시는 `EnemyFormationSlotView`가 `ShieldGaugeView.Render(shield)`에 위임한다. `RunBattlePlayerHudView`는 기존 shield fill 구현을 유지하고, 현재 shield gauge registry는 플레이어 대상 요청을 no-op으로 처리한다.
- 2026-06-10: shield 생명주기 연출 명령 구조를 추가했다. `ShieldPresenter`는 gain, `DamagePresenter`는 hit/break, `ShieldResetPresenter`는 expire 명령을 요청하고, `RunBattleScreenView`가 enemy participantId로 대상 `ShieldGaugeView`를 찾아 위임한다. Presenter는 `ShieldGaugeView`, DOTween, Image/Text를 직접 참조하지 않는다.
- 2026-06-10: `ShieldGaugeView`는 최소 연출을 담당한다. gain은 아래에서 올라오며 fade in, hit은 image 좌우 shake와 text 빨간색/확대 후 복귀, break는 image scale up + fade out, expire는 아래로 내려가며 fade out으로 처리한다. bar/사운드/복잡한 이펙트는 아직 구현하지 않았다.
- 2026-06-10: 데미지로 shield가 소모될 때는 `DamagePresenter`가 최종 snapshot 반영 전에 hit/break 연출을 요청한다. hit 시작 시 현재 표시 shield 값에서 consumed amount를 차감해 변경된 값을 먼저 보여주고, 이후 snapshot `Render`가 최종 상태를 다시 동기화한다.
- 2026-06-10: 턴 종료 shield reset은 reset 전/후 snapshot을 `ShieldReset` 이벤트에 포함한다. `ShieldResetPresenter`는 `TargetBefore.Shield > 0`일 때만 expire 연출을 요청하고, 이미 shield가 0이거나 gauge가 꺼져 있으면 다시 켜서 연출하지 않는다.
- 2026-06-10: Unity Editor에서 보존형 migration과 RunBattle prefab/scene strict MVVM 적용 상태를 확인했다. 기존 배치 리소스 유지 확인 항목은 완료 처리했다.
- 2026-06-11: 몬스터 다음 행동 표시 준비로 `EnemyUpcomingActionViewData`를 추가했다. `RunBattleScreenStateUpdater`가 `BattleSystem.TryGetUpcomingEnemyTurn()` 결과를 몬스터별 ViewData 배열로 변환해 `RunBattleEnemySlotState`까지 전달하며, 실제 `EnemyFormationSlotView` 아이콘 렌더링은 다음 단계로 남겼다.

- 엄격한 MVVM 기준은 “ViewModel이 UnityEngine과 화면 오브젝트를 모르는 것”으로 둔다. Unity 씬 생명주기와 입력 연결은 `CompositionRoot`가 담당한다.
- 카메라 셰이크는 world root를 기준으로 적용한다. 배경/몬스터를 함께 흔들지, 몬스터만 흔들지는 기존 화면 유지가 끝난 뒤 별도 migration으로 다시 판단한다.
- 새 Builder 기준으로 생성되는 씬에는 `RunBattleController`를 붙이지 않는다. 기존 `RunBattleController`와 구 `RunBattleView` 스크립트는 strict MVVM 전환 후 삭제했다.
- 몬스터 표시 경로는 `EnemyFormationSlotView` 단일 View로 유지한다. 이후 실제 아트가 들어와도 portrait, world HUD, DamageAnchor, collider는 formation slot prefab 안에서 명시적으로 연결한다.
- 2026-06-05: `dotnet build SlotRogue.slnx` 경고 0개, 오류 0개로 통과.
- 2026-06-05: 전체 `Rebuild Scene UI Prefabs` 실행으로 여러 UI prefab/scene의 외형이 크게 바뀌는 문제가 확인되어, 생성 자산은 원복했다.
- 2026-06-05: Builder 기본 rebuild는 기존 생성 자산을 덮어쓰지 않도록 막고, RunBattle은 기존 prefab이 있으면 새로 만들지 않고 hierarchy migration만 수행하도록 바꿨다. 진짜 덮어쓰기는 `Danger` 메뉴로 분리했다.
- 2026-06-05: strict MVVM migration은 기존 UI GameObject를 유지한 채 새 View 컴포넌트와 `CompositionRoot`를 붙이고, legacy `RunBattleController` / `RunBattleView` 컴포넌트를 제거하는 방식으로 구현했다. 메뉴 실행 시 닫혀 있는 `RunBattle.unity`도 additive로 열어 적용한다. Codex 쪽 Unity MCP가 Editor instance를 찾지 못해 실제 asset 적용은 Unity Editor에서 메뉴 실행이 필요하다.
- 2026-06-05: `RunBattleView`가 이미 씬의 `30_UI` 아래에 있는데 prefab 내부에도 `00_Composition` / `10_Systems` / `20_World` / `30_UI`가 다시 생기는 중첩 hierarchy 문제가 확인됐다. 보존형 migration을 scene-level 그룹과 UI-level 그룹으로 분리해, 다음 safe migration 실행 시 시스템/월드/조립 오브젝트는 `RunBattleSceneRoot` 아래로 이동하고 UI prefab 내부에는 `Run Battle Root`와 UI 섹션만 남기도록 수정했다.

## Completion

- **Finished**:
- **Outcome**:
- **Follow-ups**:
