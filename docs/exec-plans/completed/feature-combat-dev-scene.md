# 전투 Dev 씬 (Combat Dev Scene)

**Status**: completed  
**Started**: 2026-05-31  
**Owner**: _(전투 담당)_  
**Contributors**: _(없음)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md), [`slot-core.md`](../../design-docs/slot-core.md)  
**Related ADR**: [ADR-0001](../../adr/0001-combat-turn-effect-pipeline.md)  
**Depends on**: [`feature-combat-core`](./feature-combat-core.md) (완료)

## Goal

**`Dev_Battle` 씬**에서 슬롯 스핀 없이 전투 연동을 검증한다. Inspector 값으로 **임시 `SlotCombatRequest`** 를 만들고, 테스트 버튼 → `CombatEffect[]` 변환 → `BattleSystem.ApplyPlayerTurn`까지 Play Mode에서 동작한다. 전투 **Core에는 `Debug.Log`를 넣지 않고**, 씬 쪽 **`CombatEvent` 소비자**가 Unity Console에 Phase·Effect·HP/shield 스냅샷을 출력한다. 화면 연출·타임라인 UI·`Dev_Slot` 통합은 범위 밖.

## Phases

별도 plan 파일로 분리하지 않는다. Phase마다 **🔍 Review** 후 다음 Phase.

---

### Phase 1 — 연동 계층·변환 규칙

- [x] `AGENTS.md`, ADR-0001, `combat-core.md` Q1·`slot-core.md` S4 재확인
- [x] `SlotRogue.UI` asmdef — `SlotRogue.Core`, `SlotRogue.Slot` 참조 추가 (Slot asmdef는 Combat 참조 금지 유지)
- [x] `SlotCombatRequest` → `CombatEffect[]` 변환기 (연동 계층, Core/Slot 경계 밖)
- [x] 변환 규칙 MVP를 본 plan Notes에 확정·구현 일치 (아래 **변환 MVP**)
- [x] EditMode 단위 테스트 — 변환기 (순서·AttackCount·0값 스킵)

**🔍 Review:** 변환만 테스트로 green. `BattleSystem`은 기존 테스트 유지.

---

### Phase 2 — Dev 씬·Harness

- [x] `Assets/Scenes/Dev_Battle.unity` 생성 (빈 씬 + EventSystem/Canvas 또는 코드 부트스트랩)
- [x] `BattleDevHarness` — `StartBattle` / `Apply Turn` 버튼, Inspector 전투·Request 필드
- [x] Harness: Inspector → `new SlotCombatRequest(...)` (슬롯 스핀·`SlotCombatRequestBuilder` 불필요)
- [x] Harness: 변환 → `ApplyPlayerTurn`, 거부 시(Phase/Ended) Console 한 줄
- [x] 몬스터 적 턴 — Inspector 고정 `CombatEffect[]` 1세트 (적 턴 미리보기 API)

**🔍 Review:** Play Mode에서 Start → Apply 여러 번 → HP/shield·Phase가 Harness가 읽는 Participant와 일치.

---

### Phase 3 — Console 이벤트 로거 (Core 비침범)

- [x] `CombatEventConsoleLogger` — `BattleSystem` 참조, **이벤트 cursor**로 `ApplyPlayerTurn` 구간만 출력
- [x] `PhaseChanged` / `EffectApplied` / `ShieldReset` / `BattleEnded` Kind별 포맷
- [x] 턴 종료 **스냅샷** 한 줄 (Player/Monster HP·maxHp·shield, `CurrentPhase`, `EndReason`)
- [x] `BattleSystem`·`EffectApplicator` 본문에 로그 추가 **금지** (소비자만)

**🔍 Review:** 2~3턴 수동 시나리오 Console에서 이벤트 순서·수치가 design-doc shield/턴 규칙과 맞는지 확인.

---

### Phase 4 — 마무리

- [x] Play Mode 체크리스트 완료 (승리/패배/적 턴 중 사망 1회씩)
- [x] plan 체크리스트·Notes 갱신
- [x] `combat-core.md` Q1 — 변환 MVP를 design-doc Open questions에 반영 (ADR amend 없음)
- [x] `completed/` 이동 + [`STATUS.md`](../../STATUS.md) 갱신

**🔍 Review:** Dev_Battle만으로 “Request → 턴 → Console 로그” 데모 가능.

---

## Play Mode 체크리스트 (Dev_Battle)

