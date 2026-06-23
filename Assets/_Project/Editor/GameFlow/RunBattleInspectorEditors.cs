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
        private bool _showAdvancedReferences;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "RunBattleScreenView is the generated screen facade. It renders ViewModel snapshots through small child views.",
                MessageType.Info);

            DrawBindingSummary();

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Auto References"))
                {
                    RefreshReferences();
                }

                if (GUILayout.Button("Select Root"))
                {
                    Selection.activeObject = ((RunBattleScreenView)target).gameObject;
                }
            }

            _showAdvancedReferences = EditorGUILayout.Foldout(
                _showAdvancedReferences,
                "Advanced Serialized References",
                toggleOnLabelClick: true);

            if (_showAdvancedReferences)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_playerHudView"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_slotBoardView"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_actionView"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_presentationOverlayView"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_worldView"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBindingSummary()
        {
            var view = (RunBattleScreenView)target;
            Transform root = view.transform.root;

            DrawObjectStatus(
                "Player HUD view",
                serializedObject.FindProperty("_playerHudView"),
                view.GetComponentInChildren<RunBattlePlayerHudView>(true) != null);
            DrawObjectStatus(
                "Slot board view",
                serializedObject.FindProperty("_slotBoardView"),
                view.GetComponentInChildren<RunBattleSlotBoardView>(true) != null);
            DrawObjectStatus(
                "Action view",
                serializedObject.FindProperty("_actionView"),
                view.GetComponentInChildren<RunBattleActionView>(true) != null);
            DrawObjectStatus(
                "Presentation overlay",
                serializedObject.FindProperty("_presentationOverlayView"),
                view.GetComponentInChildren<RunBattlePresentationOverlayView>(true) != null);
            DrawObjectStatus(
                "World view",
                serializedObject.FindProperty("_worldView"),
                root != null && root.GetComponentInChildren<RunBattleWorldView>(true) != null);

            int formationSlotCount = root != null
                ? root.GetComponentsInChildren<EnemyFormationSlotView>(true).Length
                : 0;
            DrawStatus(
                "Formation slots",
                formationSlotCount >= 3 ? $"{formationSlotCount}/3" : $"{formationSlotCount}/3 missing",
                formationSlotCount >= 3);
        }

        private void RefreshReferences()
        {
            var view = (RunBattleScreenView)target;
            Undo.RecordObject(view, "Refresh RunBattleScreenView References");
            view.EnsureReferences();
            EditorUtility.SetDirty(view);
            serializedObject.Update();
        }

        private static void DrawObjectStatus(
            string label,
            SerializedProperty property,
            bool canAutoResolve)
        {
            bool serialized = property != null && property.objectReferenceValue != null;
            bool ready = serialized || canAutoResolve;
            DrawStatus(label, serialized ? "Ready" : ready ? "Auto" : "Missing", ready);
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

    [CustomEditor(typeof(BattleSceneCompositionRoot), true)]
    internal sealed class BattleSceneCompositionRootEditor : UnityEditor.Editor
    {
        private bool _showAdvancedReferences;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "BattleSceneCompositionRoot owns scene references and assembles the battle flow. Turn order lives in the pure C# BattleFlowController.",
                MessageType.Info);

            EditorGUILayout.LabelField("Presentation Views", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_floatingTextLayerView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_turnBannerView"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Temporary Test Override", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "TEMPORARY TEST HOOK: Assign a MonsterDefinition here to bypass the tier-based encounter builder while verifying MonsterDefinition/MonsterTurnPattern assets.",
                MessageType.Warning);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_devMonsterDefinitionOverride"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Encounter Selection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Assign an EncounterTable when Dev Monster Definition Override is empty. " +
                "The WaveSchedule provides tier/cycle and EncounterSelector selects from the table.",
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
            var compositionRoot = (BattleSceneCompositionRoot)target;
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
            var compositionRoot = (BattleSceneCompositionRoot)target;
            Transform root = compositionRoot.transform.root;

            Undo.RecordObject(compositionRoot, "Refresh BattleSceneCompositionRoot References");

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
