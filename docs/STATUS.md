# 프로젝트 상태

_Last updated: 2026-05-31_

---

## 현재 포커스

**게임 플로우 루프** — [`feature-game-flow-loop`](./exec-plans/active/feature-game-flow-loop.md): 게임 시작 → 시작 유물 → 전체 그래프 맵 노드 선택 → 전투 → 보상 → 맵 반복. UI는 View 프리팹 + Controller 방식이며, 전투 코드는 수정하지 않고 기존 API로 연결.

**슬롯 MVP** — [`feature-slot-core`](./exec-plans/active/feature-slot-core.md): `Dev_Slot`에서 5×3 검증. 다음: 게임 플로우 전투 씬에서 `SlotCombatRequest` 연결.

**몬스터 패턴 SO** — [`feature-monster-pattern-so`](./exec-plans/completed/feature-monster-pattern-so.md) 완료.

**몬스터 턴 스케줄** — [`feature-monster-turn-schedule`](./exec-plans/completed/feature-monster-turn-schedule.md) 완료 (Q2).

**전투 Dev 씬** — [`feature-combat-dev-scene`](./exec-plans/completed/feature-combat-dev-scene.md) 완료.

**전투 코어 MVP** — [`feature-combat-core`](./exec-plans/completed/feature-combat-core.md) 완료.

---

## 주차 마일스톤

High-level 마일스톤. 각 주차 안에서 기능 단위 exec-plan으로 분해한다.

- [ ] **Week 1 — playable core loop**
  - 스핀 시작 → 릴 회전 → 결과 결정 → 페이아웃 평가 → 결과 표시
  - 단위 테스트: RNG / 페이아웃
- [ ] **Week 2 — meta progression**
  - 런 구조, 노드 맵, 첫 보상 풀, 세이브/로드
- [ ] **Week 3 — content & balance**
  - 심볼·이벤트 콘텐츠 확장, 경제 1차 튜닝
- [ ] **Week 4 — polish & build**
  - 사운드, 연출, UX 정리, Android 빌드, 실기기 검증

---

## Active exec-plans

| Plan | Owner | Started | Goal (한 줄) |
|------|-------|---------|----------------|
| [`feature-game-flow-loop.md`](./exec-plans/active/feature-game-flow-loop.md) | _(슬롯 담당)_ | 2026-05-31 | 시작/유물/맵/전투/보상 반복 루프 |
| [`feature-slot-core.md`](./exec-plans/active/feature-slot-core.md) | _(슬롯 담당)_ | 2026-05-28 | Dev_Slot에서 5 x 3 슬롯 MVP 테스트 |

새 plan을 시작하면 [`exec-plans/active/`](./exec-plans/active/)에 `feature-<name>.md`로 추가하고 같은 커밋에 이 표에도 등록.

---

## Recently completed

| Plan | Finished | Outcome (한 줄) |
|------|----------|-----------------|
| [`feature-monster-pattern-so.md`](./exec-plans/completed/feature-monster-pattern-so.md) | 2026-05-31 | 몬스터 패턴·정의 SO + Factory + Goblin asset, Harness SO-only |
| [`feature-monster-turn-schedule.md`](./exec-plans/completed/feature-monster-turn-schedule.md) | 2026-05-31 | `MonsterTurnSchedule` 턴 순환 + Dev_Battle 3턴 Inspector |
| [`feature-combat-dev-scene.md`](./exec-plans/completed/feature-combat-dev-scene.md) | 2026-05-31 | `Dev_Battle` Harness + Request 변환 + Console 이벤트 로거 |
| [`feature-combat-core.md`](./exec-plans/completed/feature-combat-core.md) | 2026-05-31 | `BattleSystem` 턴 파이프라인 + EditMode 테스트 20개 |

가장 최근 5~10개의 완료 plan만 표시. 전체는 [`exec-plans/completed/`](./exec-plans/completed/) 참조.

---

## Known issues / blockers

- **기획 문서 미확정**: 로그라이크 메타 / 경제.
- **브랜치 / PR 워크플로 미정**: 사용해본 후 결정 → ADR로 박제 예정.
- **ADR-0001 Status**: 구현 완료, `accepted` 전환은 팀 합의 후.

---

## 테스팅 단계

- **현재 Stage 1**: 결정론적 로직(슬롯 RNG, 페이아웃, 전투 Effect/턴)에 EditMode 단위 테스트.
- **다음 트리거**: 세이브 시스템 도입 시 PlayMode 통합 테스트 추가 (Stage 2).
- 상세는 추후 [`design-docs/`](./design-docs/)에 `testing-policy.md`로 분리.
