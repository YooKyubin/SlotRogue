# 몬스터 턴 스케줄 (Monster Turn Schedule)

**Status**: completed  
**Started**: 2026-05-31  
**Owner**: _(전투 담당)_  
**Contributors**: _(없음)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md)  
**Related ADR**: [ADR-0001](../../adr/0001-combat-turn-effect-pipeline.md)  
**Depends on**: [`feature-combat-core`](./feature-combat-core.md), [`feature-combat-dev-scene`](./feature-combat-dev-scene.md)

## Goal

`combat-core.md` Q2: 적 턴마다 **다른 Effect 목록**을 순환 적용한다. Core에 `MonsterTurnSchedule`(턴 세트 배열 + 인덱스)을 추가하고, `BattleSystem.UpcomingEnemyActions`는 **다음 적 턴** 미리보기를 반환한다. Dev_Battle Harness Inspector에서 3턴 분량 설정·Play Mode 검증.

## Phases

### Phase 1 — Core 스케줄

- [x] `MonsterTurnSchedule` — 턴 세트 배열, `UpcomingActions`, `ConsumeUpcomingTurn()` (적용 후 인덱스 순환)
- [x] `BattleSystem` — `StartBattle(..., MonsterTurnSchedule)`, 단일 `CombatEffect[]` 오버로드 유지(1턴 스케줄 래핑)
- [x] `UpcomingEnemyActions` / `UpcomingMonsterTurnIndex` — UI·테스트용
- [x] EditMode 테스트 — 3턴 순환, 1턴 스케줄 기존 동작, 빈 스케줄 fallback

**🔍 Review:** 기존 `BattleSystemTests` green + 스케줄 테스트 green.

### Phase 2 — Dev Harness

- [x] Inspector `MonsterTurnDefinition[]` (턴당 `CombatEffectDefinition[]`)
- [x] Status UI — upcoming turn index 표시
- [x] `Dev_Battle` 기본값 3턴 예시 (damage 2 / 4 / 6)

**🔍 Review:** Play Mode 3 Apply → Console·Status에서 턴별 다른 적 피해 확인.

### Phase 3 — 마무리

- [x] `combat-core.md` Q2 닫기
- [x] plan completed + `STATUS.md`

## Notes

- **순환**: 마지막 턴 다음은 index 0. 패턴 SO·가중치 RNG는 Later.
- **빈 스케줄**: 0-length turn list → 단일 빈 Effect 목록 1턴.
- ADR amend 불필요 — design-doc Q2 닫기.

## Completion

- **Finished**: 2026-05-31
- **Outcome**: `MonsterTurnSchedule`, `BattleSystem` 스케줄 API, Harness 3턴 Inspector, EditMode 테스트 4개 추가.
- **Follow-ups**: 몬ster SO/패턴 에디터, 턴 미리보기 UI, `Dev_Slot` 연동.
