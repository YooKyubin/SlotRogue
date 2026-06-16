# 로컬 알림과 주간 랭킹

**Status**: accepted
**Last updated**: 2026-06-14

## Purpose

앱 이탈 24시간 뒤 복귀 알림을 보내고, 한국 시간 기준 주간 랭킹 마감과 리셋 시각을 모든 기기에서 동일하게 계산한다.

## Decisions

- [ADR-0016](../adr/0016-local-notifications-weekly-ranking-reset.md): 시간 기반 알림은 로컬 예약하며 랭킹은 UGS Dashboard에서 한국 수요일에 리셋한다.
- [ADR-0012](../adr/0012-leaderboard-nickname-only-profile.md): 기존 닉네임 프로필과 자동 점수 제출 흐름을 유지한다.

## 시간 계약

| 항목 | 기본값 |
|------|--------|
| 랭킹 리셋 | 매주 수요일 00:00 KST |
| 마감 알림 | 리셋 3시간 전, 화요일 21:00 KST |
| 복귀 알림 | 마지막 앱 이탈 24시간 뒤 |

`WeeklyRankingSchedule`은 UTC를 입력받아 다음 KST 리셋과 마감 시각을 UTC로 반환한다. 기기 현지 시간대는 알림 API에 전달할 때만 사용한다.

## 런타임 흐름

```text
BootScene
  -> LocalNotificationController 초기화
  -> Android 채널 등록 / 플랫폼 알림 권한 요청
  -> 다음 주간 마감 알림 예약

앱 resume
  -> 대기 중인 24시간 복귀 알림 취소
  -> 다음 주간 마감 알림 재계산

앱 pause 또는 quit
  -> 마지막 플레이 UTC 저장
  -> 24시간 복귀 알림 예약
  -> 다음 주간 마감 알림 예약
```

## UGS Dashboard 계약

- Leaderboard ID: `Slot_Rogue_Leaderboard`
- Reset schedule: 매주 화요일 15:00 UTC
- 한국 기준: 매주 수요일 00:00 KST
- Sort order와 `Keep Best` 전략은 현재 설정을 유지한다.

Reset schedule은 서비스 권한이 필요한 운영 설정이므로 저장소 코드가 대신 생성하지 않는다.

## 수동 검증

1. Android 13 이상 또는 iOS에서 최초 실행 시 알림 권한 요청을 확인한다.
2. Inspector의 테스트용 짧은 지연값으로 앱을 백그라운드 전환한 뒤 복귀 알림을 확인한다.
3. 앱을 알림 전에 다시 열면 기존 복귀 알림이 취소되는지 확인한다.
4. `WeeklyRankingSchedule` 테스트에서 수요일 경계 전후의 다음 리셋 시각을 확인한다.
5. UGS Dashboard에서 화요일 15:00 UTC 주간 Reset schedule을 설정한다.
6. 실제 리셋 이후 이전 점수가 현재 랭킹에서 제외되고 새 제출이 1위부터 시작하는지 확인한다.
