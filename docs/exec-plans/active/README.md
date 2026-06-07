# Active exec-plans

진행 중인 작업 단위별 체크리스트가 여기에 들어간다.

> 작성·갱신·완료 규칙: [`../../GOVERNANCE.md`](../../GOVERNANCE.md)의 exec-plan 규칙 참조.

---

## 현재 상태

| Plan | Started |
|------|---------|
| [`feature-game-flow-loop.md`](./feature-game-flow-loop.md) | 2026-05-31 |
| [`feature-slot-core.md`](./feature-slot-core.md) | 2026-05-28 |
| [`feature-attribute-artifacts.md`](./feature-attribute-artifacts.md) | 2026-06-04 |
| [`feature-run-battle-mvvm.md`](./feature-run-battle-mvvm.md) | 2026-06-05 |

새 plan을 시작하면 같은 커밋에서 [`../../STATUS.md`](../../STATUS.md)의 "Active exec-plans" 표에도 등록한다.

---

## 네이밍

`feature-<short-name>.md` 형식을 사용한다. 기능 단위로 만들고 사람 단위로 만들지 않는다.

예:
- `feature-spin-core.md`
- `feature-meta-map.md`
- `feature-shop.md`
- `feature-save-load.md`
- `feature-android-build.md`

phase 분할이 필요하면 `feature-<name>-phase-1.md`, `feature-<name>-phase-2.md`처럼 나눈다.

---

## 템플릿

```markdown
# <Plan 제목>

**Status**: active
**Started**: YYYY-MM-DD
**Owner**: @<github-id>
**Contributors**: @<id> (역할), ...
**Related design-docs**: links

## Goal

한 문단으로 "완료"가 어떤 모습인지 적는다.

## Checklist

- [ ] Task A — @<assignee>
- [ ] Task B — @<assignee>

## Notes

작업 중 바뀐 결정, 미룬 것, 확인할 것.

## Completion

- **Finished**:
- **Outcome**:
- **Follow-ups**:
```
