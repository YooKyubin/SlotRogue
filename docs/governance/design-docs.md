# design-doc 규칙

design-doc은 시스템이 *어떻게* 맞물려 돌아가는지 설명하는 narrative 문서다.

---

## 작성 시점

새 시스템의 자명하지 않은 코드를 쓰기 전, 또는 큰 결정이 바뀔 때. 예:

- 새 서브시스템 (슬롯 코어, 메타 진행, 경제, 세이브, UI 흐름, 데이터 파이프라인)
- 큰 아키텍처 변경 (예: PlayerPrefs → 파일 기반 세이브 마이그레이션)
- 횡단 정책 (testing-policy, build-and-release)

**SlotRogue 현재 상태**: 기획 문서가 확정되기 전이라 design-doc은 제한적으로만 작성한다. [`../design-docs/INDEX.md`](../design-docs/INDEX.md)에 현재 후보 목록을 둔다.

---

## 형식

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

이 외 섹션은 자유다. 다이어그램, 코드 스케치, 레퍼런스 등을 필요할 때 추가한다.

---

## ADR과의 관계

- 결정 본문은 ADR에 둔다.
- design-doc은 ADR을 인용하되 Context/Alternatives/Consequences를 재서술하지 않는다.
- design-doc 내부 결정은 ADR 인용이어야 한다.
- 본문에는 navigation용 짧은 요약만 허용한다.

---

## 갱신

- 작은 명확화: in-place 편집, `Last updated` bump.
- 큰 접근 변경: 옛 doc을 `superseded` 마크, 새 doc으로 링크.
- 조용한 재작성 금지.
