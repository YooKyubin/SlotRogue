# 리더보드

**Status**: accepted
**Last updated**: 2026-06-13

## Purpose

UGS Leaderboards의 `Slot_Rogue_Leaderboard`에 모든 정상 런의 최고기록을 자동 제출하고, GameStart와 패배 화면에서 상위 플레이어의 이름·점수·도달 wave·보유 유물을 조회한다. 최초 게임 시작 전에는 닉네임을 지정한 공개 프로필이 필요하다.

## Decisions

| # | 결정 | 요약 |
|---|------|------|
| L1 | [ADR-0012](../adr/0012-leaderboard-nickname-only-profile.md) | 프로필은 닉네임만 저장하며 패배 기록은 자동 제출한다. |
| L2 | [ADR-0008](../adr/0008-ui-strict-mvvm-boundary.md) | GameStart View는 입력과 렌더링만 담당하고 ViewModel이 조회 상태를 관리한다. |

## Runtime flow

```text
BootController
→ UnityServices.InitializeAsync()
→ AuthenticationService.SignInAnonymouslyAsync()

GameStart / 최초 프로필 없음
→ 닫을 수 없는 LOGIN 패널
→ 닉네임 입력
→ AuthenticationService.UpdatePlayerNameAsync()
→ 로컬 공개 프로필 저장

RunBattleResultRecorder / Defeat
→ 현재 런 snapshot 생성
→ LeaderboardsService.AddPlayerScoreAsync()
→ score + metadata 제출
→ RESTART / RANKING / HOME 선택

GameStart 또는 RunGame / Leaderboard
→ LeaderboardsService.GetScoresAsync(IncludeMetadata: true)
→ metadata parse
→ Top 10 ViewState 렌더링
```

Boot 초기화는 fire-and-forget으로 시작한다. GameStart는 로컬 프로필이 없으면 즉시 `LOGIN` 패널을 열며 저장 성공 전에는 Start를 허용하지 않는다. 실패한 UGS 초기화는 다음 프로필 저장·조회·제출에서 재시도한다.

## Data contract

| 필드 | 출처 | 의미 |
|------|------|------|
| `score` | `GameFlowSession.Victories` | 클리어한 wave 수. 순위 정렬 기준. |
| `playerName` | UGS Authentication | 사용자가 지정한 플레이어 표시 이름. UGS discriminator 포함 가능. |
| `Wave` | `CurrentBattleNumber` | 패배한 전투를 포함한 도달 wave. |
| `RelicIds` | `OwnedRelics[].Id` | 보유 유물 ID 목록. 중복 획득은 그대로 유지. |
| `SchemaVersion` | 상수 `2` | metadata 마이그레이션 기준. v1의 `CountryCode`는 읽을 때 무시한다. |

metadata는 공개 리더보드에서 다른 플레이어가 읽을 수 있는 정보만 포함한다. 이메일, 플랫폼 계정 ID, 기기 식별자 같은 개인정보는 저장하지 않는다.

## UI boundary

`GameStartSceneRoot`와 `RunGameSceneRoot`가 각각 `LeaderboardViewModel`과 `LeaderboardView`를 조립한다. View는 각 씬 Canvas 아래에 런타임 fallback 레이아웃을 만들며 RunGame에서는 패배 화면의 `RANKING` 버튼으로만 연다.

- `LeaderboardView`: 열기/닫기/새로고침/프로필 저장 입력 event, ViewState 렌더링
- `LeaderboardViewModel`: 필수 프로필·loading/error/entry 상태와 비동기 요청
- `LeaderboardPlayerProfileStore`: 명시적 로그인 완료 여부와 닉네임 저장
- `SlotRogueLeaderboardService`: UGS 초기화·인증·제출·조회·프로필 저장
- `RunBattleResultRecorder`: 패배 순간 런 snapshot 캡처와 제출 시작
- `RunDefeatViewModel`: 재시작·리더보드·홈 선택 event

## Dashboard contract

실제 서비스 동작 전 [`leaderboard-setup.md`](../guides/leaderboard-setup.md)를 확인한다.

- Leaderboard ID: `Slot_Rogue_Leaderboard`
- Sort order: descending
- Update strategy: keep best score
- Unity 프로젝트의 Cloud Project ID와 Dashboard 프로젝트 일치

## Open questions

| ID | 질문 | 비고 |
|----|------|------|
| Q1 | 익명 계정의 플랫폼 계정 연결 | 기기 변경·재설치 시 기록 유지에 필요. |
| Q2 | 오프라인 최고기록 재제출 | 세이브 시스템과 함께 로컬 pending record 도입 검토. |
| Q4 | 유물 아이콘 표시 | 현재는 ID 텍스트. Addressable icon row는 후속 UI polish 범위. |

## Alternatives considered

- 리더보드 상세를 한 씬에만 두지 않고 GameStart와 패배 화면 양쪽에서 같은 View/ViewModel 계약을 사용한다.
- 씬 YAML을 직접 편집하는 방식은 Prefab fileID와 현재 UI 작업 충돌 위험이 있어 런타임 fallback으로 시작한다.
