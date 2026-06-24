using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;
using SlotRogue.UI.Combat;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.Combat
{
    public sealed class SlotCombatRequestToCombatEffectsConverterTests
    {
        private SlotCombatRequestToCombatEffectsConverter _converter = null!;

        [SetUp]
        public void SetUp()
        {
            _converter = new SlotCombatRequestToCombatEffectsConverter();
        }

        [Test]
        public void Convert_NullRequest_ReturnsEmpty()
        {
            CombatEffect[] effects = _converter.Convert(null!);

            Assert.That(effects, Is.Empty);
        }

        [Test]
        public void Convert_AllZeroValues_ReturnsEmpty()
        {
            CombatEffect[] effects = _converter.Convert(SlotCombatRequest.Empty);

            Assert.That(effects, Is.Empty);
        }

        [Test]
        public void Convert_ShieldHealAndDamage_FollowsMvpOrder()
        {
            var request = new SlotCombatRequest(
                damage: 4,
                defense: 2,
                attackCount: 2,
                healAmount: 3,
                isCritical: true,
                patternName: "Triple Strike");

            CombatEffect[] effects = _converter.Convert(request);

            Assert.That(effects, Is.EqualTo(new[]
            {
                new CombatEffect(CombatEffectKind.Shield, 2, CombatEffectTarget.Self),
                new CombatEffect(CombatEffectKind.Heal, 3, CombatEffectTarget.Self),
                new CombatEffect(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
                new CombatEffect(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
            }));
        }

        [Test]
        public void Convert_ZeroDefenseAndHeal_SkipsThoseEffects()
        {
            var request = new SlotCombatRequest(5, 0, 1, 0, false, "Attack");

            CombatEffect[] effects = _converter.Convert(request);

            Assert.That(effects, Is.EqualTo(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            }));
        }

        [Test]
        public void Convert_AttackCountZeroWithDamage_EmitsSingleDamage()
        {
            var request = new SlotCombatRequest(7, 0, 0, 0, false, "Single");

            CombatEffect[] effects = _converter.Convert(request);

            Assert.That(effects, Is.EqualTo(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 7, CombatEffectTarget.Enemy),
            }));
        }

        [Test]
        public void Convert_AttackCountRepeatsDamageEffects()
        {
            var request = new SlotCombatRequest(3, 0, 3, 0, false, "Multi");

            CombatEffect[] effects = _converter.Convert(request);

            Assert.That(effects, Has.Length.EqualTo(3));
            Assert.That(effects[0], Is.EqualTo(new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy)));
            Assert.That(effects[1], Is.EqualTo(new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy)));
            Assert.That(effects[2], Is.EqualTo(new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy)));
        }

        [Test]
        public void Convert_SelectedTargetId_AssignsDamageTargets()
        {
            var request = new SlotCombatRequest(3, 0, 2, 0, false, "Multi");
            var selectedTargetId = new CombatParticipantId(101);

            CombatEffect[] effects = _converter.Convert(request, selectedTargetId);

            Assert.That(effects, Has.Length.EqualTo(2));
            Assert.That(effects[0].Target, Is.EqualTo(CombatEffectTarget.SelectedEnemy(selectedTargetId)));
            Assert.That(effects[1].Target, Is.EqualTo(CombatEffectTarget.SelectedEnemy(selectedTargetId)));
        }

        [Test]
        public void Convert_DamageZeroWithAttackCount_SkipsDamage()
        {
            var request = new SlotCombatRequest(0, 1, 2, 2, false, "Support");

            CombatEffect[] effects = _converter.Convert(request);

            Assert.That(effects, Is.EqualTo(new[]
            {
                new CombatEffect(CombatEffectKind.Shield, 1, CombatEffectTarget.Self),
                new CombatEffect(CombatEffectKind.Heal, 2, CombatEffectTarget.Self),
            }));
        }

        [Test]
        public void Convert_StatusEffectToApply_AppendsApplyStatusEffect()
        {
            var request = new SlotCombatRequest(5, 0, 1, 0, false, "Attack");
            var selectedTargetId = new CombatParticipantId(101);

            CombatEffect[] effects = _converter.Convert(
                request,
                selectedTargetId,
                new[]
                {
                    new TargetedStatusEffectSpec(
                        new StatusEffectSpec(
                            StatusEffectKind.Infection,
                            duration: 0,
                            magnitude: 1,
                            StatusStackMode.Stack),
                        CombatTargetMode.SelectedEnemy),
                });

            Assert.That(effects, Has.Length.EqualTo(2));
            Assert.That(effects[0].Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(effects[1].Kind, Is.EqualTo(CombatEffectKind.ApplyStatus));
            Assert.That(effects[1].StatusEffect.Kind, Is.EqualTo(StatusEffectKind.Infection));
            Assert.That(effects[1].Target.ParticipantId, Is.EqualTo(selectedTargetId));
        }

        [Test]
        public void Convert_TargetedStatusEffects_UsesEachRequestedTarget()
        {
            var request = new SlotCombatRequest(5, 0, 1, 0, false, "Attack");
            var selectedTargetId = new CombatParticipantId(101);
            var statusEffects = new[]
            {
                new TargetedStatusEffectSpec(
                    new StatusEffectSpec(
                        StatusEffectKind.Infection,
                        duration: 0,
                        magnitude: 2,
                        StatusStackMode.Stack),
                    CombatTargetMode.SelectedEnemy),
                new TargetedStatusEffectSpec(
                    new StatusEffectSpec(
                        StatusEffectKind.Thorns,
                        duration: 0,
                        magnitude: 3,
                        StatusStackMode.Refresh),
                    CombatTargetMode.Self),
            };

            CombatEffect[] effects = _converter.Convert(
                request,
                selectedTargetId,
                statusEffects);

            Assert.That(effects, Has.Length.EqualTo(3));
            Assert.That(
                effects[1].Target,
                Is.EqualTo(CombatEffectTarget.SelectedEnemy(selectedTargetId)));
            Assert.That(effects[2].Target, Is.EqualTo(CombatEffectTarget.Self));
        }
    }
}