| # | 시나리오 | Inspector 힌트 | 기대 |
|---|----------|----------------|------|
| 1 | **승리** | Monster maxHp=5, Request damage=5 | `BattleEnded: Victory`, Monster HP 0 |
| 2 | **패배** (플레이어 턴 중) | Player currentHp=5, Request damage=0, Monster action 없음 → 대신 player self-damage 테스트는 Core 테스트로 커버. Dev: Monster maxHp 높고 player currentHp=1, enemy damage=2, player damage=0 | 적 턴 후 `Defeat` |
| 3 | **적 턴 중 사망** | Player currentHp=3, Request damage=0, Enemy damage=4 | `EnemyTurn` 중 Effect 후 `Defeat` |

Console: `[Combat]` 이벤트 순서 Resolving → Effect → monster ShieldReset → EnemyTurn → … → Snapshot. Status UI와 Snapshot HP/shield 일치.

---

## 변환 MVP (`SlotCombatRequest` → `CombatEffect[]`)

본 plan 구현 시 **아래 순서·규칙**을 따른다. 기획 변경 시 Notes 갱신 + 변환 테스트 수정.

| Request 필드 | Effect | 순서 | 비고 |
|--------------|--------|------|------|
| `Defense` > 0 | `Shield`, Amount=`Defense`, Target=`Self` | 1 | 0이면 생략 |
| `HealAmount` > 0 | `Heal`, Amount=`HealAmount`, Target=`Self` | 2 | 0이면 생략 |
| `AttackCount` ≥ 1, `Damage` > 0 | `Damage`, Amount=`Damage`, Target=`Enemy` | 3… | **횟수만큼 Effect 행 반복** |
| `AttackCount` ≤ 0, `Damage` > 0 | `Damage` × 1 | 3 | AttackCount 0은 1타로 처리 |
| `IsCritical` | _(Effect 없음)_ | — | MVP: Console/Request 로그만. 배율은 Later |
| `PatternName` | _(Effect 없음)_ | — | MVP: Console/Request 로그만 |

- Effect **재정렬 금지** — 위 표 순서가 입력 목록 순서 (ADR-0001).
- 변환기: `SlotRogue.UI.Combat.SlotCombatRequestToCombatEffectsConverter`. **`SlotRogue.Slot` 내부 금지.**

## Dev 씬 Inspector (Harness 초안)

| 그룹 | 필드 | 용도 |
|------|------|------|
| Player | maxHp, (optional currentHp) | `CombatParticipant` |
| Monster | maxHp, (optional currentHp) | `CombatParticipant` |
| Monster turn | `CombatEffect[]` 또는 damage/target 직렬화 | `StartBattle` 적 행동 |
| Request (테스트) | damage, defense, attackCount, healAmount, isCritical, patternName | 임시 `SlotCombatRequest` |
| Buttons | Start Battle, Apply Turn | — |

## Console 로거 (cursor 패턴)

```
eventCursor = battle.Events.Count
ApplyPlayerTurn(...)
for i in [eventCursor .. Events.Count): Log(Events[i])
LogSnapshot(Player, Monster, Phase, EndReason)
```

`Events`는 `StartBattle` 때만 Clear — cursor 없이 전체 순회하면 턴이 쌓여 중복 로그.

## Notes

- 씬 경로: `Assets/Scenes/Dev_Battle.unity` (`Dev_Slot`과 동일 레벨).
- 코드: `Assets/_Project/Scripts/UI/Combat/` (네임스페이스 `SlotRogue.UI.Combat`).
- 테스트 (중앙 `Tests/`, asmdef 형제 분리): `Tests/Core/` → `SlotRogue.Core.Tests` (`Combat/`), `Tests/UI/` → `SlotRogue.UI.Tests`. **`Tests/` 루트 asmdef 없음.**
- `combat-core.md` Q1 닫음 — 본 plan **변환 MVP** 표가 design-doc 기준.

## Out of scope (follow-up plan)

- `Dev_Slot`에서 SPIN → 자동 `ApplyPlayerTurn`
- HP 바·타임라인 UI·Addressables 몬스터
- `IsCritical` → Damage 배율 Effect
- 몬스터 턴별 다른 행동 스케줄 (`combat-core.md` Q2)
- PlayMode 자동 테스트 (MVP는 수동 Dev 씬 체크리스트)

## Completion

- **Finished**: 2026-05-31
- **Outcome**: `Dev_Battle` + `BattleDevHarness` + `SlotCombatRequestToCombatEffectsConverter` + `CombatEventConsoleLogger`. EditMode 변환 테스트 7개. Core 로그 없이 Console cursor 로깅.
- **Follow-ups**: `Dev_Slot` SPIN 연동 plan, 몬ster turn pattern (Q2), `IsCritical` 배율, 전투 UI/연출.
