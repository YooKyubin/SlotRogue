using System;
using System.Collections.Generic;
using SlotRogue.Data.GameFlow;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SlotRogue.Editor.GameFlow
{
    public static class GameFlowScenePrefabBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI/GameFlow";
        private const string EnemyFormationSlotPrefabName = "EnemyFormationSlot";
        private const int FormationSlotCount = 3;
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const float MapWidth = 760f;
        private const float MapHeight = 950f;
        private const float NodeWidth = 138f;
        private const float NodeHeight = 72f;
        private const float EdgeThickness = 7f;

        private static readonly Color32 BackgroundColor = new(18, 20, 28, 255);
        private static readonly Color32 RootPanelColor = new(32, 35, 48, 235);
        private static readonly Color32 PanelColor = new(28, 36, 50, 245);
        private static readonly Color32 PanelAltColor = new(38, 48, 66, 245);
        private static readonly Color32 FrameColor = new(72, 84, 102, 255);
        private static readonly Color32 ButtonColor = new(217, 151, 28, 255);
        private static readonly Color32 MapColor = new(42, 78, 69, 255);
        private static readonly Color32 ArenaColor = new(47, 35, 78, 255);
        private static readonly Color32 MonsterColor = new(117, 58, 92, 255);
        private static readonly Color32 SlotBoardColor = new(12, 30, 50, 255);
        private static readonly Color32 SlotCellColor = new(22, 43, 67, 255);
        private static readonly Color32 HpColor = new(226, 47, 45, 255);
        private static readonly Color32 ShieldColor = new(39, 144, 235, 255);
        private static readonly Color32 EnergyColor = new(39, 203, 235, 255);

        private const string InGameLeverTexturePath = "Assets/Resources/Textures/Ingame_lever.png";

        [MenuItem("SlotRogue/Game Flow/Patch Run Battle Lever (Keep UI)")]
        public static void PatchRunBattleLeverOnly()
        {
            string prefabPath = $"{PrefabFolder}/RunBattleView.prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset != null)
            {
                using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                {
                    PatchLeverInRoot(scope.prefabContentsRoot);
                    UnityEngine.Debug.Log("[SlotRogue] RunBattleView 프리팹 레버 패치 완료.");
                }
            }

            foreach (Scene scene in GetOpenRunBattleScenes())
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    RunBattleView view = root.GetComponentInChildren<RunBattleView>(true);
                    if (view != null)
                    {
                        PatchLeverInRoot(view.gameObject);
                        EditorSceneManager.MarkSceneDirty(scene);
                        UnityEngine.Debug.Log("[SlotRogue] RunBattle 씬 레버 패치 완료.");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void PatchLeverInRoot(GameObject root)
        {
            Transform spinButtonTransform = FindDeepChild(root.transform, "Spin Button");
            RectTransform spinButton = spinButtonTransform != null
                ? spinButtonTransform as RectTransform
                : null;

            Transform leverTransform = FindDeepChild(root.transform, "Spin Lever");
            GameObject leverObject;

            if (leverTransform != null)
            {
                leverObject = leverTransform.gameObject;
            }
            else
            {
                leverObject = new GameObject("Spin Lever",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform leverRect = leverObject.GetComponent<RectTransform>();
                Transform parent = spinButton != null ? spinButton.parent : root.transform;
                leverRect.SetParent(parent, false);
            }

            RectTransform lever = leverObject.GetComponent<RectTransform>();
            lever.sizeDelta = new Vector2(96f, 170f);
            lever.localScale = Vector3.one;
            lever.localRotation = Quaternion.identity;

            if (spinButton != null)
            {
                lever.anchorMin = spinButton.anchorMin;
                lever.anchorMax = spinButton.anchorMax;
                lever.pivot = new Vector2(0.5f, 0.5f);
                lever.anchoredPosition = spinButton.anchoredPosition + new Vector2(160f, 10f);
                lever.SetSiblingIndex(spinButton.GetSiblingIndex() + 1);
            }

            Image leverImage = leverObject.GetComponent<Image>();
            if (leverImage == null)
            {
                leverImage = leverObject.AddComponent<Image>();
            }

            leverImage.raycastTarget = false;
            Sprite[] sprites = LoadSprites(InGameLeverTexturePath);
            if (sprites != null && sprites.Length > 0)
            {
                leverImage.sprite = sprites[0];
            }
            leverImage.preserveAspect = true;
            leverImage.enabled = true;

            SlotLeverView leverView = leverObject.GetComponent<SlotLeverView>();
            if (leverView == null)
            {
                leverView = leverObject.AddComponent<SlotLeverView>();
            }
            leverView.Bind(leverImage, sprites);

            RunBattleController controller = root.GetComponentInChildren<RunBattleController>(true);
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                SerializedProperty leverProp = so.FindProperty("_spinLeverView");
                if (leverProp != null)
                {
                    leverProp.objectReferenceValue = leverView;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static List<Scene> GetOpenRunBattleScenes()
        {
            var scenes = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.path.Contains("RunBattle"))
                {
                    scenes.Add(scene);
                }
            }
            return scenes;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        [MenuItem("SlotRogue/Game Flow/Rebuild Scene UI Prefabs")]
        public static void BuildAll()
        {
            EnsureFolder(PrefabFolder);

            BuildGameStart();
            BuildArtifactSelection();
            BuildRunMap();
            BuildRunBattle();
            BuildRunReward();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildAllFromCommandLine()
        {
            BuildAll();
        }

        private static void BuildGameStart()
        {
            GameObject canvas = CreateCanvasRoot("Game Start UI");
            var view = canvas.AddComponent<GameStartView>();
            canvas.AddComponent<GameStartController>();
            RectTransform root = CreateRootPanel(canvas.transform, "Game Start Root");

            CreateText(root, "Title", "SlotRogue", 48, TextAnchor.MiddleCenter, new Vector2(0f, 760f), new Vector2(820f, 90f));
            CreatePanel(root, "Start Hero Image", "start/hero", new Color32(46, 64, 95, 255), new Vector2(0f, 320f), new Vector2(820f, 650f));
            Text summary = CreateTextPanel(
                root,
                "Start Summary Panel",
                "start/summary-panel",
                "Start a new run.",
                new Vector2(0f, -130f),
                new Vector2(820f, 220f),
                24);
            Button startButton = CreateButton(root, "Start Button", "start/start-button", "Start New Run", new Vector2(0f, -430f), new Vector2(680f, 110f), 34);
            view.Bind(summary, startButton);

            SavePrefabAndScene(canvas, "GameStartView", "GameStart");
        }

        private static void BuildArtifactSelection()
        {
            GameObject canvas = CreateCanvasRoot("Starter Artifact UI");
            var view = canvas.AddComponent<StartArtifactSelectionView>();
            canvas.AddComponent<StartArtifactSelectionController>();
            RectTransform root = CreateRootPanel(canvas.transform, "Starter Artifact Root");

            CreateText(root, "Title", "Choose Starter Artifact", 42, TextAnchor.MiddleCenter, new Vector2(0f, 760f), new Vector2(820f, 90f));
            Text summary = CreateTextPanel(root, "Starter Artifact Summary Panel", "artifact/summary-panel", "Summary", new Vector2(0f, 600f), new Vector2(820f, 170f), 22);
            var options = new List<GameFlowOptionView>();

            for (int index = 0; index < 3; index++)
            {
                options.Add(CreateOptionCard(
                    root,
                    $"Artifact Option {index + 1}",
                    $"artifact/option-{index + 1}",
                    new Vector2(0f, 330f - (index * 190f)),
                    new Vector2(820f, 160f)));
            }

            view.Bind(summary, options.ToArray());
            SavePrefabAndScene(canvas, "StartArtifactSelectionView", "StartArtifactSelection");
        }

        private static void BuildRunReward()
        {
            GameObject canvas = CreateCanvasRoot("Run Reward UI");
            var view = canvas.AddComponent<RunRewardView>();
            canvas.AddComponent<RunRewardController>();
            RectTransform root = CreateRootPanel(canvas.transform, "Run Reward Root");

            CreateText(root, "Title", "Claim Reward", 44, TextAnchor.MiddleCenter, new Vector2(0f, 760f), new Vector2(820f, 90f));
            CreatePanel(root, "Reward Chest Image", "reward/chest", new Color32(86, 72, 42, 255), new Vector2(0f, 465f), new Vector2(820f, 420f));
            Text summary = CreateTextPanel(root, "Reward Summary Panel", "reward/summary-panel", "Summary", new Vector2(0f, 150f), new Vector2(820f, 165f), 21);
            var options = new List<GameFlowOptionView>();

            for (int index = 0; index < 3; index++)
            {
                options.Add(CreateOptionCard(
                    root,
                    $"Reward Option {index + 1}",
                    $"reward/option-{index + 1}",
                    new Vector2(0f, -80f - (index * 170f)),
                    new Vector2(820f, 145f)));
            }

            view.Bind(summary, options.ToArray());
            SavePrefabAndScene(canvas, "RunRewardView", "RunReward");
        }

        private static void BuildRunMap()
        {
            GameObject canvas = CreateCanvasRoot("Run Map UI");
            var view = canvas.AddComponent<RunMapView>();
            canvas.AddComponent<RunMapController>();
            RectTransform root = CreateRootPanel(canvas.transform, "Run Map Root");
            RunMapGraphDefinition graph = RunMapNodeCatalog.BuildDefaultGraph();

            CreateText(root, "Title", "Run Map", 44, TextAnchor.MiddleCenter, new Vector2(0f, 800f), new Vector2(820f, 80f));
            RectTransform board = CreatePanel(root, "Map Board Image", "map/board", MapColor, new Vector2(0f, 145f), new Vector2(820f, 1120f));
            var edgeViews = new List<RunMapEdgeView>();
            var nodeViews = new List<RunMapNodeView>();

            for (int index = 0; index < graph.Edges.Length; index++)
            {
                edgeViews.Add(CreateMapEdge(board, graph, graph.Edges[index]));
            }

            for (int index = 0; index < graph.Nodes.Length; index++)
            {
                nodeViews.Add(CreateMapNode(board, graph.Nodes[index], graph.MaxFloor));
            }

            Text summary = CreateTextPanel(root, "Map Summary Panel", "map/summary-panel", "Summary", new Vector2(0f, -675f), new Vector2(820f, 300f), 20);
            view.Bind(summary, nodeViews.ToArray(), edgeViews.ToArray());

            SavePrefabAndScene(canvas, "RunMapView", "RunMap");
        }

        private static void BuildRunBattle()
        {
            GameObject canvas = CreateCanvasRoot("Run Battle UI");
            var view = canvas.AddComponent<RunBattleView>();
            canvas.AddComponent<RunBattleController>();
            RectTransform root = CreateRootPanel(canvas.transform, "Run Battle Root");

            Text[] slotCells = new Text[SlotSpinResult.CellCount];
            CreateBattleTopHud(root, out Text playerHud, out Image playerHp, out Image playerShield);
            EnsureEnemyFormationSlotPrefab();
            CreateBattleArena(root, out EnemyFormationSlotView[] formationSlots, out Text enemyIntent);
            CreateSlotMachine(root, slotCells);
            CreateBattleActionRow(root, out Text resultValue, out Button spinButton, out Button continueButton, out Button restartButton, out Text slotResult);
            CreateBattleBottomRow(root, out Text statusText);
            RectTransform presentationOverlay = CreatePresentationOverlay(canvas.transform);
            RectTransform playerDamageAnchor = CreateDamageAnchor(
                presentationOverlay,
                "player-damage-anchor",
                new Vector2(0f, -120f));
            presentationOverlay.SetAsLastSibling();

            view.Bind(
                slotCells,
                statusText,
                slotResult,
                resultValue,
                playerHud,
                enemyIntent,
                playerHp,
                playerShield,
                spinButton,
                continueButton,
                restartButton,
                presentationOverlay,
                playerDamageAnchor,
                formationSlots);

            SavePrefabAndScene(canvas, "RunBattleView", "RunBattle");
        }

        private static RectTransform CreatePresentationOverlay(Transform canvasTransform)
        {
            RectTransform overlay = CreateRect("Presentation Overlay", canvasTransform, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            var canvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Image image = overlay.gameObject.AddComponent<Image>();
            image.color = new Color32(0, 0, 0, 0);
            image.raycastTarget = false;

            var imageSlot = overlay.gameObject.AddComponent<GameFlowImageSlot>();
            imageSlot.Bind("battle/presentation-overlay", image);

            return overlay;
        }

        private static RectTransform CreateDamageAnchor(
            Transform parent,
            string anchorName,
            Vector2 anchoredPosition)
        {
            RectTransform anchor = CreateRect(anchorName, parent, anchoredPosition, Vector2.zero);
            anchor.anchorMin = new Vector2(0.5f, 0.5f);
            anchor.anchorMax = new Vector2(0.5f, 0.5f);
            anchor.pivot = new Vector2(0.5f, 0.5f);
            anchor.sizeDelta = Vector2.zero;
            return anchor;
        }

        private static void CreateBattleTopHud(RectTransform root, out Text playerHud, out Image playerHp, out Image playerShield)
        {
            RectTransform panel = CreatePanel(root, "Player Status HUD", "battle/player-status-panel", PanelColor, new Vector2(-220f, 790f), new Vector2(470f, 145f));
            CreateText(panel, "HP Label", "HP", 30, TextAnchor.MiddleLeft, new Vector2(-160f, 35f), new Vector2(100f, 42f));
            CreateText(panel, "Shield Label", "SHIELD", 25, TextAnchor.MiddleLeft, new Vector2(-145f, -35f), new Vector2(135f, 42f));
            playerHp = CreateFillBar(panel, "Player HP Bar", "battle/player-hp-bar", "battle/player-hp-fill", new Vector2(82f, 35f), new Vector2(220f, 28f), HpColor);
            playerShield = CreateFillBar(panel, "Player Shield Bar", "battle/player-shield-bar", "battle/player-shield-fill", new Vector2(82f, -34f), new Vector2(220f, 28f), ShieldColor);
            playerHud = CreateText(panel, "Player HUD Values", "-", 24, TextAnchor.MiddleRight, new Vector2(144f, 0f), new Vector2(290f, 112f));

            RectTransform wave = CreatePanel(root, "Wave HUD", "battle/wave-panel", PanelAltColor, new Vector2(160f, 810f), new Vector2(220f, 90f));
            CreateOverlayText(wave, "Wave Text", "WAVE 1/6", 30, TextAnchor.MiddleCenter, new RectOffset(8, 8, 8, 8));
            RectTransform settings = CreatePanel(root, "Settings HUD", "battle/settings-button-placeholder", PanelAltColor, new Vector2(360f, 810f), new Vector2(112f, 90f));
            CreateOverlayText(settings, "Settings Text", "SET", 25, TextAnchor.MiddleCenter, new RectOffset(8, 8, 8, 8));
        }

        private static void EnsureEnemyFormationSlotPrefab()
        {
            string prefabPath = $"{PrefabFolder}/{EnemyFormationSlotPrefabName}.prefab";
            var canvasObject = new GameObject("Enemy Formation Slot Builder", typeof(RectTransform));
            try
            {
                RectTransform tempRoot = canvasObject.GetComponent<RectTransform>();
                EnemyFormationSlotView slot = CreateEnemyFormationSlot(tempRoot, 0, Vector2.zero);
                PrefabUtility.SaveAsPrefabAsset(slot.gameObject, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static void CreateBattleArena(
            RectTransform root,
            out EnemyFormationSlotView[] formationSlots,
            out Text enemyIntent)
        {
            RectTransform arena = CreatePanel(root, "Battle Arena Image", "battle/arena", ArenaColor, new Vector2(0f, 380f), new Vector2(860f, 650f));
            CreateOverlayText(arena, "Arena Background Label", "ALIEN FRONTIER", 23, TextAnchor.UpperCenter, new RectOffset(22, 22, 20, 560));

            RectTransform formationRoot = CreateRect("Formation Slots Root", arena, Vector2.zero, Vector2.zero);
            formationRoot.sizeDelta = Vector2.zero;

            string slotPrefabPath = $"{PrefabFolder}/{EnemyFormationSlotPrefabName}.prefab";
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(slotPrefabPath);
            formationSlots = new EnemyFormationSlotView[FormationSlotCount];

            for (int index = 0; index < FormationSlotCount; index++)
            {
                Vector2 slotPosition = ResolveEnemySlotPosition(index, FormationSlotCount);
                GameObject slotObject = slotPrefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, formationRoot)
                    : CreateEnemyFormationSlot(formationRoot, index, slotPosition).gameObject;

                slotObject.name = $"Formation Slot {index + 1}";
                if (slotObject.transform is RectTransform slotTransform)
                {
                    slotTransform.anchoredPosition = slotPosition;
                }

                formationSlots[index] = slotObject.GetComponent<EnemyFormationSlotView>();
                if (formationSlots[index] == null)
                {
                    formationSlots[index] = slotObject.AddComponent<EnemyFormationSlotView>();
                }
            }

            enemyIntent = CreateOverlayText(arena, "Enemy Intent Text", "ENEMY INTENT: -", 24, TextAnchor.LowerCenter, new RectOffset(36, 36, 560, 22));
        }

        private static EnemyFormationSlotView CreateEnemyFormationSlot(
            Transform parent,
            int index,
            Vector2 position)
        {
            RectTransform root = CreatePanel(
                parent,
                $"Formation Slot {index + 1}",
                $"battle/formation-slot-{index + 1}",
                new Color32(0, 0, 0, 0),
                position,
                new Vector2(255f, 420f));
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            if (button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = true;
            }

            RectTransform portrait = CreatePanel(
                root,
                "Portrait",
                $"battle/monster-{index + 1}-portrait",
                MonsterColor,
                new Vector2(0f, 70f),
                new Vector2(240f, 280f));
            GameFlowImageSlot portraitSlot = portrait.GetComponent<GameFlowImageSlot>();
            Text placeholder = CreateOverlayText(
                portrait,
                "Portrait Placeholder Text",
                "MONSTER\nSPRITE SLOT",
                24,
                TextAnchor.MiddleCenter,
                new RectOffset(14, 14, 18, 18));

            RectTransform statusPanel = CreatePanel(
                root,
                "Status Panel",
                $"battle/monster-{index + 1}-status-panel",
                PanelColor,
                new Vector2(0f, -155f),
                new Vector2(255f, 92f));
            Image statusBackground = statusPanel.GetComponent<Image>();
            Text hudText = CreateOverlayText(
                statusPanel,
                "HUD Text",
                $"MONSTER {index + 1}",
                20,
                TextAnchor.UpperCenter,
                new RectOffset(8, 8, 8, 44));
            Image hpFill = CreateFillBar(
                statusPanel,
                "HP Bar",
                $"battle/monster-{index + 1}-hp-bar",
                $"battle/monster-{index + 1}-hp-fill",
                new Vector2(0f, -22f),
                new Vector2(205f, 22f),
                HpColor);
            RectTransform damageAnchor = CreateDamageAnchor(root, "Damage Anchor", new Vector2(0f, 130f));

            ConfigureFormationSlotRaycasts(root);

            var slotView = root.gameObject.AddComponent<EnemyFormationSlotView>();
            slotView.Bind(root, button, portraitSlot, hudText, hpFill, statusBackground, damageAnchor, placeholder);
            return slotView;
        }

        private static void ConfigureFormationSlotRaycasts(RectTransform root)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int index = 0; index < graphics.Length; index++)
            {
                Graphic graphic = graphics[index];
                graphic.raycastTarget = graphic.transform == root;
            }
        }

        private static Vector2 ResolveEnemySlotPosition(int index, int slotCount)
        {
            if (slotCount <= 1)
            {
                return Vector2.zero;
            }

            float spacing = slotCount <= 2 ? 300f : 270f;
            float startX = -(slotCount - 1) * spacing * 0.5f;
            return new Vector2(startX + (index * spacing), 0f);
        }

        private static void CreateSlotMachine(RectTransform root, Text[] slotCells)
        {
            RectTransform slotMachine = CreatePanel(root, "Slot Machine Panel", "battle/slot-machine-panel", SlotBoardColor, new Vector2(0f, -205f), new Vector2(860f, 430f));
            const float cellWidth = 154f;
            const float cellHeight = 112f;
            const float spacing = 9f;
            float totalWidth = (SlotSpinResult.Columns * cellWidth) + ((SlotSpinResult.Columns - 1) * spacing);
            float totalHeight = (SlotSpinResult.Rows * cellHeight) + ((SlotSpinResult.Rows - 1) * spacing);
            float startX = -totalWidth * 0.5f + (cellWidth * 0.5f);
            float startY = totalHeight * 0.5f - (cellHeight * 0.5f);

            for (int row = 0; row < SlotSpinResult.Rows; row++)
            {
                for (int column = 0; column < SlotSpinResult.Columns; column++)
                {
                    int index = SlotSpinResult.ToIndex(column, row);
                    RectTransform cell = CreatePanel(
                        slotMachine,
                        $"Slot Cell {index:00}",
                        $"battle/slot-cell-{index:00}",
                        SlotCellColor,
                        new Vector2(startX + (column * (cellWidth + spacing)), startY - (row * (cellHeight + spacing))),
                        new Vector2(cellWidth, cellHeight));
                    slotCells[index] = CreateOverlayText(cell, $"Slot Cell Text {index:00}", "-", 22, TextAnchor.MiddleCenter, new RectOffset(8, 8, 8, 8));
                    slotCells[index].fontStyle = FontStyle.Bold;
                }
            }
        }

        private static void CreateBattleActionRow(
            RectTransform root,
            out Text resultValue,
            out Button spinButton,
            out Button continueButton,
            out Button restartButton,
            out Text slotResult)
        {
            RectTransform resultPanel = CreatePanel(root, "Attack Result Panel", "battle/attack-result-panel", PanelColor, new Vector2(-300f, -560f), new Vector2(260f, 150f));
            CreateOverlayText(resultPanel, "Attack Result Label", "ATTACK RESULT", 22, TextAnchor.UpperCenter, new RectOffset(8, 8, 14, 88));
            resultValue = CreateOverlayText(resultPanel, "Attack Result Value", "-", 46, TextAnchor.MiddleCenter, new RectOffset(8, 8, 50, 8));
            resultValue.color = new Color32(255, 207, 59, 255);
            resultValue.fontStyle = FontStyle.Bold;

            spinButton = CreateButton(root, "Spin Button", "battle/spin-button", "SPIN", new Vector2(0f, -560f), new Vector2(270f, 150f), 45);
            continueButton = CreateButton(root, "Claim Reward Button", "battle/claim-reward-button", "CLAIM\nREWARD", new Vector2(0f, -560f), new Vector2(270f, 150f), 31);
            restartButton = CreateButton(root, "Return To Start Button", "battle/restart-button", "RETURN\nSTART", new Vector2(0f, -560f), new Vector2(270f, 150f), 31);
            continueButton.gameObject.SetActive(false);
            restartButton.gameObject.SetActive(false);

            RectTransform nextPanel = CreatePanel(root, "Next Attack Panel", "battle/spin-result-panel", PanelColor, new Vector2(300f, -560f), new Vector2(260f, 150f));
            slotResult = CreateOverlayText(nextPanel, "Next Attack Text", "NEXT ATTACK\n-", 21, TextAnchor.MiddleCenter, new RectOffset(12, 12, 10, 10));
        }

        private static void CreateBattleBottomRow(RectTransform root, out Text statusText)
        {
            RectTransform statusPanel = CreatePanel(root, "Battle Status Panel", "battle/status-panel", PanelColor, new Vector2(-300f, -755f), new Vector2(260f, 145f));
            statusText = CreateOverlayText(statusPanel, "Battle Status Text", "STATUS", 18, TextAnchor.MiddleCenter, new RectOffset(12, 12, 12, 12));
            RectTransform energy = CreatePanel(root, "Energy Panel", "battle/energy-panel", PanelColor, new Vector2(0f, -755f), new Vector2(260f, 145f));
            CreateOverlayText(energy, "Energy Text", "ENERGY\n3/3", 29, TextAnchor.MiddleCenter, new RectOffset(8, 8, 8, 8)).color = EnergyColor;
            RectTransform credits = CreatePanel(root, "Credits Panel", "battle/credits-panel", PanelColor, new Vector2(300f, -755f), new Vector2(260f, 145f));
            CreateOverlayText(credits, "Credits Text", "CREDITS\n0", 27, TextAnchor.MiddleCenter, new RectOffset(8, 8, 8, 8));
        }

        private static GameFlowOptionView CreateOptionCard(
            Transform parent,
            string name,
            string slotId,
            Vector2 position,
            Vector2 size)
        {
            RectTransform card = CreatePanel(parent, name, $"{slotId}/card", PanelAltColor, position, size);
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            RectTransform art = CreatePanel(card, $"{name} Art", slotId, new Color32(44, 49, 66, 245), new Vector2(-310f, 0f), new Vector2(150f, size.y - 28f));
            Text title = CreateText(card, $"{name} Title", "Title", 27, TextAnchor.MiddleLeft, new Vector2(95f, 28f), new Vector2(530f, 44f));
            title.fontStyle = FontStyle.Bold;
            Text description = CreateText(card, $"{name} Description", "Description", 21, TextAnchor.MiddleLeft, new Vector2(95f, -32f), new Vector2(530f, 80f));
            var optionView = card.gameObject.AddComponent<GameFlowOptionView>();
            optionView.Bind(button, title, description, art.GetComponent<GameFlowImageSlot>());
            return optionView;
        }

        private static RunMapEdgeView CreateMapEdge(
            Transform parent,
            RunMapGraphDefinition graph,
            RunMapGraphEdge edge)
        {
            RunMapNodeDefinition fromNode = graph.GetNode(edge.FromNodeId);
            RunMapNodeDefinition toNode = graph.GetNode(edge.ToNodeId);
            Vector2 from = ResolveNodePosition(fromNode, graph.MaxFloor);
            Vector2 to = ResolveNodePosition(toNode, graph.MaxFloor);
            Vector2 midpoint = (from + to) * 0.5f;
            Vector2 delta = to - from;
            RectTransform line = CreateRect($"Map Edge {edge.FromNodeId} To {edge.ToNodeId}", parent, midpoint, new Vector2(delta.magnitude, EdgeThickness));
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Image image = AddImageSlot(line.gameObject, $"map/edge/{edge.FromNodeId}-{edge.ToNodeId}", new Color32(82, 91, 104, 180));
            var edgeView = line.gameObject.AddComponent<RunMapEdgeView>();
            edgeView.Bind(edge.FromNodeId, edge.ToNodeId, image);
            return edgeView;
        }

        private static RunMapNodeView CreateMapNode(Transform parent, RunMapNodeDefinition node, int maxFloor)
        {
            RectTransform nodeTransform = CreatePanel(parent, $"Map Node {node.NodeId}", $"map/node/{node.NodeId}", new Color32(60, 65, 78, 255), ResolveNodePosition(node, maxFloor), new Vector2(NodeWidth, NodeHeight));
            Button button = nodeTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = nodeTransform.GetComponent<Image>();
            Text label = CreateOverlayText(nodeTransform, $"Map Node Label {node.NodeId}", node.DisplayName, 21, TextAnchor.MiddleCenter, new RectOffset(8, 8, 4, 4));
            label.fontStyle = FontStyle.Bold;
            var nodeView = nodeTransform.gameObject.AddComponent<RunMapNodeView>();
            nodeView.Bind(node.NodeId, button, nodeTransform.GetComponent<Image>(), label);
            return nodeView;
        }

        private static Vector2 ResolveNodePosition(RunMapNodeDefinition node, int maxFloor)
        {
            float normalizedFloor = maxFloor <= 0 ? 0f : (float)node.Floor / maxFloor;
            float y = Mathf.Lerp(-MapHeight * 0.5f, MapHeight * 0.5f, normalizedFloor);
            float x = (node.Lane - 1f) * (MapWidth / 3f);
            return new Vector2(x, y);
        }

        private static GameObject CreateCanvasRoot(string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            RectTransform background = CreateRect("Scene Background Image", canvasObject.transform, Vector2.zero, Vector2.zero);
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            AddImageSlot(background.gameObject, "scene-background", BackgroundColor);
            CreateEventSystem(canvasObject.transform);
            return canvasObject;
        }

        private static RectTransform CreateRootPanel(Transform parent, string name)
        {
            RectTransform root = CreatePanel(parent, name, $"{name}/frame", RootPanelColor, Vector2.zero, new Vector2(920f, 1760f));
            return root;
        }

        private static Text CreateTextPanel(
            Transform parent,
            string name,
            string slotId,
            string value,
            Vector2 position,
            Vector2 size,
            int fontSize)
        {
            RectTransform panel = CreatePanel(parent, name, slotId, PanelAltColor, position, size);
            return CreateOverlayText(panel, $"{name} Text", value, fontSize, TextAnchor.MiddleLeft, new RectOffset(22, 22, 16, 16));
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            string slotId,
            Color32 color,
            Vector2 position,
            Vector2 size)
        {
            RectTransform panel = CreateRect(name, parent, position, size);
            AddImageSlot(panel.gameObject, slotId, color);
            CreateFrame(panel);
            return panel;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string slotId,
            string label,
            Vector2 position,
            Vector2 size,
            int fontSize)
        {
            RectTransform panel = CreatePanel(parent, name, slotId, ButtonColor, position, size);
            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            Text text = CreateOverlayText(panel, $"{name} Text", label, fontSize, TextAnchor.MiddleCenter, new RectOffset(8, 8, 8, 8));
            text.fontStyle = FontStyle.Bold;
            return button;
        }

        private static Image CreateFillBar(
            Transform parent,
            string name,
            string baseSlotId,
            string fillSlotId,
            Vector2 position,
            Vector2 size,
            Color32 color)
        {
            RectTransform bar = CreatePanel(parent, name, baseSlotId, new Color32(11, 16, 24, 255), position, size);
            RectTransform fill = CreateRect($"{name} Fill", bar, new Vector2(4f, 0f), new Vector2(size.x - 8f, size.y - 8f));
            fill.anchorMin = new Vector2(0f, 0.5f);
            fill.anchorMax = new Vector2(0f, 0.5f);
            fill.pivot = new Vector2(0f, 0.5f);
            return AddImageSlot(fill.gameObject, fillSlotId, color);
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 position,
            Vector2 size)
        {
            RectTransform transform = CreateRect(name, parent, position, size);
            Text text = transform.gameObject.AddComponent<Text>();
            ConfigureText(text, value, fontSize, alignment);
            return text;
        }

        private static Text CreateOverlayText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            RectOffset padding)
        {
            RectTransform transform = CreateRect(name, parent, Vector2.zero, Vector2.zero);
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.offsetMin = new Vector2(padding.left, padding.bottom);
            transform.offsetMax = new Vector2(-padding.right, -padding.top);
            Text text = transform.gameObject.AddComponent<Text>();
            ConfigureText(text, value, fontSize, alignment);
            return text;
        }

        private static void ConfigureText(Text text, string value, int fontSize, TextAnchor alignment)
        {
            text.font = GetDefaultFont();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            return rectTransform;
        }

        private static Image AddImageSlot(GameObject gameObject, string slotId, Color32 color)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.preserveAspect = false;

            var imageSlot = gameObject.AddComponent<GameFlowImageSlot>();
            imageSlot.Bind(slotId, image);
            return image;
        }

        private static void CreateFrame(RectTransform parent)
        {
            CreateFrameLine(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 6f));
            CreateFrameLine(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 3f), new Vector2(0f, 6f));
            CreateFrameLine(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f), new Vector2(6f, 0f));
            CreateFrameLine(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(6f, 0f));
        }

        private static void CreateFrameLine(
            RectTransform parent,
            string suffix,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            RectTransform line = CreateRect($"Frame {suffix}", parent, position, Vector2.zero);
            line.anchorMin = anchorMin;
            line.anchorMax = anchorMax;
            line.sizeDelta = size;
            AddImageSlot(line.gameObject, $"game-flow/frame/{parent.name}/{suffix}", FrameColor);
        }

        private static void CreateEventSystem(Transform parent)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(parent, false);
            eventSystem.AddComponent<EventSystem>();

            Type inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                eventSystem.AddComponent(inputModuleType);
                return;
            }

            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void SavePrefabAndScene(GameObject canvas, string prefabName, string sceneName)
        {
            string prefabPath = $"{PrefabFolder}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvas, prefabPath);
            UnityEngine.Object.DestroyImmediate(canvas);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            CreateMainCamera();
            EditorSceneManager.SaveScene(scene, $"{SceneFolder}/{sceneName}.unity");
        }

        private static void CreateMainCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();
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

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Sprite[] LoadSprites(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            var sprites = new List<Sprite>();
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }
            sprites.Sort((a, b) => GetFrameIndex(a).CompareTo(GetFrameIndex(b)));
            return sprites.ToArray();
        }

        private static int GetFrameIndex(Sprite sprite)
        {
            if (sprite == null || string.IsNullOrEmpty(sprite.name)) return int.MaxValue;
            int sep = sprite.name.LastIndexOf('_');
            if (sep < 0 || sep >= sprite.name.Length - 1) return int.MaxValue;
            return int.TryParse(sprite.name.Substring(sep + 1), out int idx) ? idx : int.MaxValue;
        }
    }
}
