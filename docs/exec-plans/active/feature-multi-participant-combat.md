# 다인전 전투 확장 (Multi-participant Combat)

**Status**: active  
**Started**: 2026-06-02  
**Owner**: _(전투 담당)_  
**Contributors**: _(UI 담당: 타겟 선택/바인딩)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md), [`ADR-0004`](../../adr/0004-multi-participant-combat.md)

## Background

ADR-0004에서 다인전 확장 규칙이 확정되었다. 핵심은 participant id 기반 roster, `TargetMode + TargetParticipantId`, 플레이어 직접 타겟 선택, 멀티히트 재타겟, 적 턴 좌→우 순차와 schedule index 유지다. 이번 plan은 이 결정을 Core/Flow/UI/테스트로 안전하게 반영하는 기능 단위 작업이다.

## Goal

기존 1:1 전투를 `CombatParticipantId` 기반 roster 구조로 확장해, Enemy 2명 이상 전투를 안정적으로 처리한다. 플레이어 직접 타겟 선택(`SelectedEnemy` + `TargetParticipantId`), 멀티히트 재타겟(중간 사망 시 잔여타 이관), 적 턴 좌→우 순차와 schedule index 유지 규칙을 Core/Flow/UI에서 일관되게 동작시키면 완료다.

## Phases

---

### Phase 1 — 계약 고정과 Core 모델 전환

- [x] `AGENTS.md`, `docs/INDEX.md`, ADR-0001/0003/0004, `combat-core.md` 확장 섹션 재확인
- [x] Core participant 모델을 `_player`/`_monster` 고정 필드에서 id 기반 roster 구조로 전환
- [x] 팀 단위 승패 판정으로 전환 (Enemy team 전멸 / Player team 전멸)
- [x] `CombatEvent` 대상 표현을 `TargetParticipantId` 중심으로 정리 (`IsPlayerParticipant` 의존 제거)

**🔍 Review:** 1:1 전투가 roster 특수 케이스로 깨지지 않는지 기존 테스트/Dev 흐름으로 회귀 확인.

---

### Phase 2 — 타겟 모델과 공격 해석

- [x] `CombatEffectTarget`(또는 동등 모델)을 `TargetMode` + `TargetParticipantId` 구조로 확장
- [x] `TargetMode` MVP(`Self`, `SelectedEnemy`, `AllEnemies`, `RandomEnemy`) 해석 로직 구현
- [x] `SelectedEnemy` 유효성 검증(존재/생존/Enemy team) 및 invalid target 처리 정책 구현
- [x] 대체 대상 없음 시 처리(스킵/실패/소멸)와 이벤트/로그 일관성 정책 고정

**🔍 Review:** `SelectedEnemy` 정상/비정상 케이스에서 Core가 의도대로 reject 또는 처리하는지 단위 테스트로 확인.

---

### Phase 3 — 멀티히트/적 턴 스케줄 규칙 구현

- [x] 멀티히트 기본 규칙(한 대상 반복) + 중간 사망 시 잔여타 재타겟 구현
- [x] 대체 대상 없음 시 잔여타 소멸 처리 고정
- [x] 적 턴 좌→우 고정 순차 적용 구현
- [x] 사망 participant 스킵 + 생존 participant schedule index 유지 규칙 구현

**🔍 Review:** "3타 중 2타 사망 -> 남은 1타 재타겟"과 "좌→우 + index 유지"를 테스트로 고정.

---

### Phase 4 — Flow/UI 연결

- [x] UI 타겟 선택 상태를 Flow에 연결해 `SelectedEnemy` id를 Core 호출에 전달
- [x] `CombatViewModel`/`RunBattleView` HUD를 participant id 기반 상태 맵으로 전환
- [x] Dev_Battle / RunBattle에서 다수 몬스터 HUD 바인딩 규칙 정리
- [x] 연출/입력 잠금 중 선택 상태 변경 정책 정리 (중복 입력 방지)

**🔍 Review:** Dev_Battle/RunBattle에서 선택한 적에게 정확히 적용되고, 사망/재타겟 시 HUD가 올바르게 갱신되는지 수동 확인.

---

### Phase 5 — 테스트/검증 및 문서 정리

- [ ] EditMode 테스트 추가: selected target 유효/무효, 멀티히트 재타겟, 잔여타 소멸, 좌→우 순차, schedule 유지
- [ ] Dev_Battle / RunBattle 수동 플레이 검증 (타겟 선택, 다수 몬스터, 중간 사망 케이스)
- [ ] 이 plan의 체크리스트/Notes 갱신
- [ ] 관련 문서(`combat-core.md`, ADR-0004, STATUS)와 용어/규칙 정합성 재검토

**🔍 Review:** 구현 규칙과 문서 규칙이 1:1로 매칭되는지 최종 확인.

## Notes

- 이번 범위의 player team은 1명 고정이다. 플레이어 파티(2인 이상)는 후속 ADR/plan으로 분리한다.
- Core는 object handle 참조가 아니라 id 기반으로 타겟을 해석한다.
- invalid target 처리(실패/스킵/재선택 유도)는 구현 중 확정 후 이 plan에 명시한다.
- `feature-game-flow-loop`와 충돌 가능성이 있는 UI/씬 변경은 체크리스트 항목 단위로 분리 커밋을 유지한다.
- 구현 중 규칙 변경이 생기면 먼저 ADR-0004와 `combat-core.md`를 갱신하고 코드 변경을 따른다.

## Completion

_(completed/로 옮길 때 채움.)_

- **Finished**:
- **Outcome**:
- **Follow-ups**:
