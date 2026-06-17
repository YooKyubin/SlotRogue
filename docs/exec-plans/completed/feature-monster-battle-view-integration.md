# Monster Battle View 연결

**Status**: completed  
**Started**: 2026-06-16  
**Owner**: _(전투/UI 담당)_  
**Related design-docs**: [`../../design-docs/game-flow.md`](../../design-docs/game-flow.md), [`../../design-docs/combat-core.md`](../../design-docs/combat-core.md), [`../../adr/0003-combat-presentation-replay.md`](../../adr/0003-combat-presentation-replay.md), [`../../adr/0006-runtime-asset-loading-boundary.md`](../../adr/0006-runtime-asset-loading-boundary.md)

## Goal

`MonsterDefinition`에서 선택한 몬스터 전투 외형 프리팹을 RunGame 전투 화면의 올바른 `EnemyFormationSlotView` 아래에 생성한다. 기존 HP, Shield, Intent, 클릭 선택, Damage Anchor 동작은 유지하고, 실제 적 행동 presentation 흐름에서 해당 몬스터의 Idle과 Attack 애니메이션을 재생할 수 있는 기본 경로를 완성한다.

## Checklist

- [x] `MonsterVisualDefinition` ScriptableObject 추가 및 `MonsterDefinition`에 visual 참조 연결
- [x] 기존 `MonsterDefinition.portrait` 사용처 확인 후 presentation 기반 경로로 이전
- [x] `EnemyEncounterUnit`이 `BuildFromMonsterDefinition()` 경로의 원본 `MonsterDefinition`을 보관하도록 확장
- [x] `BuildForTier()` 경로는 definition 공급자가 없음을 명시하고 null/fake definition 없이 실패하도록 정리
- [x] HUD, 클릭 선택, combat visual binding이 같은 slot을 쓰도록 formation slot resolver 공통화
- [x] `BattleScreenController.BeginBattle()`에서 기존 combat visual 정리 후 roster 기반 prefab binding 수행
- [x] `RunBattleScreenView` → `RunBattleWorldView` → `EnemyFormationView` → `EnemyFormationSlotView` combat visual 전달 API 추가
- [x] `EnemyFormationSlotView`에 `VisualRoot`와 combat visual prefab instance 수명 관리 추가
- [x] `IEnemyCombatVisual`과 `MoonRabitCombatVisual` 추가로 Animator 참조 검증 및 Idle/Attack 요청 API 준비
- [x] 기존 combat presentation command 경로에 enemy attack animation 명령 연결
- [x] `ActionStartedPresenter` 명령 전달 단위 테스트 추가
- [x] 관련 EditMode 테스트 추가 및 `dotnet build SlotRogue.sln --no-restore` 검증
- [x] Unity Editor에서 VisualRoot, MoonRabit visual SO, combat visual prefab, Animator, Idle/Attack clip wiring 수동 검증

## Notes

