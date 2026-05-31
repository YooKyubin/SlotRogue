# 게임 플로우 루프

**Status**: active  
**Started**: 2026-05-31  
**Owner**: _(슬롯 담당)_  
**Contributors**: _(전투 담당: 코드 수정 없음)_  
**Related design-docs**: [`game-flow.md`](../../design-docs/game-flow.md)

## Goal

전투 코드를 수정하지 않고 `게임 시작 → 시작 유물 선택 → 맵 → 전투 → 보상 → 맵`으로 반복되는 playable loop를 만든다. `RunBattle`은 기존 슬롯 결과를 기존 전투 API에 연결한다.

## Checklist

- [x] `AGENTS.md`, 문서 인덱스, 슬롯/전투 design-doc 확인
- [x] 게임 플로우 ADR/design-doc 작성
- [x] 시작/유물/맵/전투/보상 씬 추가
- [x] `SlotRogue.UI.GameFlow` 런 상태와 씬 Controller 구현
- [x] 시작 유물/보상 후처리 구현 및 테스트 추가
- [x] 빌드 설정에 플로우 씬 등록
- [x] 컴파일 또는 Unity refresh 검증
- [x] 추후 sprite 교체용 기본 이미지 슬롯 UI 배치
- [x] 맵 자동 진입 제거, 현재 노드와 다음 노드 선택 UI 추가
- [x] 전체 맵 그래프, 연결선, 현재 위치, 선택 가능 노드 표시
- [x] 전투 씬 세로형 HUD 재배치, 몬스터 이미지 슬롯과 고정 5 x 3 슬롯 보드 표시
- [x] 런타임 bootstrap 제거, View 프리팹 + Controller 방식으로 씬 갱신
- [x] 전투 스핀 결과에서 기본 공격과 패턴 성공 강조 표시
- [ ] Unity Editor에서 `GameStart`부터 Play 검증

## Notes

- 전투 관련 기존 파일은 수정하지 않는다.
- MVP 맵은 전체 그래프를 보여주며, 현재 위치에서 연결된 다음 노드만 클릭해야 전투에 진입한다.
- MVP 전투 씬은 참고 이미지처럼 상단 HUD, 몬스터 전투 영역, 5 x 3 슬롯 보드, 하단 결과/스핀/자원 패널을 한 화면에 고정 배치한다.
- 각 플로우 씬은 `Assets/_Project/Prefabs/UI/GameFlow/`의 View 프리팹을 인스턴스로 들고, Controller는 배치된 참조만 갱신한다. 필요 시 Unity 메뉴 `SlotRogue > Game Flow > Rebuild Scene UI Prefabs`로 재생성한다.
- 시작 유물과 보상은 `SlotCombatRequest`를 후처리한다.
- `GameFlowImageSlot`의 `SlotId` 기준으로 배경/카드/전투/보상 이미지를 교체한다.
- 2026-05-31: Unity 생성 `.csproj`가 아직 새 GameFlow 파일을 포함하지 않아, 별도 `Temp/GameFlowCompileCheck`와 `Temp/GameFlowTestsCompileCheck`로 새 코드/테스트 컴파일 확인. 경고/오류 0개. Unity Editor 인스턴스가 없어 MCP refresh/Test Runner/Play 검증은 실행하지 못함.
- 2026-05-31: RunBattle에서 패턴 성공 시 `PATTERN HIT!` 문구와 매칭 셀 색상 강조를 표시. 패턴 실패 시 `BASE ATTACK`으로 기본 공격을 명시.

## Completion

_(completed/로 옮길 때 채움.)_

- **Finished**:
- **Outcome**:
- **Follow-ups**:
