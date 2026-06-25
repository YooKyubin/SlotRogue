# STATUS.md 규칙

[`../STATUS.md`](../STATUS.md)는 프로젝트가 *지금* 뭘 하고 있는지 보여주는 살아있는 보드다.

---

## 내용

- **Last updated** (날짜)
- **현재 포커스** — 1~2줄, 지금 작업 중인 것
- **주차 마일스톤** — Week 1~4 체크박스
- **Active exec-plans** — `active/`의 파일 목록 (오너 표기)
- **Recently completed** — `completed/`의 최근 5~10개
- **Known issues / blockers** — 멈췄거나 결정 대기 중인 것
- **테스팅 단계** — 현재 단계와 다음 트리거

---

## 갱신 시점

- **필수**: exec-plan을 `completed/`로 옮기는 같은 커밋에.
- **권장**: 새 exec-plan 시작 시 (Active에 추가), 블로커 발생 시, 테스팅 단계 변경 시.
- **권장**: 매일 1줄 (현재 포커스). 강제는 아님.
- **회피**: STATUS만 단독으로 커밋. 가능하면 변경 근거가 되는 작업과 묶는다.
