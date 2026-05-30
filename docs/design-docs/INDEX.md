# design-docs 인덱스

시스템 narrative의 진입점. ADR 인용을 통해 결정을 참조하고, *어떻게* 시스템이 맞물려 돌아가는지 설명한다.

> 형식·작성 규칙: [`../GOVERNANCE.md`](../GOVERNANCE.md) "design-doc 규칙" 섹션.

---

## 현재 상태

| 파일 | Status | 범위 |
|------|--------|------|
| [`slot-core.md`](./slot-core.md) | draft | 5 x 3 슬롯 MVP, lightweight MVVM, 패턴 판정, 전투 요청 DTO |
| [`combat-core.md`](./combat-core.md) | draft | 1스핀=1턴, Effect 목록 파이프라인, Participant, shield, CombatEvent |

---

## 작성 예정 후보

기획 확정 시 다음을 우선 작성한다. *지금 미리 빈 파일을 만들지 않는다* — 결정될 때 작성한다.

| 파일 | 범위 |
|------|------|
| `architecture-overview.md` | 씬/시스템 의존성, asmdef 경계, 이벤트 흐름 |
| `roguelike-meta.md` | 런 구조, 노드 맵, 보상 풀, 시드 전략 |
| `economy.md` | 통화, 상점, 드롭율, 인플레이션 곡선 |
| `save-system.md` | 저장 키, 마이그레이션, 클라우드 세이브 여부 |
| `ui-flow.md` | 화면 스택, 모달 규칙, 입력 lock |
| `data-pipeline.md` | ScriptableObject vs JSON vs CSV, 밸런스 데이터 편집 워크플로 |
| `build-and-release.md` | Android/iOS 빌드 설정, 키스토어, 버전 코드 정책 |
| `testing-policy.md` | EditMode/PlayMode 테스트 범위, 플레이테스트 로그 양식 |

슬롯 연동 시 `slot-core.md` Q3과 `combat-core.md` Q1(`SlotCombatRequest` → `CombatEffect[]`)을 맞춘다.

---

## 헤더 형식

각 design-doc은 다음으로 시작:

```markdown
# <시스템명>

**Status**: draft | accepted | superseded
**Last updated**: YYYY-MM-DD

## Purpose
한 문단: 이 시스템이 무엇을 하고 왜 존재하는가.

## Decisions
ADR 인용 + 한 줄 navigation 요약. 결정 본문 재서술 금지.

## Open questions
나중에 결정으로 미룬 것들.

## Alternatives considered
거절한 것과 이유.
```

이 외 섹션(다이어그램, 코드 스케치, 레퍼런스)은 자유 서식.

---

## 밸런스 데이터 위치 기본 방침

design-doc은 **의도**(왜 이 곡선인가, 어떤 플레이어 경험을 노리나)만 설명한다.
**실제 수치**(심볼별 출현 확률, 보상량 등)는 `Assets/_Project/Data/`의 ScriptableObject에 둔다.

이렇게 분리하는 이유:
- 수치를 design-doc에 박제하면 변경 시 doc이 빠르게 stale.
- ScriptableObject는 Unity 인스펙터에서 비-프로그래머도 편집 가능.
- 코드와 데이터의 머지 충돌 분리.

세부 워크플로는 `data-pipeline.md` 작성 시 확정.
