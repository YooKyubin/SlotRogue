# 전체 UI strict MVVM

**Status**: active  
**Started**: 2026-06-11  
**Owner**: _(슬롯 담당)_  
**Related design-docs**: [`../../design-docs/game-flow.md`](../../design-docs/game-flow.md), [`../../adr/0008-ui-strict-mvvm-boundary.md`](../../adr/0008-ui-strict-mvvm-boundary.md)

## Goal

`GameStart`와 `RunGame`의 모든 production 화면을 View 입력 event + 순수 ViewModel 화면 상태 + SceneRoot wiring 구조로 통일한다. 별도 `RunBattle` 씬이 존재하지 않는 현재 구조를 코드와 문서에 반영하고, 과거 명칭과 직접 의존을 제거한다.

## Checklist

- [x] 현재 씬이 `BootScene`, `GameStart`, `RunGame` 세 개뿐인지 확인
- [x] strict MVVM 경계를 ADR-0008로 확정
- [x] `GameStart`를 View + ViewModel + SceneRoot로 분리
- [x] 시작 유물 View의 ViewModel·도메인 직접 참조 제거
- [x] 보상 View의 ViewModel·도메인 직접 참조와 중복 추첨 제거
- [x] 공통 HUD View의 ViewModel 직접 참조 제거
- [x] BattleView의 Flow Controller 직접 참조 제거
- [x] production View에 남은 ViewModel·Controller·게임 상태 직접 의존 전수 검사
- [ ] 과거 `RunBattle` 씬 명칭을 현재 `RunGame Battle 화면` 역할명으로 정리
- [x] ViewModel 및 wiring 테스트 추가
- [x] `dotnet build SlotRogue.slnx --no-restore` 검증
- [x] 승리 시 종료 버튼 없이 Reward View 자동 전환
- [x] 패배 시 Defeat View 자동 전환 및 새 런 command 연결
- [x] Battle View의 legacy 보상/재시작 버튼 비활성화
- [x] `GameFlowOptionView` 카드 제목·설명을 `TMP_Text`로 전환
- [ ] Unity EditMode 테스트 및 RunGame 수동 플레이테스트

## Notes

- 전투 화면 자산은 `Assets/_Project/Prefabs/UI/GameFlow/RunGame/10_BattleView.prefab`이며 별도 `RunBattle.unity`는 없다.
- 사용자 작업 중인 prefab/scene 배치를 유지하고 MonoBehaviour GUID를 보존한다.
- 전투 씬 조립은 `BattleSceneCompositionRoot`, 턴 순서는 순수 C# `BattleFlowController`, RunGame 화면 전환은 `RunGameSceneRoot`가 담당한다. 씬 저장 후 참조가 끊긴 기존 `RunBattleCompositionRoot`와 `RunGameCompositionRoot` 호환 컴포넌트는 삭제했다.
- production `*View.cs` 정적 검사에서 ViewModel 보관, Flow Controller 직접 호출, `GameFlowSession`/전투 도메인 직접 참조가 남지 않았음을 확인했다.
- `dotnet build SlotRogue.slnx --no-restore`는 경고 0개, 오류 0개로 통과했다. Unity Editor instance가 없어 EditMode 테스트와 수동 플레이테스트는 대기 중이다.
- 전투 종료 시 `BattleFlowController`가 연출 완료 직후 결과 event를 발행한다. 승리는 `Reward`, 패배는 `Defeat` 상태로 자동 전환하며 전투 화면의 `Claim Reward`/`Return To Start` 버튼은 항상 숨긴다.
- 패배 화면은 기존 빈 `GameOverView` 자리를 `RunDefeatView`가 사용하며, 화면 상태와 새 런 command는 순수 `RunDefeatViewModel`을 통해 연결한다.
- 시작 유물/보상 카드 prefab은 기존 `TextMeshProUGUI`와 Galmuri11 SDF 자산을 유지한다. `GameFlowOptionView`는 비어 있는 직렬화 필드를 카드 자식 이름으로 자동 복구한다.

## Completion

- **Finished**:
- **Outcome**:
- **Follow-ups**:
