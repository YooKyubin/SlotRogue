# 게임 플로우

**Status**: draft  
**Last updated**: 2026-07-02

## Purpose

게임 시작부터 시작 유물 선택, 전투, 보상, 다음 전투로 이어지는 무한모드 playable loop를 만든다. 슬롯과 전투는 각자의 책임을 유지하고, 실제 연결은 UI/GameFlow 계층에서 수행한다.

## Decisions

| # | 결정 | 요약 |
|---|------|------|
| F1 | [ADR-0002](../adr/0002-game-flow-is-scene-driven-ui-integration.md) | 씬 기반 플로우, 전투 비수정, UI/GameFlow 계층에서 슬롯 요청을 전투 Effect로 변환 |
| F2 | 전투 코어 수정 금지 | `BattleSystem`, 전투 Dev 하네스, 전투 테스트는 그대로 두고 public API만 사용한다. |
| F3 | 시작 유물은 프로토타입에서 비활성 | 스왑 프로토타입 런은 시작 유물 선택 없이 첫 전투로 진입한다. |
| F4 | 보상은 v33 제안 3택 | 보상 씬은 v33 제안 42종 중 3개를 제시하고, 심볼 가중치·Base·별조각 계열은 즉시 런 상태에 반영한다. |
| F5 | v1 런은 WaveSchedule + EncounterTable 기반 | 맵 노드 선택은 사용하지 않지만, `WaveScheduleDefinition`으로 전투 tier/구간을 계산하고 `EncounterTable`에서 `MonsterDefinition` 편성을 선택해 다음 전투 roster를 생성한다. |
| F6 | [ADR-0008](../adr/0008-ui-strict-mvvm-boundary.md) | UI는 strict MVVM을 따른다. View는 화면 상태 렌더링과 입력 event만 담당하고 SceneRoot가 순수 ViewModel 및 Flow Controller를 연결한다. |
| F7 | 슬롯 결과 연출은 전투 적용 전 큐로 재생 | `RunGame` Battle 화면은 스핀 후 스왑 대기 중에는 매칭 preview만 표시하고, `ATTACK` 입력 뒤 슬롯 계산과 유물 후처리를 확정한다. `SlotPresentationManager`가 패턴 → 유물 → 최종 결과 연출을 완료한 뒤 전투 Effect를 적용한다. |
| F8 | v33 제안/유물 프로토타입 | `RelicCatalog`는 v33 유물 44종 상점 데이터로 교체되었고, 전투 후 보상은 `RunRewardCatalog` v33 제안 42종을 사용한다. |
| F9 | [ADR-0014](../adr/0014-defeat-revive-window-and-relic-contribution.md) | 첫 패배는 5초 부활 유예 후 확정하고 최종 결과에 모든 보유 유물의 명목 기여량을 표시한다. |
| F10 | [ADR-0017](../adr/0017-first-run-tutorial-run-game-mode.md) | 최초 튜토리얼은 별도 Scene 복제가 아니라 `RunGame` 튜토리얼 모드로 실행한다. |

## Scene flow

```text
GameStart
├─ TutorialCompleted == false → RunGame / Tutorial Battle
└─ TutorialCompleted == true → RunGame / Battle
RunGame / Battle
├─ Victory → RunGame / Reward → RunGame / Battle → ...
└─ Defeat → RunGame / ReviveOffer
   ├─ Rewarded → RunGame / Battle
   └─ Timeout/No reward → RunGame / RunResult → StartRelicSelect
```

전투는 별도 `RunBattle` 씬이 아니라 `RunGame` 씬 내부 `BattleView` 상태다. 승리 연출이 끝나면 추가 버튼 입력 없이 보상 상태로 자동 전환한다. 첫 패배이면서 부활권이 남아 있으면 패배 View가 5초간 몬스터 초상화·카운트다운·광고 부활 버튼을 표시한다. Rewarded 부활은 몬스터 상태와 행동 순서를 유지하고 플레이어 HP만 최대 HP의 절반으로 복구해 같은 전투를 재개한다. 시간 초과 또는 광고 보상 실패 뒤에 최종 결과를 확정하며, 결과 화면은 모든 보유 유물의 발동 횟수와 누적 피해·방어·회복 기여를 표시한다.

