# 프로젝트 상태

_Last updated: 2026-05-26_

---

## 현재 포커스

**전투 코어** — Phase A 구현 완료(Resolver + EditMode 테스트). 다음: Phase B(`BattleTest` Mock, `ISpinCombatConsumer` 구현).

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
| [`feature-combat-core.md`](./exec-plans/active/feature-combat-core.md) | _(기입)_ | 2026-05-26 | EditMode 전투 resolver + BattleTest Mock 스핀 |

새 plan을 시작하면 [`exec-plans/active/`](./exec-plans/active/)에 `feature-<name>.md`로 추가하고 같은 커밋에 이 표에도 등록.

---

## Recently completed

_(없음)_

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
