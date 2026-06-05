# RunBattle 적 포메이션 슬롯 — 2D 월드 전환

**Status**: completed  
**Started**: 2026-06-05  
**Finished**: 2026-06-04  
**Owner**: _(미정)_  
**Related design-docs**: [`game-flow.md`](../../design-docs/game-flow.md), [`combat-core.md`](../../design-docs/combat-core.md)  
**Related ADR**: [ADR-0003](../../adr/0003-combat-presentation-replay.md)  
**Depends on**: [`feature-enemy-formation-slot`](./feature-enemy-formation-slot.md), [`feature-floating-combat-text`](./feature-floating-combat-text.md), [`feature-map-encounter-so`](./feature-map-encounter-so.md)

## Outcome

RunBattle enemy formation slots were moved from Overlay UI slots to world 2D objects. Rebuild now creates a world `BattleArenaRoot`, `FormationSlotsRoot`, three `EnemyFormationSlot` objects, and keeps the Overlay UI for slot machine/player HUD/buttons/floating text.

Delivered:

- `EnemyFormationSlotView` now uses `SpriteRenderer` portrait, World Space Canvas HUD refs, `Collider2D` click input, and a separate `ShakeGroup`.
- `GameFlowScenePrefabBuilder` creates world formation slots and keeps the `EventSystem` outside the Overlay Canvas so world clicks continue when UI panels are hidden.
- `RunBattleView` keeps the public formation slot API while laying out slots as world transforms.
- `DamagePresenter` converts world damage anchors back into the Overlay `FloatingTextRoot`.
- Legacy UI `EnemyFormationSlot` is superseded by `Assets/_Project/Prefabs/World/GameFlow/EnemyFormationSlot.prefab`.

## Validation

Confirmed by manual Unity checks:

- Rebuild creates world slots and compiles without errors.
- 1-enemy and 3-enemy encounters bind slots and portraits.
- `monster == null` / portrait null keeps placeholder behavior.
- Spin damage floating text appears near the slot damage anchor.
- Slot click changes target and selected HUD state.

Deferred:

- 1080x1920 reference overlap between world slots and slot machine remains a visual/layout follow-up for art review.

## Follow-ups

- Tune RunBattle world slot position, scale, and camera framing with the art team.
- Add camera shake + `ShakeGroup` behavior and HUD shake on/off comparison in a separate plan.
- Add boss/elite slot variants after layout direction is finalized.
