# 전투 피해 VFX 조합형 모듈

**Status**: active  
**Started**: 2026-07-09  
**Owner**: Codex  
**Contributors**: _(없음)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md), [`game-flow.md`](../../design-docs/game-flow.md)

## Goal

플레이어 직접 피해를 시작으로, 피해 종류별 VFX profile을 `MonoBehaviour` module 조합으로 재생할 수 있는 presentation 구조를 만든다. 전투 Core와 `CombatEvent` Replay 모델은 유지하고, `DamagePresenter`에서 화면 계층으로 피해 VFX 요청을 전달해 `EnemyFormationSlotView`가 profile별 module을 실행한다.

## Scope

- `PlayerDirectDamage` profile을 첫 적용 대상으로 두며, 이 profile의 HitFlash는 몬스터 sprite 알파 형태를 유지한 채 RGB를 흰색으로 치환하는 white override flash를 목표로 한다.
- `HitFlash`, `TintFlash`, `SlashCut`, `SparkParticle`를 독립 module로 구현할 수 있는 구조를 만든다.
- `CombatEvent` Replay, `DamagePresenter`, `CombatPresentationCommandDispatcher`, `RunBattleWorldView`, `EnemyFormationView`, `EnemyFormationSlotView` 연결 경로를 사용한다.
- Inspector에서 `CombatDamageVFXSet`으로 profile과 module 목록을 조합한다.

## Non-goals

- 상태 피해별 실제 VFX 구현.
- 크리티컬, 처치, shield block 전용 VFX 구현.
- 최종 아트 리소스 제작.
- 전투 Core의 `CombatEffect` 필드 확장.

## Current State

- 2026-07-09: 1-A 완료. 피해 VFX profile/request/context 타입, module 계약, Inspector 직렬화 set을 추가했다.
- 2026-07-09: 1-B 완료. `DamagePresenter`에 플레이어 직접 피해 VFX 판별 함수를 추가했다. command 호출 연결은 아직 하지 않았다.
- 2026-07-09: 1-C 완료. `ICombatPresentationCommands`에 Damage VFX command를 추가하고 dispatcher/null/test command 구현체를 맞췄다. 실제 전달 경로는 1-D에서 연결한다.
- 2026-07-09: 1-D 완료. `CombatPresentationCommandDispatcher`에서 `RunBattleScreenView`/`RunBattleWorldView`/`EnemyFormationView`를 거쳐 `EnemyFormationSlotView`까지 Damage VFX 요청 전달 경로를 연결했다. Slot의 module 실행은 1-E에서 구현한다.
- 2026-07-09: 1-E 완료. `EnemyFormationSlotView`에 profile-module runner를 추가하고, `DamagePresenter`가 플레이어 직접 피해에서 `PlayerDirectDamage` VFX 요청을 발생시키도록 연결했다.
- 2026-07-10: 2-A 완료. `TintFlashDamageVFXModule`을 추가해 대상 `SpriteRenderer` 색상을 flash color로 전환한 뒤 원래 색상으로 복구하도록 구현했다.
- 2026-07-10: 2-B 검토 중 시각 목표를 정정했다. `SpriteRenderer.color` 기반 구현은 tint flash로 이름을 분리하고, `PlayerDirectDamage`용 HitFlash는 shader 또는 `MaterialPropertyBlock` 기반으로 `FlashAmount`를 tween해 sprite RGB를 white override하고 alpha는 유지하는 별도 module로 구현한다.
- 완료 커밋: `9deb520 feat: 피해 VFX 조합 타입 추가`.
- 다음 작업은 2-B의 white override `HitFlashDamageVFXModule` 신규 구현부터 시작한다.

## Checklist

- [x] 1-A. 피해 VFX 타입과 module 계약 추가 — Codex
- [x] 1-B. `DamagePresenter`에 플레이어 직접 피해 판별 함수만 추가 — Codex
- [x] 1-C. `ICombatPresentationCommands`에 피해 VFX command 추가 — Codex
- [x] 1-D. `RunBattleWorldView` → `EnemyFormationView` → `EnemyFormationSlotView` 전달 경로 연결 — Codex
- [x] 1-E. `EnemyFormationSlotView`에서 profile-module runner 구현 — Codex
- [x] 2-A. `TintFlashDamageVFXModule` 구현 및 임시 연결 — Codex
- [ ] 2-B. white override `HitFlashDamageVFXModule` 신규 구현 — Codex
- [ ] 2-C. `PlayerDirectDamage` set에 white override `HitFlash` 연결 및 Unity 확인 — Codex
- [ ] 3-A. `SlashCutDamageVFXModule` 구현 — Codex
- [ ] 3-B. `SparkParticleDamageVFXModule` 구현 — Codex
- [ ] 3-C. slash prefab, spark particle 연결 및 타이밍 확인 — Codex

## Verification

- [x] `dotnet build SlotRogue.slnx --no-restore` 통과.
- [x] 플레이어 직접 피해에서만 Damage VFX 요청이 발생한다.
- [ ] 상태 피해, 반사 피해, 몬스터 공격 피해에서는 직접 피해 VFX가 발생하지 않는다.
- [x] shield에 전부 막힌 피해는 `PlayerDirectDamage` VFX를 재생하지 않는다.
- [ ] `HitFlash`가 연속 피격, 사망, 다음 몬스터 등장 후에도 색상을 복구한다.
- [ ] `SlashCut`과 `SparkParticle`이 올바른 몬스터 슬롯 위치에서 재생되고 lifetime 뒤 제거된다.

## Handoff Notes

- 1-B는 `DamagePresenter`에 판별 함수만 추가한다. 이 단계에서는 아직 command 호출을 연결하지 않는다.
- 플레이어 직접 피해 판별 기준은 `EffectApplied`, `Phase == Resolving`, 대상이 enemy, `DamageOrigin.DirectAction`, `DamageDealt > 0`이다.
- 1-C 이후부터 기존 presentation command 경로를 수정하므로, 각 단계마다 compile 확인을 권장한다.
- module은 `MonoBehaviour`로 구현하고 `ICombatDamageVFXModule`을 구현한다. `CombatDamageVFXSet.Modules`는 Inspector에서 `MonoBehaviour` 배열로 받되 runner에서 interface 구현 여부를 검증한다.
- 구현 중 누락된 module 참조는 조용히 무시하지 말고 `Debug.LogError` 또는 명확한 검증 실패로 드러낸다.
- `SpriteRenderer.color = Color.white`는 원본 texture 색에 tint를 곱하는 동작이라 몬스터 전체를 흰색으로 치환하지 않는다. `PlayerDirectDamage` HitFlash는 shader/material property로 `finalRgb = lerp(originalRgb, flashColor, flashAmount)`, `finalAlpha = originalAlpha` 형태를 목표로 한다.
- white override flash 구현 시 renderer별 shared material을 직접 수정하지 않는다. 가능하면 `MaterialPropertyBlock`으로 `_FlashAmount` / `_FlashColor` 같은 per-renderer 값을 tween하고, 종료·취소·비활성화 시 0으로 복구한다.
- `TintFlashDamageVFXModule`은 상태 피해나 약한 피격 표현 후보로 보류한다. `PlayerDirectDamage` set에 임시 연결되어 있으면 2-C에서 white override `HitFlashDamageVFXModule`로 교체한다.

## Refactor Follow-ups

- [x] `EnemyFormationSlotView`의 Damage VFX 실행 책임을 `CombatDamageVFXRunner`로 분리한다.
- [ ] `EnemyFormationSlotView`의 Status Effect icon 목록 책임을 별도 View로 분리한다.
- [ ] `EnemyFormationSlotView`의 Intent icon 목록 책임을 별도 View로 분리한다.
- [ ] 구조 정리 여유가 생기면 CombatVisual 생성/행동/사망 연출 책임을 별도 View로 분리한다.
- [ ] 구조 정리 여유가 생기면 HP bar와 shielded HP bar 표시 책임을 별도 View로 분리한다.

## Completion

- **Finished**:
- **Outcome**:
- **Follow-ups**:
