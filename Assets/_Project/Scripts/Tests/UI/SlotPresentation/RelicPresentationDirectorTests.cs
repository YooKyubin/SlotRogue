using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SlotRogue.UI.SlotPresentation;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Tests.SlotPresentation
{
    public sealed class RelicPresentationDirectorTests
    {
        [Test]
        public void PlayBurstAtAnchor_MultipleRelicsUsesOnlyHierarchyIcon()
        {
            var root = new GameObject("Relic Presentation Panel", typeof(RectTransform));
            var iconObject = new GameObject(
                "Relic Presentation Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            iconObject.transform.SetParent(root.transform, worldPositionStays: false);
            var texture = new Texture2D(1, 1);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));

            try
            {
                RelicPresentationDirector director =
                    root.AddComponent<RelicPresentationDirector>();
                Image hierarchyIcon = iconObject.GetComponent<Image>();
                SetPrivateField(director, "_iconImage", hierarchyIcon);

                var results = new List<SlotRelicTriggerPresentationResult>
                {
                    new("first", "First", sprite, "", ""),
                    new("second", "Second", sprite, "", ""),
                };

                IEnumerator playRoutine = director.PlayBurstAtAnchor(
                    results,
                    null,
                    null,
                    () => false);
                Assert.That(playRoutine.MoveNext(), Is.True);
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                Assert.That(root.transform.GetChild(0), Is.SameAs(iconObject.transform));

                var tweenRoutine = playRoutine.Current as IEnumerator;
                Assert.That(tweenRoutine, Is.Not.Null);
                tweenRoutine.MoveNext();
                director.HideImmediate();
                Assert.That(hierarchyIcon.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
