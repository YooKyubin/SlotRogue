using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;
using UnityEditor;
using UnityEngine;

namespace SlotRogue.Editor.GameFlow
{
    public static class ArtifactCatalogBuilder
    {
        private const string ArtifactFolder = "Assets/_Project/Resources";
        private const string ArtifactDefFolder = "Assets/_Project/Data/Artifacts";

        [MenuItem("SlotRogue/Artifact/Build Catalog (Reset All)")]
        public static void BuildCatalog()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Build Artifact Catalog",
                "Assets/_Project/Resources/ArtifactCatalog.asset과\n" +
                "Assets/_Project/Data/Artifacts/ 내의 유물 에셋을\n" +
                "전부 재생성합니다.\n\n계속하겠습니까?",
                "Build",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            EnsureFolder(ArtifactFolder);
            EnsureFolder(ArtifactDefFolder);

            ArtifactDefinitionSO cherry = CreateOrReplace(
                "cherry", "체리",
                "체리 아이콘 3개 이상 매치 시 피해 +5.",
                ArtifactCategory.Starter, SlotSymbolType.Cherry, 3,
                ArtifactEffectKind.BonusDamage, bonusAmount: 5);

            ArtifactDefinitionSO grape = CreateOrReplace(
                "grape", "포도",
                "포도 아이콘 3개 이상 매치 시 회복 +4.",
                ArtifactCategory.Starter, SlotSymbolType.Grape, 3,
                ArtifactEffectKind.BonusHeal, bonusAmount: 4);

            ArtifactDefinitionSO seven = CreateOrReplace(
                "seven", "세븐",
                "세븐 아이콘 3개 이상 매치 시 방어 +6.",
                ArtifactCategory.Starter, SlotSymbolType.Seven, 3,
                ArtifactEffectKind.BonusDefense, bonusAmount: 6);

            ArtifactDefinitionSO lemon = CreateOrReplace(
                "lemon", "레몬",
                "레몬 아이콘 3개 이상 매치 시 화염 부여 (3턴, 턴당 피해 2).",
                ArtifactCategory.Starter, SlotSymbolType.Lemon, 3,
                ArtifactEffectKind.ApplyBurn, statusDuration: 3, statusMagnitude: 2,
                statusStackBehavior: StatusStackBehavior.Refresh);

            ArtifactDefinitionSO bell = CreateOrReplace(
                "bell", "종",
                "종 아이콘 3개 이상 매치 시 빙결 부여 (적 행동 1턴 스킵).",
                ArtifactCategory.Starter, SlotSymbolType.Bell, 3,
                ArtifactEffectKind.ApplyFreeze, statusDuration: 1,
                statusStackBehavior: StatusStackBehavior.Refresh);

            ArtifactDefinitionSO clover = CreateOrReplace(
                "clover", "네잎클로버",
                "네잎클로버 아이콘 3개 이상 매치 시 독 스택 +1 (스택당 매 턴 피해, 최대 5).",
                ArtifactCategory.Starter, SlotSymbolType.Clover, 3,
                ArtifactEffectKind.ApplyPoison, statusMagnitude: 1,
                statusStackBehavior: StatusStackBehavior.Stack);

            string catalogPath = $"{ArtifactFolder}/ArtifactCatalog.asset";
            ArtifactCatalogSO catalog = AssetDatabase.LoadAssetAtPath<ArtifactCatalogSO>(catalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArtifactCatalogSO>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            var serializedCatalog = new SerializedObject(catalog);
            SerializedProperty artifactsProp = serializedCatalog.FindProperty("_artifacts");
            artifactsProp.ClearArray();

            ArtifactDefinitionSO[] all = { cherry, grape, seven, lemon, bell, clover };

            for (int i = 0; i < all.Length; i++)
            {
                artifactsProp.InsertArrayElementAtIndex(i);
                artifactsProp.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SlotRogue] ArtifactCatalog 빌드 완료. 유물 {all.Length}개.");
        }

        private static ArtifactDefinitionSO CreateOrReplace(
            string id,
            string displayName,
            string description,
            ArtifactCategory category,
            SlotSymbolType targetSymbol,
            int minimumMatchLength,
            ArtifactEffectKind effectKind,
            int bonusAmount = 0,
            int statusDuration = 0,
            int statusMagnitude = 0,
            StatusStackBehavior statusStackBehavior = StatusStackBehavior.Refresh)
        {
            string path = $"{ArtifactDefFolder}/{id}.asset";
            ArtifactDefinitionSO so = AssetDatabase.LoadAssetAtPath<ArtifactDefinitionSO>(path);

            if (so == null)
            {
                so = ScriptableObject.CreateInstance<ArtifactDefinitionSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var serialized = new SerializedObject(so);
            serialized.FindProperty("_artifactId").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_category").enumValueIndex = (int)category;
            serialized.FindProperty("_targetSymbol").enumValueIndex = (int)targetSymbol;
            serialized.FindProperty("_minimumMatchLength").intValue = minimumMatchLength;
            serialized.FindProperty("_effectKind").enumValueIndex = (int)effectKind;
            serialized.FindProperty("_bonusAmount").intValue = bonusAmount;
            serialized.FindProperty("_statusDuration").intValue = statusDuration;
            serialized.FindProperty("_statusMagnitude").intValue = statusMagnitude;
            serialized.FindProperty("_statusStackBehavior").enumValueIndex = (int)statusStackBehavior;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return so;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(path);

                if (!string.IsNullOrEmpty(parent))
                {
                    EnsureFolder(parent);
                }

                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
