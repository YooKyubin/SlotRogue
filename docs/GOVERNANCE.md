# 문서 거버넌스

ADR / design-doc / exec-plan / STATUS.md 유지 규칙의 허브.

High-level 워크플로는 [`../AGENTS.md`](../AGENTS.md) §3에 있다. 이 파일은 상세 규칙을 직접 담지 않고, 필요한 규칙 파일로 안내한다.

---

## 핵심 원칙

**자명하지 않은 모든 변경은 paper-trail을 남긴다.**

- 코드는 *무엇*에 답한다. 문서는 *왜*, *어떻게*, *진행*에 답한다.
- 1달 스프린트라도 팀(3명) + AI 에이전트가 세션·날짜를 가로지르므로 결정과 진행 기록이 없으면 빠르게 휘발된다.

---

## 4가지 아티팩트

| 아티팩트 | 답하는 질문 | 수명 | 위치 | 상세 규칙 |
|----------|-------------|------|------|-----------|
| **ADR** | *무엇*을 *왜* 결정했나 (결정 1건/파일) | 길다 — append-only, supersede만 | `docs/adr/` | [`governance/adr.md`](./governance/adr.md) |
| **design-doc** | 시스템이 *어떻게* 맞물려 돌아가나 (narrative, ADR 인용) | 길다 — 시스템 진화에 따라 갱신 | `docs/design-docs/` | [`governance/design-docs.md`](./governance/design-docs.md) |
| **exec-plan** | 구현이 *지금 어디까지* 왔나 | 짧다 — 작업 끝나면 종료 | `docs/exec-plans/active/` → `completed/` | [`governance/exec-plans.md`](./governance/exec-plans.md) |
| **STATUS.md** | 프로젝트가 *지금* 뭘 하고 있나 | 항상 최신 | `docs/STATUS.md` | [`governance/status.md`](./governance/status.md) |

---

## 상세 규칙

| 파일 | 내용 |
|------|------|
| [`governance/adr.md`](./governance/adr.md) | ADR 작성 시점, 형식, 번호, supersede lifecycle |
| [`governance/design-docs.md`](./governance/design-docs.md) | design-doc 작성 시점, 헤더 형식, ADR과의 관계 |
| [`governance/exec-plans.md`](./governance/exec-plans.md) | exec-plan 작성/갱신/완료 워크플로 |
| [`governance/status.md`](./governance/status.md) | STATUS.md 내용과 갱신 시점 |
| [`governance/team-workflow.md`](./governance/team-workflow.md) | 팀 규칙 A~G, GitHub Issues와의 관계, 날짜, 안티패턴, 허브 문서 sweep |
| [`governance/commit-messages.md`](./governance/commit-messages.md) | 커밋 메시지 type, 형식, PowerShell here-string 예시 |

---

## 급할 때 최소 실천

- 변경이 국지적·되돌리기 쉬우면 design-doc 불요.
- exec-plan은 Goal + Checklist만 있어도 충분. 템플릿은 최대치이지 최소치가 아니다.
- STATUS.md 갱신은 exec-plan을 `completed/`로 이동하는 순간 **타협 불가**.
- 허브 문서 구조를 바꾸면 [`governance/team-workflow.md`](./governance/team-workflow.md) "허브 문서 상호 참조 sweep"을 따른다.
