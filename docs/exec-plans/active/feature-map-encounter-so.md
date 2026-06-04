# 맵 노드 Encounter SO (RunEncounterDefinition)

**Status**: active  
**Started**: 2026-06-04  
**Owner**: _(게임 플로우/전투 데이터 담당)_  
**Contributors**: _(없음)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md), [`game-flow.md`](../../design-docs/game-flow.md)  
**Related ADR**: [`ADR-0004`](../../adr/0004-multi-participant-combat.md), [`ADR-0002`](../../adr/0002-game-flow-is-scene-driven-ui-integration.md)  
**Predecessor plan**: [`feature-multi-participant-play-ui`](../completed/feature-multi-participant-play-ui.md) (RunBattle 다인전 HUD·타겟·roster, 2026-06-04)

## Background

[`feature-multi-participant-play-ui`](../completed/feature-multi-participant-play-ui.md) Phase 1 **A안**으로 `RunMapNodeDefinition.EnemyCount`만 추가해 2/3몹 roster를 검증했다. 이후 본 plan에서 encounter SO·노드 SO·그래프 SO까지 이전했다.

| 영역 | 초기 (A안) | 완료 후 |
|------|------------|---------|
| 맵 노드 | 코드 카탈로그 + `EnemyCount` | `RunMapNodeDefinition` SO, 노드 asset마다 `encounter` 직접 참조 |
| 맵 그래프 | `RunMapNodeCatalog` 하드코딩 | `RunMapGraphDefinition` SO (`Resources/DefaultRunMapGraph`) |
| roster | `RunBattleController` 내부 | `RunEncounterRosterBuilder` — `encounter.entries`만 사용 |
| 몬스터 종류 | floor/type 휴리스틱 | entry별 `MonsterDefinition` |
| 배치 | roster index = slot | entry `formationSlot` → HUD |
| 전투 Core | `StartBattle(...)` | **변경 없음** |
| RunBattle UI | HUD·타겟·Converter | **대규모 재작업 없음** |

**검증 노드 (Play Mode):**

| nodeId | 표시 이름 | encounter SO |
|--------|-----------|----------------|
| `monster-1-0` 등 1몹 Monster | Monster 1-A … | `Encounter_SingleMonster` |
| `elite-1-2` 등 1몹 Elite | Elite 1-C … | `Encounter_SingleElite` |
| `monster-2-0` | Duo 2-A | `Encounter_Duo2A` (2 entry) |
| `elite-2-1` | Elite Trio 2-B | `Encounter_EliteTrio2B` (3 entry) |
| `boss-6-1` | Boss Gate | `Encounter_Boss` |
| `start-0` | Start | 없음 (전투 없음) |

맵 경로 예: Start → Monster 1-A → Duo 2-A / Start → Elite 1-C → Elite Trio 2-B

## Goal

맵 전투 노드가 **`RunEncounterDefinition` ScriptableObject**로 몬스터 수·종류·HUD 배치·(선택) HP/턴 패턴을 정의하고, `RunBattleController`는 **roster 빌드 입력만 SO로 교체**한 채 기존 다인전 Play 경로(HUD·타겟·Core id `100+index`)를 유지한다.

**추가 완료 (follow-up):** 맵 노드·그래프 전체 SO화, `RunEncounterAssetCatalog` 제거, `enemyCount` 제거, [`coding-style.md`](../../guides/coding-style.md)에 맞춘 `[SerializeField] private` + `_camelCase` 필드.

## Out of scope

- RunBattle HUD / 타겟 / `SlotCombatRequestToCombatEffectsConverter` / Presenter **대규모** 재작업.
- `BattleSystem` Core 규칙·invalid target fallback 변경.
- 플레이어 파티 2인 이상.
- encounter pool·층별 랜덤 테이블 (후속).
- `Resources.Load` → Addressables 전환 ([`coding-style.md`](../../guides/coding-style.md) 권장과 상충 — 후속).

## Phases

---

### Phase 0 — 계약·갭 확인

- [x] [`ADR-0004`](../../adr/0004-multi-participant-combat.md), [`combat-core.md`](../../design-docs/combat-core.md) Multi-participant·Validation 재확인
- [x] 완료 plan Follow-ups: [`feature-multi-participant-play-ui`](../completed/feature-multi-participant-play-ui.md) Completion
- [x] 핵심 코드 읽기 완료
- [x] encounter SO 우선 규칙 확정 (아래 Notes)

---

### Phase 1 — `RunEncounterDefinition` SO (Data)

- [x] `RunEncounterDefinition`, `RunEncounterEntry` (`SlotRogue.Data.GameFlow`)
- [x] 샘플: `Encounter_Duo2A`, `Encounter_EliteTrio2B`
- [x] 템플릿 추가: `Encounter_SingleMonster`, `Encounter_SingleElite`, `Encounter_Boss`

---

### Phase 2 — Roster 빌더 + 맵 노드 참조

