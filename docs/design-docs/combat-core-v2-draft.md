# 전투 코어 v2 (설계 초안)

**Status**: in-discussion  
**Last updated**: 2026-05-29  
**Supersedes (예정)**: [`combat-core.md`](./combat-core.md), [`ADR-0001`](../adr/0001-combat-turn-event-log.md)  
**유지 계약**: [`ISpinCombatConsumer`](../../Assets/_Project/Scripts/Core/Combat/ISpinCombatConsumer.cs) + `CombatSpinOutcome` (attack / defense)

> 구현 전 논의 기록. Locked는 합의된 결정. Open은 다음 세션에서 이어서 논의한다.

---

## Purpose

슬롯 1스핀 = 전투 1라운드. **참가자(플레이어·몬스터) 대칭**으로 행동 큐를 두고, **로직 적용 후 Presenter 연출을 await** 하며 순서대로 진행한다. 기존 v1(`BattleResolver` + `CombatEventKind` + 동기 이벤트 로그)은 본 문서 확정 후 교체·폐기 예정.

---

## Locked decisions

| ID | 결정 |
|----|------|
| **V1** | 1스핀 = 1라운드. 참가자 순서 **항상 [플레이어 → 몬스터]** |
| **V2** | **BattleDirector**가 라운드·입력 잠금·큐 실행의 단일 진입점. 참가자 큐 + **행동 큐**. 행동 목록은 **미리 계획**, 실행 중 HP≤0이면 **남은 행동 스킵** |
| **V3** | `Apply`(로직) 즉시 확정 → `await Presenter.PlayAsync` → 다음 행동 |
| **V4** | HP·버프는 **Participant** 소유. 버프는 **N턴 지속** 전제, 규칙은 `ApplyBuff` 단일 진입점 (중첩/갱신 상세 **TBD**) |
| **V5** | 몬스터가 먼저 HP≤0 → **몬스터 차례 생략**. 플레이어 HP≤0 → **즉시 전투 종료**(몬스터 차례 없음) |
| **V6** | **동시 사망** → **패배 우선** (플레이어 HP 0은 다음 전투 참여 불가) |
| **V7** | 슬롯 입력: `ISpinCombatConsumer.OnSpinResolved(CombatSpinOutcome)`. 당분간 **attack / defense** 만 |
| **V8** | 사망: Participant가 HP 변경 **단일 진입점**에서 **`Died` 이벤트** 발행. 승패·차례 생략·동시 사망은 **Director**만 처리. **전역 static bus 금지** |
| **V9** | 플레이어 스핀 → 행동 계획: **`Attack` → `Defend` 순**, 둘 다 행동 큐 + 연출. `defense`는 행동 후 Participant에 **이번 라운드 방어값** 저장, 몬스터 공격 시 소비 |
| **V10** | 몬스터 데이터 **1안**: `MonsterDefinition` 안에 `TurnStep[] Pattern` + `Loop`. 행동 SO 참조 없음 |
| **V11** | 몬스터 패턴: **1스핀당 Pattern 인덱스 1칸 진행** (연타·StepsPerRound는 Open) |

---

## 아키텍처 (가칭)

```text
OnSpinResolved(outcome)
    → BattleDirector.RunRoundAsync()
         1) Plan: 플레이어 행동 리스트, 몬스터 행동 리스트
         2) ExecutePlayerTurnAsync (행동 큐)
         3) 전투 종료 아니면 ExecuteMonsterTurnAsync
         4) 라운드 종료 (버프 턴 감소 등)
```

| 구성요소 | 책임 | Unity |
|----------|------|-------|
| **BattleDirector** | 라운드·큐·입력 잠금·`Died` 구독·승패 | MonoBehaviour 또는 씬 서비스 |
| **ICombatParticipant** | Player / Monster. HP·버프·`PlanTurn`·`Apply`·`Died` | **순수 C#** 권장 |
| **CombatAction** | 공격·방어·버프·죽음 연출 등 (enum Kind 폭발 대신 타입/데이터) | Core DTO |
| **IActionPresenter** | `PlayAsync(action, result)` — 애니·UI만 | **MonoBehaviour** |

**asmdef**: Core는 UI 참조 안 함. UI → Core.

**확장 (나중)**: 유물·속성·특수기는 `PlanTurn` 단계 후크 또는 `CombatSpinOutcome` 필드 확장. `ICombatModifier` 이름은 미채택.

---

## 한 라운드 흐름 (MVP)

```text
[계획] 플레이어: Attack(outcome.Attack) → Defend(outcome.Defense)
[실행] foreach action: Apply → await Presenter
         → Died 시 Director: 스킵 / 몬스터 차례 생략 / CheckEnd

[계획] 몬스터: Pattern[PatternIndex] → CombatAction 1개
[실행] 동일
         → PatternIndex++ (Loop)

CheckEnd: playerDead && monsterDead → 패배
```

MVP 시나리오: **스핀 → 플레이어 때림·방어 연출 → 몬스터 패턴 공격 → HP 0이면 끝**.

---

## 사망 이벤트

```text
Participant.ChangeHp(...)  // 모든 HP 변경 경로 통합
  → if HP≤0 && !wasDead → Died 발행 (1회)

Director (구독, 전투 시작~종료):
  → 남은 행동 스킵, 몬스터 차례 생략
  → (선택) Death 연출 행동 1스텝 await
  → CheckEnd (동시 사망 → 패배)
```

`ActionResult`는 데미지 수·막힘 등 **행동 결과**용. 큐 제어는 **Died 이벤트 기준**.

---

## 데이터 스케치 — 1안 (`TurnStep`)

### `MonsterDefinition`

| 필드 | 타입 | 설명 |
|------|------|------|
| `MaxHp` | int | 최대 HP |
| `Loop` | bool | Pattern 끝 → 인덱스 0 |
| `Pattern` | `TurnStep[]` | 턴 순서 테이블 |

### `TurnStep`

```csharp
public enum TurnStepKind
{
    Attack,
    Defend,
    Buff,
    Special,
}

[Serializable]
public sealed class TurnStep
{
    public TurnStepKind Kind;
    public int Value;      // Attack: RawAttack, Defend: DefendValue, Buff: magnitude (TBD)
    public string BuffId;  // Kind == Buff / Special
}
```

### Kind별 `Value` (MVP)

| Kind | Value | BuffId |
|------|-------|--------|
| Attack | 공격력 | — |
| Defend | 방어 수치 | — |
| Buff | 효과 크기 (TBD) | 버프 id |
| Special | TBD | 특수 id |

### 예시 (고블린)

| # | Kind | Value | BuffId |
|---|------|-------|--------|
| 0 | Attack | 12 | |
| 1 | Defend | 8 | |
| 2 | Attack | 12 | |
| 3 | Buff | 3 | `goblin_rage` |

`Loop = true`

**나중 분리**: `Pattern`만 별도 SO로 옮겨도 `MonsterParticipant.PlanTurn` 한곳만 수정하면 됨 (Director는 `CombatAction` 리스트만 받음).

---

## v1 대비 폐기·재작성 예정

| v1 (`combat-core.md`) | v2 |
|------------------------|-----|
| `BattleResolver` + `BattleState` 중앙 집중 | Director + Participant |
| `TurnResult` / `CombatEventKind` | `CombatAction` + (선택) 라운드 요약 DTO |
| `BattlePresenter` 동기 foreach | `TurnReceived` 제거 또는 Director→Timeline 직접 |
| C3 몬스터 **행동 1회**/스핀 | 몬스터도 **행동 큐** (패턴 1칸 = 기본 1행동) |
| C2/C7 당턴 방어·다음 스핀 Defend 소비 | Participant 버프/방어 모델로 **재정의** (Open) |
| SO 3단 (Definition → Pattern → ActionDefinition) | **1안** `TurnStep[]` 인라인 |

---

## Alternatives (논의에서 채택·거절)

| 주제 | 채택 | 거절 |
|------|------|------|
| 몬스터 SO | **1안** Definition 내 `Pattern[]` | 2안 별도 Pattern SO (나중 이전 가능) |
| 플레이어 방어 | **행동 큐에 Defend** | 방어는 상태만, 연출 없음 |
| 사망 신호 | **Participant `Died` → Director** | `ActionResult.TargetDied` 만 |
| 행동 계획 | **계획 큐 + 실행 중 스킵** | 매 피해 후 전체 재계획 |
| 슬롯 확장 | attack/defense 유지, Plan 단계 후크 | `ICombatModifier` (이름 미정) |

---

## Open / Next topic

| ID | 질문 |
|----|------|
| O1 | **TurnStep**: 1스핀당 패턴 1칸 vs 여러 칸 (`StepsPerRound`) |
| O2 | **TurnStep → CombatAction**: 몬스터 Defend가 Participant에 쌓이는 타이밍·소비 규칙 |
| O3 | **피해 공식**: `max(0, raw - defense)` 유지 여부, 방어가 **1피해** vs **라운드 전체** |
| O4 | **버프**: 중첩 vs 갱신 규칙 (ApplyBuff 구현 시 확정) |
| O5 | **Attack/Defend 순서** 바꿀지 |
| O6 | **0 데미지** 연출 강도 (현재: 큐에 포함) |
| O7 | **Custom Editor** for TurnStep Kind별 필드 숨김 |

**Next topic**: O1·O2 (TurnStep 세부 + Defend 타이밍)

---

## 세션 메모

| 날짜 | 내용 |
|------|------|
| 2026-05-29 | v2 방향 합의: Participant 큐, Presenter await, Died 이벤트, 1안 TurnStep 스케치, 플레이어 Attack→Defend |

---

## Related

- 구현 중 (v1 파이프라인): [`feature-combat-timeline-controller`](../exec-plans/active/feature-combat-timeline-controller.md)
- v1 설계: [`combat-core.md`](./combat-core.md), [`ADR-0001`](../adr/0001-combat-turn-event-log.md)
