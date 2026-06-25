# 팀 워크플로 규칙

3명 팀 + AI 에이전트 활용 환경에서 쓰는 운영 규칙.

---

## 팀 규칙 (A~G)

### A. 한 plan에 한 오너

공동 편집 금지. 두 명이 같은 기능을 만들어도 plan 오너는 **한 명**, 나머지는 체크리스트 항목별 assignee로 표시. 오너가 plan 갱신 책임자 = 머지 충돌이 줄어든다.

```markdown
**Owner**: @yookyubin
**Contributors**: @teammate-a (sound), @teammate-b (vfx)
```

### B. plan 단위는 기능, 사람이 아님

- 안 함: `kyubin-week1.md`, `teammate-a-week1.md` — 기능이 plan 두 곳에 흩어진다.
- 함: `feature-spin-core.md`, `feature-shop.md`, `feature-meta-map.md`.

동시에 active 4~6개가 상한. 더 많으면 분해가 잘못된 것.

### C. plan 갱신은 작업 커밋/PR과 같은 단위

체크박스를 작업 diff 안에서 같이 ✓ → 리뷰어가 "이 PR = 이 항목 종료"를 직접 검증. 코드 따로, plan 따로 커밋하면 plan은 결국 stale 된다.

### D. plan 수명 3일 ~ 2주

길어지면 phase 분할 (`feature-shop.md` → `feature-shop-phase-1.md`, `-phase-2.md`). 오래 끄는 plan은 거의 항상 "완료의 정의"가 모호한 상태.

### E. 머지 충돌 최소화

- 마크다운 체크박스는 줄 단위 머지가 잘 됨. 같은 줄을 두 명이 동시에 편집하면 충돌.
- 항목별 한 줄 유지, 항목 사이에 빈 줄을 두지 않기.
- "Notes" 같은 자유 텍스트는 오너가 일괄 정리하거나 사람별 서브섹션으로 분리.

### F. STATUS.md = 팀 보드

모든 active plan을 STATUS에 1줄로 미러링 (제목 + 오너 + 상태). 매일 아침 STATUS만 보면 "팀이 지금 뭐 하는지" 파악 끝. plan 새로 만들거나 `completed/`로 옮길 때 **반드시 같은 커밋에 STATUS 갱신**.

### G. 작은 일에는 plan 만들지 않는다

[`exec-plans.md`](./exec-plans.md)의 "작성 시점" 제외 목록을 다시 보라. 남용 시 plan 폴더가 노이즈가 되고 정작 중요한 plan이 묻힌다.

---

## GitHub Issues와의 관계

| 도구 | 잘 하는 것 | 못 하는 것 |
|------|-----------|-----------|
| GitHub Issues / Projects | 외부 보고된 버그, 토론, 라벨링 | 코드와 같은 머지 단위로 변경 추적, 에이전트가 grep·갱신, 오프라인 |
| repo-내 exec-plans | 코드와 동시 변경, 에이전트가 즉시 읽고 갱신, 영구 기록 | 비-개발자 가시성, 라이브 코멘트 스레드 |

**권장**: 외부 버그/디스커션은 Issues, 내부 작업 추적은 exec-plans. 둘 다 쓰되 역할 분리.

---

## 날짜 규칙

- 항상 절대 표기: `2026-05-26`. "어제" / "지난주" 금지.
- 몇 달 뒤 로그를 다시 읽을 때 상대 표현은 부패한다.

---

## 회피할 안티패턴

- **조용한 재작성**: design-doc의 결정을 supersede 표시 없이 바꾸는 것.
- **빈 exec-plan**: `active/foo.md`를 만들고 갱신 안 함. 작업하거나 닫거나.
- **active → completed 이동 누락**: 끝난 작업을 "일단" `active/`에 두기. 디렉터리가 거짓말을 한다.
- **STATUS drift**: STATUS.md가 `active/`/`completed/`와 어긋남. 버그로 취급.
- **과잉 문서화**: 한 줄 코드 주석으로 충분한 결정에 design-doc.
- **허브 문서 재작성 후 상호 참조 stale**: `AGENTS.md`, `docs/INDEX.md`, `docs/GOVERNANCE.md` 등을 재구조화할 때 inbound 참조 grep을 같은 변경에 안 함.
- **채팅 단계 결정 코드 박제**: `C-2`, `B-4-①`, "Category D decision 14" 같은 것은 planning 채팅에서만 존재하는 라벨이다. 커밋된 문서에는 안정된 앵커(파일 경로 + 섹션 제목)로 번역해 적는다.

---

## 허브 문서 상호 참조 sweep

**허브 문서**는 다른 파일이 섹션 번호 또는 제목으로 인용하는 파일이다. 보통 `AGENTS.md`, `docs/INDEX.md`, `docs/GOVERNANCE.md`가 해당한다.

허브 문서의 구조를 바꾸면(renumber, retitle, 내용 이동) inbound 참조가 조용히 깨진다.

**절차** — 허브 문서 편집과 같은 변경 안에서:

1. 허브 문서 파일명을 repo 전체에 grep:
   ```
   Grep "<hubdoc>\.md"
   ```
2. 각 매치에 대해 인용된 섹션/앵커가 여전히 존재하는지 확인. 없으면 갱신 또는 재표현.
3. 수정 후, 이제 무효해진 패턴을 다시 grep해 0 매치를 확인.

**왜 "기억"이 아니라 grep인가**: 이 절차가 막는 실패는 정확히 "다 체크했다고 생각했는데 실은 빠진" 케이스다. Grep은 기계적이고, 싸고(1초), 멱등이며, 0 매치라는 검증 가능한 결과를 낸다.

**새 인용을 쓸 땐 안정된 앵커 선호**: 파일 경로 + 섹션 제목 (`docs/guides/unity-setup.md` "패키지 설치")이 깨지기 쉬운 숫자 서브섹션(`§2.1`)을 이긴다.
