using NUnit.Framework;
using SlotRogue.Slot.Data;
using SlotRogue.UI.Leaderboard;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Tests.Leaderboard
{
    public sealed class LeaderboardMetadataTests
    {
        [Test]
        public void Payload_RoundTrip_PreservesRunDetails()
        {
            var source = new LeaderboardMetadataPayload(
                12,
                new[] { "S-01", "C-04", "C-04" },
                new[]
                {
                    new LeaderboardSymbolCount(SlotSymbolType.Cherry.ToString(), 9),
                    new LeaderboardSymbolCount(SlotSymbolType.Seven.ToString(), 3),
                },
                "profile-01",
                "허접ㅋ");

            string json = JsonUtility.ToJson(source);
            LeaderboardMetadataPayload parsed = LeaderboardMetadataCodec.Parse(json, 1);

            Assert.That(parsed.SchemaVersion, Is.EqualTo(3));
            Assert.That(parsed.Wave, Is.EqualTo(12));
            Assert.That(parsed.RelicIds, Is.EqualTo(new[] { "S-01", "C-04", "C-04" }));
            Assert.That(parsed.SymbolCounts.Length, Is.EqualTo(2));
            Assert.That(parsed.SymbolCounts[0].Symbol, Is.EqualTo("Cherry"));
            Assert.That(parsed.SymbolCounts[0].Count, Is.EqualTo(9));
            Assert.That(parsed.ProfileIconId, Is.EqualTo("profile-01"));
            Assert.That(parsed.Message, Is.EqualTo("허접ㅋ"));
        }

        [Test]
        public void Parse_MissingMetadata_UsesSafeFallback()
        {
            LeaderboardMetadataPayload parsed = LeaderboardMetadataCodec.Parse(null, 7);

            Assert.That(parsed.Wave, Is.EqualTo(7));
            Assert.That(parsed.RelicIds, Is.Empty);
            Assert.That(parsed.SymbolCounts, Is.Empty);
        }

        [Test]
        public void Parse_V1Metadata_IgnoresLegacyCountryCode()
        {
            const string legacyJson =
                "{\"SchemaVersion\":1,\"CountryCode\":\"KR\",\"Wave\":9,\"RelicIds\":[\"S-01\"]}";

            LeaderboardMetadataPayload parsed =
                LeaderboardMetadataCodec.Parse(legacyJson, 1);

            Assert.That(parsed.SchemaVersion, Is.EqualTo(1));
            Assert.That(parsed.Wave, Is.EqualTo(9));
            Assert.That(parsed.RelicIds, Is.EqualTo(new[] { "S-01" }));
            Assert.That(parsed.SymbolCounts, Is.Empty);
        }

        [TestCase("", false)]
        [TestCase("Player", true)]
        public void PlayerProfile_IsComplete_RequiresNickname(
            string nickname,
            bool expected)
        {
            var profile = new LeaderboardPlayerProfile(nickname);

            Assert.That(profile.IsComplete, Is.EqualTo(expected));
        }

        [Test]
        public void LeaderboardView_DoesNotCreateMissingRuntimeLayout()
        {
            var hostObject = new GameObject("LeaderboardView", typeof(RectTransform));

            try
            {
                LeaderboardView view = hostObject.AddComponent<LeaderboardView>();

                bool ready = view.EnsureRuntimeLayout();

                Assert.That(ready, Is.False);
                Assert.That(hostObject.transform.childCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void LeaderboardView_HidesPlacedPanelWhenProfileIsRequired()
        {
            var hostObject = new GameObject("LeaderboardView", typeof(RectTransform));
            GameObject openButton = CreateButton("Leaderboard Open Button", hostObject.transform);
            GameObject panel = new GameObject("Leaderboard Panel", typeof(RectTransform));
            panel.transform.SetParent(hostObject.transform, false);
            panel.SetActive(true);
            CreateButton("Close Button", panel.transform);
            var entries = new GameObject("Leaderboard Entries", typeof(RectTransform));
            entries.transform.SetParent(panel.transform, false);
            entries.AddComponent<Text>();

            try
            {
                LeaderboardView view = hostObject.AddComponent<LeaderboardView>();
                Assert.That(view.EnsureRuntimeLayout(), Is.True);

                view.Render(new LeaderboardViewState(
                    true,
                    false,
                    true,
                    string.Empty,
                    "A nickname is required before playing.",
                    System.Array.Empty<LeaderboardEntryData>()));

                Assert.That(hostObject.activeSelf, Is.True);
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.activeSelf, Is.False);
                Assert.That(openButton.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void LoginPrefab_ContainsStaticProfileControls()
        {
            const string prefabPath =
                "Assets/_Project/Prefabs/UI/GameFlow/GameStart/20_LogInArea.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<LeaderboardLoginView>(), Is.Not.Null);
            Assert.That(
                FindDescendant(prefab.transform, "Nickname Input")
                    ?.GetComponent<InputField>(),
                Is.Not.Null);
            Assert.That(
                FindDescendant(prefab.transform, "Country Dropdown"),
                Is.Null);
            Assert.That(
                FindDescendant(prefab.transform, "Confirm Button")
                    ?.GetComponent<Button>(),
                Is.Not.Null);
            Assert.That(
                FindDescendant(prefab.transform, "Status Text")
                    ?.GetComponent<Text>(),
                Is.Not.Null);
        }

        private static Transform FindDescendant(
            Transform root,
            string objectName)
        {
            Transform[] descendants =
                root.GetComponentsInChildren<Transform>(includeInactive: true);
            for (int index = 0; index < descendants.Length; index++)
            {
                if (descendants[index].name == objectName)
                {
                    return descendants[index];
                }
            }

            return null;
        }

        private static GameObject CreateButton(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<Image>();
            gameObject.AddComponent<Button>();
            return gameObject;
        }
    }
}
