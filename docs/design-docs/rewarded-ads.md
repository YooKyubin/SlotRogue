# 보상형 광고

**Status**: accepted
**Last updated**: 2026-06-14

## Purpose

LevelPlay Rewarded 광고를 게임오버 부활과 보상 리롤, 추가 보상, 보상 2배에 연결한다. 광고 SDK 생명주기는 영속 Manager가 소유하고, 게임 상태 변경은 reward callback을 받은 뒤 기존 RunGame 흐름에서만 수행한다.

## Decisions

| # | 결정 | 요약 |
|---|------|------|
| A1 | [ADR-0013](../adr/0013-levelplay-rewarded-ads.md) | LevelPlay 9.4.1 Rewarded와 단일 `AdsManager`를 사용하며 키는 Inspector에서만 입력한다. |
| A2 | [ADR-0008](../adr/0008-ui-strict-mvvm-boundary.md) | View는 버튼 event와 interactable 렌더링만 담당하고 SceneRoot가 광고와 게임 command를 연결한다. |
| A3 | [ADR-0012](../adr/0012-leaderboard-nickname-only-profile.md) | 부활 가능 첫 패배는 제출을 보류하고 최종 패배가 확정될 때 자동 제출한다. |
| A4 | [ADR-0014](../adr/0014-defeat-revive-window-and-relic-contribution.md) | 첫 패배는 5초 부활 유예 화면을 거친 뒤 결과를 확정한다. |
| A5 | [ADR-0015](../adr/0015-remove-ads-rewarded-skip-iap.md) | `remove_ads` 구매자는 동일한 횟수 제한과 보상을 유지한 채 광고 시청만 건너뛴다. |

## Runtime flow

```text
BootScene / AdsManager.Awake
→ LevelPlay.OnInitSuccess / OnInitFailed 구독
→ LevelPlay.Init(appKey)
→ Init Success
→ new LevelPlayRewardedAd(rewardedAdUnitId)
→ LoadAd()

버튼 입력
→ AdsRemoveState.IsRemoved
├─ true → 기존 보상 command 즉시 실행
└─ false → CanShowRewarded(purpose) → ShowAd(placement)
   ├─ OnAdRewarded → 기존 보상 command 1회 실행
   ├─ OnAdDisplayFailed → 보상 없음 → LoadAd()
   └─ OnAdClosed → 보상 callback 유무와 무관하게 LoadAd()
```

## Purpose mapping

| Purpose | Placement | 보상 callback |
|---------|-----------|---------------|
| `Revive` | `revive` | 런당 1회 부활 상태 기록, 플레이어 HP 절반 복구, 현재 전투 재개 |
| `RewardReroll` | `reward_reroll` | 보상 화면당 1회 기존 후보 생성 로직을 사용하는 `RunRewardViewModel.ApplyRewardedReroll()` 호출 |
| `ExtraReward` | `reward_extra` | 엘리트/보스 보상 화면당 1회 후보 하나 추가 |
| `RewardDouble` | `reward_double` | 보상 화면당 1회 선택한 보상 효과를 두 번 적용 |

## Defeat flow

첫 패배에서 `GameFlowSession`은 진행 중인 런과 결과 데이터를 유지한다. `RunDefeatView`는 몬스터 초상화와 5초 카운트다운, 광고 부활 버튼을 표시한다. 제한 시간 안에 버튼을 누르면 카운트다운을 멈추고 광고 결과를 기다린다. 부활 광고 보상을 받으면 현재 `BattleSystem`을 유지해 몬스터 HP·실드·상태이상·다음 행동 순서를 보존하고, 플레이어 HP만 최대 HP의 절반으로 복구한 뒤 플레이어 턴부터 재개한다.

시간 초과, 광고 표시 실패, 보상 없는 종료에는 현재 snapshot을 최종 패배로 제출하고 결과 화면을 표시한다. 이미 부활한 런이 다시 패배하면 유예 없이 즉시 최종 패배로 확정한다. `GameFlowSession.HasRevivedThisRun`이 현재 런 결과의 부활 여부를 유지한다.

## UI boundary

- `RunRewardView`: 리롤·추가 보상·보상 2배 입력 event, interactable 상태, 구매 상태별 문구
- `RunDefeatView`: 몬스터 초상화·카운트다운·부활 버튼과 최종 결과를 단계별 렌더링
- `RunGameSceneRoot`: 화면별 1회 사용 상태, 광고 요청, 기존 ViewModel·전투 command 실행
- `AdsManager`: SDK 초기화·로드·표시·보상 callback

현재 `RunGame` 씬의 `DefeatView`는 빈 runtime fallback host다. 패배 View가 런타임 layout으로 부활 유예와 최종 결과 UI를 생성한다.

## Logging

Android 실기기 로그에서 다음 상태를 확인한다.

- `Init Success`
- `Init Failed`
- `Rewarded Loaded`
- `Rewarded Load Failed`
- `Rewarded Displayed`
- `Rewarded Display Failed`
- `Rewarded Rewarded`
- `Rewarded Closed`

## Open questions

| ID | 질문 | 비고 |
|----|------|------|
| Q1 | 부활 HP 비율 | 초기값은 최대 HP의 50%, 최소 1이다. 플레이테스트로 조정한다. |
| Q2 | 동의 관리와 개인정보 설정 | 출시 국가와 CMP 정책 확정 후 LevelPlay privacy API를 별도 결정한다. |

## Alternatives considered

- Interstitial과 Banner는 현재 보상 흐름에 필요하지 않아 구현하지 않는다.
- 구매 전에는 "광고 보고", 구매 후에는 "광고 없이" 문구를 사용해 지급 조건을 명확히 한다.