- [x] `RunEncounterRosterBuilder` — encounter `entries` 기반 roster
- [x] encounter 없음 → `InvalidOperationException` (Start 제외, 전투 노드는 encounter 필수)
- [x] `RunBattleController` → 빌더 위임
- [x] ~~`RunEncounterAssetCatalog`~~ 제거 → 노드 SO `_encounter` 직접 참조

---

### Phase 3 — Formation slot → UI 배치

- [x] `RunEncounterRoster.FormationSlots`
- [x] `BindEnemySlots` / formation slot → HUD
- [x] 중복·범위 밖 slot: `Debug.LogWarning` + roster index 폴백

---

### Phase 4 — 테스트·문서·완료 처리

- [x] EditMode: `RunEncounterRosterBuilderTests` (`SlotRogue.UI.Tests`, `SlotRogue.Data` asmdef 참조)
- [ ] Play Mode 체크리스트 (아래 표) — 수동 확인
- [x] 본 plan·[`game-flow.md`](../../design-docs/game-flow.md) 갱신
- [ ] `git mv` → `docs/exec-plans/completed/` (Play Mode 회귀 후)
- [ ] [`STATUS.md`](../../STATUS.md) Active → Recently completed

---

## Play Mode 체크리스트

| # | 시나리오 | 진입 | 기대 |
|---|----------|------|------|
| 1 | **1몹 회귀** | `monster-1-0` 등 | `Encounter_SingleMonster` 1 entry, 중앙 HUD |
| 2 | **Duo 2-A** | → `monster-2-0` | 2몹, formation 0/2 |
| 3 | **Elite Trio 2-B** | → `elite-2-1` | 3몹, formation 0/1/2 |
| 4 | **배치** | Duo/Trio | 좌/중/우 HUD 시각 확인 |
| 5 | **Boss** | → `boss-6-1` | `Encounter_Boss` 1 entry |

---

## 완료 정의 (Definition of done)

- [x] `RunEncounterDefinition` + 샘플·템플릿 encounter asset
- [x] `RunEncounterRosterBuilder` 단일 전환 지점
- [x] 전투 노드 encounter SO 연결 (16노드)
- [x] `RunMapNodeDefinition` / `RunMapGraphDefinition` SO + `DefaultRunMapGraph`
- [x] formation slot → HUD; Core id `100+index` 불변
- [ ] Play Mode 체크리스트 전항
- [ ] plan `completed/` + STATUS 갱신

---

## 구현 메모 (파일 힌트)

| 작업 | 주요 파일 |
|------|-----------|
| Encounter SO | `Assets/_Project/Data/GameFlow/Encounters/` |
| Map node SO | `Assets/_Project/Data/GameFlow/MapNodes/Node_*.asset` |
| Map graph SO | `Assets/_Project/Resources/DefaultRunMapGraph.asset` |
| 에디터 재생성 | `SlotRogue/Game Flow/Build Default Map Graph Assets` → `RunMapGraphAssetBuilder.cs` |
| Roster 빌더 | `RunEncounterRosterBuilder.cs` |
| 맵 로더 | `RunMapNodeCatalog.cs` (`Resources.Load`, `ConfigureGraph` 테스트 주입) |
| 노드 타입 | `RunMapNodeDefinition.cs` (`SlotRogue.Data.GameFlow`) |

**데이터 흐름:**

```text
DefaultRunMapGraph (SO)
  → nodes[]: RunMapNodeDefinition (SO)
      → _encounter: RunEncounterDefinition (SO)
          → entries[] → RunEncounterRosterBuilder → RunBattle
```

**asmdef:** Data에 `RunMapNodeDefinition`·`RunMapGraphDefinition`; UI.Tests에 `SlotRogue.Data` 참조.

## Notes

- **roster 규칙 (현행):** `node.Encounter` + `entries.Length > 0` → SO roster만. 없으면 예외.
- **몬스터 수:** `encounter.entries.Length` 단일 소스. ~~`EnemyCount`~~ 제거됨.
- **HP/schedule:** entry override → entry `MonsterDefinition` → Inspector `_monsterDefinition` → 노드 floor/type 휴리스틱.
- **participant id:** `100 + rosterIndex`. UI 배치는 `formationSlot`.
- **노드 SO 필드:** [`coding-style.md`](../../guides/coding-style.md) — `[SerializeField] private` + `_camelCase` (예: `_nodeID`, `_encounter`). public 프로퍼티 `NodeId` 등.

## Follow-up (2026-06-04): 노드 SO 이전

- [x] `RunMapNodeDefinition` / `RunMapGraphDefinition` → `SlotRogue.Data` ScriptableObject
- [x] 노드 asset에서 `RunEncounterDefinition` 직접 참조 (`RunEncounterAssetCatalog` 제거)
- [x] 전투 노드 16개 encounter SO 연결
- [x] `Resources/DefaultRunMapGraph.asset` + `RunMapGraphAssetBuilder`
- [x] `enemyCount` 제거
- [x] `[SerializeField] private` + `_camelCase` / `_nodeID` ([`coding-style.md`](../../guides/coding-style.md))

## Completion

_(Play Mode 회귀 후 `completed/`로 옮길 때 채움.)_

- **Finished**:
- **Outcome**:
- **Follow-ups**:
