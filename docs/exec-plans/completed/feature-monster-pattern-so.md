# 몬스터 턴 패턴 SO (Monster Turn Pattern ScriptableObject)

**Status**: completed  
**Started**: 2026-05-31  
**Finished**: 2026-05-31  
**Owner**: _(전투 담당)_  
**Contributors**: _(없음)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md)  
**Related ADR**: [ADR-0001](../../adr/0001-combat-turn-effect-pipeline.md)  
**Depends on**: [`feature-combat-core`](../completed/feature-combat-core.md), [`feature-combat-dev-scene`](../completed/feature-combat-dev-scene.md), [`feature-monster-turn-schedule`](../completed/feature-monster-turn-schedule.md)

## Goal

몬스터 턴 패턴을 **ScriptableObject**로 저장하고, `MonsterDefinition` SO가 패턴을 참조한다. 전투 시작 시 Factory가 SO → `new MonsterTurnSchedule(...)` + `Reset()`으로 런타임 스케줄을 만든다. **`BattleSystem` / Core는 변경 최소**(이미 `MonsterTurnSchedule` 수신). `Dev_Battle` Harness는 **`MonsterDefinition` SO 필수** — 미할당 시 Console `LogError`, Inspector 몬스터 스케줄 fallback **없음**. **`SlotRogue.Slot` asmdef·코드는 변경하지 않는다.**

## Phases

별도 plan 파일로 분리하지 않는다. Phase마다 **🔍 Review** 후 다음 Phase.

---

### Phase 1 — exec-plan·문서 (본 Phase)

- [x] `feature-monster-pattern-so.md` 작성 + [`STATUS.md`](../../STATUS.md) active 등록
- [x] `AGENTS.md`, ADR-0001, `combat-core.md` Q2·설계 원칙 재확인

**🔍 Review (2026-05-31):** plan Goal·Out of scope·asmdef 의존 방향 일치. 상세는 아래 **Phase 1 재확인** 참조.

#### Phase 1 재확인

| 출처 | 확인 결과 | 본 plan과의 관계 |
|------|-----------|------------------|
| **AGENTS.md §6** | asmdef 단방향, `.meta` 동커밋, `SlotRogue.Data` 네임스페이스 | Data→Core, UI→Data 추가만. Core·Slot 역참조 없음 |
| **ADR-0001** | Effect 목록 파이프라인, Slot→Combat 참조 금지, Participant·CombatEvent | **변경 없음.** SO는 `CombatEffect` step 직렬화 + Factory만 추가 |
| **ADR-0001 Decision L24** | MVP 당시 “고정 1세트” 문구 | Q2(`MonsterTurnSchedule`)로 이미 구현됨. **ADR amend 없음** — design-doc Q2 follow-up(SO)으로 흡수 |
| **combat-core Q2** | `MonsterTurnSchedule` + 순환 index **닫음**; “패턴 SO·RNG는 Later” | 본 plan = Q2 Later 항목. 적 턴 미리보기 API 재사용 |
| **SlotRogue.Slot asmdef** | `SlotRogue.Core` / Combat 참조 **없음** (현재 `UnityEngine.UI`만) | Out of scope 준수 — Slot 변경 금지 |
| **BattleSystem (코드)** | `StartBattle(..., MonsterTurnSchedule)` + 내부 `Reset()` | Core 변경 최소 — Factory가 schedule만 생성 |

**Phase 2 결정 (재확인 시 확정):** struct 통합은 **B안** — Data `CombatEffectStep` canonical. Phase 5 이후 Harness Inspector fallback·UI struct(`CombatEffectDefinition`, `MonsterTurnDefinition`) **삭제**.

---

### Phase 2 — Data 타입·SO

- [x] `SlotRogue.Data` asmdef — `SlotRogue.Core` 참조 추가
- [x] `CombatEffectStep` — `[Serializable]` struct (`kind`, `amount`, `target`), `ToCombatEffect()`
- [x] `MonsterTurnStepDefinition` — `[Serializable]`, 턴 1개 = `CombatEffectStep[] actions`
- [x] `MonsterTurnPatternDefinition` — `ScriptableObject`, `MonsterTurnStepDefinition[] turns`
- [x] `MonsterDefinition` — `ScriptableObject`, `maxHp`, `MonsterTurnPatternDefinition turnPattern` 참조
- [x] `[CreateAssetMenu]` — `SlotRogue/Combat/Monster Turn Pattern`, `SlotRogue/Combat/Monster Definition`
- [x] Harness `CombatEffectDefinition` / `MonsterTurnDefinition`과 **필드 통합 검토** — **B안**: Data canonical, UI struct Phase 4까지 유지

**🔍 Review (2026-05-31):** `Assets/_Project/Scripts/Data/Combat/` 4파일. SO에 `MonsterTurnSchedule`/index 없음. Data→Core만 참조. Unity에서 `Create > SlotRogue/Combat/` 메뉴로 asset 생성 가능.

---

### Phase 3 — Factory·EditMode 테스트

- [x] `MonsterTurnScheduleFactory` (`SlotRogue.Data.Combat`) — `FromPattern(MonsterTurnPatternDefinition?)` → `MonsterTurnSchedule`
- [x] `FromSteps(...)` 오버로드 — SO 없이 step 배열로 변환 (EditMode 테스트용)
- [x] null/빈 pattern → 단일 fallback 턴 (기존 Harness·`MonsterTurnSchedule` 빈 스케줄 규칙과 동일)
- [x] `Assets/_Project/Scripts/Tests/Data/` + `SlotRogue.Data.Tests.asmdef` (Core.Tests / UI.Tests와 **sibling**)
- [x] EditMode 테스트 — 3턴 순환, 빈 pattern, multi-action 턴 1개

**🔍 Review (2026-05-31):** Factory fallback — `FromPattern(null)` / `FromSteps` empty → Damage 2 (**EditMode 테스트·Factory API**). Harness는 SO 필수로 fallback 미사용. `FromPattern` empty `turns[]` → `MonsterTurnSchedule` 단일 빈 턴. 테스트 5개.

#### Factory fallback 규칙

| 입력 | 결과 | Harness |
|------|------|---------|
| `FromPattern(null)` | 1턴 Damage 2 → Enemy | **미사용** (SO 필수) |
| `FromPattern` empty `turns[]` | 1턴 빈 Effect 목록 | turnPattern SO 데이터 품질 이슈 |
| `FromSteps` null/empty | 1턴 Damage 2 → Enemy | **미사용** |
| 턴 내 empty `actions` | 해당 턴 `CombatEffect[]` empty | — |

---

### Phase 4 — Dev_Battle Harness 연동

- [x] `SlotRogue.UI` asmdef — `SlotRogue.Data` 참조 추가
- [x] Harness — `[SerializeField] MonsterDefinition _monsterDefinition` (SO 드래그)
- [x] `StartBattle()` — `MonsterDefinition` SO 필수; `maxHp` + `FromPattern(turnPattern)`
- [x] `BuildMonsterTurnSchedule()` — Factory 직접 호출 (Phase 5 follow-up에서 Inspector fallback 제거)
- [x] Status UI — SO asset 이름 + upcoming turn index 표시

**🔍 Review (2026-05-31):** Phase 4 초기 구현(SO 우선 + Inspector fallback). Phase 5 follow-up에서 **SO-only**로 정리 — 아래 **Harness 정리** 참조.

#### Harness 정리 (Phase 5 follow-up)

- [x] `_monsterDefinition` / `turnPattern` 미할당 → `Debug.LogError`, `StartBattle` 중단
- [x] Inspector `_monsterTurnSchedule`, `_monsterMaxHp`, `Reset()` 3턴 기본값 **삭제**
- [x] UI struct `CombatEffectDefinition`, `MonsterTurnDefinition` **삭제** (Data `CombatEffectStep`만 사용)
- [x] 몬스터 패턴 편집은 `Assets/_Project/Data/Combat/` SO asset에서만

**🔍 Review:** Goblin SO 연결 시 Phase 5 Play Mode 체크리스트와 동일. SO 없으면 Console 에러만.

---

### Phase 5 — 샘플 asset·Play Mode

- [x] `Assets/_Project/Data/Combat/` — `GoblinTurnPattern.asset` (3턴: damage 2 / 4 / 6 → Enemy)
- [x] `Goblin.asset` — maxHp=10, `turnPattern` → GoblinTurnPattern
- [x] `Dev_Battle.unity` — Harness에 Goblin SO 할당
- [x] Play Mode — Start → Apply 3회 → Console·Status에서 Q2와 동일 턴별 피해
- [x] Harness SO-only 정리 (Inspector fallback 제거) — 위 **Harness 정리**

**🔍 Review (2026-05-31):** Play Mode 체크리스트 (아래) 통과. 패턴 수정은 Pattern SO asset에서.

---

### Phase 6 — 마무리

- [x] `combat-core.md` — Monster pattern SO·Factory·System boundary 갱신, Q2 SO follow-up 반영
- [x] plan 체크리스트·Notes·Completion 갱신
- [x] `completed/` 이동 + [`STATUS.md`](../../STATUS.md) 갱신

**🔍 Review (2026-05-31):** Goblin SO + Dev_Battle Play Mode 통과. design-doc·ADR Notes 링크.

---

## Play Mode 체크리스트 (Dev_Battle + Goblin SO)

| # | 시나리오 | 설정 | 기대 |
|---|----------|------|------|
| 1 | **SO 시작** | Harness `MonsterDefinition` = Goblin, Request damage=0 | Start 후 upcoming turn #0, damage 2 |
| 2 | **3턴 순환** | Request damage=0, Apply ×3 | Console 적 피해 2 → 4 → 6, turn index 0→1→2 |
| 3 | **4턴째 순환** | Apply 1회 더 | damage 2 (index 0으로 wrap) |
| 4 | **SO 미할당** | `_monsterDefinition` = null, Start Battle | Console `[BattleDevHarness] MonsterDefinition SO is not assigned.` — 전투 시작 안 됨 |

Console `[Combat]` 이벤트·Snapshot이 Status HP/shield와 일치.

---

## 목표 아키텍처

```
MonsterTurnPatternDefinition (SO, Data)  — turns[][], 불변 패턴
MonsterDefinition (SO, Data)             — maxHp, turnPattern 참조
MonsterTurnScheduleFactory (Data)        — SO → new MonsterTurnSchedule(...)
BattleSystem (Core)                      — 변경 최소 (이미 MonsterTurnSchedule 수신)
BattleDevHarness (UI)                    — MonsterDefinition SO 필수 (Dev 전용; 본편은 별도 presenter)
```

### asmdef 의존 (목표)

| Assembly | references |
|----------|------------|
| `SlotRogue.Core` | _(없음)_ |
| `SlotRogue.Data` | `SlotRogue.Core` |
| `SlotRogue.UI` | `SlotRogue.Core`, `SlotRogue.Slot`, `SlotRogue.Data`, `UnityEngine.UI` |
| `SlotRogue.Slot` | _(변경 없음 — Combat 참조 금지)_ |

### 코드 위치 (목표)

| 경로 | 내용 |
|------|------|
| `Assets/_Project/Scripts/Data/Combat/` | Step struct, SO 클래스, Factory |
| `Assets/_Project/Data/Combat/` | GoblinTurnPattern, Goblin asset |
| `Assets/_Project/Scripts/Tests/Data/` | Factory EditMode 테스트 |
| `Assets/_Project/Scripts/UI/Combat/BattleDevHarness.cs` | SO 필수·StartBattle·Dev Request 필드 |

### Dev vs 본편

| 구분 | Dev_Battle (`BattleDevHarness`) | 본편 (follow-up) |
|------|--------------------------------|------------------|
| 몬스터 데이터 | `MonsterDefinition` SO (Inspector) | encounter/run에서 SO 참조 |
| 플레이어 입력 | Inspector `SlotCombatRequest` 필드 | 슬롯 SPIN → `SlotCombatRequest` |
| SO 없을 때 | `LogError`, 전투 시작 안 함 | bootstrap에서 검증 |
| Inspector 몬스터 스케줄 | **없음** (삭제) | **없음** |

---

## Notes

- **SO = 불변 패턴**, **`MonsterTurnSchedule` = 전투마다 new + `StartBattle` 내부 `Reset()`** — index는 asset·SO에 저장하지 않는다.
- git에서 삭제된 v1 `Assets/_Project/Data/Combat/Goblin*.asset`은 참조만; 새 규칙(Effect pipeline + `MonsterTurnSchedule`)으로 재작성.
- Factory는 Harness·런타임 게임플레이 양쪽에서 재사용. Harness Inspector fallback·UI struct는 Phase 5 follow-up에서 제거.
- ADR amend 불필요 — design-doc Notes·Q2 follow-up 수준 갱신 (Phase 6). ADR-0001 L24 “고정 1세트”는 Q2 구현으로 실질 supersede; SO는 동일 Effect·스케줄 모델의 데이터 저장 계층.
- **SO asset 경로:** `Assets/_Project/Data/Combat/` (Patterns / Monsters 서브폴더는 콘텐츠 증가 시).
- 브랜치: `kyubin`.

## Out of scope

- `SlotRogue.Slot` asmdef·코드 변경
- Core에 `UnityEngine` / `ScriptableObject` / `Debug.Log`
- `MonsterDefinition` SO 필드에 `MonsterTurnSchedule` 직접 저장
- 패턴 에디터 커스텀 Inspector·Addressables 그룹
- 가중치 RNG·조건부 분기 턴
- `Dev_Slot` ↔ 전투 연동
- PlayMode 자동 테스트 (MVP는 수동 Dev 씬 체크리스트 + EditMode Factory 테스트)

## Completion

- **Finished**: 2026-05-31
- **Outcome**: `MonsterTurnPatternDefinition`·`MonsterDefinition` SO, `MonsterTurnScheduleFactory`, Data.Tests 5개, Goblin 샘플 asset, Harness SO-only + `LogError`. UI struct(`CombatEffectDefinition`, `MonsterTurnDefinition`) 삭제.
- **Follow-ups**: `Dev_Slot`↔전투 연동 plan, 본편 battle bootstrap(presenter), 패턴 에디터·Addressables, RNG·가중치 턴