- 이번 작업은 Addressables 기반 prefab 로딩, Hit/Death 애니메이션, 애니메이션 완료 대기, 스킨 교체, tier 기반 외형 결정 정책을 포함하지 않는다.
- `MonsterVisualDefinition`은 Data assembly가 UI 타입을 직접 참조하지 않도록 `GameObject CombatVisualPrefab`을 저장한다. 실제 `IEnemyCombatVisual` 컴포넌트 검사는 View 계층에서 수행한다.
- `Render()`는 HP, Shield, Intent, 선택 상태처럼 반복 갱신되는 상태만 다룬다. combat visual prefab 생성과 제거는 전투 시작 binding과 slot 수명 관리에서 처리한다.
- Attack 애니메이션은 별도 병렬 전투 이벤트 시스템을 만들지 않고 ADR-0003의 Replay/presentation command 흐름에 붙인다. 이번 단계에서는 애니메이션 완료를 기다리지 않는다.
- 2026-06-16: 데이터 연결 1차 범위로 `MonsterVisualDefinition`, `MonsterDefinition.Visual`, `EnemyEncounterUnit.Definition`을 추가한다. 코드에서 기존 `MonsterDefinition.portrait` 직접 사용처는 없고 asset YAML에만 남아 있어 visual SO로 이전한다. `BuildForTier()`는 `MonsterDefinition` 공급자가 없어 null/fake definition을 만들지 않고 명시적으로 실패시키며, production 호출 위치는 `BattleSceneCompositionRoot.CreateEncounterRoster()`의 dev override 미설정 경로다.
- 2026-06-16: `dotnet build SlotRogue.slnx --no-restore`는 경고 0개, 오류 0개로 통과했다. `dotnet build SlotRogue.sln --no-restore`는 기존 `Assembly-CSharp-firstpass.csproj`의 `System.Net.Http` 버전 충돌 경고 1개가 남지만 오류 0개로 통과했다.
- 2026-06-16: 전투 시작 시 `BattleScreenController.BeginBattle()`에서 각 `EnemyEncounterUnit`의 `Definition.Visual.CombatVisualPrefab`을 읽어 `RunBattleScreenView` → `RunBattleWorldView` → `EnemyFormationView` → `EnemyFormationSlotView`로 전달한다. 적 선택 Controller에는 combat visual binding 책임을 추가하지 않았다. `dotnet build SlotRogue.sln --no-restore`는 기존 `Assembly-CSharp-firstpass.csproj`의 `System.Net.Http` 버전 충돌 경고 1개가 남지만 오류 0개로 통과했다.
- 2026-06-16: 몬스터 외형 데이터 명칭은 `MonsterVisualDefinition`/`CombatVisualPrefab`으로 통일했다. 전투 이벤트 연출 계층의 `Presentation` 용어는 기존 의미가 달라 유지한다.
- 2026-06-16: `EnemyFormationSlotView`가 전달받은 `CombatVisualPrefab`을 `VisualRoot` 아래에 생성하고, 같은 슬롯 재바인딩 시 이전에 생성한 인스턴스만 제거하도록 변경했다. `EnemyFormationSlot` prefab에는 HUD/Intent/DamageAnchor/ClickCollider와 분리된 `VisualRoot`를 추가했고, 기존 portrait placeholder와 SpriteRenderer는 fallback 표시를 막기 위해 비활성화했다. `GoblinCombatVisual`, `MoonRabitCombatVisual` 최소 SpriteRenderer prefab을 추가해 각 `MonsterVisualDefinition.CombatVisualPrefab`에 연결했다.
- 2026-06-16: 전투 외형 공통 요청 인터페이스 `IEnemyCombatVisual`과 MoonRabit 전용 `MoonRabitCombatVisual`을 추가했다. `EnemyFormationSlotView`는 생성한 combat visual instance에서 인터페이스를 찾아 보관하고, 생성 직후 `PlayIdle()`을 한 번 호출하며, `PlayCombatVisualAttack()` 전달 API만 준비했다. 당시 실제 적 행동과 Attack 애니메이션 연결은 후속으로 남겼고, Animation Clip/Animator Controller/prefab component wiring은 Unity Editor 작업으로 남겼다.
- 2026-06-16: `RunBattleWorldViewTests`의 combat visual prefab 생성 테스트를 확장해 `IEnemyCombatVisual` 보관, 생성 직후 Idle 1회 요청, Attack 전달 API, 재바인딩 시 참조 초기화를 검증했다. `dotnet build SlotRogue.sln --no-restore`는 기존 `Assembly-CSharp-firstpass.csproj`의 `System.Net.Http` 버전 충돌 경고 1개가 남지만 오류 0개로 통과했다.
- 2026-06-17: `MonsterVisualDefinition.CombatVisualPrefab`은 `GameObject` 참조로 유지하고, `EnemyFormationSlotView`가 prefab 생성 직후 `IEnemyCombatVisual`을 1회 조회하는 방식을 유지한다. `EnemyCombatVisual` typed prefab 참조를 Data 또는 중립 assembly로 올리는 방식도 검토했지만, 전투 외형 제어 컴포넌트는 UI/View 책임에 가깝고 Data가 UI 타입을 알지 않도록 현재 구조를 유지한다. 별도 visual registry는 중복 wiring과 catalog 책임을 만들기 때문에 이번 범위에서는 도입하지 않는다. 조회는 반복 렌더링 경로가 아니라 prefab 생성 직후 1회만 수행한다.
- 2026-06-17: 적 행동 시작 시 `CombatEventKind.ActionStarted`를 기록하고, `ActionStartedPresenter`가 `ICombatPresentationCommands.PlayEnemyAttackAsync()`로 변환해 `RunBattleScreenView` → `RunBattleWorldView` → `EnemyFormationView` → `EnemyFormationSlotView.PlayCombatVisualAttack()` 경로로 전달한다. 이 요청은 공격 애니메이션을 시작만 하고 완료를 기다리지 않는다. 타격 프레임 및 애니메이션 종료 동기화는 후속 작업으로 남긴다.
- 2026-06-17: 현재는 모든 적 `ActionStarted`가 `PlayAttack()`으로 연결된다. ActionKey/ActionType을 이벤트에 실어 공격·방어·회복 등 action별 애니메이션을 고르는 작업, Animation Event 기반 타격 프레임 동기화, 공격 애니메이션 종료 UniTask 대기는 후속 작업으로 남긴다.
- 2026-06-17: 전투 Formation 경로의 portrait 표시 API와 `BattleTargetSelectionController` portrait 초기화는 남아 있지 않다. `MonsterVisualDefinition.Portrait`는 도감/선택 화면 등 전투 Formation 외 용도를 위해 유지한다.
- 2026-06-17: `ActionStartedPresenterTests`를 추가해 `ActionStarted`가 source participant id를 `PlayEnemyAttackAsync()`로 한 번 전달하고 다른 presentation command를 호출하지 않는지 검증했다. `RunBattleWorldViewTests`는 combat visual prefab이 `VisualRoot` 아래 한 번 생성되고 재바인딩 시 이전 인스턴스와 `IEnemyCombatVisual` 참조가 정리되며, participant id에 대응하는 슬롯만 Attack 요청을 받는지 검증한다.
- 2026-06-17: 코드 검색 기준 전투 Formation 경로의 `SetEnemyPortrait`, portrait fallback, 구 combat visual 명칭은 남아 있지 않다. `BattleView`는 RunGame 화면 상태 이름으로 유지한다. PlayMode에서 MoonRabit Idle loop, Attack 후 Idle 복귀, 실제 Animator transition은 Unity Editor 설정에 의존하므로 수동 검증 항목으로 유지한다.

## Current Flow

현재 전투 시작 시 `MonsterDefinition`에서 선택한 combat visual prefab이 formation slot까지 전달되고, `EnemyFormationSlotView`가 생성된 instance에서 `IEnemyCombatVisual`을 조회해 Idle 요청을 보낸다. 적 행동 시작 시에는 기존 CombatEvent Replay와 presentation command 경로를 통해 해당 participant에 대응하는 슬롯의 `PlayAttack()`을 호출한다. 공격 애니메이션 타격 프레임과 종료 대기는 후속 작업으로 남긴다.

```mermaid
flowchart TD
    subgraph Core["SlotRogue.Core.Combat"]
        BattleSystem["BattleSystem"]
        EnemyCombatant["EnemyCombatant"]
        CombatParticipant["CombatParticipant"]
    end

    subgraph Data["SlotRogue.Data.Combat"]
        MonsterDefinition["MonsterDefinition"]
        MonsterVisualDefinition["MonsterVisualDefinition"]
        CombatVisualPrefab["GameObject CombatVisualPrefab"]
    end

    subgraph GameFlow["SlotRogue.UI.GameFlow"]
        BattleFlowController["BattleFlowController"]
        BattleScreenController["BattleScreenController"]
        RunBattleScreenViewModel["RunBattleScreenViewModel"]
        EnemyEncounterUnit["EnemyEncounterUnit"]
        RunEncounterRoster["RunEncounterRoster"]
        RunBattleScreenView["RunBattleScreenView"]
        RunBattleWorldView["RunBattleWorldView"]
        EnemyFormationView["EnemyFormationView"]
        EnemyFormationSlotView["EnemyFormationSlotView"]
        IEnemyCombatVisual["IEnemyCombatVisual"]
        MoonRabitCombatVisual["MoonRabitCombatVisual"]
    end

    BattleFlowController --> BattleSystem
    BattleSystem --> EnemyCombatant
    EnemyCombatant --> CombatParticipant

    RunEncounterRoster --> EnemyEncounterUnit
    EnemyEncounterUnit --> EnemyCombatant
    EnemyEncounterUnit --> MonsterDefinition
    EnemyEncounterUnit -->|"FormationSlot"| BattleScreenController

    MonsterDefinition --> MonsterVisualDefinition
    MonsterVisualDefinition --> CombatVisualPrefab

    BattleFlowController --> BattleScreenController
    BattleScreenController --> RunBattleScreenViewModel
    BattleScreenController --> RunBattleScreenView
    BattleScreenController -->|"reads Definition.Visual.CombatVisualPrefab"| EnemyEncounterUnit

    BattleScreenController -->|"SetEnemyCombatVisualPrefab(slot, prefab)"| RunBattleScreenView
    RunBattleScreenView -->|"SetEnemyCombatVisualPrefab"| RunBattleWorldView
    RunBattleWorldView -->|"SetCombatVisualPrefab"| EnemyFormationView
    EnemyFormationView -->|"slot lookup"| EnemyFormationSlotView

    EnemyFormationSlotView -->|"Instantiate(prefab, VisualRoot)"| CombatVisualPrefab
    EnemyFormationSlotView -->|"GetComponentInChildren<IEnemyCombatVisual>()"| IEnemyCombatVisual
    MoonRabitCombatVisual -.implements.-> IEnemyCombatVisual
    EnemyFormationSlotView -->|"PlayIdle / PlayAttack"| IEnemyCombatVisual

    BattleSystem -->|"ActionStarted(sourceParticipantId)"| BattlePresentationController
    BattlePresentationController --> CombatPresentationPipeline
    CombatPresentationPipeline -->|"ActionStartedPresenter"| ICombatPresentationCommands
    ICombatPresentationCommands -->|"PlayEnemyAttack"| RunBattleScreenView
    RunBattleScreenView -->|"participantId"| RunBattleWorldView
    RunBattleWorldView -->|"participantId"| EnemyFormationView
    EnemyFormationView -->|"participantId mapping"| EnemyFormationSlotView
```

```mermaid
sequenceDiagram
    participant Flow as BattleFlowController
    participant Screen as BattleScreenController
    participant Unit as EnemyEncounterUnit
    participant Def as MonsterDefinition
    participant VisualDef as MonsterVisualDefinition
    participant ScreenView as RunBattleScreenView
    participant WorldView as RunBattleWorldView
    participant Formation as EnemyFormationView
    participant Slot as EnemyFormationSlotView
    participant Prefab as MoonRabitCombatVisual.prefab
    participant Visual as MoonRabitCombatVisual

    Flow->>Screen: BeginBattle(roster)
    Screen->>Unit: enemy list 순회
    Screen->>Unit: FormationSlot
    Screen->>Unit: Definition
    Unit->>Def: MonsterDefinition
    Def->>VisualDef: Visual
    VisualDef->>Screen: CombatVisualPrefab

    Screen->>ScreenView: SetEnemyCombatVisualPrefab(slot, prefab)
    ScreenView->>WorldView: SetEnemyCombatVisualPrefab(slot, prefab)
    WorldView->>Formation: SetCombatVisualPrefab(slot, prefab)
    Formation->>Slot: SetCombatVisualPrefab(prefab)

    Slot->>Prefab: Instantiate under VisualRoot
    Slot->>Visual: Get IEnemyCombatVisual from instance
    Slot->>Visual: PlayIdle()
```

## Completion

- **Finished**: 2026-06-17
- **Outcome**: `MonsterVisualDefinition.CombatVisualPrefab`이 RunGame 전투 formation slot의 `VisualRoot` 아래에 생성되고, 생성된 prefab의 `IEnemyCombatVisual`을 통해 Idle/Attack 요청을 전달한다. 적 행동 시작은 `CombatEventKind.ActionStarted` → `ActionStartedPresenter` → `ICombatPresentationCommands.PlayEnemyAttackAsync()` → `RunBattleScreenView` → `RunBattleWorldView` → `EnemyFormationView` → `EnemyFormationSlotView.PlayCombatVisualAttack()` 경로로 연결했다. `MoonRabitCombatVisual`은 Animator state `Idle`/`Attack`을 재생하며, Unity Editor에서 MoonRabit prefab, Animator Controller, Idle/Attack clip, Attack→Idle transition wiring을 수동 설정했다.
- **Verification**: `dotnet build SlotRogue.sln --no-restore`는 기존 `Assembly-CSharp-firstpass.csproj`의 `System.Net.Http` 버전 충돌 경고 1개만 남기고 오류 0개로 통과했다. `dotnet test SlotRogue.sln --no-build`는 종료 코드 0으로 통과했다. `RunBattleWorldViewTests`는 combat visual 생성·재바인딩·정리·participant id 기반 Attack 전달을 검증하고, `ActionStartedPresenterTests`는 presenter가 source participant id로 enemy attack command를 1회 호출하는지 검증한다.
- **Follow-ups**: 현재는 모든 적 `ActionStarted`가 `PlayAttack()`으로 연결된다. ActionKey/ActionType을 CombatEvent에 포함해 action별 애니메이션을 선택하는 작업, Animation Event 기반 타격 프레임 동기화, 공격 애니메이션 종료 UniTask 대기, Hit/Death 애니메이션은 후속 plan에서 다룬다.
