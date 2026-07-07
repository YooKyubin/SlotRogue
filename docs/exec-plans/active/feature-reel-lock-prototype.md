# 심볼 스왑 프로토타입
**Status**: active  
**Started**: 2026-06-29  
**Owner**: Codex  
**Contributors**: _(없음)_  
**Related design-docs**: [`slot-core.md`](../../design-docs/slot-core.md), [`game-flow.md`](../../design-docs/game-flow.md)

## Goal

전투 시스템 본체를 갈아엎지 않고, 플레이어 턴의 개입감을 검증하기 위한 `스핀 후 1회 인접 심볼 스왑` 프로토타입을 `codex/reel-lock-prototype` 브랜치에서 분리 구현한다. 릴 잠금 실험은 재미 검증에서 폐기하고, 상점/별조각 축은 v33 제안/유물 기획서 기준으로 검증한다.

## Prototype Rules

- 플레이어 턴에는 스핀을 1회만 수행한다.
- 스핀 결과가 나온 뒤, 플레이어는 인접한 두 심볼을 드래그해서 최대 1회 서로 바꿀 수 있다.
- 스왑 횟수는 플레이어 턴이 돌아올 때마다 1회로 리필되며, 남은 횟수는 누적되지 않는다.
- 스왑은 가로/세로 인접 셀만 허용한다. 대각선과 비인접 셀은 불가능하다.
- 스왑을 쓰지 않아도 `ATTACK`으로 현재 결과를 공격에 사용한다.
- PC 테스트 편의를 위해 첫 칸 클릭 후 인접 칸 클릭으로도 같은 스왑을 수행할 수 있다.
- 스핀 1회마다 기본 별조각을 지급한다. 단, 스왑을 사용한 턴은 별조각 보상을 지급하지 않는다.
- 릴 잠금 UI/로직은 플레이어 기능에서 제거한다. 씬에 남은 `LockBtn` 5개는 런타임에서 숨긴다.
- 일반/튜토리얼 런은 시작 유물을 지급하지 않고 바로 첫 전투로 진입하며, 튜토리얼 승리 후에는 일반 런으로 전환해 `RewardPanel 1` 제안 화면을 검증한다.
- 전투 승리 후 `RewardPanel 1`에는 v33 제안 기획서 42종 중 3개를 제시한다.
- v33 제안 중 심볼 가중치, 심볼 기본 피해, 별조각 즉시 지급은 현재 런 상태에 직접 적용한다. 배율/다시/상태 계열은 선택 기록을 남기며 전투 수식 연결은 후속 범위다.
- 전투 중 별도 상점 버튼을 누르면 `ShopPanel`이 열리고, `ShopPanel` 하위 `GameFlowOptionView` 유물 카드 5개와 `RerollButton`으로 별조각을 소비한다.
- 상점 버튼이 씬에 없으면 런타임 fallback `ShopButton`을 생성해 `ShopPanel` 토글에 연결한다.
- `Top Panel/Star HUD`와 `ShopPanel/StarPanel`에는 현재 별조각을 표시하며, 씬에 별도 텍스트가 없으면 런타임 텍스트를 생성한다.
- 상점 가격은 v33 유물 기획서 `price` 필드를 사용한다. 리롤은 별조각 1개다.
- 보유 유물 슬롯은 기본 5칸이며, 제안 효과로 늘어나도 최대 7칸까지만 허용한다.
- v33 유물 중 즉시 지급, 기본 피해 증가, 전투당 스왑 횟수 증가, 전투 시작 별조각 지급은 현재 런 상태에 직접 적용한다. 나머지 배율/다시/저주 수식은 보유 유물로 표시하며 전투 수식 연결은 후속 범위다.

## Checklist

- [x] `codex/reel-lock-prototype` 브랜치 분리
- [x] 릴 잠금 프로토타입 구현
- [x] `ShopPanel` 유물 5칸, 구매 버튼 5개, `RerollButton` 자동 연결
- [x] `ShopPanel` 별조각 표시와 유물 카드 fallback 렌더링 보강
- [x] 일반/튜토리얼 런 시작 유물 선택 제거
- [x] 전투 후 3택 제안을 유물 제외 보상풀로 재구성
- [x] 릴 잠금 플레이어 로직 제거
- [x] 스핀 후 1회 인접 심볼 드래그/클릭 스왑 상태/입력 구현
- [x] 슬롯 보드/레버 클릭으로 `SPIN` 요청 연결
- [x] 스왑 후 매칭 preview 갱신, `ATTACK` 시 패턴/전투 요청 확정 계산
- [x] 튜토리얼 안내를 스핀 결과 확인 단계에 맞춰 재배치하고 SWAP 별조각 미지급 규칙 반영
- [x] 스왑 대기 중 Addressable 하이라이트 심볼과 tilt pulse cue 표시
- [x] 스왑 확정 전까지 상점 입력 비활성화
- [x] `RewardPanel 1` 자동 탐색과 전투 승리 후 v33 제안 3개 표시 연결
- [x] `ShopPanel`을 상점 버튼으로 열고 구매/리롤/닫기 입력 연결
- [x] `ShopPanel`의 `GameFlowOptionView` 카드 직접 렌더링과 튜토리얼 첫 승리 후 `RewardPanel 1` 진입 보강
- [x] `RewardPanel 1` 이름 정확 매칭과 상점 버튼 fallback 생성 보강
- [x] 전투 HUD `Relic Panel`에 현재 보유 유물 아이콘 표시
- [x] `Top Panel/Star HUD`와 `ShopPanel/StarPanel` 별조각 숫자 표시 연결
- [x] `ShopPanel` 5개 유물 카드 직접 연결 기준 렌더링, 상점 open 중 spin 비활성화, 별조각 HUD 직접 연결 보강
- [x] `ShopArtifactOptionView` 등급 표시는 tint 컴포넌트 대신 부모 `RunBattleShopView`의 capsule sheet 1회 연결로 Sprite 교체
- [x] `FinalResultDirector` 초기 표시를 0/0/0으로 세팅하고 impact flash duration 경고 제거
- [x] 보유 유물 기본 5칸/최대 7칸 용량 제한과 상점 구매 차단 적용
- [x] 기존 v23 유물 카탈로그를 v33 유물 44종으로 교체
- [x] 기존 보상 제안을 v33 제안 42종으로 교체
- [x] `dotnet build SlotRogue.UI.csproj`, `dotnet build SlotRogue.UI.Tests.csproj` 컴파일 검증
- [x] EditMode 테스트 갱신
- [ ] Unity Editor에서 RunGame 전투 UI 수동 플레이테스트
- [ ] 재미검증 후 main 반영 또는 폐기 결정

## Notes

- 인벤토리는 유물 전용으로 열고, 심볼/패턴 정보는 별도 설명 패널 탭으로 유지한다.
- 유물 인벤토리 row는 아이콘, 유물 이름, 설명 텍스트를 각각 바인딩한다.

- `BattleSceneHost`의 `ShopDescriptionView`는 하위 검색으로 보강하지 않고 인스펙터 필드와 RunGame 씬 직렬화 참조로 직접 연결한다.

- 전투 피해 적용, 적 턴, Replay 이벤트 타임라인은 유지한다.
- 스왑은 전투 계산 확정 전의 보드 편집 단계다. 스핀/스왑 중에는 매칭 셀 preview만 갱신하고, `ATTACK` 이후에 패턴 계산 → 유물/보너스 → 전투 적용 순서를 따른다.
- `RunGame` 씬을 Title Boot 없이 직접 실행해도 스왑 대기 하이라이트가 보이도록 전투 씬 조립 단계에서 `Symbol Sheet Highlight`를 로드해 슬롯 보드에 주입한다.
- 실패 시 브랜치 단위로 자기장 없이 버릴 수 있도록 기존 main 흐름과 분리한다.
