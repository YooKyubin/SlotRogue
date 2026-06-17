using NUnit.Framework;
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
                new[] { "S-01", "C-04", "C-04" });

            string json = JsonUtility.ToJson(source);
            LeaderboardMetadataPayload parsed = LeaderboardMetadataCodec.Parse(json, 1);

            Assert.That(parsed.SchemaVersion, Is.EqualTo(2));
            Assert.That(parsed.Wave, Is.EqualTo(12));
            Assert.That(parsed.RelicIds, Is.EqualTo(new[] { "S-01", "C-04", "C-04" }));
        }

        [Test]
        public void Parse_MissingMetadata_UsesSafeFallback()
        {
            LeaderboardMetadataPayload parsed = LeaderboardMetadataCodec.Parse(null, 7);

            Assert.That(parsed.Wave, Is.EqualTo(7));
            Assert.That(parsed.RelicIds, Is.Empty);
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
        public void LeaderboardView_RebuildsStaleGeneratedLayoutAsHidden()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var hostObject = new GameObject("LeaderboardView", typeof(RectTransform));
            hostObject.transform.SetParent(canvasObject.transform, false);
            var staleButton = new GameObject("Leaderboard Open Button", typeof(RectTransform));
            staleButton.transform.SetParent(hostObject.transform, false);
            var stalePanel = new GameObject("Leaderboard Panel", typeof(RectTransform));
            stalePanel.transform.SetParent(hostObject.transform, false);
            stalePanel.SetActive(true);

            try
            {
                LeaderboardView view = hostObject.AddComponent<LeaderboardView>();

                view.EnsureRuntimeLayout();

                Transform panel = hostObject.transform.Find("Leaderboard Panel");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.gameObject.activeSelf, Is.False);
                Assert.That(hostObject.transform.childCount, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void LeaderboardView_HidesRuntimePanelWhenProfileIsRequired()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var hostObject = new GameObject("LeaderboardView", typeof(RectTransform));
            hostObject.transform.SetParent(canvasObject.transform, false);
            hostObject.SetActive(false);

            try
            {
                LeaderboardView view = hostObject.AddComponent<LeaderboardView>();
                hostObject.SetActive(true);
                view.EnsureRuntimeLayout();

                view.Render(new LeaderboardViewState(
                    true,
                    false,
                    true,
                    string.Empty,
                    "A nickname is required before playing.",
                    System.Array.Empty<LeaderboardEntryData>()));

                Transform panel = hostObject.transform.Find("Leaderboard Panel");
                Assert.That(hostObject.activeSelf, Is.True);
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
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
    }
}
