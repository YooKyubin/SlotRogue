# 전투 코어 (Combat Core)

**Status**: completed  
**Started**: 2026-05-30  
**Owner**: _(전투 담당)_  
**Contributors**: _(없음)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md)  
**Related ADR**: [ADR-0001](../../adr/0001-combat-turn-effect-pipeline.md)

## Goal

ADR-0001·`combat-core.md` 기준으로 **슬롯/UI 없이** 전투 턴 파이프라인을 EditMode 테스트로 검증한다. `StartBattle` → `ApplyPlayerTurn(Effect[])` → 적 턴 → shield 초기화·승패·CombatEvent 로그가 한 판 돌아가면 완료다. 슬롯 연동·`SlotCombatRequest` 변환·전투 UI는 이 plan 범위 밖이다.

## Phases

별도 plan 파일로 분리하지 않는다. 아래 Phase마다 **🔍 Review**에서 확인 후 다음 Phase로 진행.

---

### Phase 1 — 데이터·Effect 적용 (단위)

- [x] `AGENTS.md`, ADR-0001, `combat-core.md`, asmdef 경계 확인
- [x] `CombatEffect`, `CombatEffectKind`, `CombatEffectTarget` 추가
- [x] `CombatParticipant` — HP·maxHp·shield 상태
- [x] Effect 적용기 — Damage / Shield / Heal (shield 소진·Heal cap 포함)
- [x] `SlotRogue.Core.Tests` asmdef + `EffectApplicatorTests` (Phase 1 Review용 선행)

**🔍 Review:** EditMode 테스트로 Kind별 적용만 검증 (턴 루프 없이). 방어도 차감·회복 상한이 `combat-core.md`와 일치하는지 확인.

---

### Phase 2 — 턴 파이프라인

- [x] `BattlePhase`, `BattleEndReason`, `CombatEvent` / `CombatEventKind` 추가
- [x] `BattleResolver` / `BattleSystem` — StartBattle, ApplyPlayerTurn, UpcomingEnemyActions, CurrentPhase
- [x] 플레이어 턴 종료 → 몬스터 shield 초기화 → 적 턴
- [x] 적 턴 종료 → 플레이어 shield 초기화 → PlayerTurn
- [x] 사망 시 적 턴 스킵, Victory/Defeat, 잘못된 Phase에서 ApplyPlayerTurn 거부

**🔍 Review:** 테스트 또는 간단 harness로 2~3턴 수동 시나리오 (피해→방어→적 공격→승리). CombatEvent 순서·Phase 전환이 design-doc과 맞는지 확인.

---

### Phase 3 — 테스트·마무리

- [x] `SlotRogue.Core.Tests` asmdef 복구 (Phase 1에서 선행)
- [x] EditMode 테스트 — `BattleSystemTests` 추가 (shield 초기화, 턴 순환, 승패, 이벤트)
- [x] Unity Test Runner — `SlotRogue.Core.Tests` 20개 (에디터 Test Runner에서 확인; CLI batch는 프로젝트 오픈 중 실행 불가)
- [x] 체크리스트·Notes 갱신, ADR-0001 Status `accepted` 검토 (팀 합의 후 — Status는 `proposed` 유지)

**🔍 Review:** 테스트 전부 green → plan 완료·`completed/` 이동 후보.

---

## Notes

- 코드 위치: `Assets/_Project/Scripts/Core/Combat/` (네임스페이스 `SlotRogue.Core.Combat`).
- 턴 오케스트레이터는 design-doc의 `BattleResolver` 대신 **`BattleSystem`** 으로 구현 (`EffectApplicator` 주입).
- MVP 몬스터 행동: **고정 `CombatEffect[]` 1세트** 주입 (SO·순환은 Later).
- `StartBattle(player, monster, monsterTurnActions)` — Participant 참조 + 적 턴 Effect 목록을 파라미터로 전달 (`combat-core.md` Q3).
- CombatEvent MVP: `PhaseChanged`, `EffectApplied`, `ShieldReset`, `BattleEnded` (`EffectApplyResult`에 Damage/Shield/Heal 수치 포함).
- `AssemblyInfo.cs` — `InternalsVisibleTo("SlotRogue.Core.Tests")`로 Participant 상태 검증.
- 슬롯 asmdef는 Core Combat 타입 참조 금지. 연동은 후속 plan.

## Out of scope (follow-up plan)

- `SlotCombatRequest` → `CombatEffect[]` 변환
- `Dev_Battle` 씬, CombatTimeline UI
- 몬스터 ScriptableObject, 행동 배열·순환
- 속성·DoT·Gold/SP Effect Kind

## Completion

- **Finished**: 2026-05-31
- **Outcome**: `BattleSystem` + `EffectApplicator` + Participant/Effect 타입. EditMode 테스트 `EffectApplicatorTests`(7), `BattleSystemTests`(13). shield 초기화·턴 순환·승패·CombatEvent 순서·멀티턴 시나리오 검증.
- **Follow-ups**: 슬롯→전투 `CombatEffect[]` 변환 plan, 전투 UI(CombatEvent 소비), 몬스터 패턴 SO, ADR-0001 `accepted` 팀 합의.
