# 전투 코어 (Combat Core)

**Status**: draft  
**Last updated**: 2026-05-31 (Presentation 섹션 — ADR-0002)

## Purpose

슬롯 1스핀 = 전투 1턴 구조에서, **이미 계산된 Effect 목록**을 받아 HP·방어도·턴 진행·승패만 처리하는 순수 전투 코어를 정의한다. 슬롯 RNG·패턴·유물 수치 계산은 슬롯/메타 계층에 두고, 전투는 Participant 상태와 턴 파이프라인에 집중한다.

## Decisions

| # | 결정 | 요약 |
|---|------|------|
| C1 | [ADR-0001](../adr/0001-combat-turn-effect-pipeline.md) | 1스핀=1턴, Effect 목록 파이프라인, Shield-only 방어, Participant 소유 상태, CombatEvent 로그 |
| C2 | **슬롯 계산 / 전투 진행 분리** | 슬롯 asmdef는 Combat 타입을 참조하지 않는다 (`slot-core.md` S4). 연동 계층에서 `Effect[]` 변환. |
| C3 | **Resolver 순수 로직** | `BattleResolver`는 MonoBehaviour·UI 없이 EditMode 테스트 가능. |
| C4 | **플레이어·몬스터 동일 Effect 타입** | 행동 주체는 다르지만 `Kind + Amount + Target` 구조 공유. |
| C5 | [ADR-0002](../adr/0002-combat-presentation-replay.md) | MVP 연출: Replay — 동기 `ApplyPlayerTurn` 후 `CombatEvent` 순차 재생; HUD는 `CombatViewModel`; `EffectApplied` 스냅샷 |

## Turn pipeline

### Phase

| Phase | 설명 | 스핀 가능 |
|-------|------|-----------|
| `NotInBattle` | 전투 밖 | — |
| `PlayerTurn` | 플레이어 입력 대기 | ✓ |
| `Resolving` | 플레이어 Effect 적용 중 | ✗ |
| `EnemyTurn` | 몬스터 Effect 적용 중 | ✗ |
| `Ended` | Victory / Defeat | ✗ |

```mermaid
stateDiagram-v2
    [*] --> NotInBattle
    NotInBattle --> PlayerTurn: StartBattle(player, monster)

    PlayerTurn --> Resolving: ApplyPlayerTurn(effects)
    Resolving --> Ended: 몬스터 HP ≤ 0 (Victory)
    Resolving --> Ended: 플레이어 HP ≤ 0 (Defeat)
    Resolving --> EnemyTurn: 생존\n(몬스터 shield 초기화)

    EnemyTurn --> Ended: 플레이어 HP ≤ 0 (Defeat)
    EnemyTurn --> Ended: 몬스터 HP ≤ 0 (Victory)
    EnemyTurn --> PlayerTurn: 생존\n(플레이어 shield 초기화)

    Ended --> [*]
```

### 1턴 사이클

**플레이어 턴**

1. 슬롯 → `CombatEffect[]` 수신 (`ApplyPlayerTurn`)
2. 목록 순서대로 Effect 적용 (몬스터 shield가 있으면 Damage 감소·소진)
3. 몬스터 HP ≤ 0 → `Ended(Victory)`
4. 플레이어 HP ≤ 0 → `Ended(Defeat)`
5. 몬스터 **shield = 0**
6. `EnemyTurn`

**적 턴**

1. 이번 턴 **미리 정해진** 몬스터 Effect 목록 순서대로 적용
2. 사망 체크 (플레이어 Defeat / 몬스터 Victory)
3. 플레이어 **shield = 0**
4. `PlayerTurn`

```mermaid
sequenceDiagram
    participant Slot as Slot
    participant Battle as BattleSystem
    participant P as Player
    participant M as Monster

    Note over Battle: PlayerTurn
    Slot->>Battle: ApplyPlayerTurn(Effect[])
    loop 입력 순서
        Battle->>M: Damage / Shield / Heal
        Battle->>P: Damage / Shield / Heal
    end
    Battle->>M: shield 초기화
    alt 몬스터 사망
        Battle-->>Slot: BattleEnded(Victory)
    else 플레이어 사망
        Battle-->>Slot: BattleEnded(Defeat)
    else
        Note over Battle: EnemyTurn
        loop UpcomingEnemyActions
            Battle->>P: Effect 적용
            Battle->>M: Effect 적용
        end
        Battle->>P: shield 초기화
        Battle-->>Slot: PlayerTurn
    end
```

## Runtime data

### CombatEffect (MVP)

| 필드 | MVP | Later |
|------|-----|-------|
| `Kind` | `Damage`, `Shield`, `Heal` | `ApplyStatus`, `GainGold`, … |
| `Amount` | int | — |
| `Target` | `Self`, `Enemy` | 다중 적 시 확장 |
| _(optional)_ | — | `Element`, `Status`, `Duration` |

### Kind 동작

| Kind | 동작 |
|------|------|
| `Damage` | `실피해 = max(0, Amount - target.shield)` → shield 차감 → HP 감소 |
| `Shield` | `target.shield += Amount` |
| `Heal` | `target.hp = min(target.hp + Amount, target.maxHp)` |

스탯 방어력은 없다. 방어도는 `Shield` Effect로만 얻는다.

### Participant

플레이어·몬스터가 `currentHp`, `maxHp`, `shield`를 내부 관리한다. Resolver는 Participant에 Effect를 적용한다.

### Shield 지속

| 주체 | 유효 구간 | 초기화 |
|------|-----------|--------|
| 플레이어 shield | 적 턴 전체 | 적 턴 **모든** Effect 적용 후 |
| 몬스터 shield | 플레이어 턴 전체 | 플레이어 턴 **모든** Effect 적용 후 |

### Monster pattern (ScriptableObject)

몬스터 턴 패턴은 **불변 SO**로 저장하고, 전투 시작 시 런타임 `MonsterTurnSchedule`을 생성한다. 순환 index는 SO·asset에 두지 않는다.

| 타입 (SlotRogue.Data.Combat) | 역할 |
|------------------------------|------|
| `CombatEffectStep` | SO 직렬화용 step (`kind`, `amount`, `target`) |
| `MonsterTurnPatternDefinition` | 턴별 `CombatEffectStep[]` 배열 (불변 패턴) |
| `MonsterDefinition` | `maxHp`, `turnPattern` 참조 |
| `MonsterTurnScheduleFactory` | pattern SO → `new MonsterTurnSchedule(...)`; `StartBattle` 내부 `Reset()` |

- SO asset: `Assets/_Project/Data/Combat/` (예: Goblin + GoblinTurnPattern).
- Dev: `BattleDevHarness`는 `MonsterDefinition` SO **필수** — 미할당 시 Console `LogError`.
- 본편: encounter/run bootstrap에서 동일 Factory 경로 재사용 (별도 presenter plan).
- 구현: [`feature-monster-pattern-so`](../exec-plans/completed/feature-monster-pattern-so.md).

```mermaid
flowchart TD
    E[CombatEffect] --> K{Kind}
    K -->|Damage| D["실피해 = max(0, Amount - shield)\nshield -= 막은 양\nHP -= 실피해"]
    K -->|Shield| S["shield += Amount"]
    K -->|Heal| H["HP = min(HP + Amount, maxHp)"]
```

## System boundary

```mermaid
flowchart LR
    subgraph Slot["SlotRogue.Slot"]
        Spin --> Pattern --> Calc --> Build["Effect[] 또는 DTO"]
    end

    subgraph Data["SlotRogue.Data / Combat"]
        MonsterSO["MonsterDefinition SO"]
        PatternSO["MonsterTurnPatternDefinition SO"]
        Factory["MonsterTurnScheduleFactory"]
        PatternSO --> Factory
        MonsterSO --> Factory
    end

    subgraph Integration["SlotRogue.UI / Combat"]
        Adapt["SlotCombatRequestToCombatEffectsConverter"]
        Flow["BattleFlowController"]
        Pipe["CombatPresentationPipeline"]
        VM["CombatViewModel"]
        DevHarness["BattleDevHarness (Dev)"]
    end

    subgraph Combat["SlotRogue.Core / Combat"]
        Schedule["MonsterTurnSchedule"]
        StartBattle --> Resolver
        Factory --> Schedule
        Schedule --> StartBattle
        Adapt --> Flow
        Flow --> Apply["ApplyPlayerTurn() sync"]
        Apply --> Resolver
        Resolver --> Events["CombatEvent[]"]
        Resolver --> Participant["Player / Monster"]
        Events --> Pipe
        Pipe --> VM
    end

    Build --> Adapt
    Flow --> Apply
    DevHarness --> MonsterSO
    DevHarness --> StartBattle
    DevHarness --> Flow
```

슬롯 MVP는 `SlotCombatRequest` DTO를 출력하고, 변환은 `SlotRogue.UI.Combat` 연동 계층에서 수행한다 (아래 Q1 **닫음**).

## Battle API (MVP sketch)

| API | 역할 |
|-----|------|
| `StartBattle(player, monster, monsterTurnSchedule)` | 전투 시작 → `PlayerTurn`. 스케줄은 SO→Factory 또는 테스트에서 생성 |
| `ApplyPlayerTurn(IReadOnlyList<CombatEffect> effects)` | 플레이어 턴 처리. `PlayerTurn`이 아니면 거부 |
| `CurrentPhase` | UI·슬롯 스핀 가능 여부 |
| `UpcomingEnemyActions` | UI용 — **다음** 적 턴 Effect 목록 (스핀 전 표시) |
| `UpcomingMonsterTurnIndex` | UI용 — 스케줄 내 다음 적 턴 인덱스 (0-based, 순환) |

`StartBattle`의 파라미터 vs BattleSystem 멤버 보유는 구현 plan에서 확정한다.

## CombatEvent (MVP)

Resolver가 턴 처리 중 append한다. UI·디버그·테스트가 동일 소스를 구독한다.

| Kind | MVP 연출 |
|------|----------|
| `PhaseChanged` | 파이프라인 순차 await (연출 최소 가능) |
| `EffectApplied` | Kind별 Presenter; **Before/After HP·Shield 스냅샷** (ADR-0002, [`feature-combat-presentation`](../exec-plans/completed/feature-combat-presentation.md) 완료) |
| `ShieldReset` | 전용 Presenter |
| `BattleEnded` | 마지막 Damage 연출 **후** Result UI |

`EffectApplyResult` delta(`damageDealt`, `shieldConsumed`, …)는 로그·연출 보조용. HP 바 애니메이션의 권위는 **스냅샷**이다.

## Presentation layer (Replay)

ADR-0002. Core는 연출을 모른다. UI `BattleFlowController`가 턴당 이벤트 큐를 순차 재생한다.

```mermaid
sequenceDiagram
    participant Flow as BattleFlowController
    participant Core as BattleSystem
    participant Pipe as PresentationPipeline
    participant VM as CombatViewModel

    Note over Flow: _isBusy, PresentationContext sidecar
    Flow->>Core: cursor = Events.Count
    Flow->>Core: ApplyPlayerTurn(effects)
    loop new CombatEvents
        Flow->>Pipe: await PresentAsync(event)
        Pipe->>VM: snapshot-driven HUD tween
    end
    Flow->>VM: SyncFrom(Core)
```

| UI 타입 | 역할 |
|---------|------|
| `BattleFlowController` | `UniTask` 오케스트레이션, 입력 잠금 |
| `CombatPresentationPipeline` | `CombatEvent` → Presenter |
| `CombatViewModel` | 화면 HP/Shield (Core `Participant` 직접 바인딩 금지) |
| `PresentationContext` | crit, `PatternName` 등 — `CombatEffect` 비확장 |

**연출 규칙:** 이벤트·Effect **간** 순차; Effect **내** VFX/SFX/HUD 기본 `WhenAll`. Later Step API(`BattleTurnSession`) 도입 시 Presenter 계층 유지.

구현 plan (완료): [`feature-combat-presentation`](../exec-plans/completed/feature-combat-presentation.md).

## Open questions

| ID | 질문 | 비고 |
|----|------|------|
| ~~Q1~~ | ~~`SlotCombatRequest` → `CombatEffect[]` 변환 규칙~~ | **닫음 (2026-05-31).** Shield→Heal→Damage×N, 0값 스킵, `AttackCount`≤0이면 1타. `IsCritical`/`PatternName`은 Effect 없음(Console Request 로그만). 구현: `SlotCombatRequestToCombatEffectsConverter`. 상세: [`feature-combat-dev-scene`](../exec-plans/completed/feature-combat-dev-scene.md) 변환 MVP |
| ~~Q2~~ | ~~몬스터 행동 **배열 + 순환 인덱스**~~ | **닫음 (2026-05-31).** `MonsterTurnSchedule` — 턴 세트 `CombatEffect[][]`, 적용 후 index 순환. `UpcomingEnemyActions`/`UpcomingMonsterTurnIndex`. **패턴 SO:** `MonsterTurnPatternDefinition` + `MonsterDefinition` + `MonsterTurnScheduleFactory` ([`feature-monster-pattern-so`](../exec-plans/completed/feature-monster-pattern-so.md)). RNG·가중치는 Later. 스케줄 런타임: [`feature-monster-turn-schedule`](../exec-plans/completed/feature-monster-turn-schedule.md) |
| Q3 | `StartBattle` 시그니처 vs BattleSystem 멤버 | Participant 참조 전달 방식 |
| Q4 | Effect optional 필드 (`Element`, `Status`) 도입 시점 | 속성·DoT 추가 시 별도 ADR |
| Q5 | Kind별 Resolver 재정렬 필요 여부 | 디버프·상태가 많아지면 검토. MVP는 입력 순서 유지 (ADR-0001) |
| Q6 | 전투 asmdef 이름·Core 하위 네임스페이스 | `SlotRogue.Core.Combat` vs 별도 asmdef |

## Alternatives considered

### 단일 DTO 입력 — 거절

`SlotCombatRequest` 하나를 전투가 직접 소비하면 필드별 분기가 Resolver에 고정된다. Effect 목록 + 공통 Kind가 확장에 유리하다 (ADR-0001).

### 몬스터 전용 행동 타입 — 거절

`MonsterAction` enum과 플레이어 Effect 이중 Resolver를 피하고 동일 `CombatEffect`로 통합한다.

### 스탯 방어력 + Shield — 거절

MVP는 Shield Effect만 사용한다. 상시 방어력 스탯은 Later.

## Related docs

- [`slot-core.md`](./slot-core.md) — 슬롯 MVP, S4 전투 참조 금지
- [`../adr/0001-combat-turn-effect-pipeline.md`](../adr/0001-combat-turn-effect-pipeline.md)
- [`../adr/0002-combat-presentation-replay.md`](../adr/0002-combat-presentation-replay.md)
- [`../exec-plans/completed/feature-combat-presentation.md`](../exec-plans/completed/feature-combat-presentation.md)
- 개인 scratch 다이어그램: `docs/_scratch/combat-turn-pipeline-diagrams.md` (gitignored)
