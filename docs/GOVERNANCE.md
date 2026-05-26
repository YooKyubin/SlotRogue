# 문서 거버넌스

ADR / design-doc / exec-plan / STATUS.md 유지 규칙 상세.

High-level 워크플로는 [`../CLAUDE.md`](../CLAUDE.md) §3에 있다. 이 문서는 각 단계를 펼친다.

---

## 핵심 원칙

**자명하지 않은 모든 변경은 paper-trail을 남긴다.**

- 코드는 *무엇*에 답한다. 문서는 *왜*, *어떻게*, *진행*에 답한다.
- 1달 스프린트라도 팀(3명) + AI 에이전트가 세션·날짜를 가로지르므로 결정과 진행 기록이 없으면 빠르게 휘발된다.

---

## 4가지 아티팩트

| 아티팩트 | 답하는 질문 | 수명 | 위치 |
|----------|-------------|------|------|
| **ADR** | *무엇*을 *왜* 결정했나 (결정 1건/파일) | 길다 — append-only, supersede만 | `docs/adr/` |
| **design-doc** | 시스템이 *어떻게* 맞물려 돌아가나 (narrative, ADR 인용) | 길다 — 시스템 진화에 따라 갱신 | `docs/design-docs/` |
| **exec-plan** | 구현이 *지금 어디까지* 왔나 | 짧다 — 작업 끝나면 종료 | `docs/exec-plans/active/` → `completed/` |
| **STATUS.md** | 프로젝트가 *지금* 뭘 하고 있나 | 항상 최신 | `docs/STATUS.md` |

---

## ADR 규칙

### 작성 시점

거절된 대안이 1개 이상 있는 구체적·인용 가능한 결정이 내려질 때. 예:

- 슬롯 RNG / 페이아웃 모델 (고정 RTP vs 동적)
- 세이브 포맷 (JSON 평문 vs 암호화 binary)
- Addressables 그룹 / 빌드 전략
- 광고/IAP SDK 채택
- 광역 정책 (예: "모든 비동기는 UniTask")

단일 선언적 문장으로 표현 가능하고 거절된 대안이 1개 이상이면 ADR-shaped. 그렇지 않으면 (narrative, 열린 질문, 스타일 선호) design-doc, `coding-style.md`, 또는 코드 주석에 속한다.

### 형식

[`adr/TEMPLATE.md`](./adr/TEMPLATE.md) 사용. 필수 필드: Status, Date, Context, Decision, Alternatives considered, Consequences.

**Alternatives considered**: 1개 이상 **권장**. 정말 대안이 없으면 ADR이 아닐 가능성 — design-doc이나 코드 주석을 고려한다.

> 참고: KyuEngine은 "must contain ≥1 rejected alternative"였다. SlotRogue는 1달 스프린트 특성상 권장으로 완화. 단, 0개일 땐 *왜 대안이 없는지* Notes에 한 줄 적기를 권장.

### 번호와 lifecycle

- 파일명: `NNNN-kebab-case-title.md` (zero-pad 4자리).
- 번호는 **append-only**. renumber/reuse/delete 금지.
- Supersede: 새 ADR이 옛 것을 `Supersedes:`로 인용, 옛 ADR의 `Status`를 `superseded`로 바꾸고 `Superseded by:` 라인 추가. 두 파일 모두 디스크에 남는다.
- 거절된 제안도 `Status: rejected`로 디스크에 남긴다 — 거절 자체가 검색 가능해야 함.

### design-doc과의 관계

- 결정 본문은 ADR에. design-doc은 ADR을 인용하되 Context/Alternatives/Consequences를 재서술 금지.
- design-doc에서 한 줄 navigation 요약은 허용 (`ADR-0001 — 슬롯 RNG는 결정론적 시드 기반`), 구속력은 ADR에.
- ADR Decision 본문을 in-place 수정 금지. 본질 변경은 supersede로.

### 갱신

- 오타 / 명확화 수정 (결정 본문 비변경): in-place 편집, Date bump 불요.
- 본질이 바뀌면: 새 ADR로 supersede.
- [`adr/INDEX.md`](./adr/INDEX.md) 표는 새 ADR 또는 status 변경과 같은 커밋에 갱신.

---

## design-doc 규칙

### 작성 시점

새 시스템의 자명하지 않은 코드를 쓰기 전, 또는 큰 결정이 바뀔 때. 예:

- 새 서브시스템 (슬롯 코어, 메타 진행, 경제, 세이브, UI 흐름, 데이터 파이프라인)
- 큰 아키텍처 변경 (예: PlayerPrefs → 파일 기반 세이브 마이그레이션)
- 횡단 정책 (testing-policy, build-and-release)

**SlotRogue 현재 상태**: 기획 문서가 확정되기 전이라 design-doc은 비어있다. [`design-docs/INDEX.md`](./design-docs/INDEX.md)에 후보 목록만 두었다.

### 형식

각 design-doc은 다음으로 시작:

```markdown
# <시스템명>

**Status**: draft | accepted | superseded
**Last updated**: YYYY-MM-DD

## Purpose
한 문단: 이 시스템이 무엇을 하고 왜 존재하는가.

## Decisions
구체적 결정 번호 목록 (각각 짧은 rationale, 가능하면 ADR 인용).

## Open questions
나중에 결정으로 미룬 것들.

## Alternatives considered
거절한 것과 이유.
```

이 외 섹션은 자유 (다이어그램, 코드 스케치, 레퍼런스).

### 갱신

- 작은 명확화: in-place 편집, `Last updated` bump.
- 큰 접근 변경: 옛 doc을 `superseded` 마크, 새 doc으로 링크. 조용한 재작성 금지.

---

## exec-plan 규칙

### 작성 시점

한 짧은 세션을 넘어가거나 쓰인 체크리스트가 도움이 되는 작업 단위를 시작할 때.

**만들지 않을 작업** (G 규칙):

- 1~2 PR / 하루 안에 끝나는 변경
- 실험·스파이크 (결과가 코드로 남지 않을 수 있는 것)
- 단순 리팩터링

남용하면 plan이 노이즈가 된다. 1달 프로젝트 기준 총 10~20개 정도가 적당.

### 네이밍

`feature-<short-name>.md`.

예:
- `feature-spin-core.md`
- `feature-meta-map.md`
- `feature-shop.md`
- `feature-save-load.md`
- `feature-android-build.md`

### 형식

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

### 갱신

- **지속적으로**: 작업 진행에 따라 체크박스 ✓. 블로커/이탈은 Notes에 추가.
- 작업이 진행 중인데 갱신이 멈춘 plan은 위반이다.
- **plan 갱신은 작업 커밋/PR과 같은 단위에 함께 push** (규칙 C). 따로 커밋 금지.

### Completion

체크리스트의 의미 있는 항목이 전부 끝나면:

1. `Completion` 섹션 채움.
2. `git mv docs/exec-plans/active/<file>.md docs/exec-plans/completed/<file>.md`.
3. **같은 커밋에서** [`STATUS.md`](./STATUS.md) 갱신:
   - "Active exec-plans" → "Recently completed"로 항목 이동.
   - 필요 시 "현재 포커스" 갱신.
   - `_Last updated_` 날짜 bump.
4. 커밋 메시지에 plan과 STATUS 갱신을 함께 언급.

STATUS 갱신 없이 exec-plan을 `completed/`로 옮긴 커밋은 즉시 보강해야 한다.

---

## 팀 규칙 (A~G)

3명 팀 + AI 에이전트 활용 환경에서 가장 실전적인 7개 규칙. [`../CLAUDE.md`](../CLAUDE.md) §3에 요약, 여기에 상세.

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

위 "작성 시점"의 제외 목록을 다시 보라. 남용 시 plan 폴더가 노이즈가 되고 정작 중요한 plan이 묻힌다.

---

## GitHub Issues와의 관계

| 도구 | 잘 하는 것 | 못 하는 것 |
|------|-----------|-----------|
| GitHub Issues / Projects | 외부 보고된 버그, 토론, 라벨링 | 코드와 같은 머지 단위로 변경 추적, 에이전트가 grep·갱신, 오프라인 |
| repo-내 exec-plans | 코드와 동시 변경, 에이전트가 즉시 읽고 갱신, 영구 기록 | 비-개발자 가시성, 라이브 코멘트 스레드 |

**권장**: 외부 버그/디스커션은 Issues, 내부 작업 추적은 exec-plans. 둘 다 쓰되 역할 분리.

---

## STATUS.md 규칙

### 내용

- **Last updated** (날짜)
- **현재 포커스** — 1~2줄, 지금 작업 중인 것
- **주차 마일스톤** — Week 1~4 체크박스
- **Active exec-plans** — `active/`의 파일 목록 (오너 표기)
- **Recently completed** — `completed/`의 최근 5~10개
- **Known issues / blockers** — 멈췄거나 결정 대기 중인 것
- **테스팅 단계** — 현재 단계와 다음 트리거

### 갱신 시점

- **필수**: exec-plan을 `completed/`로 옮기는 같은 커밋에.
- **권장**: 새 exec-plan 시작 시 (Active에 추가), 블로커 발생 시, 테스팅 단계 변경 시.
- **권장**: 매일 1줄 (현재 포커스). 강제는 아님.
- **회피**: STATUS만 단독으로 커밋 — 가능하면 변경 근거가 되는 작업과 묶기.

---

## 날짜 규칙

- 항상 절대 표기: `2026-05-26`. "어제" / "지난주" 금지.
- 몇 달 뒤 로그를 다시 읽을 때 상대 표현은 부패한다.

---

## 회피할 안티패턴

- **조용한 재작성**: design-doc의 결정을 supersede 표시 없이 바꾸는 것.
- **빈 exec-plan**: `active/foo.md`를 만들고 갱신 안 함. 작업하거나 닫거나.
- **active → completed 이동 누락**: 끝난 작업을 "일단" `active/`에 두기. 디렉터리가 거짓말을 한다.
- **STATUS drift**: STATUS.md가 `active/`/`completed/`와 어긋남 — 버그로 취급.
- **과잉 문서화**: 한 줄 코드 주석으로 충분한 결정에 design-doc.
- **허브 문서 재작성 후 상호 참조 stale**: `CLAUDE.md`, `docs/INDEX.md`, `docs/GOVERNANCE.md` 등을 재구조화할 때 inbound 참조 grep을 같은 변경에 안 함. 아래 "허브 문서 sweep" 참조.
- **채팅 단계 결정 코드 박제**: `C-2`, `B-4-①`, "Category D decision 14" 같은 것은 planning 채팅에서만 존재하는 라벨이다. 커밋된 문서에는 안정된 앵커(파일 경로 + 섹션 제목)로 번역해 적는다.

---

## 허브 문서 상호 참조 sweep

**허브 문서**는 다른 파일이 섹션 번호 또는 제목으로 인용하는 파일 — 보통 `CLAUDE.md`, `docs/INDEX.md`, `docs/GOVERNANCE.md`. 허브 문서의 구조를 바꾸면(renumber, retitle, 내용 이동) inbound 참조가 조용히 깨진다.

**절차** — 허브 문서 편집과 같은 변경 안에서:

1. 허브 문서 파일명을 repo 전체에 grep:
   ```
   Grep "<hubdoc>\.md"
   ```
2. 각 매치에 대해 인용된 섹션/앵커가 여전히 존재하는지 확인. 없으면 갱신 또는 재표현.
3. 수정 후, 이제 무효해진 패턴을 다시 grep해 0 매치를 확인.

**왜 "기억"이 아니라 grep인가**: 이 절차가 막는 실패는 정확히 "다 체크했다고 생각했는데 실은 빠진" 케이스다. Grep은 기계적이고, 싸고(1초), 멱등이며, 0 매치라는 검증 가능한 결과를 낸다.

**새 인용을 쓸 땐 안정된 앵커 선호**: 파일 경로 + 섹션 제목 (`docs/guides/unity-setup.md` "패키지 설치")이 깨지기 쉬운 숫자 서브섹션(`§2.1`)을 이긴다.

---

## 최소 실천 (급할 때)

전체 의례가 작은 변경에 무겁게 느껴지면:

- 변경이 국지적·되돌리기 쉬우면 design-doc 불요.
- exec-plan은 Goal + Checklist만 있어도 충분. 위 형식은 최대치이지 최소치가 아니다.
- STATUS.md 갱신은 `completed/`로 이동하는 순간 **타협 불가**.
