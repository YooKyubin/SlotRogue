# Completed exec-plans

완료된 plan이 모이는 곳. `Completion` 섹션이 채워진 채로 [`../active/`](../active/)에서 `git mv`로 옮겨진다.

> 완료 워크플로: [`../../GOVERNANCE.md`](../../GOVERNANCE.md) "exec-plan 규칙 > Completion" 섹션.

---

## 현재 상태

| Plan | Finished | Outcome (한 줄) |
|------|----------|-----------------|
| [`feature-combat-dev-scene.md`](./feature-combat-dev-scene.md) | 2026-05-31 | `Dev_Battle` Harness + Request 변환 + Console 이벤트 로거 |
| [`feature-combat-core.md`](./feature-combat-core.md) | 2026-05-31 | `BattleSystem` 턴 파이프라인 + EditMode 테스트 20개 |

가장 최근 5~10개는 [`../../STATUS.md`](../../STATUS.md)의 "Recently completed" 표에도 등록된다.

---

## 보관 원칙

- 완료된 plan은 **삭제하지 않는다**. 이후 회고·재현·유사 기능 재구현 시 가장 정직한 기록이 된다.
- `Notes` 섹션의 블로커·우회·결정 변경 기록이 코드 커밋 메시지보다 정보 밀도가 높다.
- 같은 이름의 plan을 두 번 만들지 않는다 (`feature-shop.md` 완료 후 다시 손대려면 `feature-shop-phase-2.md`).
