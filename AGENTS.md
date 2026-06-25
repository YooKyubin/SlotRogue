# SlotRogue — Project Guidelines

> Cursor / Claude가 매 세션 자동 로드하는 가이드. 절대 규칙 + 핵심 결정 포인터 + 팀 워크플로 요약.

---

## 0. 언어 규칙 (CRITICAL — 절대 위반 금지)

- **내부 추론은 영어, 사용자 응답은 항상 한국어.** 예외 없음.
- 문서 언어: **전부 한국어** (`AGENTS.md`, `DOCS-STRUCTURE.md`, `docs/`, `references/` 모두).
- 코드 / 주석 / 식별자 / 파일명: **영어**.
- 기술 용어는 원문 유지 (Addressables, ScriptableObject, asmdef, UniTask, DOTween 등).

---

## 1. 프로젝트 한 줄 요약

- **SlotRogue** — Unity 6000.3.10f1 기반 슬롯 + 로그라이크 모바일 게임. **1달 스프린트**, 프로그래머 3명 (각자 기획·아트 겸업).
- C# 네임스페이스: `SlotRogue.*` (예: `SlotRogue.Core`, `SlotRogue.UI`, `SlotRogue.Data`)
- 핵심 패키지: Addressables 2.9.1, UniTask, DOTween.

---

## 2. 핵심 결정 요약

각 영역의 결정은 ADR로 기록. 본 표는 인덱스 — 산문 결정 본문은 두지 않는다.

| 영역 | ADR |
|------|-----|
| 전투 연출 (Replay / CombatEvent 타임라인) | [ADR-0003](docs/adr/0003-combat-presentation-replay.md) (proposed) |
| 슬롯 RNG / 페이아웃 모델 | _미정_ |
| 세이브 포맷 (JSON / Binary / PlayerPrefs 한계) | _미정_ |
| UGS 리더보드 최고기록 / metadata 모델 | [ADR-0012](docs/adr/0012-leaderboard-nickname-only-profile.md) (accepted) |
| Addressables 로컬 그룹 / 빌드 기준선 | [ADR-0007](docs/adr/0007-addressables-local-runtime-assets.md), [ADR-0009](docs/adr/0009-relic-icon-addressable-keys.md) (accepted) |
| 런타임 자산 로드 경계 | [ADR-0006](docs/adr/0006-runtime-asset-loading-boundary.md) (accepted) |
| 광고 / IAP SDK 선택 | [ADR-0013](docs/adr/0013-levelplay-rewarded-ads.md), [ADR-0015](docs/adr/0015-remove-ads-rewarded-skip-iap.md) (accepted) |
| 로컬 알림 / 주간 랭킹 리셋 | [ADR-0016](docs/adr/0016-local-notifications-weekly-ranking-reset.md) (accepted) |
| 타겟 해상도 / Safe Area 처리 | _미정_ |
| 브랜치 / PR 워크플로 | _미정 (사용해본 후 박제)_ |
| 유물 런타임 모델 (v23 RelicCatalog 단일화) | [ADR-0005](docs/adr/0005-relic-v23-runtime-model.md) (accepted) |
| 상태 효과 수치 필드 (`Amount` / `Magnitude` / `StackCount`) | [ADR-0021](docs/adr/0021-status-effect-numeric-field-semantics.md) (accepted) |
| 리더보드 프로필 / 자동 제출 / 패배 후 선택 | [ADR-0012](docs/adr/0012-leaderboard-nickname-only-profile.md) (accepted) |
| 패배 부활 유예 / 결과 통계 | [ADR-0014](docs/adr/0014-defeat-revive-window-and-relic-contribution.md), [ADR-0018](docs/adr/0018-defeat-result-symbol-pattern-stats.md) (accepted) |

전체 결정 목록: [`docs/adr/INDEX.md`](docs/adr/INDEX.md). 시스템별 narrative: `docs/design-docs/` (기획 문서 확정 후 추가). 빌드/툴체인 가이드: [`docs/guides/unity-setup.md`](docs/guides/unity-setup.md).

---

## 3. Documentation Governance (필수)

**모든 작업은 paper-trail을 남긴다.**

1. **구현 전**: [`docs/INDEX.md`](docs/INDEX.md) → 관련 `design-docs/` 및 인용된 `docs/adr/` 확인. 결정이 미정이면 ADR + (필요 시) design-doc 먼저.
2. **구현 중**: `docs/exec-plans/active/*.md` 체크리스트를 즉시 갱신.
3. **완료 시**: exec-plan을 `git mv`로 `active/` → `completed/`. **같은 커밋에서 [`docs/STATUS.md`](docs/STATUS.md) 갱신**.
4. **허브 문서 변경 시 sweep**: `AGENTS.md`, `docs/INDEX.md`, `docs/GOVERNANCE.md` 등 다른 문서가 인용하는 허브 문서의 섹션 구조(번호·제목·이동)를 바꿀 땐, **같은 변경 안에서** inbound 참조를 grep으로 전수 검사하고 갱신.

### 팀 규칙 요약 (A~G)

A. **한 plan에 한 오너.** 협업자는 체크리스트 항목별 assignee로만 표시.
B. **plan 단위는 기능**, 사람 단위가 아님. (`feature-spin-core.md`)
C. **plan 갱신은 작업 커밋/PR과 같은 단위**로 함께 push. 따로 커밋하지 않는다.
D. **plan 수명 3일 ~ 2주.** 길어지면 phase 분할.
E. **체크리스트 항목은 한 줄 유지** — 머지 충돌 회피.
F. **STATUS.md는 active plan 미러** + plan 이동 시 같은 커밋에 갱신.
G. **1~2 PR로 끝나는 작업 / 스파이크 / 단순 리팩터링은 plan 만들지 않는다.**

상세 규칙: [`docs/GOVERNANCE.md`](docs/GOVERNANCE.md).

---

## 4. References / 작업 외 영역 제외 규칙

1. 외부 자료는 항상 [`references/INDEX.md`](references/INDEX.md)를 먼저 읽기.
2. 내부 문서는 항상 [`docs/INDEX.md`](docs/INDEX.md)를 먼저 읽기.
3. **항상 제외 (문서로 취급 금지, 검색에서 배제)**:
   - Unity 자동 생성: `Library/`, `Temp/`, `Logs/`, `obj/`, `Build/`, `Builds/`, `UserSettings/`, `MemoryCaptures/`
   - IDE: `.vs/`, `.idea/`, `.vscode/`, `*.csproj`, `*.sln`
   - 패키지 캐시: `Packages/packages-lock.json` 외의 자동 생성물
   - 외부/벤더: `Assets/Plugins/` 하위 third-party
   - 바이너리/자산: `*.unitypackage`, `*.apk`, `*.aab`, 큰 이미지/오디오/모델

---

## 5. 테스팅 정책 (1달 스프린트용 축소판)

- **결정론적 로직** (슬롯 RNG, 페이아웃 계산, 보상 풀 추첨): EditMode 단위 테스트 **필수**.
- **메타 진행 / 세이브 / 마이그레이션**: PlayMode 통합 테스트 **권장**.
- **UI / 연출 / 사운드**: 자동 테스트 없이 수동 플레이테스트 체크리스트.
- **빌드 검증**: 각 주차 끝에 실기기(Android) 1회 빌드 + 한 런 통과 확인.

상세는 추후 `docs/design-docs/testing-policy.md`로 분리.

---

## 6. 절대 코딩 규칙 (Unity / C#)

- **`.meta` 파일은 항상 같은 커밋에 포함.** 누락 시 다른 팀원이 깨진다.
- **asmdef 경계 존중**: `SlotRogue.UI` → `SlotRogue.Core` 일방향 등 의존 방향은 단방향 유지. 역참조 금지.
- **Asset Serialization: Force Text** 강제 (`Edit > Project Settings > Editor`). 머지 가능성을 위해.
- **비동기는 UniTask**. `async void` 금지. 이벤트 핸들러는 `UniTaskVoid`, 일반은 `UniTask` / `UniTask<T>`.
- **트윈은 DOTween**. `OnDisable` / `OnDestroy`에서 `transform.DOKill(true)` 또는 핸들 보관 후 명시적 Kill.
- **`MonoBehaviour` 필드**: `[SerializeField] private` 우선. `public` 필드 금지 (불변 상수만 예외).
- **Addressables 키는 상수 / SO**로 관리. 문자열 리터럴 산재 금지.
- **Unity null 트랩**: destroyed 오브젝트는 `?.` 분기를 통과한다. 진짜로 살았는지 확인하려면 `obj != null` (Unity 오버로드된 `==`) 사용.
- **로깅**: `Debug.Log` 직접 호출은 임시. 릴리즈 전 정리 또는 wrapper로 대체.
- **예외**: 게임 로직에서 예외로 흐름 제어 금지. 데이터 검증은 반환값/assert.

코딩 스타일 상세: [`docs/guides/coding-style.md`](docs/guides/coding-style.md).
포매팅 권위: [`.editorconfig`](.editorconfig) — IDE 자동 포맷 / `dotnet format SlotRogue.sln`.
Unity 셋업 (버전 / 패키지 / 프로젝트 설정): [`docs/guides/unity-setup.md`](docs/guides/unity-setup.md).

---

## 7. Tool / Workflow 사용 (에이전트 지침)

- **TodoWrite**: 다단계 작업 시 적극 사용. 시작 시 `in_progress`, 끝나면 즉시 `completed`. 일괄 처리 금지.
- **Glob / Grep / Read**: 파일·심볼 탐색의 1차 도구. `find`/`rg`/`cat` 직접 호출 금지.
- **Edit > Write**: 기존 파일은 Edit (StrReplace), 신규 또는 전면 재작성만 Write.
- **새 시스템 구현 순서**: 결정 ADR(들) → design-doc (narrative) → exec-plan → 코드.
- **브랜치 / PR 전략은 미정** (`§2` 참조). 사용해보고 결정되면 ADR-XXXX로 박제. 그 전까지는 main 직접 커밋도 허용하되 exec-plan 갱신은 작업 커밋과 같이.
- **커밋 메시지**: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:` 등 type으로 시작하고 한 줄 요약 뒤 빈 줄 + 불렛 리스트로 세부 내용을 쓴다. PowerShell에서 커밋할 때는 **반드시** `git commit -m @" ... "@` here-string을 사용해 전체 메시지가 하나로 들어가게 한다. 상세는 [`docs/governance/commit-messages.md`](docs/governance/commit-messages.md) 참조.

---

## 8. 빠른 참조

| 매 세션 진입점 | 경로 |
|----------------|------|
| 본 가이드 | `AGENTS.md` |
| 문서 진입점 | [`docs/INDEX.md`](docs/INDEX.md) |
| 현재 상태 | [`docs/STATUS.md`](docs/STATUS.md) |
| 운용 규칙 상세 | [`docs/GOVERNANCE.md`](docs/GOVERNANCE.md) |
| 결정 인덱스 | [`docs/adr/INDEX.md`](docs/adr/INDEX.md) |
| 외부 자료 | [`references/INDEX.md`](references/INDEX.md) |
| 포매팅 권위 | [`.editorconfig`](.editorconfig) |
| Blame skip 목록 | [`.git-blame-ignore-revs`](.git-blame-ignore-revs) |

| 작업별 디렉터리 | 경로 |
|-----------------|------|
| 결정 기록 (ADR) | `docs/adr/` |
| 시스템 설계 narrative | `docs/design-docs/` |
| 진행 중 작업 | `docs/exec-plans/active/` |
| 완료 작업 | `docs/exec-plans/completed/` |
| 빌드 / 가이드 | `docs/guides/` |
| Unity 코드 | `Assets/_Project/Scripts/` _(권장 — 팀 합의 필요)_ |
