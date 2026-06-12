using System.Collections.Generic;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace SlotRogue.Editor.Slot
{
    [CustomEditor(typeof(SlotPatternCatalogAsset))]
    public sealed class SlotPatternCatalogAssetEditor : UnityEditor.Editor
    {
        private const string CatalogFolder = "Assets/_Project/Data";
        private const string CatalogPath = "Assets/_Project/Data/SlotPatternCatalog.asset";
        private const float CellSize = 30f;
        private const float CellGap = 4f;
        private const float BoardPadding = 8f;

        private static readonly Color EmptyCellColor = new Color(0.15f, 0.17f, 0.2f, 1f);
        private static readonly Color FixedCellColor = new Color(1f, 0.72f, 0.2f, 1f);
        private static readonly Color HorizontalPreviewColor = new Color(0.42f, 0.66f, 1f, 1f);
        private static readonly Color InvalidCellColor = new Color(0.92f, 0.23f, 0.23f, 1f);
        private static readonly Color BoardBackgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
        private static readonly Color BorderColor = new Color(0.05f, 0.05f, 0.06f, 1f);
        private static readonly GUIContent EntriesLabel = new GUIContent("Pattern Entries");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var catalog = (SlotPatternCatalogAsset)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset To Default Patterns"))
                {
                    Undo.RecordObject(catalog, "Reset Slot Pattern Catalog");
                    catalog.ResetToDefaults();
                    EditorUtility.SetDirty(catalog);
                }

                if (GUILayout.Button("Sort By Order Index"))
                {
                    Undo.RecordObject(catalog, "Sort Slot Pattern Catalog");
                    catalog.SortByOrderIndex();
                    EditorUtility.SetDirty(catalog);
                }
            }

            EditorGUILayout.Space(8f);
            DrawEntries(serializedObject.FindProperty("_entries"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            DrawValidation(catalog);
        }

        [MenuItem("SlotRogue/Slot Patterns/Create Default Catalog Asset")]
        public static void CreateDefaultCatalogAsset()
        {
            EnsureFolder(CatalogFolder);

            SlotPatternCatalogAsset existing = AssetDatabase.LoadAssetAtPath<SlotPatternCatalogAsset>(CatalogPath);

            if (existing != null)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Reset Slot Pattern Catalog",
                    "SlotPatternCatalog.asset already exists. Reset it to the current default patterns?",
                    "Reset",
                    "Cancel");

                if (!confirmed)
                {
                    Selection.activeObject = existing;
                    return;
                }

                Undo.RecordObject(existing, "Reset Slot Pattern Catalog");
                existing.ResetToDefaults();
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                Selection.activeObject = existing;
                return;
            }

            SlotPatternCatalogAsset catalog = SlotPatternCatalogAsset.CreateDefaultCatalog();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = catalog;
        }

        [MenuItem("SlotRogue/Addressables/Configure Runtime Assets")]
        public static void ConfigureAddressables()
        {
            ConfigureAddressables(logSuccess: true);
        }

        private static bool ConfigureAddressables(bool logSuccess)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[SlotRogue] AddressableAssetSettings is missing.");
                return false;
            }

            string catalogGuid = AssetDatabase.AssetPathToGUID(CatalogPath);
            if (string.IsNullOrEmpty(catalogGuid))
            {
                Debug.LogError($"[SlotRogue] Slot pattern catalog is missing at '{CatalogPath}'.");
                return false;
            }

            AddressableAssetEntry existingEntry = settings.FindAssetEntry(catalogGuid);
            bool changed = existingEntry == null || existingEntry.parentGroup != settings.DefaultGroup;
            AddressableAssetEntry entry =
                settings.CreateOrMoveEntry(catalogGuid, settings.DefaultGroup, false, false);

            if (entry.address != SlotPatternCatalog.Address)
            {
                entry.address = SlotPatternCatalog.Address;
                changed = true;
            }

            changed |= entry.SetLabel("default", true, true, false);

            if (settings.ActivePlayModeDataBuilderIndex != 0)
            {
                settings.ActivePlayModeDataBuilderIndex = 0;
                changed = true;
            }

            if (settings.BuildAddressablesWithPlayerBuild !=
                AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer)
            {
                settings.BuildAddressablesWithPlayerBuild =
                    AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
                changed = true;
            }

            if (changed)
            {
                settings.SetDirty(
                    AddressableAssetSettings.ModificationEvent.EntryModified,
                    entry,
                    true,
                    true);
                EditorUtility.SetDirty(settings);
                EditorUtility.SetDirty(settings.DefaultGroup);
                AssetDatabase.SaveAssets();
            }

            if (logSuccess)
            {
                Debug.Log($"[SlotRogue] Addressables configured: {SlotPatternCatalog.Address}");
            }

            return true;
        }

        [MenuItem("SlotRogue/Addressables/Build Player Content")]
        public static void BuildAddressables()
        {
            if (!ConfigureAddressables(logSuccess: false))
            {
                return;
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"[SlotRogue] Addressables build failed: {result.Error}");
                return;
            }

            Debug.Log("[SlotRogue] Addressables player content build completed.");
        }

        private static void DrawValidation(SlotPatternCatalogAsset catalog)
        {
            List<string> messages = catalog.ValidateEntries();

            if (messages.Count == 0)
            {
                EditorGUILayout.HelpBox("Catalog validation passed.", MessageType.Info);
                return;
            }

            for (int index = 0; index < messages.Count; index++)
            {
                EditorGUILayout.HelpBox(messages[index], MessageType.Warning);
            }
        }

        private void DrawEntries(SerializedProperty entriesProperty)
        {
            if (entriesProperty == null)
            {
                EditorGUILayout.HelpBox("Entries property was not found.", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(EntriesLabel, EditorStyles.boldLabel);

                if (GUILayout.Button("Add", GUILayout.Width(72f)))
                {
                    entriesProperty.InsertArrayElementAtIndex(entriesProperty.arraySize);
                    SerializedProperty newEntry = entriesProperty.GetArrayElementAtIndex(entriesProperty.arraySize - 1);
                    InitializeNewEntry(newEntry, entriesProperty.arraySize - 1);
                }
            }

            EditorGUI.indentLevel++;

            for (int index = 0; index < entriesProperty.arraySize; index++)
            {
                SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);
                DrawEntry(entriesProperty, entryProperty, index);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawEntry(SerializedProperty entriesProperty, SerializedProperty entryProperty, int index)
        {
            SerializedProperty patternIdProperty = entryProperty.FindPropertyRelative("_patternId");
            SerializedProperty displayNameProperty = entryProperty.FindPropertyRelative("_displayName");
            SerializedProperty enabledProperty = entryProperty.FindPropertyRelative("_enabled");
            SerializedProperty matchKindProperty = entryProperty.FindPropertyRelative("_matchKind");

            string displayName = !string.IsNullOrWhiteSpace(displayNameProperty.stringValue)
                ? displayNameProperty.stringValue
                : "Unnamed Pattern";
            string patternId = !string.IsNullOrWhiteSpace(patternIdProperty.stringValue)
                ? patternIdProperty.stringValue
                : "empty-id";
            string title = $"{index:00}. {displayName} ({patternId})";

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    entryProperty.isExpanded = EditorGUILayout.Foldout(entryProperty.isExpanded, title, true);
                    enabledProperty.boolValue = EditorGUILayout.ToggleLeft("Enabled", enabledProperty.boolValue, GUILayout.Width(86f));

                    GUI.enabled = index > 0;
                    if (GUILayout.Button("Up", GUILayout.Width(42f)))
                    {
                        entriesProperty.MoveArrayElement(index, index - 1);
                    }

                    GUI.enabled = index < entriesProperty.arraySize - 1;
                    if (GUILayout.Button("Down", GUILayout.Width(54f)))
                    {
                        entriesProperty.MoveArrayElement(index, index + 1);
                    }

                    GUI.enabled = true;

                    if (GUILayout.Button("Duplicate", GUILayout.Width(78f)))
                    {
                        entriesProperty.InsertArrayElementAtIndex(index);
                        entriesProperty.GetArrayElementAtIndex(index).isExpanded = true;
                    }

                    if (GUILayout.Button("Delete", GUILayout.Width(58f)))
                    {
                        entriesProperty.DeleteArrayElementAtIndex(index);
                    }
                }

                DrawBoardPreview(entryProperty);

                if (!entryProperty.isExpanded)
                {
                    return;
                }

                EditorGUILayout.Space(4f);
                DrawEntryFields(entryProperty, matchKindProperty);
            }
        }

        private void DrawEntryFields(SerializedProperty entryProperty, SerializedProperty matchKindProperty)
        {
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("_patternId"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("_displayName"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("_orderIndex"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("_multiplier"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("_rank"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("_isJackpot"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("_includeInSpinEvaluation"), new GUIContent("Evaluate In Spin"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("_includeInForcedPatternPool"), new GUIContent("Can Be Forced"));
            EditorGUILayout.PropertyField(matchKindProperty);

            SlotPatternMatchKind matchKind = (SlotPatternMatchKind)matchKindProperty.enumValueIndex;

            if (matchKind == SlotPatternMatchKind.HorizontalRun)
            {
                EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("_horizontalLength"));
                EditorGUILayout.HelpBox("Horizontal Run patterns are evaluated dynamically on every row. The board below previews the run length.", MessageType.Info);
                return;
            }

            SerializedProperty cellsProperty = entryProperty.FindPropertyRelative("_cells");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Cells"))
                {
                    cellsProperty.ClearArray();
                }

                if (GUILayout.Button("Fill All"))
                {
                    FillAllCells(cellsProperty);
                }
            }

            EditorGUILayout.PropertyField(cellsProperty, new GUIContent("Cells (Advanced)"), true);
        }

        private static void DrawBoardPreview(SerializedProperty entryProperty)
        {
            SerializedProperty matchKindProperty = entryProperty.FindPropertyRelative("_matchKind");
            SlotPatternMatchKind matchKind = (SlotPatternMatchKind)matchKindProperty.enumValueIndex;
            SerializedProperty cellsProperty = entryProperty.FindPropertyRelative("_cells");
            SerializedProperty horizontalLengthProperty = entryProperty.FindPropertyRelative("_horizontalLength");
            bool editable = matchKind == SlotPatternMatchKind.FixedCells;
            float boardWidth = BoardPadding * 2f + SlotSpinResult.Columns * CellSize + (SlotSpinResult.Columns - 1) * CellGap;
            float boardHeight = BoardPadding * 2f + SlotSpinResult.Rows * CellSize + (SlotSpinResult.Rows - 1) * CellGap;
            Rect boardRect = GUILayoutUtility.GetRect(boardWidth, boardHeight);
            boardRect.x += EditorGUI.indentLevel * 14f;
            boardRect.width = boardWidth;

            EditorGUI.DrawRect(boardRect, BoardBackgroundColor);

            for (int row = 0; row < SlotSpinResult.Rows; row++)
            {
                for (int column = 0; column < SlotSpinResult.Columns; column++)
                {
                    Rect cellRect = new Rect(
                        boardRect.x + BoardPadding + column * (CellSize + CellGap),
                        boardRect.y + BoardPadding + row * (CellSize + CellGap),
                        CellSize,
                        CellSize);
                    int cellIndex = FindCellIndex(cellsProperty, column, row);
                    bool selected = cellIndex >= 0;
                    bool invalid = selected && !IsCellInBounds(cellsProperty.GetArrayElementAtIndex(cellIndex));
                    bool horizontalPreview = matchKind == SlotPatternMatchKind.HorizontalRun && column < Mathf.Clamp(horizontalLengthProperty.intValue, 1, SlotSpinResult.Columns);
                    Color fillColor = invalid
                        ? InvalidCellColor
                        : selected
                            ? FixedCellColor
                            : horizontalPreview
                                ? HorizontalPreviewColor
                                : EmptyCellColor;

                    EditorGUI.DrawRect(cellRect, BorderColor);
                    Rect innerRect = new Rect(cellRect.x + 2f, cellRect.y + 2f, cellRect.width - 4f, cellRect.height - 4f);
                    EditorGUI.DrawRect(innerRect, fillColor);

                    string label = $"{column},{row}";
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(innerRect, label, labelStyle);

                    if (editable && Event.current.type == EventType.MouseDown && Event.current.button == 0 && innerRect.Contains(Event.current.mousePosition))
                    {
                        ToggleCell(cellsProperty, column, row);
                        GUI.changed = true;
                        Event.current.Use();
                    }
                }
            }
        }

        private static void InitializeNewEntry(SerializedProperty entryProperty, int index)
        {
            entryProperty.FindPropertyRelative("_enabled").boolValue = true;
            entryProperty.FindPropertyRelative("_includeInSpinEvaluation").boolValue = true;
            entryProperty.FindPropertyRelative("_includeInForcedPatternPool").boolValue = false;
            entryProperty.FindPropertyRelative("_matchKind").enumValueIndex = (int)SlotPatternMatchKind.FixedCells;
            entryProperty.FindPropertyRelative("_patternId").stringValue = $"pattern-{index + 1}";
            entryProperty.FindPropertyRelative("_displayName").stringValue = $"Pattern {index + 1}";
            entryProperty.FindPropertyRelative("_orderIndex").intValue = index;
            entryProperty.FindPropertyRelative("_multiplier").floatValue = 1f;
            entryProperty.FindPropertyRelative("_rank").enumValueIndex = 0;
            entryProperty.FindPropertyRelative("_isJackpot").boolValue = false;
            entryProperty.FindPropertyRelative("_horizontalLength").intValue = 3;
            entryProperty.FindPropertyRelative("_cells").ClearArray();
            entryProperty.isExpanded = true;
        }

        private static void ToggleCell(SerializedProperty cellsProperty, int column, int row)
        {
            int existingIndex = FindCellIndex(cellsProperty, column, row);

            if (existingIndex >= 0)
            {
                cellsProperty.DeleteArrayElementAtIndex(existingIndex);
                return;
            }

            int newIndex = cellsProperty.arraySize;
            cellsProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty cellProperty = cellsProperty.GetArrayElementAtIndex(newIndex);
            cellProperty.FindPropertyRelative("_col").intValue = column;
            cellProperty.FindPropertyRelative("_row").intValue = row;
        }

        private static void FillAllCells(SerializedProperty cellsProperty)
        {
            cellsProperty.ClearArray();

            for (int row = 0; row < SlotSpinResult.Rows; row++)
            {
                for (int column = 0; column < SlotSpinResult.Columns; column++)
                {
                    int index = cellsProperty.arraySize;
                    cellsProperty.InsertArrayElementAtIndex(index);
                    SerializedProperty cellProperty = cellsProperty.GetArrayElementAtIndex(index);
                    cellProperty.FindPropertyRelative("_col").intValue = column;
                    cellProperty.FindPropertyRelative("_row").intValue = row;
                }
            }
        }

        private static int FindCellIndex(SerializedProperty cellsProperty, int column, int row)
        {
            for (int index = 0; index < cellsProperty.arraySize; index++)
            {
                SerializedProperty cellProperty = cellsProperty.GetArrayElementAtIndex(index);

                if (cellProperty.FindPropertyRelative("_col").intValue == column &&
                    cellProperty.FindPropertyRelative("_row").intValue == row)
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool IsCellInBounds(SerializedProperty cellProperty)
        {
            int column = cellProperty.FindPropertyRelative("_col").intValue;
            int row = cellProperty.FindPropertyRelative("_row").intValue;
            return column >= 0 &&
                column < SlotSpinResult.Columns &&
                row >= 0 &&
                row < SlotSpinResult.Rows;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
