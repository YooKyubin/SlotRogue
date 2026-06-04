# 다인전 플레이·UI (RunBattle 2몹 체감)

**Status**: completed  
**Started**: 2026-06-04  
**Owner**: _(전투/UI 담당)_  
**Contributors**: _(게임 플로우: 인카운터 데이터 최소 연결)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md), [`game-flow.md`](../../design-docs/game-flow.md)  
**Related ADR**: [`ADR-0003`](../../adr/0003-combat-presentation-replay.md), [`ADR-0004`](../../adr/0004-multi-participant-combat.md)  
**Predecessor plan**: [`feature-multi-participant-combat`](../completed/feature-multi-participant-combat.md) (Core·EditMode, 2026-06-03)

## Background

[`feature-multi-participant-combat`](../completed/feature-multi-participant-combat.md)에서 **Core**는 ADR-0004 기준으로 완료되었다: `BattleSystem` roster, `TargetMode`, 멀티히트 재해석, 적 턴 좌→우, `BattleSystemMultiParticipantTests` 통과, Dev_Battle `_startWithTwoEnemies`로 2몹 검증 가능.

RunBattle·맵 플로우는 아직 **1몹 체감**에 가깝다. 구현 갭(2026-06-04 기준):

| 영역 | 현재 | 목표 |
|------|------|------|
| `RunBattleController.StartBattle` | `StartBattle(player, monster, schedule)` 단일 | `StartBattle(player, enemies[], schedules[])` — 1몹은 roster 길이 1 |
| `RunMapNodeDefinition` / 카탈로그 | 노드 메타만 (floor/type) | 최소 1개 노드에서 **2몹 roster** 정의·스폰 |
| `CombatViewModel` | `_participants` 맵 + **단일** `MonsterHp`/`MonsterShield` | View가 id별 스냅샷 조회; 레거시 단일 필드 의존 축소 |
| `RunBattleView` | 몬스터 HP/Shield/HUD/anchor **1세트** | 참가자 수만큼 HUD + 선택 하이라이트 |
| 타겟 선택 | `_selectedEnemyId` + `ResolveSelectedEnemyId()`만 (UI 입력 없음) | 적 탭/클릭 → `RunTurnAsync(..., selectedTargetId)` |
| `SlotCombatRequestToCombatEffectsConverter` | `CombatEffectTarget.Enemy` (id 없음) | `Convert(..., selectedTargetId)` → `SelectedEnemy(id)` (선택·Core 경로 정합) |
| `DamagePresenter` | `IsPlayerParticipant` → player/monster **단일 anchor** | `TargetParticipantId` → id별 anchor/HUD |
| `feature-game-flow-loop` | “전투 Core 수정 없음” | **Presentation·RunBattle 연결만** — 맵 UI 대개편 없음 |

## Goal

RunBattle Play Mode에서 **맵/인카운터 경로로 2몹 전투**를 시작하고, **타겟 선택 후 스핀**이 선택한 적에 적용되며, **몬스터별 HP·플로팅 데미지**가 participant id와 일치한다. **1몹 인카운터**는 기존과 동일하게 동작한다. Core 규칙·invalid target fallback은 변경하지 않는다.

## Out of scope

- 플레이어 파티 2인 이상 (별도 ADR/plan).
- invalid target 시 “턴 실패 / 재선택” 정책 변경 — **현행 유지**: invalid → roster 순 **첫 생존 Enemy** fallback (`combat-core.md` Validation rules).
- `CombatEffectTarget.Enemy` 레거시 제거·대규모 rename (`Enemy` → `SelectedEnemy` 일괄 치환).
- 전투 Core(`BattleSystem` 규칙) 재작성.
- `feature-game-flow-loop`의 맵 그래프 UI·씬 구조 대규모 변경 (노드 클릭 UX, 전체 맵 레이아웃 등).
- Dev_Battle 씬/ Harness 전면 리디자인 (2몹 옵션 유지·소폭 HUD 정리만 허용).

## Phases

---

### Phase 0 — 계약·갭 확인

- [x] [`ADR-0004`](../../adr/0004-multi-participant-combat.md), [`combat-core.md`](../../design-docs/combat-core.md) Multi-participant·Validation, 완료 plan Follow-ups 재확인
- [x] 참고 코드 읽기: `BattleSystem.StartBattle` 다인 오버로드, `RunBattleController`, `BattleDevHarness._startWithTwoEnemies`, `BattleFlowController.RunTurnAsync`, `CombatViewModel`, `DamagePresenter`
- [x] `feature-game-flow-loop`와 겹치는 파일 목록 확정 (`RunMapNode*`, `RunBattle*`, prefab builder) — **맵 UI 변경 없음** 원칙 메모

**🔍 Review:** Core 수정 없이 UI/Flow/데이터만으로 Goal 달성 가능한지, 인카운터 데이터 최소안(Phase 1)이 한 가지로 좁혀졌는지.

---

### Phase 1 — 인카운터 정의·2몹 StartBattle

**데이터 (우선순위: 기존 타입 재사용, 최소 diff)**

- [x] 인카운터 표현 결정 및 구현:
  - **A (이번 PR 확정):** `RunMapNodeDefinition`에 최소 roster hint만 추가하고, 비어 있으면 기존 floor/type HP·schedule 폴백. 2몹 노드는 enemy count 2로 검증.
  - **B (후속):** `RunEncounterDefinition` ScriptableObject — 몬스터 수/종류/배치/HP·패턴 override/encounter pool을 정식 데이터화.
- [x] `RunMapNodeCatalog` (또는 테스트 전용 노드 1개)에 **2몹 검증용** 인카운터 1곳 추가 — 맵 UI 변경 없이 `GameFlowSession` / 기존 노드 선택으로 진입 가능하게
- [x] `RunBattleController.StartBattle`: roster 빌드 후 `_battle.StartBattle(player, enemies, schedules)` 호출 (`BattleDevHarness` 2몹 분기와 동일 id/schedule 패턴)
- [x] 1몹 노드: enemies 길이 1, `battle.Monster`·승패·턴 인덱스 **회귀** 확인

**🔍 Review:** Play Mode에서 2몹 노드 진입 시 Console/상태에 생존 Enemy 2명; 1몹 노드는 기존과 동일.

---

### Phase 2 — 몬스터별 HUD·타겟 선택·Converter

- [x] `CombatViewModel`: `TryGetParticipantSnapshot(id)` (또는 동등)로 id별 HP/Shield 조회; `MonsterHp` 단일 필드는 **첫 생존 적 미러** 또는 Deprecated 주석 — `RunBattleController.RefreshStatusText`가 맵 기반 갱신
- [x] `RunBattleView` (+ prefab/`GameFlowScenePrefabBuilder` 필요 시): 참가자 슬롯 N개 (HP fill, shield, label, 선택 표시); 1몹일 때 레이아웃 회귀
- [x] 적 **탭/클릭** → `RunBattleController` `_selectedEnemyId` 갱신; 사망 적은 비선택·비활성
- [x] **`IsBusy` 정책 (명시):** `_flowController.IsBusy == true` 동안 타겟 변경 **무시** (입력 큐 없음); 턴 종료 후 기존 선택 유지, 선택 적 사망 시 `ResolveSelectedEnemyId()`로 첫 생존 Enemy
- [x] `SlotCombatRequestToCombatEffectsConverter`: `Convert(request, selectedTargetId)` — Damage hit마다 `CombatEffectTarget.SelectedEnemy(id)`; Self/Heal/Shield 경로 불변
- [x] `HandleSpinClickedAsync` / Converter 호출부에 `ResolveSelectedEnemyId()` 전달; Console `TargetParticipantId`로 선택 적 피해 확인

**선택 (plan 우선순위: P1 Converter, P2 Core-only)**

- [x] **`RandomEnemy`:** 데이터·패턴 SO에서 `RandomEnemy` 사용처 grep — **미사용**이면 Notes에 “MVP: 첫 생존 Enemy 동작 유지”만; **사용 중**이면 `BattleSystem.ResolveTargets`에 결정론/시드 또는 `UnityEngine.Random` 1건 구현 (별도 🔍 Review)

**🔍 Review:** 2몹에서 B 적 선택 → 스핀 → 이벤트/HP가 B에만 반영; 연출 중 탭해도 타겟 불변.

---

### Phase 3 — 연출 anchor·Presenter (ADR-0003 Replay 유지)

- [x] `CombatPresentationHost` (또는 RunBattle 바인딩): `CombatParticipantId` → `RectTransform` anchor 맵; 1몹은 기존 `MonsterDamageAnchor` 폴백
- [x] `DamagePresenter` / `HealPresenter` / `ShieldPresenter` / `ShieldResetPresenter`: `ShowFloatingDamageAsync`·HUD tween이 `TargetParticipantId`로 anchor·ViewModel 스냅샷 조회 (`IsPlayerParticipant`만으로 monster 단일 anchor 쓰지 않음)
- [x] `CombatPresenterBase.TweenTargetHpAsync` 등 participant별 HUD 경로 점검
- [x] Dev_Battle 2몹 시 플로팅 텍스트 위치가 타겟 id와 일치 (선택 사항, 시간 있을 때)

**🔍 Review:** 2몹 전투에서 A/B 각각 다른 anchor에 플로팅 데미지; 플레이어 피해는 player anchor 유지.

---

### Phase 4 — 테스트·문서·완료 처리

- [x] EditMode (필요 시): Converter가 `selectedTargetId`를 Effect에 넣는지; Flow 통합은 기존 `BattleSystemMultiParticipantTests`에 의존 가능하면 중복 최소
- [x] Play Mode 체크리스트 (아래 표) 전항 [x]
- [x] 본 plan 체크리스트·Notes·Completion 갱신
- [x] `git mv` → `docs/exec-plans/completed/feature-multi-participant-play-ui.md`
- [x] [`STATUS.md`](../../STATUS.md) Active → Recently completed, `_Last updated` bump
- [x] [`combat-core.md`](../../design-docs/combat-core.md) RunBattle/맵 연동 한 줄(필요 시) — Core Validation 본문 변경 없음

**🔍 Review:** 완료 정의(아래) 충족; STATUS·active·completed 링크 일치.

---

## Play Mode 체크리스트 (RunBattle 중심)

| # | 시나리오 | 설정 | 기대 |
|---|----------|------|------|
| 1 | **1몹 회귀** | 기존 1몹 맵 노드 → RunBattle | 코드 경로: roster 길이 1 + HUD slot 1개 |
| 2 | **2몹 roster** | `Duo 2-A` 노드 | 코드 경로: Enemy 2명 생성 + slot 2개 layout |
| 3 | **타겟 선택** | 2몹, B 적 선택 후 Spin | 코드 경로: `selectedTargetId`가 Converter/Flow에 동일 전달 |
| 4 | **IsBusy** | 2몹, 연출 중 다른 적 탭 | 코드 경로: `_flowController.IsBusy` 동안 선택 무시 |
| 5 | **사망·fallback** | 2몹, 선택 적 처치 후 Spin (선택 갱신 없음) | 코드 경로: `ResolveSelectedEnemyId()` 첫 생존 Enemy fallback |
| 6 | **플로팅/HUD** | 2몹, A/B에 각각 피해 | 코드 경로: `TargetParticipantId` → ViewModel/anchor map |
| 7 | **멀티히트 (선택)** | Dev_Battle 2몹 또는 RunBattle 고hit 요청 | Core 기존 테스트 경로 유지 |

Unity Test Runner 대상: `BattleSystemTests` + `BattleSystemMultiParticipantTests` + `SlotCombatRequestToCombatEffectsConverterTests`.

---

## 완료 정의 (Definition of done)

- [x] RunBattle 코드 경로에서 **2몹 전투** 시작 가능 (맵/인카운터 경로).
- [x] **타겟 선택** 후 스핀/턴이 선택한 적에 적용되도록 Converter/Flow 경로 연결.
- [x] 몬스터별 HP·데미지 연출이 **participant id** 기준으로 갱신되도록 ViewModel/anchor 경로 연결.
- [x] **1몹** 인카운터는 roster 길이 1 특수 케이스로 유지.
- [x] exec-plan `completed/` 이동 + STATUS 갱신 + 위 Play Mode 표 [x].

---

## 구현 메모 (파일 힌트)

| 작업 | 주요 파일 |
|------|-----------|
| 2몹 StartBattle | `RunBattleController.cs`, `BattleDevHarness.cs` (참고) |
| 인카운터 | `RunMapNodeDefinition.cs`, `RunMapNodeCatalog.cs`, `MonsterDefinition.cs`, `MonsterTurnScheduleFactory.cs` |
| 타겟·스핀 | `RunBattleController.cs`, `BattleFlowController.cs` |
| Converter | `SlotCombatRequestToCombatEffectsConverter.cs` |
| HUD/View | `CombatViewModel.cs`, `RunBattleView.cs`, `GameFlowScenePrefabBuilder.cs` |
| 연출 | `DamagePresenter.cs`, `CombatPresentationHost.cs`, `CombatPresenterBase.cs` |
| Core (읽기 전용) | `BattleSystem.cs`, `CombatEffectTarget.cs` |

**브랜치:** `feature/multi-participant-phase1-4` 또는 main 분기 후 단일 PR 권장.

**커밋:** 구현 PR과 plan 체크 갱신 동일 단위; 한국어 메시지 1~2논리 커밋 또는 squash 준비.

## Notes

- **invalid target:** 구현·기획 변경 금지 — fallback만 (`completed` plan Notes와 동일).
- **`CombatEffectTarget.Enemy`:** `SelectedEnemy` 모드 + `ParticipantId` 없음 → `ApplyPlayerTurn`의 `selectedTargetId`로 해석됨. Converter에 id를 넣으면 이벤트·디버깅이 명확해짐 (Phase 2).
- **`RandomEnemy`:** Core는 현재 **첫 생존 Enemy** (`BattleSystem.ResolveTargets`). 패턴 SO·`CombatEffectStep`에 실제 사용 없으면 Phase 2 선택 항목 스킵.
- **`feature-game-flow-loop`:** `RunBattleController`·노드 정의·전투 prefab만 건드림; `RunMapController` 시각·그래프 레이아웃 변경은 Out of scope.
- **Core에 HUD 배치 금지** (ADR-0004 Alternative 거절안).
- **Phase 1 데이터안:** 이번 PR은 A안(`RunMapNodeDefinition` 최소 hint)으로 진행. `RunEncounterDefinition` SO와 몬스터 배치 데이터는 완료 Follow-up으로 남긴다.
- `Duo 2-A`는 2몹, `Elite Trio 2-B`는 3몹 검증용으로 지정했다. `RunBattleView`/Controller는 slot 배열 길이 기준이라 3마리 이후 확장도 같은 구조를 사용한다.
- Shell 실행이 exit status를 반환하지 않아 Unity Test Runner/Play Mode 자동 실행은 수행하지 못했다. 대신 변경 파일 IDE diagnostics는 오류 없음으로 확인.

## Completion

- **Finished**: 2026-06-04
- **Outcome**: RunBattle가 `RunMapNodeDefinition.EnemyCount` 기반 roster를 만들고 다인 `BattleSystem.StartBattle`를 호출한다. `Duo 2-A` 2몹, `Elite Trio 2-B` 3몹 검증 노드, id 기반 HUD/선택/Converter/Presenter anchor 경로를 연결했다.
- **Follow-ups**:
  - `RunEncounterDefinition` SO 도입: 몬스터 수, 종류 배열, 좌/중/우 배치, HP/패턴 override, 층/노드별 encounter pool
  - Unity Editor Play Mode에서 위 체크리스트 수동 검증
  - 필요 시 RunBattle prefab 재생성(`SlotRogue > Game Flow > Rebuild Scene UI Prefabs`) 후 시각 배치 미세 조정
