# Completed exec-plans

완료된 plan이 모이는 곳. `Completion` 섹션이 채워진 채로 [`../active/`](../active/)에서 `git mv`로 옮겨진다.

> 완료 워크플로: [`../../GOVERNANCE.md`](../../GOVERNANCE.md) "exec-plan 규칙 > Completion" 섹션.

---

## 현재 상태

| Plan | Finished | Outcome (한 줄) |
|------|----------|-----------------|
| [`feature-monster-battle-view-integration.md`](./feature-monster-battle-view-integration.md) | 2026-06-17 | 몬스터 combat visual prefab 생성과 ActionStarted 기반 Idle/Attack 애니메이션 경로 연결 |
| [`feature-notifications-weekly-ranking.md`](./feature-notifications-weekly-ranking.md) | 2026-06-14 | 24시간 복귀 알림과 한국 수요일 주간 랭킹 리셋 전 마감 알림 예약 |
| [`feature-attribute-artifacts.md`](./feature-attribute-artifacts.md) | 2026-06-11 | 구 Artifact 시작 유물 모델을 v20.3 RelicCatalog로 대체 |
| [`feature-enemy-formation-slot.md`](./feature-enemy-formation-slot.md) | 2026-06-04 | EnemyFormationSlot 프리팹, MonsterDefinition.portrait, 슬롯 DamageAnchor·플로팅 좌표 |
| [`feature-map-encounter-so.md`](./feature-map-encounter-so.md) | 2026-06-04 | RunEncounterDefinition·맵 그래프 SO, roster 빌더, formation 3슬롯 HUD |
| [`feature-multi-participant-play-ui.md`](./feature-multi-participant-play-ui.md) | 2026-06-04 | RunBattle 2/3몹 roster hint, 몬스터별 HUD·타겟 선택·id anchor 연결 |
| [`feature-floating-combat-text.md`](./feature-floating-combat-text.md) | 2026-06-02 | 데미지 텍스트 prefab/view + anchor 기반 전환, Dev/RunBattle 공용 계약 |
| [`feature-run-battle-presentation.md`](./feature-run-battle-presentation.md) | 2026-06-01 | RunBattle Replay — RunTurnAsync, ViewModel HUD, presentation overlay |
| [`feature-combat-presentation.md`](./feature-combat-presentation.md) | 2026-05-31 | Replay MVP — Flow/Presenter/ViewModel, Dev_Battle RunTurnAsync |
| [`feature-monster-pattern-so.md`](./feature-monster-pattern-so.md) | 2026-05-31 | 몬스터 패턴·정의 SO + Factory + Goblin asset, Harness SO-only |
| [`feature-monster-turn-schedule.md`](./feature-monster-turn-schedule.md) | 2026-05-31 | `MonsterTurnSchedule` 턴 순환 + Dev_Battle 3턴 Inspector |
| [`feature-combat-dev-scene.md`](./feature-combat-dev-scene.md) | 2026-05-31 | `Dev_Battle` Harness + Request 변환 + Console 이벤트 로거 |
| [`feature-combat-core.md`](./feature-combat-core.md) | 2026-05-31 | `BattleSystem` 턴 파이프라인 + EditMode 테스트 20개 |

가장 최근 5~10개는 [`../../STATUS.md`](../../STATUS.md)의 "Recently completed" 표에도 등록된다.

---

## 보관 원칙

- 완료된 plan은 **삭제하지 않는다**. 이후 회고·재현·유사 기능 재구현 시 가장 정직한 기록이 된다.
- `Notes` 섹션의 블로커·우회·결정 변경 기록이 코드 커밋 메시지보다 정보 밀도가 높다.
- 같은 이름의 plan을 두 번 만들지 않는다 (`feature-shop.md` 완료 후 다시 손대려면 `feature-shop-phase-2.md`).
