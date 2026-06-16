# ADR-0013: 보상형 광고는 LevelPlay 단일 AdsManager로 제공한다

**Status**: accepted
**Date**: 2026-06-13
**Supersedes**: none
**Superseded by**: none
**Related design-docs**: [`rewarded-ads.md`](../design-docs/rewarded-ads.md), [`game-flow.md`](../design-docs/game-flow.md)

---

## Context

게임오버 부활과 보상 리롤은 광고 시청 완료를 조건으로 게임 상태를 변경해야 한다. 프로젝트에는 Ads Mediation `com.unity.services.levelplay` 9.4.1과 Mobile Dependency Resolver가 설치되어 있으며, App Key와 Rewarded Ad Unit ID는 배포 환경별 비밀값이라 저장소에 남기면 안 된다.

광고 콜백을 각 View가 직접 처리하면 SDK 생명주기와 게임 규칙이 화면에 섞이고, 씬 전환 중 콜백이나 중복 클릭으로 보상이 여러 번 지급될 수 있다.

## Decision

- Rewarded 광고는 `BootScene`에 배치한 단일 `AdsManager`가 초기화·로드·표시·콜백을 소유한다.
- `AdsManager`는 Singleton과 `DontDestroyOnLoad`를 사용하고 App Key와 Rewarded Ad Unit ID를 `[SerializeField] private` 필드로만 받는다.
- 광고 목적은 `RewardedAdPurpose.Revive`, `RewardedAdPurpose.RewardReroll`로 구분하고 placement는 각각 `revive`, `reward_reroll`을 사용한다.
- 게임 보상 callback은 LevelPlay `OnAdRewarded`에서 한 번만 실행한다. 로드 실패, 표시 실패, 준비되지 않음, 보상 없이 닫힘에는 실행하지 않는다.
- View는 SDK를 참조하지 않고 버튼 입력 event와 interactable 상태만 담당한다. `RunGameSceneRoot`가 광고 요청과 기존 ViewModel·전투 흐름을 연결한다.
- 첫 패배에 부활 기회가 남아 있으면 리더보드 제출을 잠시 보류한다. 부활을 포기하거나 부활 후 다시 패배했을 때 최종 패배 snapshot을 제출한다.

## Alternatives considered

- **각 화면에서 LevelPlay 직접 호출** — 씬별 중복 초기화와 보상 중복 지급 위험이 있고 strict MVVM 경계를 깨뜨려 제외했다.
- **App Key와 Ad Unit ID를 상수로 저장** — 실제 키가 Git 기록에 남을 수 있어 제외했다.
- **광고가 닫힐 때 보상 지급** — 중간 종료도 닫힘 callback을 발생시킬 수 있어 제외했다.
- **첫 패배를 즉시 리더보드에 제출한 뒤 부활** — 부활한 런이 패배 기록으로 먼저 확정되므로 제외했다.

## Consequences

- BootScene의 `AdsManager` Inspector에 실제 키를 로컬로 입력해야 광고가 초기화된다.
- Rewarded 준비 상태에 따라 부활·리롤 버튼이 비활성화된다.
- Android 실기기에서 네트워크와 mediation adapter를 포함한 최종 검증이 필요하다.
- Interstitial, Banner, IAP는 이 결정과 구현 범위에 포함하지 않는다.
