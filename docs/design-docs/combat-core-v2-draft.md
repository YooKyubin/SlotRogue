# 전투 코어 v2 (설계 초안)

**Status**: in-discussion  
**Last updated**: 2026-05-29  
**Supersedes (예정)**: [`combat-core.md`](./combat-core.md), [`ADR-0001`](../adr/0001-combat-turn-event-log.md)  
**유지 계약**: [`ISpinCombatConsumer`](../../Assets/_Project/Scripts/Core/Combat/ISpinCombatConsumer.cs) + `CombatSpinOutcome` (attack / defense)

> 구현 전 논의 기록. **Locked**는 합의된 결정. **Open**은 다음 세션에서 이어서 논의한다.

---

## Purpose

슬롯 **스핀 1회**(릴 결과 확정) = 전투 **라운드 1회**. **참가자(플레이어 → 몬스터)** 순으로 **행동 큐**를 실행하고, **로직 `Apply` 직후 `await Presenter`** 로 연출한다. v1(`BattleResolver`, `CombatEventKind`, `TurnResult` canonical)은 본 문서 확정 후 교체·폐기 예정.

---

## Locked decisions

| ID | 결정 |
|----|------|
| **V1** | 1스핀 = 1라운드. 참가자 순서 **항상 [플레이어 → 몬스터]** |
| **V2** | **BattleDirector**가 라운드·입력 잠금·큐 실행의 단일 진입점. 행동 목록 **미리 계획**, 실행 중 HP≤0이면 **남은 행동 스킵** |
| **V3** | `Apply`(로직) 즉시 확정 → `await Presenter.PlayAsync` → 다음 행동 |
| **V4** | HP·버프는 **Participant** 소유. 버프 **N턴** 전제, `ApplyBuff` 단일 진입점 (**중첩/갱신 규칙 TBD**) |
| **V5** | 몬스터 선사망 → **몬스터 차례 생략**. 플레이어 사망 → **즉시 전투 종료** |
| **V6** | **동시 사망** → **패배 우선** |
| **V7** | 슬롯: `ISpinCombatConsumer` + `CombatSpinOutcome` (**attack / defense** 만, 당분간) |
| **V8** | 사망: Participant **HP 단일 진입점** → **`Died` 이벤트**. 승패·큐 제어는 **Director**. **전역 static bus 금지** |
| **V9** | 플레이어 스핀 행동 순서: **`Attack` → `Defend`** (당분간 고정). 둘 다 큐+연출 (**값 0이면 생략**, V15) |
| **V10** | 몬스터 데이터 **1안**: `MonsterDefinition`에 **`PatternBundles` 2차원** (행동 묶음). 별도 Action SO 없음 |
| **V11** | **1스핀 = `PatternBundles[turnIndex]` 행 1개 전체 실행** 후 `turnIndex++` (`Loop` 시 0) |
| **V12** | **피해·방어 감소 (O3)**: `actualDamage = max(0, rawAttack - defense)`; 방어 수치 `defense = max(0, defense - rawAttack)` (**공격에 의해 방어도 감소**) |
| **V13** | **몬스터 Defend (D4)**: 몬스터 차례 `Defend`로 방어 저장 → **다음 스핀** 플레이어 `Attack`에 적용(V12) → **플레이어 차례 종료 시 남은 방어 만료** (Attack 0·잔여 방어 동일) |
| **V14** | **플레이어 Defend**: `Defend` 후 **모든 피해 유형**에 V12 적용. **몬스터 차례(묶음 행동 전부) 종료 시** 남은 방어 만료. 몬스터 Attack 연속 시 방어 **누적 감소** |
| **V15** | 플레이어: `attack==0` / `defense==0` → 해당 **행동 큐 생략** (연출·Apply 없음) |
| **V16** | 몬스터: `TurnStep` Value 0이어도 **행동 유지** (데이터 의도·연출) |
| **V17** | 플레이어 `attack==0` && `defense==0` → 플레이어 행동 없음, **몬스터 묶음·`turnIndex++`는 그대로** |
| **V18** | `PatternBundles[i]`가 **빈 배열** → 몬스터 차례 즉시 종료, **`turnIndex++` 유지** |
| **V19** | `TurnStepKind.Buff` MVP: **로그 + 짧은 대기**만 (게임 효과 나중) |

---

## 피해·방어 (V12~V14)

### 공통 식

```text
actualDamage = max(0, rawAttack - defense)
defense      = max(0, defense - rawAttack)   // 피해 계산 후 남은 방어
```

### 몬스터 Defend (V13) — 고블린 예 기준

| 시점 | 동작 |
|------|------|
| 스핀 N, 몬스터 `Defend` | 방어 값 저장 (연출) |
| 스핀 N+1, 플레이어 `Attack` | V12 적용 후 소비/감소 |
| 스핀 N+1, 플레이어 차례 **끝** | 남은 몬스터 방어 **0** (Attack 5 vs 방어 8 → 3 남아도 만료) |

### 플레이어 Defend (V14)

| 시점 | 동작 |
|------|------|
| 스핀 N, 플레이어 `Defend` | 방어 값 저장 |
| 스핀 N, 몬스터 차례 | 묶음 내 **모든 피해**마다 V12 (Attack 여러 번 가능) |
| 스핀 N, 몬스터 차례 **끝** | 남은 플레이어 방어 **0** |

### 대칭 요약

| | 유효 구간 | 만료 |
|--|-----------|------|
| **플레이어 방어** | 같은 스핀 **몬스터 차례** | 몬스터 차례 끝 |
| **몬스터 방어** | 다음 스핀 **플레이어 차례** | 플레이어 차례 끝 |

---

## Canonical 예시 — 고블린 (V11·V13)

```text
PatternBundles[0] = [ Attack 12, Defend 8 ]
PatternBundles[1] = [ Attack 15 ]
Loop = true
```

**스핀 1**

1. 플레이어: `Attack`(spin.attack) → `Defend`(spin.defense) — 0이면 각각 생략(V15)
2. 몬스터: `Attack 12` → `Defend 8` (다음 스핀 플레이어 Attack에 8 적용 예정)
3. `turnIndex` → 1

**스핀 2**

1. 플레이어: `Attack` — 몬스터 방어 8 적용(V12), 플레이어 차례 끝에 몬스터 방어 만료(V13)
2. 플레이어: `Defend` — 몬스터 `Attack 15`에 사용, 몬스터 차례 끝에 플레이어 방어 만료(V14)
3. 몬스터: `Attack 15`
4. `turnIndex` → 0 (`Loop`)

**플레이어 방어 연속 소모 예:** 방어 8, 몬스터 묶음 `[Attack 5, Attack 5]` → 방어 3 → 피해 2 → 몬스터 차례 끝 → 방어 0.

---

## 아키텍처 (가칭)

```text
OnSpinResolved(outcome)
    → BattleDirector.RunRoundAsync()
         1) PlanPlayerActions(outcome)   // Attack→Defend, 0 생략
         2) ExecutePlayerTurnAsync
         3) CheckEnd / Died → 몬스터 차례 생략?
         4) PlanMonsterActions(PatternBundles[turnIndex++])
         5) ExecuteMonsterTurnAsync
         6) 라운드 종료 (버프 턴 감소 등)
```

| 구성요소 | 책임 | Unity |
|----------|------|-------|
| **BattleDirector** | 라운드·큐·입력 잠금·`Died` 구독·승패 | MonoBehaviour 또는 씬 서비스 |
| **ICombatParticipant** | Player / Monster. HP·방어·버프·`PlanTurn`·`Apply`·`ChangeHp`·`Died` | **순수 C#** 권장 |
| **CombatAction** | 공격·방어·버프·죽음 등 (타입/데이터, `CombatEventKind` 대체) | Core DTO |
| **IActionPresenter** | `PlayAsync(action, result)` | **MonoBehaviour** |

**패턴 해석**: `MonsterParticipant`(가칭) / `IMonsterTurnPlanner` **한곳**에서만 `PatternBundles` 읽기 → `CombatAction[]` 변환.

**확장 (나중)**: 유물·속성·특수기 — `PlanTurn` 후크 또는 `CombatSpinOutcome` 필드. `ICombatModifier` 이름 미채택.

---

## 한 라운드 흐름 (MVP)

```text
[플레이어] Plan: Attack( if attack>0 ) → Defend( if defense>0 )
           Exec: Apply → await Presenter (each)
           → Died / CheckEnd

[몬스터]   Plan: PatternBundles[turnIndex] → N actions (빈 배열이면 0)
           Exec: each step (0 Value도 실행, V16)
           → turnIndex++
           → 플레이어/몬스터 방어 만료 (V13/V14 시점)
```

MVP 시나리오: **스핀 → (플레이어 공격·방어) → 몬스터 묶음 → HP 0이면 끝**.

---

## 사망 이벤트 (V8)

```text
Participant.ChangeHp(...)  // 모든 HP 변경 경로
  → if HP≤0 && !wasDead → Died (1회)

Director:
  → 행동 큐 스킵, 몬스터 차례 생략(V5)
  → CheckEnd: 동시 사망 → 패배(V6)
```

큐 제어는 **`Died` 기준**. `ActionResult`는 연출·피드백용.

---

## 데이터 스케치 — `PatternBundles` (2차원)

### `MonsterDefinition`

| 필드 | 타입 | 설명 |
|------|------|------|
| `MaxHp` | int | 최대 HP |
| `Loop` | bool | `turnIndex` 끝 → 0 |
| `PatternBundles` | `TurnStep[][]` | **행** = 스핀당 몬스터 묶음, **열** = 묶음 내 행동 순서 |

```csharp
// SlotRogue.Data.Combat — 스케치
public enum TurnStepKind { Attack, Defend, Buff, Special }

[Serializable]
public sealed class TurnStep
{
    public TurnStepKind Kind;
    public int Value;
    public string BuffId;
}

[CreateAssetMenu(menuName = "SlotRogue/Combat/Monster Definition")]
public sealed class MonsterDefinition : ScriptableObject
{
    public int MaxHp;
    public bool Loop = true;
    public TurnStep[][] PatternBundles = Array.Empty<TurnStep[]>();
}
```

