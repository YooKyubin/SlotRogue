# LevelPlay Rewarded 광고

**Status**: active
**Started**: 2026-06-13
**Owner**: _(광고 연동)_
**Related design-docs**: [`../../design-docs/rewarded-ads.md`](../../design-docs/rewarded-ads.md), [`../../adr/0013-levelplay-rewarded-ads.md`](../../adr/0013-levelplay-rewarded-ads.md)

## Goal

LevelPlay Rewarded 광고를 BootScene 영속 Manager로 초기화하고, 게임오버 부활과 보상 리롤을 광고 reward callback 뒤에만 한 번씩 실행한다.

## Checklist

- [x] 프로젝트 UI 흐름과 LevelPlay 9.4.1 API 확인
- [x] 광고 SDK·키 주입·보상 지급 결정을 ADR과 design-doc에 기록
- [x] `AdsManager`와 `RewardedAdPurpose` 구현 및 BootScene 배치
- [x] 기존 리롤 버튼과 선택 완료 잠금 연결
- [x] 부활 버튼 연결 계약과 런당 1회 부활 흐름 구현
- [x] 최종 패배 제출과 런 부활 상태 반영
- [x] 전체 solution 컴파일 검증 — 경고 0개, 오류 0개
- [x] 패배 몬스터 초상화·5초 카운트다운·부활 버튼 단계 구현
- [x] 광고 실패·보상 없는 종료 후 최종 결과 전환
- [x] 런 전체 유물별 발동·피해·방어·회복 기여도 집계
- [x] 최종 결과 화면에 보유 유물 전체 기여도 표시 — 이후 심볼 통계로 대체
- [x] 최종 결과 화면을 심볼별 족보 등장 횟수·기본 공격력·유물 공격력 통계로 교체 — Codex
- [x] 확장 구현 후 전체 solution 재검증 — 경고 0개, 오류 0개
- [x] AdsManager root 전환과 RunGame DefeatView 씬 참조 경고 제거
- [x] RunGame 씬의 리롤 버튼 null override 제거 및 광고 상태 문구/회귀 테스트 추가
- [x] 부활 시 현재 몬스터 상태를 보존하고 플레이어만 최대 HP의 절반으로 복구
- [ ] Unity Test Runner와 Android 실기기 로그 검증

## Notes

- `RunRewardView`에는 `Reroll Button`이 프리팹에 연결되어 있다.
- `RunGame`의 `DefeatView`는 빈 host다. 후속 요구에 따라 runtime layout에 부활 유예와 최종 결과 UI를 구성한다.
- 실제 App Key와 Rewarded Ad Unit ID는 저장소에 기록하지 않는다.
- `dotnet build SlotRogue.slnx`에 새 Ads 소스와 설치된 `Unity.LevelPlay.dll`을 포함해 검증했다.
- 유물 피해 기여는 추가 공격력에 해당 턴 공격 횟수를 곱한 명목 수치이며, 방어·회복도 계산된 추가량을 기록한다.
- 2026-06-19: [ADR-0018](../../adr/0018-defeat-result-symbol-pattern-stats.md)에 따라 최종 결과 화면의 기본 표시를 유물 기여도에서 심볼별 족보 등장 횟수, 기본 공격력, 유물 공격력으로 교체했다. 유물 기여도 누적 코드는 내부 데이터로 유지한다.
- `dotnet test`는 Unity Test Framework 테스트를 실행하지 않아 Unity Test Runner 수동 검증이 남아 있다.
- 2026-06-13: `AdsManager`가 `00_Common` 자식이어도 `Awake()`에서 root로 분리한 뒤 `DontDestroyOnLoad`를 적용한다. `RunGame`의 빈 `DefeatView` host에는 `RunDefeatView`를 직렬화해 SceneRoot가 fallback 경고 없이 기존 host를 사용한다.
- 2026-06-14: 부활 시 기존 `BattleSystem`을 재개해 몬스터 상태와 다음 행동을 보존한다. `dotnet build SlotRogue.slnx`는 경고 0개, 오류 0개로 통과했다.
- 2026-06-19: 심볼별 결과 통계를 기본 공격력과 유물 공격력 분리 표시까지 확장한 뒤 `dotnet build SlotRogue.slnx --no-restore`는 경고 0개, 오류 0개로 통과했다. `dotnet test SlotRogue.slnx --no-build`는 종료 코드 0이지만 Unity Test Runner 테스트 개수 출력은 없어 수동 검증은 남아 있다.

## Completion

- **Finished**:
- **Outcome**:
- **Follow-ups**:
