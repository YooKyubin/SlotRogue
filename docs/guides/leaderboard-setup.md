# 리더보드 셋업 가이드

`Slot_Rogue_Leaderboard`를 로컬 Unity 프로젝트와 연결하고 런타임 제출·조회를 검증하는 절차다. 데이터 계약은 [ADR-0012](../adr/0012-leaderboard-nickname-only-profile.md), 런타임 구조는 [`leaderboard.md`](../design-docs/leaderboard.md)를 따른다.

## 프로젝트 연결

1. Unity Editor에서 `Edit > Project Settings > Services`를 연다.
2. Dashboard에서 `Slot_Rogue_Leaderboard`를 만든 Cloud Project를 선택해 프로젝트를 연결한다.
3. `ProjectSettings/ProjectSettings.asset`의 `cloudProjectId`가 비어 있지 않은지 확인한다.
4. 개발 환경을 별도로 쓴다면 Dashboard와 클라이언트 environment 이름을 맞춘다. 현재 코드는 기본 production environment를 사용한다.

현재 저장소의 `cloudProjectId`는 비어 있으므로 이 연결 전에는 인증과 리더보드 요청이 성공하지 않는다. Cloud Project ID는 임의로 입력하지 말고 Unity Services 연결 절차로 설정한다.

## Dashboard 설정

| 항목 | 값 |
|------|----|
| Leaderboard ID | `Slot_Rogue_Leaderboard` |
| Sort order | Descending |
| Update strategy | Keep Best |
| Score type | Numeric |
| Reset schedule | 매주 화요일 15:00 UTC |

화요일 15:00 UTC는 한국 시간 수요일 00:00 KST다. Dashboard의 Reset schedule은 이 시각에 매주 반복되도록 설정한다. 클라이언트의 마감 알림 기본값은 리셋 3시간 전이다.

최고 점수보다 낮은 score를 제출했을 때 기존 metadata도 유지되는지 Dashboard 설정과 패키지 동작을 플레이테스트로 확인한다.

## 런타임 확인

1. `BootScene`부터 Play한다.
2. 최초 진입 시 닫을 수 없는 `LOGIN` 패널이 열리는지 확인한다.
3. 닉네임을 입력하고 `LOGIN`을 눌러 Authentication Player Name과 로컬 공개 프로필이 저장되는지 확인한다.
4. 런에서 패배한 뒤 참가 확인 없이 score와 metadata가 자동 제출되는지 확인한다.
5. 패배 화면의 `RESTART`, `RANKING`, `HOME` 버튼이 각각 새 런, Top 10 패널, GameStart 이동을 수행하는지 확인한다.
6. Dashboard에서 해당 엔트리의 `Wave`, `RelicIds`, `SchemaVersion`을 확인한다.

## 실패 진단

| 증상 | 확인 |
|------|------|
| Project ID missing | Unity 프로젝트가 올바른 Cloud Project에 연결되었는지 확인 |
| Leaderboard not found | ID 대소문자와 environment 확인 |
| Unauthorized | 익명 인증 활성화와 Authentication 설정 확인 |
| 이름 변경 실패 | 공백 제거, UGS Player Name 정책 확인 |
| metadata가 비어 있음 | 조회 옵션의 `IncludeMetadata`와 기존 엔트리 재제출 확인 |