### Kind별 `Value` (MVP)

| Kind | Value | BuffId | MVP |
|------|-------|--------|-----|
| Attack | rawAttack | — | 피해 |
| Defend | defendValue | — | V13 방어 저장 |
| Buff | (TBD) | id | V19 스텁 연출 |
| Special | TBD | id | 미구현 |

### 예시 데이터

```text
PatternBundles[0] = [ {Attack,12}, {Defend,8} ]
PatternBundles[1] = [ {Attack,15} ]
```

**나중**: `PatternBundles`만 별도 SO로 분리 가능 — Planner 한곳만 수정.

---

## v1 대비 폐기·재작성 예정

| v1 | v2 |
|----|-----|
| `BattleResolver` + 중앙 `BattleState` | Director + Participant |
| `TurnResult` / `CombatEventKind` | `CombatAction` (+ 선택적 라운드 요약) |
| C3 몬스터 1행동/스핀 | **묶음 1행 = 행동 N개** (V11) |
| C7 Defend 다음 스핀 1회 (단순 pending) | V12~V14 (감소·만료 구간 명시) |
| SO 3단 (Definition→Pattern→Action SO) | `PatternBundles[][]` (V10) |

---

## Alternatives (논의 기록)

| 주제 | 채택 | 거절 |
|------|------|------|
| 몬스터 패턴 | **2D 묶음** (V10~V11) | 1스핀 1 `TurnStep` only |
| 몬스터 Defend | **D4** (+ 고블린 예) | D1만 (문서상 MVP와 동일 결과) |
| 플레이어 0 수치 | **큐 생략** (V15) | 0도 연출 |
| 몬스터 0 수치 | **행동 유지** (V16) | 생략 |
| 사망 | **Died 이벤트** | `ActionResult`만 |
| Buff MVP | **스텁 연출** (V19) | 금지 / 완전 구현 |

---

## Open / Next topic

| ID | 질문 |
|----|------|
| O4 | 버프 **중첩 vs 갱신** (`ApplyBuff` 구현 시) |
| O7 | `TurnStep` Kind별 인스펙터 필드 숨김 (Custom Editor) |
| — | **`CombatAction` 타입** MVP 최소 집합 스케치 |
| — | `Special` / 유물·속성 공격 확장 시점 |
| — | v2 확정 후 **ADR supersede** (`0001`), `combat-core.md` → v2 승격 |

**Next topic**: `CombatAction` MVP 스케치

---

## 세션 메모

| 날짜 | 내용 |
|------|------|
| 2026-05-29 | v2 방향: Participant 큐, Presenter await, Died, 플레이어 Attack→Defend |
| 2026-05-29 | `PatternBundles[][]`, D4 몬스터 방어, V12~V14 피해·방어·만료, O5~O19, 고블린 canonical 예시 |

---

## Related

- v1 구현 중: [`feature-combat-timeline-controller`](../exec-plans/active/feature-combat-timeline-controller.md) — v2 확정 시 재정렬
- v1 설계: [`combat-core.md`](./combat-core.md), [`ADR-0001`](../adr/0001-combat-turn-event-log.md)

---

## 세션 시작 프롬프트 (복붙용)

아래 블록을 새 세션 첫 메시지에 붙여 넣는다. **Locked는 임의 변경 금지** — 변경 시 사용자 확인 후 draft·ADR 갱신.

```text
SlotRogue — 전투 v2 설계 이어하기.

【필수 읽기】
- docs/design-docs/combat-core-v2-draft.md (Locked = 합의됨, Open = 논의 중)
- docs/STATUS.md (한 줄 맥락)

【유지】
- ISpinCombatConsumer + CombatSpinOutcome(attack/defense) — v2에서도 슬롯 경계

【Locked 요약】
- 1스핀 = 1라운드: 플레이어(Attack→Defend, 값0 생략) → 몬스터
- 몬스터: PatternBundles[][] — 스핀당 행 1묶음 전부 실행, turnIndex++, 빈 [] OK
- Director + Participant 행동 큐, Apply 후 await Presenter
- 피해: damage=max(0,raw-def), def=max(0,def-raw)
- 몬스터 Defend: 다음 스핀 플레이어 Attack에 적용 → 플레이어 차례 끝 만료
- 플레이어 Defend: 모든 피해, 몬스터 묶음 끝까지 → 몬스터 차례 끝 만료
- 몬스터 TurnStep 값0: 행동 유지. Died 이벤트→Director. 동시사망=패배
- Buff MVP: 로그+대기만. 고블린 예시는 draft canonical 참고

【Open / Next】
- Next: CombatAction MVP 타입 스케치
- TBD: 버프 중첩/갱신(O4), TurnStep 에디터(O7), ADR supersede

【작업 방식】
- 설계 논의: 한 번에 질문 1개, 한국어 간결
- 코드/공식 draft 반영: 사용자 요청 시 Agent 모드
- v1 combat-core.md / ADR-0001 은 supersede 전까지 참고만

이번 목표: <여기에 한 줄, 예: CombatAction MVP 스케치>
```
