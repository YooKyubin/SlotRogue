# 맵 노드 Encounter SO (RunEncounterDefinition)

**Status**: active  
**Started**: 2026-06-04  
**Owner**: _(게임 플로우/전투 데이터 담당)_  
**Contributors**: _(없음)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md), [`game-flow.md`](../../design-docs/game-flow.md)  
**Related ADR**: [`ADR-0004`](../../adr/0004-multi-participant-combat.md), [`ADR-0002`](../../adr/0002-game-flow-is-scene-driven-ui-integration.md)  
**Predecessor plan**: [`feature-multi-participant-play-ui`](../completed/feature-multi-participant-play-ui.md) (RunBattle 다인전 HUD·타겟·roster, 2026-06-04)

## Background

[`feature-multi-participant-play-ui`](../completed/feature-multi-participant-play-ui.md) Phase 1 **A안**으로 `RunMapNodeDefinition.EnemyCount`만 추가해 2/3몹 roster를 검증했다. 모든 적은 동일 HP·동일 `MonsterTurnSchedule`, roster index = UI slot index = `CombatParticipantId` `100 + index`.

| 영역 | 현재 | 이번 plan 목표 |
|------|------|----------------|
| 맵 노드 | `EnemyCount` int (코드 생성) | `RunEncounterDefinition` SO 참조 (선택) |
| roster | `RunBattleController.BuildEncounterRoster` | 동일 진입점, **빌더**가 SO·폴백 읽기 |
| 몬스터 종류 | Inspector 단일 `_monsterDefinition` 또는 floor/type 휴리스틱 | entry별 `MonsterDefinition` |
| 배치 | roster index = slot index | entry `FormationSlot` → HUD slot layout |
| HP / 패턴 | 노드 단위 1세트 | entry별 override (선택), 없으면 정의·노드 폴백 |
| 전투 Core | `StartBattle(player, enemies[], schedules[])` | **변경 없음** |
| RunBattle UI | HUD·타겟·Converter·Presenter | **대규모 재작업 없음** — `BindEnemySlots` 배치 매핑만 |

**검증 노드 (회귀·다인전):**

| nodeId | 표시 이름 | 현재 EnemyCount | SO 연결 후 기대 |
|--------|-----------|-----------------|-----------------|
| `monster-2-0` | Duo 2-A | 2 | encounter SO 2 entry |
| `elite-2-1` | Elite Trio 2-B | 3 | encounter SO 3 entry |
| `monster-1-0` 등 | 1몹 | 1 (기본) | SO 없음 또는 1 entry — 기존과 동일 |

맵 경로 예: Start → Monster 1-A → Duo 2-A / Start → Elite 1-C → Elite Trio 2-B

## Goal

맵 전투 노드가 **`RunEncounterDefinition` ScriptableObject**로 몬스터 수·종류·HUD 배치·(선택) HP/턴 패턴을 정의하고, `RunBattleController`는 **roster 빌드 입력만 SO로 교체**한 채 기존 다인전 Play 경로(HUD·타겟·Core id `100+index`)를 유지한다. **1몹 노드**와 **Duo / Elite Trio** 검증 노드는 Play Mode에서 회귀 통과한다.

## Out of scope

- RunBattle HUD / 타겟 / `SlotCombatRequestToCombatEffectsConverter` / Presenter **대규모** 재작업 (완료 plan 범위 유지).
- `BattleSystem` Core 규칙·invalid target fallback 변경.
- 플레이어 파티 2인 이상.
- [`feature-game-flow-loop`](./feature-game-flow-loop.md) 맵 그래프 UI·씬 구조 대개편.
- encounter pool·층별 랜덤 테이블 (후속).
- `RunMapNodeDefinition` 전체를 에디터 SO 카탈로그로 이전 (이번에는 **참조 필드 + 샘플 1~2노드**만).

## Phases

---

### Phase 0 — 계약·갭 확인

- [x] [`ADR-0004`](../../adr/0004-multi-participant-combat.md), [`combat-core.md`](../../design-docs/combat-core.md) Multi-participant·Validation 재확인
- [x] 완료 plan Follow-ups: [`feature-multi-participant-play-ui`](../completed/feature-multi-participant-play-ui.md) Completion
- [x] 핵심 코드 읽기: `RunMapNodeDefinition`, `RunMapNodeCatalog`, `RunBattleController.BuildEncounterRoster` / `BindEnemySlots`, `MonsterDefinition`, `MonsterTurnScheduleFactory`, `BattleSystem.StartBattle` 다인 오버로드, `RunBattleView.LayoutEnemySlots`
- [x] `EnemyCount` vs encounter SO **폴백 규칙** 초안 확정 (아래 Notes "폴백 규칙")

**🔍 Review:** Core/UI 경계(ADR-0004) 유지한 채 roster 입력만 바꿀 수 있는지, participant id `100+rosterIndex`를 깨지 않는지.

---

### Phase 1 — `RunEncounterDefinition` SO (Data)

- [x] `SlotRogue.Data.GameFlow`에 타입 추가:
  - `RunEncounterDefinition` : `ScriptableObject`
  - `RunEncounterEntry` : `[Serializable]` — `MonsterDefinition monster`, `int formationSlot` (0=좌, 1=중, 2=우; prefab 최대 3 slot과 정합), `int hpOverride` (0=미사용), `MonsterTurnPatternDefinition turnPatternOverride` (null=미사용)
  - `RunEncounterEntry[] entries` (길이 1~3, 빈 배열은 에디터/런타임 guard)
- [x] `[CreateAssetMenu(menuName = "SlotRogue/Game Flow/Run Encounter Definition")]`
- [x] 샘플 asset 2개 (`Assets/_Project/Data/GameFlow/Encounters/`):
  - `Encounter_Duo2A` — 2 entry, formation 0/2
  - `Encounter_EliteTrio2B` — 3 entry, formation 0/1/2

**🔍 Review:** Unity에서 Create 메뉴로 asset 생성 가능; entry가 `MonsterDefinition`·optional override만 참조 (Core 역참조 없음).

---

### Phase 2 — Roster 빌더 + 맵 노드 참조

- [ ] `RunEncounterRosterBuilder` (`SlotRogue.UI.GameFlow`, static) — 입력: `RunEncounterDefinition? encounter`, `RunMapNodeDefinition node`, `int floor`, optional legacy `MonsterDefinition? inspectorFallback` → `EncounterRoster` (enemies[], schedules[])
  - encounter 있음: `entries.Length`만큼 roster; per-entry HP (`hpOverride` → `monster.maxHp` → `GetMonsterMaxHp(node)`); schedule (`turnPatternOverride` → `monster.turnPattern` → `CreateMonsterTurnSchedule(node, floor)`)
  - encounter 없음: **현행** `EnemyCount` 루프 + 노드/floor/Inspector 폴백 (회귀)
  - participant id: **`100 + rosterIndex`** (Core·기존 테스트와 동일; roster 순서는 entries 배열 순)
- [ ] `RunMapNodeDefinition`: optional `RunEncounterDefinition Encounter` (생성자 오버로드 또는 nullable 프로퍼티); `EnemyCount`는 encounter 없을 때만 사용, encounter 있으면 `entries.Length` 우선 (Obsolete 주석 가능)
- [ ] `RunBattleController`: `BuildEncounterRoster` → 빌더 위임; `_monsterDefinition`은 encounter·entry 모두 없을 때만 단일 몬스터 폴백으로 유지
- [ ] `RunMapNodeCatalog`: `monster-2-0`, `elite-2-1`에 샘플 encounter SO 연결 (asset 참조 — GUID는 `.meta` 동커밋)

**🔍 Review:** encounter 없는 1몹 노드 diff 없음; Duo/Elite Trio는 SO roster 길이 2/3.

---

### Phase 3 — Formation slot → UI 배치

- [ ] roster 빌드 결과에 **formation slot per enemy** 노출 (예: `EncounterRoster`에 `int[] FormationSlots` 또는 parallel struct)
- [ ] `BindEnemySlots` / `RefreshEnemySlots`: `slotIndex = formationSlot` (클램프 0..`EnemySlotCount-1`); roster index ≠ slot index일 때 participant id는 roster 기준, HUD·anchor·클릭은 formation slot 기준
- [ ] 중복 formation slot / 범위 밖 slot: `Debug.LogWarning` + roster index 폴백 (크래시 금지)
- [ ] 1몹: formation 1(중앙) 또는 0 — 기존 단일 anchor 레이아웃 회귀

**🔍 Review:** 2몹 Duo에서 좌/우(또는 지정) 슬롯에 HP HUD가 맞고, 타겟 클릭 id는 `100+i`와 일치.

---

### Phase 4 — 테스트·문서·완료 처리

- [ ] (선택) EditMode: `RunEncounterRosterBuilder` — 2 entry HP/schedule override, encounter null 시 `EnemyCount` 폴백 (`SlotRogue.UI.Tests` 또는 Data.Tests — asmdef 의존 최소 경로)
- [ ] Play Mode 체크리스트 (아래 표) 전항
- [ ] 본 plan 체크리스트·Notes·Completion 갱신
- [ ] `git mv` → `docs/exec-plans/completed/feature-map-encounter-so.md`
- [ ] [`STATUS.md`](../../STATUS.md) Active → Recently completed, `_Last updated` bump
- [ ] (필요 시) [`game-flow.md`](../../design-docs/game-flow.md) encounter SO 한 줄 — Core 본문 변경 없음

**🔍 Review:** 완료 정의 충족; STATUS·active·completed 링크 일치.

---

## Play Mode 체크리스트

| # | 시나리오 | 진입 | 기대 |
|---|----------|------|------|
| 1 | **1몹 회귀** | `monster-1-0` 등 (encounter SO 없음) | roster 1, HUD 1, 기존 HP/턴·승패 동일 |
| 2 | **Duo 2-A** | `monster-1-0` → `monster-2-0` | SO 2몹, 2 HUD slot, 타겟·피해 id 정합 |
| 3 | **Elite Trio 2-B** | `elite-1-2` → `elite-2-1` | SO 3몹, 3 slot layout |
| 4 | **배치** | Duo/Trio | formation slot에 맞는 좌/중/우 HUD (시각 확인) |
| 5 | **폴백** | encounter SO 미연결 다인 노드 (있으면) | `EnemyCount`만으로 기존 Phase 1 A안 동작 |

---

## 완료 정의 (Definition of done)

- [ ] `RunEncounterDefinition` + CreateAssetMenu + 샘플 Duo/Trio asset
- [ ] `RunEncounterRosterBuilder` 단일 전환 지점; `RunBattleController.BuildEncounterRoster` 위임
- [ ] `RunMapNodeCatalog` 검증 노드 2곳 SO 연결
- [ ] formation slot → HUD 매핑; Core `StartBattle` 시그니처·id 규칙 불변
- [ ] 1몹·Duo·Elite Trio Play Mode 회귀
- [ ] plan `completed/` + STATUS 갱신

---

## 구현 메모 (파일 힌트)

| 작업 | 주요 파일 |
|------|-----------|
| Encounter SO | `Assets/_Project/Scripts/Data/GameFlow/` (신규) 또는 `Data/Combat/` — `MonsterDefinition` 참조만 |
| Roster 빌더 | `RunEncounterRosterBuilder.cs` (신규, UI.GameFlow) |
| 맵 노드 | `RunMapNodeDefinition.cs`, `RunMapNodeCatalog.cs` |
| 전투 진입 | `RunBattleController.cs` (`BuildEncounterRoster`, `BindEnemySlots`) |
| HUD layout | `RunBattleView.cs` (`LayoutEnemySlots` — 필요 시 visible slot만 재배치) |
| Core (읽기 전용) | `BattleSystem.cs` |
| 참고 | `BattleDevHarness.cs` (`_startWithTwoEnemies`), `feature-monster-pattern-so` SO 관례 |

**asmdef:** SO·entry struct → `SlotRogue.Data`; 빌더 → `SlotRogue.UI` (이미 Data·Core 참조). Data가 `RunMapNodeDefinition`을 참조하지 않도록 빌더가 노드 메타 폴백 담당.

**커밋:** 구현 PR과 plan 체크 갱신 동일 단위 ([`GOVERNANCE.md`](../../GOVERNANCE.md) 규칙 C).

## Notes

- **가장 중요한 수정 지점:** `RunBattleController.BuildEncounterRoster` — 내부만 SO 입력으로 교체하면 HUD/타겟/연출은 대부분 유지.
- **폴백 규칙 (구현 시 확정):**
  1. `node.Encounter != null` && `entries.Length > 0` → SO roster
  2. else → `Mathf.Max(1, node.EnemyCount)` 루프 (현행)
  3. HP/schedule: entry override → entry `MonsterDefinition` → Inspector `_monsterDefinition` (전원 동일, 현행) → `GetMonsterMaxHp` / `CreateMonsterTurnSchedule(node, floor)`
- **participant id:** roster 배열 인덱스 기준 `100 + index` 유지. UI는 `formationSlot`으로 슬롯 바인딩; 타겟·Converter는 id 기준 (변경 없음).
- **invalid target:** 구현·기획 변경 금지 — 첫 생존 Enemy fallback (`combat-core.md`).
- **`EnemyCount`:** encounter SO 도입 후 catalog 신규 노드는 encounter 우선; 기존 필드는 폴백·마이그레이션용으로 당분간 유지.
- **Inspector `_monsterDefinition`:** encounter 없는 노드·entry에 monster 없을 때만 사용 (Dev/단일 몬스터 튜닝 경로 유지).

## Completion

_(completed/로 옮길 때 채움.)_

- **Finished**:
- **Outcome**:
- **Follow-ups**:
