# Active exec-plans

진행 중인 작업 단위별 체크리스트가 여기에 들어간다.

> 작성·갱신·완료 규칙: [`../../GOVERNANCE.md`](../../GOVERNANCE.md) "exec-plan 규칙" 섹션.

---

## 현재 상태

| Plan | Started |
|------|---------|
| [`feature-slot-core.md`](./feature-slot-core.md) | 2026-05-28 |

새 plan을 시작하면 같은 커밋에서 [`../../STATUS.md`](../../STATUS.md)의 "Active exec-plans" 표에도 등록한다.

---

## 네이밍

`feature-<short-name>.md` (기능 단위, 사람 단위 금지).

예:
- `feature-spin-core.md`
- `feature-meta-map.md`
- `feature-shop.md`
- `feature-save-load.md`
- `feature-android-build.md`

phase 분할이 필요하면 `feature-<name>-phase-1.md`, `-phase-2.md`.

---

## 템플릿 (요약)

```markdown
# <Plan 제목>

**Status**: active
**Started**: YYYY-MM-DD
**Owner**: @<github-id>
**Contributors**: @<id> (역할), ...
**Related design-docs**: links (있을 때)

## Goal
한 문단: "완료"가 어떤 모습인가.

## Checklist
- [ ] Step 1 — @<assignee>
- [ ] Step 2 — @<assignee>

## Notes
자유 서식: 마주친 블로커, 도중 바뀐 결정, 미룬 것.

## Completion
(`completed/`로 옮길 때 채움.)
- **Finished**: YYYY-MM-DD
- **Outcome**: 실제로 전달된 것 요약
- **Follow-ups**: 다음 plan으로 미룬 것
```

전체 형식과 팀 규칙은 [`../../GOVERNANCE.md`](../../GOVERNANCE.md) 참조.

---

## 핵심 규칙 요약

- **한 plan에 한 오너.** 협업자는 체크리스트 항목별 assignee로만.
- **체크박스 갱신은 작업 커밋/PR과 같은 단위에 함께 push.** 따로 커밋 금지.
- **수명 3일 ~ 2주.** 길어지면 phase 분할.
- 1~2 PR로 끝나는 작업·스파이크·단순 리팩터링은 plan 만들지 않는다.
