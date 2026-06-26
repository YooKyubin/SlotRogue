using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BattleSystemEnemyCombatantTests
    {
        private BattleSystem _battle = null!;

        [SetUp]
        public void SetUp()
        {
            _battle = new BattleSystem();
        }

        [Test]
        public void StartBattle_WithEnemyCombatant_PreparesInitialPlan()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            EnemyCombatant combatant = Combatant(
                enemy,
                Plan(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy));

            _battle.StartBattle(player, new[] { combatant });

            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            Assert.That(upcomingTurn.ParticipantId.Value, Is.EqualTo(enemy.Id.Value));
            AssertPlan(upcomingTurn.Plan, CombatEffectKind.Damage, 4);
        }

        [Test]
        public void ApplyPlayerTurn_ExecutesStoredPlanAndAdvancesNextPlan()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 30);
            EnemyCombatant combatant = Combatant(
                enemy,
                Plan(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
                Plan(CombatEffectKind.Shield, 5, CombatEffectTarget.Self));
            _battle.StartBattle(player, new[] { combatant });

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(player.CurrentHp, Is.EqualTo(27));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            AssertPlan(upcomingTurn.Plan, CombatEffectKind.Shield, 5);
        }

        [Test]
        public void ApplyPlayerTurn_SkippedEnemyAction_StillAdvancesNextPlan()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 30);
            EnemyCombatant combatant = Combatant(
                enemy,
                Plan(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
                Plan(CombatEffectKind.Damage, 7, CombatEffectTarget.Enemy));
            _battle.StartBattle(player, new[] { combatant });

            _battle.ApplyPlayerTurn(new[]
            {
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(StatusEffectKind.Freeze, duration: 1, magnitude: 0, StatusStackMode.Refresh),
                    CombatEffectTarget.Enemy),
            });

            Assert.That(player.CurrentHp, Is.EqualTo(30));
            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.ActionSkipped &&
                e.StatusEffectKind == StatusEffectKind.Freeze));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            AssertPlan(upcomingTurn.Plan, CombatEffectKind.Damage, 7);
        }

        [Test]
        public void ApplyPlayerTurn_MultipleEnemyCombatants_ExecutesInRosterOrder()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy0 = Enemy(id: 100, maxHp: 20);
            CombatParticipant enemy1 = Enemy(id: 101, maxHp: 20);
            _battle.StartBattle(
                player,
                new[]
                {
                    Combatant(enemy0, Plan(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy)),
                    Combatant(enemy1, Plan(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy)),
                });

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            int[] damageAmounts = _battle.Events
                .Where(e =>
                    e.Kind == CombatEventKind.EffectApplied &&
                    e.Effect.Kind == CombatEffectKind.Damage &&
                    e.IsPlayerParticipant)
                .Select(e => e.Effect.Amount)
                .ToArray();
            Assert.That(damageAmounts, Is.EqualTo(new[] { 2, 5 }));
            Assert.That(player.CurrentHp, Is.EqualTo(23));
        }

        [Test]
        public void ApplyPlayerTurn_EnemyActionStartedPrecedesEffectAppliedAndActionCompleted()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            _battle.StartBattle(
                player,
                new[] { Combatant(enemy, NamedPlan("Attack", CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy)) });

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            int startedIndex = FindEventIndex(CombatEventKind.ActionStarted, enemy.Id);
            int effectIndex = FindEventIndex(CombatEventKind.EffectApplied, enemy.Id);
            int completedIndex = FindEventIndex(CombatEventKind.ActionCompleted, enemy.Id);
            Assert.That(startedIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(effectIndex, Is.GreaterThan(startedIndex));
            Assert.That(completedIndex, Is.GreaterThan(effectIndex));
            Assert.That(_battle.Events[startedIndex].Phase, Is.EqualTo(BattlePhase.EnemyTurn));
            Assert.That(_battle.Events[startedIndex].SourceParticipantId.Value, Is.EqualTo(enemy.Id.Value));
            Assert.That(_battle.Events[startedIndex].ActionName, Is.EqualTo("Attack"));
        }

        [Test]
        public void ApplyPlayerTurn_EnemyActionEndingBattleRecordsActionCompletedBeforeBattleEnded()
        {
            CombatParticipant player = Player(maxHp: 4);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            _battle.StartBattle(
                player,
                new[] { Combatant(enemy, NamedPlan("Attack", CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy)) });

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            int startedIndex = FindEventIndex(CombatEventKind.ActionStarted, enemy.Id);
            int effectIndex = FindEventIndex(CombatEventKind.EffectApplied, enemy.Id);
            int completedIndex = FindEventIndex(CombatEventKind.ActionCompleted, enemy.Id);
            int battleEndedIndex = FindFirstEventIndex(CombatEventKind.BattleEnded);
            Assert.That(startedIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(effectIndex, Is.GreaterThan(startedIndex));
            Assert.That(completedIndex, Is.GreaterThan(effectIndex));
            Assert.That(battleEndedIndex, Is.GreaterThan(completedIndex));
            Assert.That(_battle.Events[completedIndex].ActionName, Is.EqualTo("Attack"));
            Assert.That(_battle.Events.Count(e =>
                e.Kind == CombatEventKind.ActionCompleted &&
                e.SourceParticipantId.Value == enemy.Id.Value), Is.EqualTo(1));
            Assert.That(_battle.EndReason, Is.EqualTo(BattleEndReason.Defeat));
        }

        [Test]
        public void ApplyPlayerTurn_DirectTargetingRetargetsWhenSelectedEnemyDies()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy0 = Enemy(id: 100, maxHp: 5);
            CombatParticipant enemy1 = Enemy(id: 101, maxHp: 20);
            _battle.StartBattle(
                player,
                new[]
                {
                    Combatant(enemy0, Plan(CombatEffectKind.Damage, 0, CombatEffectTarget.Enemy)),
                    Combatant(enemy1, Plan(CombatEffectKind.Damage, 0, CombatEffectTarget.Enemy)),
                });

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        5,
                        CombatEffectTarget.SelectedEnemy(enemy0.Id)),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        4,
                        CombatEffectTarget.SelectedEnemy(enemy0.Id)),
                },
                selectedTargetId: enemy0.Id);

            Assert.That(enemy0.IsDead, Is.True);
            Assert.That(enemy1.CurrentHp, Is.EqualTo(16));
        }

        [Test]
        public void ApplyPlayerTurn_PlayerEffectsApplyInInputOrder()
        {
            CombatParticipant player = new(
                maxHp: 30,
                currentHp: 20,
                shield: 0,
                new CombatParticipantId(1),
                CombatTeam.Player);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            _battle.StartBattle(
                player,
                new[] { Combatant(enemy, Plan(CombatEffectKind.Damage, 0, CombatEffectTarget.Enemy)) });

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(CombatEffectKind.Shield, 3, CombatEffectTarget.Self),
                    new CombatEffect(CombatEffectKind.Damage, 4, CombatEffectTarget.SelectedEnemy(enemy.Id)),
                    new CombatEffect(CombatEffectKind.Heal, 2, CombatEffectTarget.Self),
                },
                selectedTargetId: enemy.Id);

            CombatEffectKind[] resolvingEffects = _battle.Events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Phase == BattlePhase.Resolving)
                .Select(e => e.Effect.Kind)
                .ToArray();
            Assert.That(
                resolvingEffects,
                Is.EqualTo(new[]
                {
                    CombatEffectKind.Shield,
                    CombatEffectKind.Damage,
                    CombatEffectKind.Heal,
                }));
            Assert.That(player.CurrentHp, Is.EqualTo(22));
            Assert.That(enemy.CurrentHp, Is.EqualTo(16));
        }

        private static EnemyCombatant Combatant(CombatParticipant enemy, params EnemyActionPlan[] plans)
        {
            return new EnemyCombatant(enemy, new FixedSequenceEnemyActionPlanner(plans));
        }

        private static EnemyActionPlan Plan(
            CombatEffectKind kind,
            int amount,
            CombatEffectTarget target)
        {
            return new EnemyActionPlan(new[] { new CombatEffect(kind, amount, target) });
        }

        private static EnemyActionPlan NamedPlan(
            string actionName,
            CombatEffectKind kind,
            int amount,
            CombatEffectTarget target)
        {
            return EnemyActionPlan.FromActions(new[]
            {
                new EnemyPlannedAction(
                    new EnemyActionKey(1),
                    actionName,
                    new[]
                    {
                        EnemyActionEffect.FromCombatEffect(new CombatEffect(kind, amount, target)),
                    }),
            });
        }

        private int FindEventIndex(CombatEventKind kind, CombatParticipantId sourceParticipantId)
        {
            for (int index = 0; index < _battle.Events.Count; index++)
            {
                CombatEvent combatEvent = _battle.Events[index];
                if (combatEvent.Kind == kind &&
                    combatEvent.SourceParticipantId.Value == sourceParticipantId.Value)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindFirstEventIndex(CombatEventKind kind)
        {
            for (int index = 0; index < _battle.Events.Count; index++)
            {
                if (_battle.Events[index].Kind == kind)
                {
                    return index;
                }
            }

            return -1;
        }

        private static void AssertPlan(
            EnemyActionPlan plan,
            CombatEffectKind expectedKind,
            int expectedAmount)
        {
            IReadOnlyList<CombatEffect> effects = plan.Effects;
            Assert.That(effects.Count, Is.EqualTo(1));
            Assert.That(effects[0].Kind, Is.EqualTo(expectedKind));
            Assert.That(effects[0].Amount, Is.EqualTo(expectedAmount));
        }

        private static CombatParticipant Player(int maxHp)
        {
            return new CombatParticipant(maxHp, maxHp, shield: 0, new CombatParticipantId(1), CombatTeam.Player);
        }

        private static CombatParticipant Enemy(int id, int maxHp)
        {
            return new CombatParticipant(maxHp, maxHp, shield: 0, new CombatParticipantId(id), CombatTeam.Enemy);
        }
    }

    public sealed class CombatActionResolverTests
    {
        [Test]
        public void ResolvePlayerEffects_ApplyStatus_UsesStatusEffectEngine()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy();
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };

            resolver.ResolvePlayerEffects(
                new[]
                {
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Burn,
                            duration: 1,
                            magnitude: 2,
                            StatusStackMode.Refresh),
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(enemy.StatusEffects, Has.Count.EqualTo(1));
            Assert.That(enemy.StatusEffects[0].Kind, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.StatusApplied &&
                e.TargetParticipantId.Value == enemy.Id.Value &&
                e.StatusEffectKind == StatusEffectKind.Burn), Is.True);
            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.ApplyStatus &&
                e.TargetParticipantId.Value == enemy.Id.Value), Is.True);
        }

        [Test]
        public void ResolvePlayerEffects_DirectDamage_AppliesOutgoingBeforeIncomingModifiers()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 20);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            var outgoingModifier = new AddOutgoingDamageComponent(2);
            var incomingModifier = new MultiplyIncomingDamageComponent(2);
            ApplyStatusWithComponents(player, outgoingModifier);
            ApplyStatusWithComponents(enemy, incomingModifier);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        3,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(enemy.CurrentHp, Is.EqualTo(10));
            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage &&
                e.Effect.Amount == 10 &&
                e.Effect.DamageOrigin == DamageOrigin.DirectAction &&
                e.ApplyResult.DamageDealt == 10), Is.True);
            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.ActionCompleted &&
                e.Effect.Kind == CombatEffectKind.Damage &&
                e.Effect.Amount == 3), Is.True);
            Assert.That(outgoingModifier.LastContext.CurrentDamage, Is.EqualTo(3));
            Assert.That(outgoingModifier.LastContext.SourceParticipantId.Value, Is.EqualTo(player.Id.Value));
            Assert.That(outgoingModifier.LastContext.TargetParticipantId.Value, Is.EqualTo(enemy.Id.Value));
            Assert.That(outgoingModifier.LastContext.DamageOrigin, Is.EqualTo(DamageOrigin.DirectAction));
            Assert.That(incomingModifier.LastContext.CurrentDamage, Is.EqualTo(5));
            Assert.That(incomingModifier.LastContext.SourceParticipantId.Value, Is.EqualTo(player.Id.Value));
            Assert.That(incomingModifier.LastContext.TargetParticipantId.Value, Is.EqualTo(enemy.Id.Value));
            Assert.That(incomingModifier.LastContext.DamageOrigin, Is.EqualTo(DamageOrigin.DirectAction));
        }

        [Test]
        public void ResolvePlayerEffects_MultipleDamageEffects_UseSingleActionState()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy();
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            var usage = new CountDamageModifierUsageComponent();
            ApplyStatusWithComponents(
                player,
                StatusEffectKind.Burn,
                magnitude: 1,
                new AddSnapshotMagnitudeOutgoingDamageComponent(),
                usage);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Burn,
                            duration: 1,
                            magnitude: 10,
                            StatusStackMode.Refresh),
                        CombatEffectTarget.Self),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        2,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        2,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            int[] appliedDamage = events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Effect.Kind == CombatEffectKind.Damage)
                .Select(e => e.Effect.Amount)
                .ToArray();

            Assert.That(appliedDamage, Is.EqualTo(new[] { 3, 3 }));
            Assert.That(enemy.CurrentHp, Is.EqualTo(14));
            Assert.That(player.StatusEffects[0].Magnitude, Is.EqualTo(10));
            Assert.That(usage.ConsumedCount, Is.Zero);
        }

        [Test]
        public void ResolveEnemyPlannedActions_MultiHit_UsesActionStartModifierSnapshot()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy();
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatusWithComponents(
                enemy,
                StatusEffectKind.Burn,
                magnitude: 1,
                new AddSnapshotMagnitudeOutgoingDamageComponent());

            resolver.ResolveEnemyPlannedActions(
                new[]
                {
                    new EnemyPlannedAction(
                        new EnemyActionKey(1),
                        "Double Attack",
                        new[]
                        {
                            EnemyActionEffect.FromCombatEffect(
                                CombatEffect.ApplyStatus(
                                    new StatusEffectSpec(
                                        StatusEffectKind.Burn,
                                        duration: 1,
                                        magnitude: 10,
                                        StatusStackMode.Refresh),
                                    CombatEffectTarget.Self)),
                            EnemyActionEffect.FromCombatEffect(
                                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy)),
                            EnemyActionEffect.FromCombatEffect(
                                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy)),
                        }),
                },
                enemy,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: player.Id,
                BattlePhase.EnemyTurn,
                events,
                shouldEndBattle: () => false);

            int[] appliedDamage = events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Effect.Kind == CombatEffectKind.Damage)
                .Select(e => e.Effect.Amount)
                .ToArray();

            Assert.That(appliedDamage, Is.EqualTo(new[] { 3, 3 }));
            Assert.That(player.CurrentHp, Is.EqualTo(24));
            Assert.That(enemy.StatusEffects[0].Magnitude, Is.EqualTo(10));
        }

        [Test]
        public void ResolveEnemyPlannedActions_MultiHit_ConsumesUsedModifierOncePerAction()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy();
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            var modifier = new ConsumedOutgoingDamageComponent(1);
            ApplyStatusWithComponents(enemy, StatusEffectKind.Freeze, modifier);

            resolver.ResolveEnemyPlannedActions(
                new[]
                {
                    new EnemyPlannedAction(
                        new EnemyActionKey(1),
                        "Double Attack",
                        new[]
                        {
                            EnemyActionEffect.FromCombatEffect(
                                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy)),
                            EnemyActionEffect.FromCombatEffect(
                                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy)),
                        }),
                },
                enemy,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: player.Id,
                BattlePhase.EnemyTurn,
                events,
                shouldEndBattle: () => false);

            Assert.That(modifier.ConsumedCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolveEnemyPlannedActions_DifferentActions_UseSeparateActionStates()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy();
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            var usage = new CountDamageModifierUsageComponent();
            ApplyStatusWithComponents(
                enemy,
                StatusEffectKind.Burn,
                magnitude: 1,
                new AddSnapshotMagnitudeOutgoingDamageComponent(),
                usage);

            resolver.ResolveEnemyPlannedActions(
                new[]
                {
                    new EnemyPlannedAction(
                        new EnemyActionKey(1),
                        "Attack 1",
                        new[]
                        {
                            EnemyActionEffect.FromCombatEffect(
                                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy)),
                        }),
                    new EnemyPlannedAction(
                        new EnemyActionKey(2),
                        "Attack 2",
                        new[]
                        {
                            EnemyActionEffect.FromCombatEffect(
                                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy)),
                        }),
                },
                enemy,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: player.Id,
                BattlePhase.EnemyTurn,
                events,
                shouldEndBattle: () => false);

            int[] appliedDamage = events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Effect.Kind == CombatEffectKind.Damage)
                .Select(e => e.Effect.Amount)
                .ToArray();

            Assert.That(appliedDamage, Is.EqualTo(new[] { 3, 3 }));
            Assert.That(usage.ConsumedCount, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePlayerEffects_DirectDamageFullyBlockedByShield_RecordsModifierUsage()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(shield: 10);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            var usage = new CountDamageModifierUsageComponent();
            ApplyStatusWithComponents(player, new AddOutgoingDamageComponent(1), usage);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        2,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.ApplyResult.DamageDealt, Is.Zero);
            Assert.That(damageEvent.ApplyResult.ShieldConsumed, Is.EqualTo(3));
            Assert.That(enemy.CurrentHp, Is.EqualTo(20));
            Assert.That(enemy.Shield, Is.EqualTo(7));
            Assert.That(usage.ConsumedCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_NoValidTarget_DoesNotRecordModifierUsage()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant defeatedEnemy = CombatParticipantFactory.CreateEnemy(currentHp: 0);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [defeatedEnemy.Id.Value] = defeatedEnemy,
            };
            var usage = new CountDamageModifierUsageComponent();
            ApplyStatusWithComponents(player, new AddOutgoingDamageComponent(1), usage);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        2,
                        CombatEffectTarget.SelectedEnemy(defeatedEnemy.Id)),
                },
                player,
                player,
                new[] { defeatedEnemy },
                participantsById,
                selectedTargetId: defeatedEnemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage), Is.False);
            Assert.That(usage.ConsumedCount, Is.Zero);
        }

        [Test]
        public void ResolvePlayerEffects_StatusRemovedAndReplacedBeforeActionComplete_DoesNotConsumeOldOrNewStatus()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy();
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            var oldUsage = new CountDamageModifierUsageComponent();
            StatusEffectInstance oldStatus = ApplyStatusWithComponents(
                player,
                StatusEffectKind.Burn,
                magnitude: 1,
                new AddSnapshotMagnitudeOutgoingDamageComponent(),
                oldUsage);
            var newUsage = new CountDamageModifierUsageComponent();
            bool replaced = false;

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        2,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () =>
                {
                    if (!replaced)
                    {
                        player.RemoveStatusEffect(oldStatus);
                        ApplyStatusWithComponents(
                            player,
                            StatusEffectKind.Burn,
                            magnitude: 5,
                            new AddSnapshotMagnitudeOutgoingDamageComponent(),
                            newUsage);
                        replaced = true;
                    }

                    return false;
                });

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(3));
            Assert.That(enemy.CurrentHp, Is.EqualTo(17));
            Assert.That(player.StatusEffects, Has.Count.EqualTo(1));
            Assert.That(player.StatusEffects[0].Magnitude, Is.EqualTo(5));
            Assert.That(oldUsage.ConsumedCount, Is.Zero);
            Assert.That(newUsage.ConsumedCount, Is.Zero);
        }

        [Test]
        public void ResolvePlayerEffects_BattleEndsMidAction_CompletesUsedModifiersOnce()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 3);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            var usage = new CountDamageModifierUsageComponent();
            ApplyStatusWithComponents(player, new AddOutgoingDamageComponent(1), usage);

            bool ended = resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        2,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        2,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => enemy.IsDead);

            Assert.That(ended, Is.True);
            Assert.That(events.Count(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage), Is.EqualTo(1));
            Assert.That(enemy.IsDead, Is.True);
            Assert.That(usage.ConsumedCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_NonDirectOrigins_ReturnUnchangedDamageAndPreserveOrigin()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy();
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            var usage = new CountDamageModifierUsageComponent();
            ApplyStatusWithComponents(player, new AddOutgoingDamageComponent(5), usage);
            ApplyStatusWithComponents(enemy, new MultiplyIncomingDamageComponent(3));

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        4,
                        CombatEffectTarget.SelectedEnemy(enemy.Id),
                        DamageOrigin.Status),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        4,
                        CombatEffectTarget.SelectedEnemy(enemy.Id),
                        DamageOrigin.Reflection),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEffect[] appliedEffects = events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Effect.Kind == CombatEffectKind.Damage)
                .Select(e => e.Effect)
                .ToArray();

            Assert.That(appliedEffects.Select(e => e.Amount).ToArray(), Is.EqualTo(new[] { 4, 4 }));
            Assert.That(
                appliedEffects.Select(e => e.DamageOrigin).ToArray(),
                Is.EqualTo(new[] { DamageOrigin.Status, DamageOrigin.Reflection }));
            Assert.That(enemy.CurrentHp, Is.EqualTo(12));
            Assert.That(usage.ConsumedCount, Is.Zero);
        }

        [TestCase(10, 8)]
        [TestCase(3, 3)]
        [TestCase(1, 1)]
        [TestCase(0, 0)]
        public void ResolvePlayerEffects_Weaken_ReducesDirectDamageWithCeiling(
            int damage,
            int expectedDamage)
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        damage,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(expectedDamage));
            Assert.That(damageEvent.ApplyResult.DamageDealt, Is.EqualTo(expectedDamage));
            Assert.That(enemy.CurrentHp, Is.EqualTo(30 - expectedDamage));
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(damage > 0 ? 1 : 2));
        }

        [Test]
        public void ResolvePlayerEffects_WeakenMultiHit_AppliesToAllHitsAndConsumesOnce()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        5,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            int[] appliedDamage = events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Effect.Kind == CombatEffectKind.Damage)
                .Select(e => e.Effect.Amount)
                .ToArray();
            Assert.That(appliedDamage, Is.EqualTo(new[] { 8, 4 }));
            Assert.That(enemy.CurrentHp, Is.EqualTo(18));
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(1));
            CombatEvent statusValueChanged = events.Single(
                e => e.Kind == CombatEventKind.StatusValueChanged);
            Assert.That(statusValueChanged.TargetParticipantId.Value, Is.EqualTo(player.Id.Value));
            Assert.That(statusValueChanged.StatusEffectKind, Is.EqualTo(StatusEffectKind.Weaken));
            Assert.That(statusValueChanged.StatusStackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_WeakenAllEnemies_ConsumesOnce()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy1 = CombatParticipantFactory.CreateEnemy(id: 100, maxHp: 30);
            CombatParticipant enemy2 = CombatParticipantFactory.CreateEnemy(id: 101, maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy1.Id.Value] = enemy1,
                [enemy2.Id.Value] = enemy2,
            };
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        new CombatEffectTarget(CombatTargetMode.AllEnemies)),
                },
                player,
                player,
                new[] { enemy1, enemy2 },
                participantsById,
                selectedTargetId: enemy1.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(enemy1.CurrentHp, Is.EqualTo(22));
            Assert.That(enemy2.CurrentHp, Is.EqualTo(22));
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_Weaken_DoesNotApplyOrConsumeForNonDirectOrigins()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id),
                        DamageOrigin.Status),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id),
                        DamageOrigin.Reflection),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            int[] appliedDamage = events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Effect.Kind == CombatEffectKind.Damage)
                .Select(e => e.Effect.Amount)
                .ToArray();
            Assert.That(appliedDamage, Is.EqualTo(new[] { 10, 10 }));
            Assert.That(enemy.CurrentHp, Is.EqualTo(10));
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePlayerEffects_WeakenAppliedMidAction_AppliesNextAction()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };

            var firstEvents = new List<CombatEvent>();
            resolver.ResolvePlayerEffects(
                new[]
                {
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Weaken,
                            duration: 0,
                            magnitude: 2,
                            StatusStackMode.Refresh),
                        CombatEffectTarget.Self),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                firstEvents,
                shouldEndBattle: () => false);

            var secondEvents = new List<CombatEvent>();
            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                secondEvents,
                shouldEndBattle: () => false);

            Assert.That(enemy.CurrentHp, Is.EqualTo(12));
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_WeakenRefreshMidAction_KeepsRefreshedStack()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 1);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Weaken,
                            duration: 0,
                            magnitude: 5,
                            StatusStackMode.Refresh),
                        CombatEffectTarget.Self),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(8));
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(5));
        }

        [Test]
        public void ResolvePlayerEffects_WeakenBlockedByShield_ConsumesOnce()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30, shield: 20);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(8));
            Assert.That(damageEvent.ApplyResult.DamageDealt, Is.Zero);
            Assert.That(damageEvent.ApplyResult.ShieldConsumed, Is.EqualTo(8));
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_WeakenNoValidTarget_DoesNotConsume()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(currentHp: 0);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage), Is.False);
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePlayerEffects_WeakenOneUse_RemovesStatusAndEmitsExpired()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 1);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(player.StatusEffects, Is.Empty);
            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.StatusExpired &&
                e.StatusEffectKind == StatusEffectKind.Weaken &&
                e.TargetParticipantId.Value == player.Id.Value), Is.True);
        }

        [Test]
        public void ResolveEnemyPlannedActions_WeakenEnemySource_AppliesAndConsumes()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy();
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Weaken, magnitude: 2);

            resolver.ResolveEnemyPlannedActions(
                new[]
                {
                    new EnemyPlannedAction(
                        new EnemyActionKey(1),
                        "Attack",
                        new[]
                        {
                            EnemyActionEffect.FromCombatEffect(
                                new CombatEffect(CombatEffectKind.Damage, 10, CombatEffectTarget.Enemy)),
                        }),
                },
                enemy,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: player.Id,
                BattlePhase.EnemyTurn,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(8));
            Assert.That(player.CurrentHp, Is.EqualTo(22));
            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_WeakenAndVulnerable_AppliesOutgoingBeforeIncoming()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 2);
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        6,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(6));
            Assert.That(damageEvent.AppliedStatusModifiers, Has.Count.EqualTo(2));
            Assert.That(
                damageEvent.AppliedStatusModifiers.Select(modifier => modifier.Kind),
                Is.EqualTo(new[] { StatusEffectKind.Weaken, StatusEffectKind.Vulnerable }));
            Assert.That(
                damageEvent.AppliedStatusModifiers.Select(modifier => modifier.OwnerParticipantId.Value),
                Is.EqualTo(new[] { player.Id.Value, enemy.Id.Value }));
            Assert.That(
                damageEvent.AppliedStatusModifiers.Select(modifier => modifier.OwnerTeam),
                Is.EqualTo(new[] { CombatTeam.Player, CombatTeam.Enemy }));
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(1));
            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_WeakenRefreshAndStack_UseCommonStackRules()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            var events = new List<CombatEvent>();

            statusEffectEngine.ApplyStatus(
                new StatusEffectSpec(
                    StatusEffectKind.Weaken,
                    duration: 0,
                    magnitude: 2,
                    StatusStackMode.Refresh),
                player,
                BattlePhase.Resolving,
                events);
            statusEffectEngine.ApplyStatus(
                new StatusEffectSpec(
                    StatusEffectKind.Weaken,
                    duration: 0,
                    magnitude: 5,
                    StatusStackMode.Refresh),
                player,
                BattlePhase.Resolving,
                events);

            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(5));

            statusEffectEngine.ApplyStatus(
                new StatusEffectSpec(
                    StatusEffectKind.Weaken,
                    duration: 0,
                    magnitude: 3,
                    StatusStackMode.Stack),
                player,
                BattlePhase.Resolving,
                events);

            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(8));
        }

        [TestCase(10, 12)]
        [TestCase(3, 4)]
        [TestCase(1, 2)]
        [TestCase(0, 0)]
        public void ResolvePlayerEffects_Vulnerable_IncreasesDirectDamageWithCeiling(
            int damage,
            int expectedDamage)
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        damage,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(expectedDamage));
            Assert.That(damageEvent.ApplyResult.DamageDealt, Is.EqualTo(expectedDamage));
            Assert.That(enemy.CurrentHp, Is.EqualTo(30 - expectedDamage));
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableMultiHit_AppliesToAllHitsAndConsumesOnce()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            int[] appliedDamage = events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Effect.Kind == CombatEffectKind.Damage)
                .Select(e => e.Effect.Amount)
                .ToArray();

            Assert.That(appliedDamage, Is.EqualTo(new[] { 12, 12 }));
            Assert.That(enemy.CurrentHp, Is.EqualTo(6));
            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(1));
            CombatEvent statusValueChanged = events.Single(
                e => e.Kind == CombatEventKind.StatusValueChanged);
            Assert.That(statusValueChanged.TargetParticipantId.Value, Is.EqualTo(enemy.Id.Value));
            Assert.That(statusValueChanged.StatusEffectKind, Is.EqualTo(StatusEffectKind.Vulnerable));
            Assert.That(statusValueChanged.StatusStackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableTargets_AreConsumedIndependently()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy1 = CombatParticipantFactory.CreateEnemy(id: 100, maxHp: 30);
            CombatParticipant enemy2 = CombatParticipantFactory.CreateEnemy(id: 101, maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy1.Id.Value] = enemy1,
                [enemy2.Id.Value] = enemy2,
            };
            ApplyStatus(enemy1, StatusEffectKind.Vulnerable, magnitude: 2);
            ApplyStatus(enemy2, StatusEffectKind.Vulnerable, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        new CombatEffectTarget(CombatTargetMode.AllEnemies)),
                },
                player,
                player,
                new[] { enemy1, enemy2 },
                participantsById,
                selectedTargetId: enemy1.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(enemy1.CurrentHp, Is.EqualTo(18));
            Assert.That(enemy2.CurrentHp, Is.EqualTo(18));
            Assert.That(enemy1.StatusEffects.Single().StackCount, Is.EqualTo(1));
            Assert.That(enemy2.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_Vulnerable_DoesNotApplyOrConsumeForNonDirectOrigins()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id),
                        DamageOrigin.Status),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id),
                        DamageOrigin.Reflection),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            int[] appliedDamage = events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Effect.Kind == CombatEffectKind.Damage)
                .Select(e => e.Effect.Amount)
                .ToArray();

            Assert.That(appliedDamage, Is.EqualTo(new[] { 10, 10 }));
            Assert.That(enemy.CurrentHp, Is.EqualTo(10));
            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableAppliedMidAction_AppliesNextAction()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };

            resolver.ResolvePlayerEffects(
                new[]
                {
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Vulnerable,
                            duration: 0,
                            magnitude: 2,
                            StatusStackMode.Refresh),
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            int[] appliedDamage = events
                .Where(e => e.Kind == CombatEventKind.EffectApplied && e.Effect.Kind == CombatEffectKind.Damage)
                .Select(e => e.Effect.Amount)
                .ToArray();

            Assert.That(appliedDamage, Is.EqualTo(new[] { 10, 12 }));
            Assert.That(enemy.CurrentHp, Is.EqualTo(8));
            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableRefresh_UpdatesStackCount()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 1);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Vulnerable,
                            duration: 0,
                            magnitude: 5,
                            StatusStackMode.Refresh),
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(5));
            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.StatusApplied &&
                e.StatusEffectKind == StatusEffectKind.Vulnerable &&
                e.StatusStackCount == 5), Is.True);
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableStack_AddsStackCount()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 1);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Vulnerable,
                            duration: 0,
                            magnitude: 5,
                            StatusStackMode.Stack),
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(6));
            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.StatusApplied &&
                e.StatusEffectKind == StatusEffectKind.Vulnerable &&
                e.StatusStackCount == 6), Is.True);
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableRefreshWithSameValue_IncrementsRevisionAndSkipsCurrentUsage()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            StatusEffectInstance vulnerable = ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 1);
            int revisionBefore = vulnerable.Revision;

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Vulnerable,
                            duration: 0,
                            magnitude: 1,
                            StatusStackMode.Refresh),
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(12));
            Assert.That(vulnerable.Revision, Is.GreaterThan(revisionBefore));
            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableRefreshMidAction_UsesOriginalSnapshotAndKeepsRefreshedStack()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 1);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Vulnerable,
                            duration: 0,
                            magnitude: 5,
                            StatusStackMode.Refresh),
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(12));
            Assert.That(enemy.CurrentHp, Is.EqualTo(18));
            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(5));
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableBlockedByShield_ConsumesOnce()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30, shield: 20);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(12));
            Assert.That(damageEvent.ApplyResult.DamageDealt, Is.Zero);
            Assert.That(damageEvent.ApplyResult.ShieldConsumed, Is.EqualTo(12));
            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableNoValidTarget_DoesNotConsume()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(currentHp: 0);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 2);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage), Is.False);
            Assert.That(enemy.StatusEffects.Single().StackCount, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePlayerEffects_VulnerableOneUse_RemovesStatusAndEmitsExpired()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer();
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy(maxHp: 30);
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(enemy, StatusEffectKind.Vulnerable, magnitude: 1);

            resolver.ResolvePlayerEffects(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        10,
                        CombatEffectTarget.SelectedEnemy(enemy.Id)),
                },
                player,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: enemy.Id,
                BattlePhase.Resolving,
                events,
                shouldEndBattle: () => false);

            Assert.That(enemy.StatusEffects, Is.Empty);
            Assert.That(events.Any(e =>
                e.Kind == CombatEventKind.StatusExpired &&
                e.StatusEffectKind == StatusEffectKind.Vulnerable &&
                e.TargetParticipantId.Value == enemy.Id.Value), Is.True);
        }

        [Test]
        public void ResolveEnemyPlannedActions_VulnerablePlayerTarget_AppliesAndConsumes()
        {
            var effectApplicator = new EffectApplicator();
            var statusEffectEngine = new StatusEffectEngine(effectApplicator);
            var resolver = new CombatActionResolver(effectApplicator, statusEffectEngine);
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant enemy = CombatParticipantFactory.CreateEnemy();
            var events = new List<CombatEvent>();
            var participantsById = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
            ApplyStatus(player, StatusEffectKind.Vulnerable, magnitude: 2);

            resolver.ResolveEnemyPlannedActions(
                new[]
                {
                    new EnemyPlannedAction(
                        new EnemyActionKey(1),
                        "Attack",
                        new[]
                        {
                            EnemyActionEffect.FromCombatEffect(
                                new CombatEffect(CombatEffectKind.Damage, 10, CombatEffectTarget.Enemy)),
                        }),
                },
                enemy,
                player,
                new[] { enemy },
                participantsById,
                selectedTargetId: player.Id,
                BattlePhase.EnemyTurn,
                events,
                shouldEndBattle: () => false);

            CombatEvent damageEvent = events.Single(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            Assert.That(damageEvent.Effect.Amount, Is.EqualTo(12));
            Assert.That(player.CurrentHp, Is.EqualTo(18));
            Assert.That(player.StatusEffects.Single().StackCount, Is.EqualTo(1));
        }

        private static StatusEffectInstance ApplyStatusWithComponents(
            CombatParticipant participant,
            params IStatusEffectComponent[] components)
        {
            return ApplyStatusWithComponents(participant, StatusEffectKind.Burn, components);
        }

        private static StatusEffectInstance ApplyStatusWithComponents(
            CombatParticipant participant,
            StatusEffectKind kind,
            params IStatusEffectComponent[] components)
        {
            return participant.ApplyStatusEffect(
                new StatusEffectInstance(
                    kind,
                    remainingTurns: 1,
                    magnitude: 0,
                    stackCount: 1,
                    components),
                StatusStackMode.Refresh);
        }

        private static StatusEffectInstance ApplyStatusWithComponents(
            CombatParticipant participant,
            StatusEffectKind kind,
            int magnitude,
            params IStatusEffectComponent[] components)
        {
            return participant.ApplyStatusEffect(
                new StatusEffectInstance(
                    kind,
                    remainingTurns: 1,
                    magnitude: magnitude,
                    stackCount: 1,
                    components),
                StatusStackMode.Refresh);
        }

        private static StatusEffectInstance ApplyStatus(
            CombatParticipant participant,
            StatusEffectKind kind,
            int magnitude)
        {
            var spec = new StatusEffectSpec(kind, duration: 0, magnitude, StatusStackMode.Refresh);
            return participant.ApplyStatusEffect(new StatusEffectFactory().Create(spec), spec.StackMode);
        }

        private sealed class AddOutgoingDamageComponent : StatusEffectComponent, IOutgoingDamageModifier
        {
            private readonly int _amount;

            public AddOutgoingDamageComponent(int amount)
            {
                _amount = amount;
            }

            public DamageModifierContext LastContext { get; private set; }

            public int ModifyDamage(int currentDamage, in DamageModifierContext context)
            {
                LastContext = context;
                return currentDamage + _amount;
            }
        }

        private sealed class AddSnapshotMagnitudeOutgoingDamageComponent : StatusEffectComponent, IOutgoingDamageModifier
        {
            public int ModifyDamage(int currentDamage, in DamageModifierContext context)
            {
                return currentDamage + context.StatusSnapshot.Magnitude;
            }
        }

        private sealed class ConsumedOutgoingDamageComponent :
            StatusEffectComponent,
            IOutgoingDamageModifier,
            IDamageModifierUsageHandler
        {
            private readonly int _amount;

            public ConsumedOutgoingDamageComponent(int amount)
            {
                _amount = amount;
            }

            public int ConsumedCount { get; private set; }

            public int ModifyDamage(int currentDamage, in DamageModifierContext context)
            {
                return currentDamage + _amount;
            }

            public void OnDamageModifierUsed(StatusEffectContext context)
            {
                ConsumedCount++;
            }
        }

        private sealed class CountDamageModifierUsageComponent : StatusEffectComponent, IDamageModifierUsageHandler
        {
            public int ConsumedCount { get; private set; }

            public void OnDamageModifierUsed(StatusEffectContext context)
            {
                ConsumedCount++;
            }
        }

        private sealed class MultiplyIncomingDamageComponent : StatusEffectComponent, IIncomingDamageModifier
        {
            private readonly int _multiplier;

            public MultiplyIncomingDamageComponent(int multiplier)
            {
                _multiplier = multiplier;
            }

            public DamageModifierContext LastContext { get; private set; }

            public int ModifyDamage(int currentDamage, in DamageModifierContext context)
            {
                LastContext = context;
                return currentDamage * _multiplier;
            }
        }
    }
}