## Runtime flow

```mermaid
sequenceDiagram
    participant Start as GameStart
    participant Battle as RunGame/Battle
    participant Reward as RunReward
    participant Defeat as RunDefeat
    Start->>Battle: 새 런 시작
    Battle->>Battle: WaveSchedule.Evaluate(CurrentBattleNumber)
    Battle->>Battle: EncounterSelector.Select()
    Battle->>Battle: RunEncounterRosterBuilder.Build(selection, context, balance)
    Battle->>Battle: SlotTurnController.SpinAsync()
    Battle->>Battle: 스왑 대기(매칭 preview만 표시)
    Battle->>Battle: ATTACK 입력 후 SlotTurnController.ResolveCurrentSpinResult()
    Battle->>Battle: RelicTurnResolver.Resolve()
    Battle->>Battle: CombatTurnRequestBuilder.Build()
    Battle->>Battle: SlotPresentationResult 생성
    Battle->>Battle: 패턴 → 유물 → 최종 결과 연출
    Battle->>Battle: SlotCombatRequestToCombatEffectsConverter.Convert()
    Battle->>Battle: BattleSystem.ApplyPlayerTurn()
    Battle->>Reward: Victory 자동 전환
    Reward->>Battle: 보상 저장 후 다음 전투
    Battle->>Defeat: ReviveOffer 자동 전환
    Defeat->>Battle: 5초 내 Rewarded 부활
    Defeat->>Defeat: Timeout/No reward → RunResult
    Defeat->>Battle: 새 런
```

## System boundary

| 영역 | 책임 |
|------|------|
| `SlotRogue.Slot` | 슬롯 결과 생성, 패턴 판정, `SlotCombatRequest` 생성 |
| `SlotRogue.Core.Combat` | `CombatEffect[]` 적용, HP/Shield/승패 처리 |
| `SlotRogue.UI.Combat` | 기존 `SlotCombatRequestToCombatEffectsConverter` |
| `SlotRogue.UI.GameFlow` | 씬 전환, 런 상태, 시작 유물/보상 후처리, 전투 API 호출 |

`SlotRogue.UI.GameFlow`는 전투 코드를 수정하지 않고 `BattleSystem` public API만 사용한다.

## MVP content

### 시작 유물

스왑 프로토타입 런은 시작 유물을 지급하지 않는다. `RunRewardService.IsStarterSelection`은 false이며, `RunGame` 단독 실행과 Title 경유 실행 모두 첫 화면을 전투로 둔다. 기존 v23 시작 유물 S-01~S-06은 v33 유물 교체 범위에서 런타임 카탈로그에 남기지 않는다.

### 보상

전투 승리 후 `RewardPanel 1`을 우선 탐색해 `RunRewardView`로 등록하고, v33 제안 기획서 42종 중 중복 없는 3종을 제시한다. 튜토리얼 첫 전투 승리도 프로토타입 검증을 위해 일반 런으로 전환한 뒤 같은 제안 화면으로 진입한다. 전투 등급별 보상 풀 분리는 아직 적용하지 않으며 모든 전투 등급이 같은 제안 풀을 사용한다.

| 제안 계열 | 현재 적용 |
|-----------|-----------|
| 심볼 가중치 | `GameFlowSession.SlotPool`에 즉시 반영 |
| 심볼 Base 피해 | `SlotSymbolAttackValues` 런 보너스로 즉시 반영 |
| 별조각 | `GameFlowSession.RunCoins`에 즉시 반영 |
| 배율/다시/상태/위험 계약 | 선택 기록만 남김. 전투 수식 연결은 후속 범위 |

상점 유물은 전투 중 `ShopPanel`에서 별조각으로 구매한다. 상점 패널은 기본적으로 닫혀 있고, 전투 화면의 상점 버튼을 눌렀을 때만 열린다. `ShopPanel` 하위 `GameFlowOptionView` 카드 5개를 상점 슬롯으로 직접 렌더링하며, 구매 가격은 v33 유물 기획서의 `price` 필드를 사용하고 리롤은 별조각 1개다. `R-01` 같은 즉시 지급 유물은 구매 즉시 별조각을 지급하고 인벤토리에 남기지 않는다. 심볼 Base 증가, 전투당 스왑 횟수 증가, 전투 시작 별조각 지급처럼 현재 엔진에 바로 연결 가능한 유물 효과는 런 상태에 반영한다. 배율/다시/저주 수식은 보유 유물로 표시하며 전투 계산 연결은 후속 범위다.

보상/상점 아이콘은 기존 Addressable `Symbol Sheet Highlight`와 `Relic Sheet Highlight` 캐시를 사용한다. 심볼 제안은 해당 심볼 아이콘을, 경제/상점/상태/위험 제안은 대표 유물 아이콘을 사용한다.

## UI image slots

MVP UI는 `Assets/_Project/Prefabs/UI/GameFlow/`의 View 프리팹으로 배치한다. 런타임 조립자는 배치된 UI를 참조해 텍스트, 버튼 이벤트, 상태 색상만 갱신한다. 이미지 교체 대상은 `GameFlowImageSlot` 컴포넌트의 `SlotId`로 찾는다.

| 씬/상태 | 주요 View | 런타임 조립 |
|---------|-----------|-------------|
| `Battle` | `BattleView`, `RunBattleScreenView`, `RunInventoryView` | `RunGameSceneRoot` + `BattleSceneCompositionRoot` + `BattleFlowController` + `RunInventoryViewModel` |
| `Reward` | `RewardPanel 1` / `RunRewardView` | `RunGameSceneRoot` + `RunRewardViewModel` |
| `Defeat` | `RunDefeatView` | `RunGameSceneRoot` + `RunDefeatViewModel` |

프리팹/씬 재생성이 필요하면 Unity 메뉴 `SlotRogue > Game Flow > Rebuild Scene UI Prefabs`를 실행한다. 해당 메뉴는 `GameFlowScenePrefabBuilder`가 제공한다.

| 씬 | 주요 SlotId |
|----|-------------|
| 공통 | `scene-background`, `<root>/frame` |
| `GameStart` | `start/hero`, `start/summary-panel` |
| `RunGame/StartRelicSelect` | 프로토타입에서는 비활성. 첫 진입은 전투 |
| `RunGame/Battle` | `battle/player-status-panel`, `battle/wave-panel`, `battle/arena`, `battle/slot-machine-panel`, `battle/slot-cell-00`~`battle/slot-cell-14`, `battle/attack-result-panel`, `battle/spin-button`, `battle/spin-result-panel`, `battle/status-panel`, `battle/energy-panel`, `battle/credits-panel`, `battle/presentation-overlay`, `battle/presentation/relic-inventory-origin` |
| `RunGame/Battle Shop` | `ShopPanel`. 상점 버튼 입력으로 열고 `GameFlowOptionView` 카드 5개와 `RerollButton`을 자동 탐색 |
| `RunReward` | `RewardPanel 1`의 제안 카드 3개. 메인 아이콘은 `GameFlowImageSlot`, 수치 배지는 `GameFlowOptionView`의 `CountImage` Image |

## Infinite Mode MVP

v1 런은 맵 노드 선택 없이 전투 번호 기반으로 무한 진행한다. `GameFlowSession.CurrentBattleNumber`는 1부터 증가하고, `BattleSceneCompositionRoot`가 `WaveScheduleDefinition`으로 만든 `WaveSchedule`을 평가해 현재 `EncounterTier`, 구간 index, 구간 내부 위치를 얻는다. 기본 `WaveScheduleDefault`는 10전투 패턴을 반복하며 5번째 전투를 Elite, 10번째 전투를 Boss로 만든다.

```text
BATTLE 1 (Normal)
→ BATTLE 2 (Normal)
→ ...
→ ELITE
→ ...
→ BOSS
→ ...
```

전투 진입 시 `BattleSceneCompositionRoot`는 `GameFlowSession.RunSeed`, `CurrentBattleNumber`, `WaveResult.EncounterTier`, `WaveResult.ThemeSectionIndex`를 사용한다. `EncounterThemeIndexSelector`가 `RunSeed + ThemeSectionIndex + ThemeGroupCount`로 `ThemeGroupIndex`를 결정하고, `EncounterSelector`는 `EncounterTable.GetEncounters(ThemeGroupIndex)` 결과에서 현재 tier 후보를 Weight 기반으로 결정적으로 선택한다. `EncounterThemeIndexSelector`는 `ThemeGroupCount > 1`일 때 직전 theme section 결과를 재계산해 인접한 10전투 구간이 같은 ThemeGroup을 반복하지 않도록 보정한다. 선택 결과는 `SelectedEncounterMonster` 목록이며, 각 항목은 원본 `MonsterDefinition`과 `FormationSlot`을 가진다.

`RunEncounterRosterBuilder`는 선택 결과를 받아 `EnemyCombatantFactory`와 `EnemyActionPlannerFactory`로 `RunEncounterRoster`를 조립한다. Builder는 Encounter 선택, Weight 추첨, tier 계산, formation slot 계산, HP 성장 공식을 직접 수행하지 않는다.

적 HP는 `MonsterDefinition.maxHp`를 원본 base HP로 유지하고, `EncounterBalanceSettings`가 만든 Core `EncounterBalanceConfig`와 `EncounterScaling`으로 런타임 max HP를 계산한다. `EncounterBalanceSettingsDefault`의 초기 값은 battle당 HP 증가, theme section당 HP 증가, Normal/Elite/Boss tier HP 배율을 보관한다. 계산 결과만 `EnemyCombatantFactory`에 전달되며, `MonsterDefinition` asset 자체는 수정하지 않는다.

튜토리얼이 아닌 전투는 항상 `EncounterTable` 선택 경로를 사용한다. `BattleSceneCompositionRoot`는 일반 전투에서 임시 dev 몬스터 override를 허용하지 않으며, 선택 결과는 `RunEncounterRosterBuilder.Build(selection, context, balance)`로만 조립한다.

## Battle MVP

`RunGame`의 Battle 화면은 세로 모바일 한 화면을 기준으로 다음 영역을 고정 배치한다. 전투 로직은 수정하지 않고, 기존 `BattleSystem` 상태를 HUD에 표시한다. `BattleSceneCompositionRoot`는 씬 참조와 객체 조립만 담당하고, `BattleFlowController`가 슬롯 → 유물 → 요청 합산 → 전투 적용 → Replay 순서를 관리한다. HUD·입력은 `BattleScreenController`, 적 선택은 `BattleTargetSelectionController`, 런 승패 반영은 `RunBattleResultRecorder`가 담당한다. 세부 계산은 각각 `SlotTurnController`, `RelicTurnResolver`, `CombatTurnRequestBuilder`, `BattlePresentationController`로 위임한다.

`BattleFlowController`는 `GameFlowSession`, Unity View, ViewModel을 직접 참조하지 않는다. `BattleSceneCompositionRoot`가 `BattleFlowContext`를 만들어 입력하고, 완료 시 받은 `BattleFlowResult`를 `RunBattleResultRecorder`에 전달한다.

```text
플레이어 HP/Shield HUD        Wave / 설정
몬스터 이름 + HP
몬스터 이미지 슬롯
5 x 3 슬롯 보드
공격 결과 / SPIN / 다음 공격
상태 / Energy / Credits
```

슬롯 셀은 `battle/slot-cell-00`~`battle/slot-cell-14`의 고정 크기 칸으로 배치한다. 몬스터 formation은 Overlay Canvas 밖의 월드 `BattleArenaRoot` 아래 `EnemyFormationSlot` 3개로 표시하며, 각 슬롯은 World Space Canvas HUD를 가진다. 현재 v1 전투 생성 경로는 `EncounterTable` 선택 결과의 `MonsterDefinition.Visual`을 통해 초상화와 combat visual prefab을 공급한다. 플로팅 데미지 spawn parent는 `battle/presentation-overlay`이고, 위치 기준은 각 월드 슬롯 자식 `DamageAnchor`이다.

스핀 결과는 먼저 스왑 대기 상태로 들어가며, 이때는 공격 수치를 계산하거나 표시하지 않고 Addressable `Symbol Sheet Highlight` 스프라이트와 짧은 tilt pulse cue로 매칭 셀만 보여준다. 플레이어가 스왑을 쓰거나 그대로 `ATTACK`을 누르면 그 시점의 보드로 패턴과 `SlotCombatRequest`를 확정한다. 패턴이 없으면 공격을 생성하지 않는다. 패턴 피해는 `심볼 기본 공격력 × 매칭 칸 수 × 족보 배율`로 계산하며, 현재 기본값은 체리 2, 레몬 2, 클로버 3, 종 4, 다이아 5, 7은 7이다.

`40_SlotMachineArea` 하위 `Relic Inventory Origin`은 전투 중 런 인벤토리 열기 버튼으로 사용한다. `RunInventoryViewModel`은 `GameFlowSession.SlotPool`과 `OwnedRelics`를 읽어 심볼 탭에는 6종 심볼의 현재 한 칸 출현 확률과 가중치를, 유물 탭에는 상점에서 구매한 보유 유물을 획득 순서대로 표시한다. View는 버튼/탭/닫기 입력 event와 렌더링만 담당하고, 탭 상태와 데이터 스냅샷은 SceneRoot가 ViewModel을 통해 갱신한다.

### 슬롯 결과 연출

`SlotRogue.UI.SlotPresentation`은 실제 계산 로직과 분리된 표시 계층이다. `SlotTurnController`는 스핀/스왑 대기 중에는 보드와 매칭 preview만 노출하고, `ATTACK` 이후 스핀 결과, 패턴 목록, 최종 `SlotCombatRequest`를 읽어 연출용 DTO를 만든다. 이 DTO는 전투 수치를 다시 계산하지 않는다. 큐 진행은 Coroutine으로 관리하고, 각 UI 이동/확대 애니메이션은 DOTween으로 재생한다.

슬롯 심볼은 `Symbol Sheet Normal`과 `Symbol Sheet Animation` Addressable Sprite에 `Image.SetNativeSize()`를 적용했을 때의 크기를 기준으로 1.25배 표시한다. 족보 연출은 `SlotPatternResolver.ResolveAll()`이 반환한 작은 족보 순서를 유지하며, 각 단계에서 해당 칸을 확대했다가 원래 크기로 복귀한 뒤 다음 단계로 이동한다. 유물이 해당 족보에서 발동하면 패턴 복귀 직후 유물 카드와 `공격력 이전값 → 적용값 (+증가량)`을 표시한 다음 다음 족보를 재생한다.

```text
SlotMachineViewModel.Spin()
→ 스왑 대기: 매칭 preview / 셀 강조 cue
→ ATTACK 입력
→ SlotMachineViewModel.ResolveCurrentSpinResult()
→ RelicTurnResolver.Resolve()
→ CombatTurnRequestBuilder.Build()
→ SlotPresentationResult 생성
→ SlotPresentationQueue 재생
   1. PatternPresentationView (작은 족보부터 확대 → 원복)
   2. 해당 패턴의 RelicPresentationView
   3. 다음 PatternPresentationView 반복
   4. FinalResultView
→ Completed callback
→ BattleSystem.ApplyPlayerTurn()
```

MVP 슬롯 판정은 현재 대표 패턴 1개만 반환하지만, `SlotPresentationResult.Patterns`와 `RelicTriggers`는 배열 기반이라 여러 패턴/여러 유물 발동으로 확장 가능하다. `SlotPatternPresentationResult.IsFinale`가 켜진 패턴은 일반 패턴을 대체하지 않고, 일반 패턴 정산 연출이 모두 끝난 뒤 추가 특별 연출로 재생한다.

연출만 확인할 때는 Unity 메뉴 `SlotRogue > Slot Presentation > Rebuild Demo Scene`으로 `Dev_SlotPresentation` 씬을 생성한다. 이 씬은 전투/런 상태를 사용하지 않고 `SlotPresentationDemoController`가 더미 패턴, 더미 유물, 최종 결과 DTO를 만들어 `SlotPresentationManager`에 직접 넣는다. 데모 결과는 15칸이 모두 체리 아이콘인 보드에서 작은 패턴, 가로 패턴 3줄, 세로 패턴, 대각선 패턴, 큰 패턴을 낮은 족보부터 순차 재생하고, 마지막에 `Perfect Spin x15`를 `IsFinale` 특별 패턴으로 재생한다. 패턴 SFX는 `Assets/Resources/Sounds`의 `SFX_C_Low`부터 `SFX_C_High`까지 단계적으로 연결한다. 배경은 `Assets/Resources/Textures/Background_Outside.png`와 `Background_Inside.png`를 사용하고, 슬롯 칸 및 유물 카드 아이콘은 `Icon_Slot.png`의 sub-sprite를 사용한다.

## Open questions

| ID | 질문 | 비고 |
|----|------|------|
| ~~Q1~~ | ~~몬스터/인카운터 생성 방식~~ | **닫음 (2026-06-20).** `WaveScheduleDefinition` → `EncounterSelector` → `EncounterSelection` → `RunEncounterRosterBuilder` 경로로 `MonsterDefinition` 기반 Encounter 선택을 재도입했다. 후속 후보는 테마별 EncounterTable 분리와 다수 적 실전 검증. |
| Q2 | 유물 카탈로그 외부 데이터화 | 현재는 ADR-0005에 따라 코드 카탈로그. 밸런스 반복 비용이 커지면 별도 ADR로 재결정. |
| Q3 | 전투 화면 시각화 | RunGame Battle 몬스터 formation은 월드 2D, 슬롯머신·플레이어 HUD·버튼·플로팅 텍스트는 Overlay UI로 유지한다. |
| Q4 | 런 종료/세이브 | 패배 View에서 결과를 표시하고 새 런만 제공한다. 저장은 추후. |

## Alternatives considered

### 기존 `BattleDevHarness` 재사용

Dev 하네스는 인스펙터 테스트에는 좋지만 슬롯 스핀과 씬 플로우를 직접 연결하기 어려워 본편 플로우에는 채택하지 않았다. 본편 통합 후 `Dev_Battle` 씬이 제거되어 참조가 끊긴 `BattleDevHarness`도 2026-06-12 삭제했다.
RunGame Battle overlay의 전투 텍스트 anchor/prefab 계약은 [`feature-floating-combat-text`](../exec-plans/completed/feature-floating-combat-text.md)에서 정리했다.

### 슬롯 View 재사용

`SlotMachineView`는 독립 슬롯 테스트용이다. 전투 턴 처리까지 묶으려면 상태 표시가 달라지므로 `RunGame` Battle 전용 UI를 만든다. 슬롯 계산은 동일하게 `SlotMachineViewModel`을 재사용한다.
