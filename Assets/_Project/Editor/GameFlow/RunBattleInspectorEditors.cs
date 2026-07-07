using SlotRogue.UI.Combat.Presentation;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.SlotPresentation;
using UnityEditor;
using UnityEngine;

namespace SlotRogue.Editor.GameFlow
{
    [CustomEditor(typeof(RunBattleScreenView))]
    internal sealed class RunBattleScreenViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "RunBattleScreenView is the generated screen facade. It renders ViewModel snapshots through small child views.",
                MessageType.Info);

            DrawBindingSummary();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Required References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_playerHudView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_slotBoardView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_actionView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_shopView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_shopButton"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_currencyView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_presentationOverlayView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_worldView"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Shop Shutter", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_slotScreenShutters"), includeChildren: true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_shopFlipDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_shopShutterFoldedVisibleHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_shopShutterRevealDelay"));

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Root"))
                {
                    Selection.activeObject = ((RunBattleScreenView)target).gameObject;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBindingSummary()
        {
            DrawObjectStatus("Player HUD view", serializedObject.FindProperty("_playerHudView"));
            DrawObjectStatus("Slot board view", serializedObject.FindProperty("_slotBoardView"));
            DrawObjectStatus("Action view", serializedObject.FindProperty("_actionView"));
            DrawObjectStatus("Shop view", serializedObject.FindProperty("_shopView"));
            DrawObjectStatus("Shop button", serializedObject.FindProperty("_shopButton"));
            DrawObjectStatus("Currency view", serializedObject.FindProperty("_currencyView"));
            DrawObjectStatus("Presentation overlay", serializedObject.FindProperty("_presentationOverlayView"));
            DrawObjectStatus("World view", serializedObject.FindProperty("_worldView"));
        }

        private static void DrawObjectStatus(
            string label,
            SerializedProperty property)
        {
            bool serialized = property != null && property.objectReferenceValue != null;
            DrawStatus(label, serialized ? "Ready" : "Missing", serialized);
        }

        private static void DrawStatus(string label, string value, bool ready)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(170f));
                GUIStyle style = ready ? EditorStyles.label : EditorStyles.boldLabel;
                Color previousColor = GUI.color;
                GUI.color = ready ? previousColor : new Color(1f, 0.62f, 0.35f, 1f);
                EditorGUILayout.LabelField(value, style);
                GUI.color = previousColor;
            }
        }
    }

    [CustomEditor(typeof(BattleSceneHost), true)]
    internal sealed class BattleSceneHostEditor : UnityEditor.Editor
    {
        private bool _showAdvancedReferences;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "BattleSceneHost owns scene references and assembles the battle flow. Turn order lives in the pure C# BattleFlowController.",
                MessageType.Info);

            EditorGUILayout.LabelField("Presentation Views", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_floatingTextLayerView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_turnBannerView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_shopDescriptionView"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Encounter Selection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The WaveSchedule provides tier/cycle and EncounterSelector selects from the EncounterTable.",
                MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_waveScheduleDefinition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_encounterTable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_encounterBalanceSettings"));

            EditorGUILayout.Space(6f);
            DrawBindingSummary();

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Refresh Local References"))
            {
                RefreshReferences();
            }

            _showAdvancedReferences = EditorGUILayout.Foldout(
                _showAdvancedReferences,
                "Advanced Scene References",
                toggleOnLabelClick: true);

            if (_showAdvancedReferences)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_view"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_waveScheduleDefinition"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_encounterTable"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_encounterBalanceSettings"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_shopDescriptionView"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_floatingTextLayerView"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_turnBannerView"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_spinLeverView"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_slotMachineFrameView"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_slotPresentationManager"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBindingSummary()
        {
            var compositionRoot = (BattleSceneHost)target;
            Transform root = compositionRoot.transform.root;
            DrawObjectStatus(
                "Screen view",
                serializedObject.FindProperty("_view"),
                root != null && root.GetComponentInChildren<RunBattleScreenView>(true) != null);
            DrawObjectStatus(
                "Floating text layer",
                serializedObject.FindProperty("_floatingTextLayerView"),
                root != null && root.GetComponentInChildren<FloatingCombatTextLayerView>(true) != null);
            DrawObjectStatus(
                "Turn banner view",
                serializedObject.FindProperty("_turnBannerView"),
                root != null && root.GetComponentInChildren<TurnBannerView>(true) != null);
            DrawObjectStatus(
                "Shop description",
                serializedObject.FindProperty("_shopDescriptionView"),
                false);
            DrawObjectStatus(
                "Spin lever",
                serializedObject.FindProperty("_spinLeverView"),
                root != null && root.GetComponentInChildren<SlotLeverView>(true) != null);
            DrawObjectStatus(
                "Slot machine frame",
                serializedObject.FindProperty("_slotMachineFrameView"),
                root != null && root.GetComponentInChildren<SlotMachineFrameView>(true) != null);
            DrawObjectStatus(
                "Slot presentation",
                serializedObject.FindProperty("_slotPresentationManager"),
                root != null && root.GetComponentInChildren<SlotPresentationManager>(true) != null);
        }

        private void RefreshReferences()
        {
            var compositionRoot = (BattleSceneHost)target;
            Transform root = compositionRoot.transform.root;

            Undo.RecordObject(compositionRoot, "Refresh BattleSceneHost References");

            SerializedProperty view = serializedObject.FindProperty("_view");
            SerializedProperty floatingTextLayer = serializedObject.FindProperty("_floatingTextLayerView");
            SerializedProperty turnBanner = serializedObject.FindProperty("_turnBannerView");
            SerializedProperty lever = serializedObject.FindProperty("_spinLeverView");
            SerializedProperty slotMachineFrame = serializedObject.FindProperty("_slotMachineFrameView");
            SerializedProperty slotPresentation = serializedObject.FindProperty("_slotPresentationManager");

            view.objectReferenceValue =
                root != null ? root.GetComponentInChildren<RunBattleScreenView>(true) : null;
            floatingTextLayer.objectReferenceValue =
                root != null ? root.GetComponentInChildren<FloatingCombatTextLayerView>(true) : null;
            turnBanner.objectReferenceValue =
                root != null ? root.GetComponentInChildren<TurnBannerView>(true) : null;
            lever.objectReferenceValue =
                root != null ? root.GetComponentInChildren<SlotLeverView>(true) : null;
            slotMachineFrame.objectReferenceValue =
                root != null ? root.GetComponentInChildren<SlotMachineFrameView>(true) : null;
            slotPresentation.objectReferenceValue =
                root != null ? root.GetComponentInChildren<SlotPresentationManager>(true) : null;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(compositionRoot);
        }

        private static void DrawObjectStatus(
            string label,
            SerializedProperty property,
            bool canAutoResolve)
        {
            bool serialized = property != null && property.objectReferenceValue != null;
            bool ready = serialized || canAutoResolve;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(170f));
                GUIStyle style = ready ? EditorStyles.label : EditorStyles.boldLabel;
                Color previousColor = GUI.color;
                GUI.color = ready ? previousColor : new Color(1f, 0.62f, 0.35f, 1f);
                EditorGUILayout.LabelField(serialized ? "Ready" : ready ? "Auto" : "Missing", style);
                GUI.color = previousColor;
            }
        }
    }

}
