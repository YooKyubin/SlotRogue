# 프로젝트 상태

_Last updated: 2026-05-29_

---

## 현재 포커스

**전투 v2 설계 (논의 중)** — [`combat-core-v2-draft.md`](./design-docs/combat-core-v2-draft.md). 구현·ADR 승격 전. v1 코드/문서는 참고용.

**전투 UI 타임라인 (v1 파이프라인)** — [`feature-combat-timeline-controller`](./exec-plans/active/feature-combat-timeline-controller.md): `TurnResult` 순차 연출, Mock 입력 잠금. v2 확정 시 재정렬 예정.

**슬롯 MVP** — [`feature-slot-core`](./exec-plans/active/feature-slot-core.md): `Dev_Slot`에서 Battle 수정 없이 5×3 검증.

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
| [`feature-combat-timeline-controller.md`](./exec-plans/active/feature-combat-timeline-controller.md) | _(기입)_ | 2026-05-28 | `CombatTimelineController`로 턴 이벤트 순차 연출 + 입력 잠금 |
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

- **기획 문서 미확정**: 슬롯 메커닉(`slot-core.md`) / 로그라이크 메타 / 경제 — 전투는 [`combat-core.md`](./design-docs/combat-core.md) draft로 착수 가능.
- **브랜치 / PR 워크플로 미정**: 사용해본 후 결정 → ADR로 박제 예정.

---

## 테스팅 단계

- **현재 Stage 1**: 결정론적 로직(슬롯 RNG, 페이아웃, 보상 풀 추첨)에 EditMode 단위 테스트만.
- **다음 트리거**: 세이브 시스템 도입 시 PlayMode 통합 테스트 추가 (Stage 2).
- 상세는 추후 [`design-docs/`](./design-docs/)에 `testing-policy.md`로 분리.
