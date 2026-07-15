using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SlotRogue.UI.GameFlow;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class EnemyFormationPositionerTests
    {
        [Test]
        public void ApplyLayout_TwoEnemies_MovesSideSlotsToTwoEnemyAnchors()
        {
            using var fixture = new FormationFixture();

            fixture.Positioner.ApplyLayout(fixture.SlotViews, new[] { 0, 2 });

            Assert.That(fixture.SlotViews[0].Root.position, Is.EqualTo(fixture.TwoEnemyAnchors[0].position));
            Assert.That(fixture.SlotViews[2].Root.position, Is.EqualTo(fixture.TwoEnemyAnchors[1].position));
        }

        [Test]
        public void ApplyLayout_ThreeEnemiesAfterTwoEnemies_RestoresThreeEnemyAnchors()
        {
            using var fixture = new FormationFixture();

            fixture.Positioner.ApplyLayout(fixture.SlotViews, new[] { 0, 2 });
            fixture.Positioner.ApplyLayout(fixture.SlotViews, new[] { 0, 1, 2 });

            Assert.That(fixture.SlotViews[0].Root.position, Is.EqualTo(fixture.ThreeEnemyAnchors[0].position));
            Assert.That(fixture.SlotViews[1].Root.position, Is.EqualTo(fixture.ThreeEnemyAnchors[1].position));
            Assert.That(fixture.SlotViews[2].Root.position, Is.EqualTo(fixture.ThreeEnemyAnchors[2].position));
        }

        [Test]
        public void ApplyLayout_OneEnemy_MovesCenterSlotToOneEnemyAnchor()
        {
            using var fixture = new FormationFixture();

            fixture.Positioner.ApplyLayout(fixture.SlotViews, new[] { 1 });

            Assert.That(fixture.SlotViews[1].Root.position, Is.EqualTo(fixture.OneEnemyAnchors[0].position));
        }

        [Test]
        public void ApplyLayout_PreservesSlotParentRotationAndScale()
        {
            using var fixture = new FormationFixture();
            Transform slotRoot = fixture.SlotViews[0].Root;
            Transform parent = slotRoot.parent;
            Quaternion rotation = slotRoot.rotation;
            Vector3 scale = slotRoot.localScale;

            fixture.Positioner.ApplyLayout(fixture.SlotViews, new[] { 0, 2 });

            Assert.That(slotRoot.parent, Is.SameAs(parent));
            Assert.That(slotRoot.rotation, Is.EqualTo(rotation));
            Assert.That(slotRoot.localScale, Is.EqualTo(scale));
        }

        [Test]
        public void ApplyLayout_InvalidAnchorConfiguration_DoesNotPartiallyMoveSlots()
        {
            using var fixture = new FormationFixture();
            Vector3 leftSlotPosition = fixture.SlotViews[0].Root.position;
            Vector3 rightSlotPosition = fixture.SlotViews[2].Root.position;
            SetPrivateField(fixture.Positioner, "_twoEnemyAnchors", new[] { fixture.TwoEnemyAnchors[0] });
            LogAssert.Expect(LogType.Error, "[EnemyFormationPositioner] Anchor count for 2 enemies must be 2.");

            fixture.Positioner.ApplyLayout(fixture.SlotViews, new[] { 0, 2 });

            Assert.That(fixture.SlotViews[0].Root.position, Is.EqualTo(leftSlotPosition));
            Assert.That(fixture.SlotViews[2].Root.position, Is.EqualTo(rightSlotPosition));
        }

        [Test]
        public void ApplyLayout_OutOfRangeSlot_DoesNotPartiallyMoveSlots()
        {
            using var fixture = new FormationFixture();
            Vector3 leftSlotPosition = fixture.SlotViews[0].Root.position;
            Vector3 rightSlotPosition = fixture.SlotViews[2].Root.position;
            LogAssert.Expect(LogType.Error, "[EnemyFormationPositioner] Formation slot 3 is outside the available slot range.");

            fixture.Positioner.ApplyLayout(fixture.SlotViews, new[] { 0, 3 });

            Assert.That(fixture.SlotViews[0].Root.position, Is.EqualTo(leftSlotPosition));
            Assert.That(fixture.SlotViews[2].Root.position, Is.EqualTo(rightSlotPosition));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            field.SetValue(target, value);
        }

        private sealed class FormationFixture : System.IDisposable
        {
            private readonly GameObject _root;

            public FormationFixture()
            {
                _root = new GameObject("Formation Fixture");
                Positioner = _root.AddComponent<EnemyFormationPositioner>();

                var slotRoot = new GameObject("Slots");
                slotRoot.transform.SetParent(_root.transform, false);
                SlotViews = CreateSlotViews(slotRoot.transform);

                var anchorRoot = new GameObject("Anchors");
                anchorRoot.transform.SetParent(_root.transform, false);
                OneEnemyAnchors = new[] { CreateAnchor(anchorRoot.transform, "One Center", 0f) };
                TwoEnemyAnchors = new[]
                {
                    CreateAnchor(anchorRoot.transform, "Two Left", -1f),
                    CreateAnchor(anchorRoot.transform, "Two Right", 1f),
                };
                ThreeEnemyAnchors = new[]
                {
                    CreateAnchor(anchorRoot.transform, "Three Left", -1.8f),
                    CreateAnchor(anchorRoot.transform, "Three Center", 0f),
                    CreateAnchor(anchorRoot.transform, "Three Right", 1.8f),
                };

                SetPrivateField(Positioner, "_oneEnemyAnchors", OneEnemyAnchors);
                SetPrivateField(Positioner, "_twoEnemyAnchors", TwoEnemyAnchors);
                SetPrivateField(Positioner, "_threeEnemyAnchors", ThreeEnemyAnchors);
            }

            public EnemyFormationPositioner Positioner { get; }

            public EnemyFormationSlotView[] SlotViews { get; }

            public Transform[] OneEnemyAnchors { get; }

            public Transform[] TwoEnemyAnchors { get; }

            public Transform[] ThreeEnemyAnchors { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(_root);
            }

            private static EnemyFormationSlotView[] CreateSlotViews(Transform parent)
            {
                var slotViews = new EnemyFormationSlotView[3];
                for (int index = 0; index < slotViews.Length; index++)
                {
                    var slot = new GameObject($"Slot {index}");
                    slot.transform.SetParent(parent, false);
                    slot.transform.localPosition = new Vector3(index - 1, 0f, 0f);
                    slotViews[index] = slot.AddComponent<EnemyFormationSlotView>();
                }

                return slotViews;
            }

            private static Transform CreateAnchor(Transform parent, string anchorName, float x)
            {
                var anchor = new GameObject(anchorName);
                anchor.transform.SetParent(parent, false);
                anchor.transform.localPosition = new Vector3(x, 0f, 0f);
                return anchor.transform;
            }
        }
    }
}
