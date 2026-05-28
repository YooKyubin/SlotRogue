# 문서 인덱스

SlotRogue의 모든 문서 진입점. `docs/`를 처음 탐색할 때 이 파일부터 읽는다.

> 모든 문서는 한국어로 작성된다. 코드/식별자만 영어. 자세한 언어 규칙은 [`../AGENTS.md`](../AGENTS.md) §0 참조.

---

## 최상위

| 파일 | 목적 |
|------|------|
| [`STATUS.md`](./STATUS.md) | 프로젝트 상태 보드. 현재 포커스, 주차 마일스톤, active/completed 작업. |
| [`GOVERNANCE.md`](./GOVERNANCE.md) | ADR / design-doc / exec-plan / STATUS 갱신 규칙 + 팀 규칙 상세. |

---

## 카테고리

### `adr/` — 결정 (single source of truth)

결정 1건당 파일 1개. 각 ADR은 Context / Decision / Alternatives / Consequences. design-doc은 ADR을 번호로 인용할 뿐 rationale을 재서술하지 않는다.

- [`adr/INDEX.md`](./adr/INDEX.md) — 모든 결정 목록, 현재 상태, 날짜
- [`adr/TEMPLATE.md`](./adr/TEMPLATE.md) — 새 ADR 작성 템플릿

### `design-docs/` — 왜 & 어떻게 (narrative)

시스템 개요, 인터페이스 스케치, 열린 질문. design-doc 내부 결정은 ADR 인용이어야 하며 본문 재서술 금지. 각 파일 헤더에 `Status: draft | accepted | superseded` 명시.

- [`design-docs/INDEX.md`](./design-docs/INDEX.md) — **기획 문서 확정 후** 시스템별 doc 추가 (현재 후보 목록만)

자명하지 않은 결정이 구현 전에 필요할 때마다 새 design-doc을 추가한다.

### `exec-plans/` — 진행 상황

active/완료 작업의 단계별 체크리스트. 어떤 plan이 active인지: [`STATUS.md`](./STATUS.md).

- `active/` — 진행 중 ([README](./exec-plans/active/README.md))
- `completed/` — 완료 (Completion 섹션 채워 옮긴 plan, [README](./exec-plans/completed/README.md))

active → completed 워크플로는 [`GOVERNANCE.md`](./GOVERNANCE.md) 참조.

### `guides/` — How-to

design-doc도 exec-plan도 아닌 실용 가이드.

- [`guides/unity-setup.md`](./guides/unity-setup.md) — Unity 버전, 패키지(Addressables/UniTask/DOTween), 프로젝트 설정, asmdef 분할
- [`guides/coding-style.md`](./guides/coding-style.md) — C#/Unity 네이밍, MonoBehaviour 패턴, 비동기/트윈/로깅

**필요해지면 추가** (지금은 만들지 않는다): `mobile-build.md`(Android/iOS 빌드 + 키스토어), `unity-profiling.md`(Profiler/Frame Debugger), `addressables-workflow.md`(그룹/빌드/원격 호스팅), `package-setup.md`.

---

## 새 기여자 (또는 새 세션) 읽기 순서

1. 루트 [`../AGENTS.md`](../AGENTS.md) — 규칙과 결정 인덱스
2. [`STATUS.md`](./STATUS.md) — 프로젝트 현재 위치
3. [`GOVERNANCE.md`](./GOVERNANCE.md) — 작업을 어떻게 추적하는가
4. 손댈 시스템의 `design-docs/` → 거기서 인용된 `adr/NNNN-*.md` ADR
5. 외부 자료가 필요할 때만 [`../references/INDEX.md`](../references/INDEX.md)

---

## 횡단 컨벤션

- 파일명은 `kebab-case.md`.
- 각 design-doc은 한 줄 purpose + `Status` 라인으로 시작 (`draft` / `accepted` / `superseded`).
- 각 exec-plan은 체크리스트 + `Completion` 섹션 (completed/로 옮길 때 채움).
- 날짜는 절대 표기 (`YYYY-MM-DD`), "어제" / "지난주" 금지.
