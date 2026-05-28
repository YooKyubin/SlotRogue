# 프로젝트 상태

_Last updated: 2026-05-28_

---

## 현재 포커스

**전투 턴 이벤트 로그 + 슬롯 독립 테스트** — 전투 진입 어댑터 정리는 완료됐고, 현재 슬롯 쪽은 `Dev_Slot`에서 Battle 수정 없이 5 x 3 MVP를 검증 중이다. 다음 우선: UI 타임라인 컨트롤러 후속 plan (`CombatTimelineController`)과 입력 잠금 정책 확정.

---

## 주차 마일스톤

High-level 마일스톤. 각 주차 안에서 기능 단위 exec-plan으로 분해한다.

- [ ] **Week 1 — playable core loop**
  - 스핀 시작 → 릴 회전 → 결과 결정 → 페이라인 평가 → 결과 표시
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
| [`feature-slot-core.md`](./exec-plans/active/feature-slot-core.md) | _(슬롯 담당)_ | 2026-05-28 | Dev_Slot에서 Battle 수정 없이 5 x 3 슬롯 MVP 테스트 |

새 plan을 시작하면 [`exec-plans/active/`](./exec-plans/active/)에 `feature-<name>.md`로 추가하고 같은 커밋에 이 표에도 등록.

---

## Recently completed

| Plan | Finished | Outcome (한 줄) |
|------|----------|-----------------|
| [`feature-combat-turn-events.md`](./exec-plans/completed/feature-combat-turn-events.md) | 2026-05-27 | `TurnResult` return + `BattlePresenter.Consume` 이벤트 스트림 |
| [`feature-combat-core.md`](./exec-plans/completed/feature-combat-core.md) | 2026-05-27 | `BattleResolver` + `BattleTest` Mock, `ISpinCombatConsumer` |

가장 최근 5~10개의 완료 plan만 표시. 전체는 [`exec-plans/completed/`](./exec-plans/completed/) 참조.

---

## Known issues / blockers

- **기획 문서 미확정**: 로그라이크 메타 / 경제. 슬롯 메커닉은 [`slot-core.md`](./design-docs/slot-core.md) draft로 착수, 전투는 [`combat-core.md`](./design-docs/combat-core.md) draft로 착수 가능.
- **브랜치 / PR 워크플로 미정**: 사용해본 후 결정 → ADR로 박제 예정.

---

## 테스팅 단계

- **현재 Stage 1**: 결정론적 로직(슬롯 RNG, 페이아웃, 보상 풀 추첨)에 EditMode 단위 테스트만.
- **다음 트리거**: 세이브 시스템 도입 시 PlayMode 통합 테스트 추가 (Stage 2).
- 상세는 추후 [`design-docs/`](./design-docs/)에 `testing-policy.md`로 분리.
