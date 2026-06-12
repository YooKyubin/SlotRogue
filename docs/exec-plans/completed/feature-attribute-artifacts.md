# 속성 유물 시스템 (화염·빙결·독)

**Status**: completed  
**Started**: 2026-06-04  
**Owner**: _(슬롯 담당)_  
**Related design-docs**: [`game-flow.md`](../../design-docs/game-flow.md)

## Goal

속성 상태이상(화염·빙결·독)을 발동하는 유물 3종을 추가하고, 전체 유물 데이터를 ScriptableObject로 관리한다.

- `ArtifactDefinitionSO`: 인스펙터에서 유물 데이터 확인·편집. 카테고리(시작 유물/상점 전용 등) 구분.
- `ArtifactCatalogSO`: 전체 유물 목록 SO. `Assets/Resources/ArtifactCatalog.asset`에 배치.
- 상태이상 tick: `BattleSystem`이 매 적 턴 시작 전 화염·독 피해 적용, 빙결 시 적 행동 스킵.

## Checklist

- [x] exec-plan 생성 및 STATUS.md 갱신
- [x] Core/Combat: `StatusEffectKind`, `StatusEffect` 추가
- [x] Core/Combat: `CombatEventKind`에 `StatusEffectTicked`, `EnemyFrozen` 추가
- [x] Core/Combat: `CombatParticipant`에 `StatusEffects`, `IsFrozen` 추가
- [x] Core/Combat: `BattleSystem`에 `TickMonsterStatusEffects` 삽입, 빙결 처리
- [x] UI/GameFlow: `ArtifactCategory`, `ArtifactEffectKind` 추가
- [x] UI/GameFlow: `ArtifactDefinitionSO`, `ArtifactCatalogSO` 추가
- [x] UI/GameFlow: `StarterArtifactId`를 슬롯 아이콘 6종 기준으로 정리
- [x] UI/GameFlow: `StarterArtifactCatalog`를 SO 기반으로 교체
- [x] UI/GameFlow: `RunCombatRequestResult`에 `StatusEffectToApply` 추가
- [x] UI/GameFlow: `RunCombatRequestResolver`를 `ArtifactDefinitionSO` 기반으로 갱신
- [x] UI/GameFlow: `GameFlowSession`에 `SelectedArtifactId: string` 추가
- [x] UI/GameFlow: `StartArtifactSelectionController` 갱신
- [x] UI/GameFlow: `RunBattleController`에서 상태이상 적용 추가
- [x] `StarterArtifactDefinition.cs` 제거
- [x] Editor: `ArtifactCatalogBuilder` 메뉴 추가
- [ ] Unity Editor에서 `SlotRogue/Artifact/Build Catalog` 실행 후 에셋 확인
- [x] `dotnet build SlotRogue.slnx` 통과
- [ ] Unity Editor에서 기존 EditMode 테스트 통과 확인

## 유물 설계

| ID | 이름 | 아이콘 | 조건 | 효과 |
|---|---|---|---|---|
| `cherry` | 체리 | 체리 | 3+ | 피해 +5 |
| `grape` | 포도 | 포도 | 3+ | 회복 +4 |
| `seven` | 세븐 | 세븐 | 3+ | 방어 +6 |
| `lemon` | 레몬 | 레몬 | 3+ | 화염 3턴 (매 턴 피해 2) |
| `bell` | 종 | 종 | 3+ | 빙결 1턴 (적 행동 스킵) |
| `clover` | 네잎클로버 | 네잎클로버 | 3+ | 독 스택 +1 (스택당 1 피해/턴, 최대 5) |

## Notes

- `StarterArtifactDefinition`은 이 작업에서 제거하고 `ArtifactDefinitionSO`로 대체한다.
- `StarterArtifactCatalog`는 SO 미존재 시 코드 fallback을 유지해 테스트가 SO 없이도 통과하도록 한다.
- `GameFlowSession.SelectStarterArtifact(StarterArtifactId)` 오버로드는 테스트 backward compat을 위해 유지한다.

## Completion

- **Finished**: 2026-06-11
- **Outcome**: 화염·빙결·독 전투 상태이상 기반은 남기되, 시작 유물 데이터 모델과 6종 기획은 v20.3 `RelicCatalog`로 대체했다. 구 Artifact 타입과 자산은 2026-06-12 참조 검증 후 삭제했다.
- **Follow-ups**: 신규 유물 런타임 연결은 `feature-game-flow-loop.md`와 `relic-system.md`에서 추적한다.
