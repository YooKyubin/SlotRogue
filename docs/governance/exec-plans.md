# exec-plan 규칙

exec-plan은 구현이 *지금 어디까지* 왔는지 남기는 짧은 수명 체크리스트다.

---

## 작성 시점

한 짧은 세션을 넘어가거나 쓰인 체크리스트가 도움이 되는 작업 단위를 시작할 때.

**만들지 않을 작업**:

- 1~2 PR / 하루 안에 끝나는 변경
- 실험·스파이크 (결과가 코드로 남지 않을 수 있는 것)
- 단순 리팩터링

남용하면 plan이 노이즈가 된다. 1달 프로젝트 기준 총 10~20개 정도가 적당하다.

---

## 네이밍

`feature-<short-name>.md`.

예:

- `feature-spin-core.md`
- `feature-meta-map.md`
- `feature-shop.md`
- `feature-save-load.md`
- `feature-android-build.md`

---

## 형식

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
- ...

## Notes
자유 서식: 마주친 블로커, 도중 바뀐 결정, 미룬 것.

## Completion
(`completed/`로 옮길 때 채움.)
- **Finished**: YYYY-MM-DD
- **Outcome**: 실제로 전달된 것 요약
- **Follow-ups**: 다음 plan으로 미룬 것
```

---

## 갱신

- **지속적으로**: 작업 진행에 따라 체크박스 ✓. 블로커/이탈은 Notes에 추가.
- 작업이 진행 중인데 갱신이 멈춘 plan은 위반이다.
- **plan 갱신은 작업 커밋/PR과 같은 단위에 함께 push**. 따로 커밋하지 않는다.

---

## Completion

체크리스트의 의미 있는 항목이 전부 끝나면:

1. `Completion` 섹션 채움.
2. `git mv docs/exec-plans/active/<file>.md docs/exec-plans/completed/<file>.md`.
3. **같은 커밋에서** [`../STATUS.md`](../STATUS.md) 갱신:
   - "Active exec-plans" → "Recently completed"로 항목 이동.
   - 필요 시 "현재 포커스" 갱신.
   - `_Last updated_` 날짜 bump.
4. 커밋 메시지에 plan과 STATUS 갱신을 함께 언급.

STATUS 갱신 없이 exec-plan을 `completed/`로 옮긴 커밋은 즉시 보강해야 한다.
