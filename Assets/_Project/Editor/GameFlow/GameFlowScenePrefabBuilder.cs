using System;
using System.Collections.Generic;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.SlotPresentation;
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
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string BackgroundOutsideTexturePath = "Assets/Resources/Textures/Background_Outside.png";
        private const string BackgroundInsideTexturePath = "Assets/Resources/Textures/Background_Inside.png";
        private const string SlotIconTexturePath = "Assets/Resources/Textures/Icon_Slot.png";
        private const string SlotIconAnimatedTexturePath = "Assets/Resources/Textures/icon_slot_ani.png";
        private const string InGameSlotTexturePath = "Assets/Resources/Textures/Ingame_Slot.png";
        private const string InGameHpTexturePath = "Assets/Resources/Textures/Ingame_hp.png";
        private const string InGameSpinButtonTexturePath = "Assets/Resources/Textures/Ingame_bt_spin.png";
        private const string InGameLeverTexturePath = "Assets/Resources/Textures/Ingame_lever.png";
        private const string InGamePauseButtonTexturePath = "Assets/Resources/Textures/ingame_bt_pause.png";
        private const string InGameSpecButtonTexturePath = "Assets/Resources/Textures/ingame_bt_spec.png";
        private const string InGameCoinPanelTexturePath = "Assets/Resources/Textures/ingame_panel_coin.png";
        private const string InGamePotionSlot1TexturePath = "Assets/Resources/Textures/ingame_slot_potion1.png";
        private const string InGamePotionSlot2TexturePath = "Assets/Resources/Textures/ingame_slot_potion2.png";
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

        [MenuItem("SlotRogue/Game Flow/Rebuild Scene UI Prefabs")]
        public static void BuildAll()
        {
            bool confirmed = UnityEditor.EditorUtility.DisplayDialog(
                "Rebuild Scene UI Prefabs",
                "씬·프리팹 전체를 재생성합니다.\n수동으로 배치한 UI 변경사항이 모두 초기화됩니다.\n\n계속하겠습니까?",
                "Rebuild",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

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
            EnsureFolder(PrefabFolder);

            BuildGameStart();
            BuildArtifactSelection();
            BuildRunMap();
            BuildRunBattle();
            BuildRunReward();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("SlotRogue/Game Flow/Rebuild Run Battle Scene UI")]
        public static void BuildRunBattleOnly()
        {
            bool confirmed = UnityEditor.EditorUtility.DisplayDialog(
                "Rebuild Run Battle Scene UI",
                "RunBattle 씬·프리팹을 재생성합니다.\n수동으로 배치한 UI 변경사항이 초기화됩니다.\n\n계속하겠습니까?",
                "Rebuild",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            EnsureFolder(PrefabFolder);
            EnsureFolder(SceneFolder);

            BuildRunBattle();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("SlotRogue/Game Flow/Patch Run Battle Lever (Keep UI)")]
        public static void PatchRunBattleLeverOnly()
        {
            string prefabPath = $"{PrefabFolder}/RunBattleView.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            if (root == null)
            {
                UnityEngine.Debug.LogError(
                    $"[SlotRogue] Prefab not found: {prefabPath}");
                return;
            }

            try
            {
                SlotLeverView leverView = PatchRunBattleLever(root);

                if (leverView == null)
                {
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
                int sceneInstanceCount = PatchLoadedRunBattleSceneInstances(out GameObject selectedLever);

                if (selectedLever != null)
                {
                    Selection.activeGameObject = selectedLever;
                    EditorGUIUtility.PingObject(selectedLever);
                }

                UnityEngine.Debug.Log(
                    "[SlotRogue] RunBattle Spin Lever patched. UI layout was kept intact. " +
                    $"Loaded scene instances: {sceneInstanceCount}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("SlotRogue/Slot Presentation/Patch Bindings Only (Keep UI)")]
        public static void PatchSlotPresentationBindings()
        {
            string prefabPath = $"{PrefabFolder}/DevSlotPresentationView.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            if (root == null)
            {
                UnityEngine.Debug.LogError(
                    $"[SlotRogue] Prefab not found: {prefabPath}\n" +
                    "Run 'Rebuild Demo Scene' once before patching bindings.");
                return;
            }

            try
            {
                PatchSpinViewBindings(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log(
                    "[SlotRogue] SpinView bindings patched. UI layout was kept intact.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("SlotRogue/Slot Presentation/Rebuild Demo Scene (Reset UI)")]
        public static void BuildSlotPresentationDemo()
        {
            bool confirmed = UnityEditor.EditorUtility.DisplayDialog(
                "Reset Demo UI Layout",
                "This will rebuild DevSlotPresentationView from scratch.\n" +
                "Manual UI layout edits on the prefab will be reset.\n\n" +
                "Use 'Patch Bindings Only (Keep UI)' if you only need component references updated.",
                "Rebuild",
                "Cancel");

            if (!confirmed) return;

            EnsureFolder(PrefabFolder);
            EnsureFolder(SceneFolder);

            GameObject canvas = CreateCanvasRoot("Slot Presentation Demo UI");
            var controller = canvas.AddComponent<SlotPresentationDemoController>();
            RectTransform root = CreateRootPanel(canvas.transform, "Slot Presentation Demo Root");
            CreateInsideTextureBackdrop(root);

            CreateText(root, "Title", "Perfect Board Presentation Demo", 42, TextAnchor.MiddleCenter, new Vector2(0f, 790f), new Vector2(820f, 80f));
            Text status = CreateTextPanel(
                root,
                "Demo Status Panel",
                "slot-presentation-demo/status-panel",
                "Perfect board pattern ladder plays on start.",
                new Vector2(0f, 560f),
                new Vector2(820f, 145f),
                21);

            Text[] slotCells = new Text[SlotSpinResult.CellCount];
            Image[] slotCellIcons = new Image[SlotSpinResult.CellCount];
            Sprite[] slotIconSprites = LoadSprites(SlotIconTexturePath);
            Sprite[] slotSpinIconSprites = LoadSprites(SlotIconAnimatedTexturePath);
            CreateSlotMachine(root, slotCells, slotCellIcons);
            SlotPresentationManager manager = CreateSlotPresentationLayer(root, slotCells, slotCellIcons, slotIconSprites, slotSpinIconSprites);

            Button fullDemoButton = CreateButton(root, "Pattern Ladder Button", "slot-presentation-demo/full-button", "PATTERN\nLADDER", new Vector2(-315f, -635f), new Vector2(190f, 125f), 24);
            Button multiDemoButton = CreateButton(root, "Relic Chain Button", "slot-presentation-demo/multi-button", "RELIC\nCHAIN", new Vector2(-105f, -635f), new Vector2(190f, 125f), 27);
            Button replayBestButton = CreateButton(root, "Replay Best Button", "slot-presentation-demo/replay-best-button", "REPLAY\nLADDER", new Vector2(105f, -635f), new Vector2(190f, 125f), 22);
            Button skipButton = CreateButton(root, "Skip Demo Button", "slot-presentation-demo/skip-button", "SKIP", new Vector2(315f, -635f), new Vector2(190f, 125f), 30);

            controller.Bind(manager, status, fullDemoButton, multiDemoButton, replayBestButton, skipButton, GetSpriteOrNull(slotIconSprites, 0));

            SavePrefabAndScene(canvas, "DevSlotPresentationView", "Dev_SlotPresentation");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
            RectTransform root = CreateRunBattleRoot(canvas.transform);
            CreateInsideTextureBackdrop(root);

            Text[] slotCells = new Text[SlotSpinResult.CellCount];
            Image[] slotCellIcons = new Image[SlotSpinResult.CellCount];
            Sprite[] slotIconSprites = LoadSprites(SlotIconTexturePath);
            Sprite[] slotSpinIconSprites = LoadSprites(SlotIconAnimatedTexturePath);

            CreateTopCurrencyHud(root);
            CreatePauseButton(root);
            CreateInGameSlotMachine(root, slotCells, slotCellIcons);
            CreatePlayerHpGauge(root, out Image playerHp);
            Text resultValue = CreateAttackPowerReadout(root);
            CreateSpinControls(root, out Button spinButton, out Button continueButton, out Button restartButton);
            CreatePotionInventory(root);
            Text statusText = null;
            Text slotResult = null;
            Text playerHud = null;
            Text monsterHud = null;
            Text enemyIntent = null;
            Image playerShield = null;
            Image monsterHp = null;
            CreateSlotPresentationLayer(root, slotCells, slotCellIcons, slotIconSprites, slotSpinIconSprites);

            view.Bind(
                slotCells,
                statusText,
                slotResult,
                resultValue,
                playerHud,
                monsterHud,
                enemyIntent,
                playerHp,
                playerShield,
                monsterHp,
                spinButton,
                continueButton,
                restartButton);

            SavePrefabAndScene(canvas, "RunBattleView", "RunBattle");
        }

        private static RectTransform CreateRunBattleRoot(Transform parent)
        {
            RectTransform root = CreateRect("Run Battle Root", parent, Vector2.zero, new Vector2(1080f, 1920f));
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            return root;
        }

        private static void CreateTopCurrencyHud(RectTransform root)
        {
            RectTransform panel = CreateSpriteImage(
                root,
                "Currency HUD",
                "battle/currency-panel",
                InGameCoinPanelTexturePath,
                new Vector2(-372f, 868f),
                new Vector2(280f, 104f),
                true);
            Text value = CreateOverlayText(panel, "Currency Text", "0", 26, TextAnchor.MiddleLeft, new RectOffset(24, 12, 8, 8));
            value.fontStyle = FontStyle.Bold;
            value.color = new Color32(230, 238, 246, 255);
        }

        private static Button CreatePauseButton(RectTransform root)
        {
            return CreateSpriteButton(
                root,
                "Pause Button",
                "battle/pause-button",
                InGamePauseButtonTexturePath,
                new Vector2(440f, 868f),
                new Vector2(104f, 104f));
        }

        private static void CreateInGameSlotMachine(RectTransform root, Text[] slotCells, Image[] slotCellIcons)
        {
            RectTransform slotMachine = CreateSpriteImage(
                root,
                "Slot Machine Panel",
                "battle/slot-machine-panel",
                InGameSlotTexturePath,
                new Vector2(0f, -462f),
                new Vector2(1072f, 580f),
                true);
            const float cellWidth = 153f;
            const float cellHeight = 143f;
            float startX = -cellWidth * 2f;
            float startY = cellHeight - 50f;

            for (int row = 0; row < SlotSpinResult.Rows; row++)
            {
                for (int column = 0; column < SlotSpinResult.Columns; column++)
                {
                    int index = SlotSpinResult.ToIndex(column, row);
                    RectTransform cell = CreateRect(
                        $"Slot Cell {index:00}",
                        slotMachine,
                        new Vector2(startX + (column * cellWidth), startY - (row * cellHeight)),
                        new Vector2(cellWidth, cellHeight));

                    if (slotCellIcons != null && index < slotCellIcons.Length)
                    {
                        slotCellIcons[index] = CreateSlotCellIcon(cell, index);
                    }

                    Text symbolText = CreateOverlayText(cell, $"Slot Cell Text {index:00}", "-", 22, TextAnchor.MiddleCenter, new RectOffset(6, 6, 6, 6));
                    symbolText.fontStyle = FontStyle.Bold;
                    symbolText.color = new Color(1f, 1f, 1f, 0.08f);
                    slotCells[index] = symbolText;
                }
            }
        }

        private static void CreatePlayerHpGauge(RectTransform root, out Image playerHp)
        {
            playerHp = CreateVerticalFillBar(
                root,
                "Player HP Gauge",
                "battle/player-hp-gauge",
                "battle/player-hp-fill",
                new Vector2(-470f, -505f),
                new Vector2(72f, 364f),
                new Color32(255, 255, 255, 255),
                LoadFirstSprite(InGameHpTexturePath));
        }

        private static Text CreateAttackPowerReadout(RectTransform root)
        {
            RectTransform panel = CreateRect("Attack Power HUD", root, new Vector2(0f, -242f), new Vector2(340f, 72f));
            Text text = CreateOverlayText(panel, "Attack Power Text", "ATK 0", 35, TextAnchor.MiddleCenter, new RectOffset(8, 8, 8, 8));
            text.fontStyle = FontStyle.Bold;
            text.color = new Color32(255, 224, 93, 255);
            return text;
        }

        private static void CreateSpinControls(
            RectTransform root,
            out Button spinButton,
            out Button continueButton,
            out Button restartButton)
        {
            Vector2 position = new Vector2(0f, -800f);
            Vector2 size = new Vector2(384f, 184f);
            spinButton = CreateSpriteButton(root, "Spin Button", "battle/spin-button", InGameSpinButtonTexturePath, position, size);
            Text spinText = CreateText(spinButton.transform, "Spin Button Text", "SPIN", 56, TextAnchor.MiddleCenter, new Vector2(0f, -4f), new Vector2(310f, 100f));
            spinText.fontStyle = FontStyle.Bold;
            spinText.color = new Color32(238, 248, 255, 255);
            CreateSpinLever(root, spinButton.transform as RectTransform);
            continueButton = CreateButton(root, "Claim Reward Button", "battle/claim-reward-button", "CLAIM", position, size, 36);
            restartButton = CreateButton(root, "Return To Start Button", "battle/restart-button", "RETURN", position, size, 36);
            continueButton.gameObject.SetActive(false);
            restartButton.gameObject.SetActive(false);
        }

        private static void CreatePotionInventory(RectTransform root)
        {
            CreateSpriteImage(root, "Potion Slot 1", "battle/potion-slot-1", InGamePotionSlot1TexturePath, new Vector2(340f, -800f), new Vector2(152f, 160f), true);
            CreateSpriteImage(root, "Potion Slot 2", "battle/potion-slot-2", InGamePotionSlot2TexturePath, new Vector2(490f, -800f), new Vector2(148f, 160f), true);
        }

        private static SlotLeverView CreateSpinLever(RectTransform parent, RectTransform spinButton)
        {
            RectTransform lever = CreateSpriteImage(parent, "Spin Lever", "battle/spin-lever", InGameLeverTexturePath, Vector2.zero, new Vector2(96f, 170f), true);
            ConfigureSpinLeverTransform(lever, spinButton);

            Image leverImage = lever.GetComponent<Image>();
            SlotLeverView leverView = lever.gameObject.AddComponent<SlotLeverView>();
            leverView.Bind(leverImage, LoadSprites(InGameLeverTexturePath));
            return leverView;
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

        private static void CreateBattleArena(RectTransform root, out Text monsterHud, out Text enemyIntent, out Image monsterHp)
        {
            RectTransform arena = CreatePanel(root, "Battle Arena Image", "battle/arena", ArenaColor, new Vector2(0f, 380f), new Vector2(860f, 650f));
            CreateOverlayText(arena, "Arena Background Label", "ALIEN FRONTIER", 23, TextAnchor.UpperCenter, new RectOffset(22, 22, 20, 560));
            RectTransform monsterStatus = CreatePanel(arena, "Monster Status HUD", "battle/monster-status-panel", PanelColor, new Vector2(0f, 248f), new Vector2(570f, 92f));
            monsterHud = CreateOverlayText(monsterStatus, "Monster HUD Text", "MONSTER", 28, TextAnchor.UpperCenter, new RectOffset(12, 12, 8, 44));
            monsterHp = CreateFillBar(monsterStatus, "Monster HP Bar", "battle/monster-hp-bar", "battle/monster-hp-fill", new Vector2(0f, -22f), new Vector2(460f, 24f), HpColor);
            RectTransform portrait = CreatePanel(arena, "Monster Portrait Image", "battle/monster-portrait", MonsterColor, new Vector2(0f, 50f), new Vector2(470f, 330f));
            CreateOverlayText(portrait, "Monster Placeholder Text", "MONSTER\nSPRITE SLOT", 30, TextAnchor.MiddleCenter, new RectOffset(18, 18, 18, 18));
            enemyIntent = CreateOverlayText(arena, "Enemy Intent Text", "ENEMY INTENT: -", 24, TextAnchor.LowerCenter, new RectOffset(36, 36, 560, 22));
        }

        private static void CreateSlotMachine(RectTransform root, Text[] slotCells, Image[] slotCellIcons = null)
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
                    if (slotCellIcons != null && index < slotCellIcons.Length)
                    {
                        slotCellIcons[index] = CreateSlotCellIcon(cell, index);
                    }

                    slotCells[index] = CreateOverlayText(cell, $"Slot Cell Text {index:00}", "-", 22, TextAnchor.MiddleCenter, new RectOffset(8, 8, 8, 8));
                    slotCells[index].fontStyle = FontStyle.Bold;
                }
            }
        }

        private static Image CreateSlotCellIcon(Transform parent, int index)
        {
            RectTransform icon = CreateRect($"Slot Cell Icon {index:00}", parent, Vector2.zero, new Vector2(32f, 32f));
            Image image = icon.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
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

        private static void CreateInsideTextureBackdrop(RectTransform root)
        {
            Texture2D texture = LoadTexture(BackgroundInsideTexturePath);

            if (texture == null)
            {
                return;
            }

            RectTransform backdrop = CreateRect("Inside Texture Backdrop", root, Vector2.zero, Vector2.zero);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            backdrop.SetAsFirstSibling();

            RawImage image = backdrop.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static SlotPresentationManager CreateSlotPresentationLayer(
            RectTransform root,
            Text[] slotCells,
            Image[] slotCellIcons = null,
            Sprite[] slotIconSprites = null,
            Sprite[] slotSpinIconSprites = null)
        {
            RectTransform layer = CreateRect("Slot Presentation Layer", root, Vector2.zero, Vector2.zero);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            Image tapSkipGraphic = layer.gameObject.AddComponent<Image>();
            tapSkipGraphic.color = new Color(0f, 0f, 0f, 0f);
            tapSkipGraphic.raycastTarget = false;

            var manager = layer.gameObject.AddComponent<SlotPresentationManager>();
            AudioSource audioSource = layer.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            RectTransform relicOrigin = CreateRelicInventoryOrigin(layer, slotCellIcons != null, GetSpriteOrNull(slotIconSprites, 0));
            PatternPresentationView patternView = CreatePatternPresentationView(layer, slotCells, slotCellIcons);
            RelicPresentationView relicView = CreateRelicPresentationView(layer, relicOrigin);
            FinalResultView finalResultView = CreateFinalResultView(layer);
            AudioClip[] patternScaleClips = LoadPatternScaleClips();
            AudioClip relicClip = patternScaleClips.Length > 5 ? patternScaleClips[5] : null;
            AudioClip finalClip = patternScaleClips.Length > 7 ? patternScaleClips[7] : null;

            SlotCellSpinView spinView = null;
            if (slotCellIcons != null)
            {
                spinView = layer.gameObject.AddComponent<SlotCellSpinView>();
                spinView.Bind(slotCellIcons, slotIconSprites, slotSpinIconSprites);
            }

            manager.Bind(patternView, relicView, finalResultView, audioSource,
                patternScaleClips, relicClip, finalClip, tapSkipGraphic, spinView);

            patternView.HideImmediate();
            relicView.gameObject.SetActive(false);
            finalResultView.HideImmediate();

            return manager;
        }

        private static RectTransform CreateRelicInventoryOrigin(Transform parent, bool visible, Sprite iconSprite)
        {
            const string name = "Relic Inventory Origin";
            var position = new Vector2(-416f, -798f);
            var size = new Vector2(104f, 104f);

            if (!visible)
            {
                return CreateRect(name, parent, position, size);
            }

            RectTransform origin = CreatePanel(
                parent,
                name,
                "battle/presentation/relic-inventory-origin",
                new Color32(25, 31, 43, 245),
                position,
                size);
            Image originImage = origin.GetComponent<Image>();
            if (originImage != null)
            {
                originImage.raycastTarget = false;
            }

            RectTransform icon = CreateRect("Relic Inventory Icon", origin, new Vector2(0f, 12f), new Vector2(72f, 72f));
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            ApplySprite(iconImage, iconSprite, true);
            iconImage.enabled = iconSprite != null;

            Text label = CreateText(origin, "Relic Inventory Label", "RELIC", 14, TextAnchor.MiddleCenter, new Vector2(0f, -39f), new Vector2(92f, 22f));
            label.color = new Color32(255, 207, 59, 255);
            label.raycastTarget = false;
            return origin;
        }

        private static PatternPresentationView CreatePatternPresentationView(Transform parent, Text[] slotCells, Image[] slotCellIcons = null)
        {
            RectTransform panel = CreatePanel(
                parent,
                "Pattern Presentation Panel",
                "battle/presentation/pattern-panel",
                new Color32(30, 41, 62, 245),
                new Vector2(0f, 100f),
                new Vector2(620f, 135f));
            Text title = CreateText(panel, "Pattern Presentation Title", "PATTERN HIT!", 31, TextAnchor.MiddleCenter, new Vector2(0f, 42f), new Vector2(610f, 42f));
            title.fontStyle = FontStyle.Bold;
            Text description = CreateText(panel, "Pattern Presentation Description", "Matched symbols", 22, TextAnchor.MiddleCenter, new Vector2(0f, 4f), new Vector2(610f, 34f));
            Text bonus = CreateText(panel, "Pattern Presentation Bonus", "+0", 27, TextAnchor.MiddleCenter, new Vector2(0f, -42f), new Vector2(610f, 42f));
            bonus.color = new Color32(255, 207, 59, 255);
            bonus.fontStyle = FontStyle.Bold;
            var view = panel.gameObject.AddComponent<PatternPresentationView>();
            view.Bind(slotCells, panel, panel.GetComponent<Image>(), title, description, bonus, slotCellIcons);
            return view;
        }

        private static RelicPresentationView CreateRelicPresentationView(Transform parent, RectTransform originAnchor)
        {
            RectTransform panel = CreatePanel(
                parent,
                "Relic Presentation Panel",
                "battle/presentation/relic-panel",
                new Color32(26, 34, 52, 250),
                new Vector2(-245f, -390f),
                new Vector2(430f, 205f));
            RectTransform icon = CreatePanel(panel, "Relic Presentation Icon", "battle/presentation/relic-icon", new Color32(63, 72, 96, 255), new Vector2(-160f, 37f), new Vector2(92f, 92f));
            Text name = CreateText(panel, "Relic Presentation Name", "Relic", 26, TextAnchor.MiddleLeft, new Vector2(62f, 68f), new Vector2(250f, 38f));
            name.fontStyle = FontStyle.Bold;
            Text description = CreateText(panel, "Relic Presentation Description", "Effect description", 18, TextAnchor.MiddleLeft, new Vector2(62f, 18f), new Vector2(250f, 62f));
            Text value = CreateText(panel, "Relic Presentation Value", "+0", 24, TextAnchor.MiddleLeft, new Vector2(62f, -58f), new Vector2(250f, 40f));
            value.fontStyle = FontStyle.Bold;
            value.color = new Color32(255, 207, 59, 255);
            var view = panel.gameObject.AddComponent<RelicPresentationView>();
            view.Bind(panel, panel.GetComponent<Image>(), icon.GetComponent<Image>(), name, description, value, originAnchor);
            return view;
        }

        private static FinalResultView CreateFinalResultView(Transform parent)
        {
            RectTransform panel = CreatePanel(
                parent,
                "Final Result Presentation Panel",
                "battle/presentation/final-panel",
                new Color32(18, 29, 40, 250),
                new Vector2(0f, 100f),
                new Vector2(620f, 170f));
            Text title = CreateText(panel, "Final Result Title", "FINAL RESULT", 29, TextAnchor.MiddleCenter, new Vector2(0f, 48f), new Vector2(580f, 40f));
            title.fontStyle = FontStyle.Bold;
            Text summary = CreateText(panel, "Final Result Summary", "DMG 0 / DEF 0", 25, TextAnchor.MiddleCenter, new Vector2(0f, -22f), new Vector2(580f, 92f));
            summary.color = new Color32(255, 207, 59, 255);
            var view = panel.gameObject.AddComponent<FinalResultView>();
            view.Bind(panel, panel.GetComponent<Image>(), title, summary);
            return view;
        }

        private static AudioClip[] LoadPatternScaleClips()
        {
            return new[]
            {
                LoadAudioClip("Assets/Resources/Sounds/SFX_C_Low.wav"),
                LoadAudioClip("Assets/Resources/Sounds/SFX_D.wav"),
                LoadAudioClip("Assets/Resources/Sounds/SFX_E.wav"),
                LoadAudioClip("Assets/Resources/Sounds/SFX_F.wav"),
                LoadAudioClip("Assets/Resources/Sounds/SFX_G.wav"),
                LoadAudioClip("Assets/Resources/Sounds/SFX_A.wav"),
                LoadAudioClip("Assets/Resources/Sounds/SFX_B.wav"),
                LoadAudioClip("Assets/Resources/Sounds/SFX_C_High.wav")
            };
        }

        private static AudioClip LoadAudioClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static Texture2D LoadTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Sprite LoadFirstSprite(string path)
        {
            Sprite[] sprites = LoadSprites(path);
            return GetSpriteOrNull(sprites, 0);
        }

        private static Sprite[] LoadSprites(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            var loadedSprites = new List<Sprite>();

            if (assets != null)
            {
                for (int index = 0; index < assets.Length; index++)
                {
                    if (assets[index] is Sprite sprite)
                    {
                        loadedSprites.Add(sprite);
                    }
                }
            }

            Sprite[] sprites = loadedSprites.ToArray();

            if (sprites == null || sprites.Length == 0)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                return sprite != null ? new[] { sprite } : Array.Empty<Sprite>();
            }

            Array.Sort(
                sprites,
                (left, right) =>
                {
                    int yCompare = right.rect.y.CompareTo(left.rect.y);
                    return yCompare != 0 ? yCompare : left.rect.x.CompareTo(right.rect.x);
                });

            return sprites;
        }

        private static Sprite GetSpriteOrNull(Sprite[] sprites, int index)
        {
            if (sprites == null || index < 0 || index >= sprites.Length)
            {
                return null;
            }

            return sprites[index];
        }

        private static void ApplySprite(Image image, Sprite sprite, bool preserveAspect)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
        }

        private static SlotLeverView PatchRunBattleLever(GameObject prefabRoot)
        {
            RectTransform layoutRoot = FindRectTransform(prefabRoot, "Run Battle Root");

            if (layoutRoot == null)
            {
                UnityEngine.Debug.LogError(
                    "[SlotRogue] Run Battle Root was not found.");
                return null;
            }

            RectTransform spinButton = FindRectTransform(prefabRoot, "Spin Button");
            RectTransform lever = FindRectTransform(prefabRoot, "Spin Lever");

            if (lever == null)
            {
                var leverObject = new GameObject("Spin Lever", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lever = leverObject.GetComponent<RectTransform>();
                lever.SetParent(layoutRoot, false);
            }
            else if (lever.parent != layoutRoot)
            {
                lever.SetParent(layoutRoot, false);
            }

            ConfigureSpinLeverTransform(lever, spinButton);

            if (spinButton != null && spinButton.parent == lever.parent)
            {
                lever.SetSiblingIndex(spinButton.GetSiblingIndex() + 1);
            }

            Image leverImage = lever.GetComponent<Image>();
            if (leverImage == null)
            {
                leverImage = lever.gameObject.AddComponent<Image>();
            }

            leverImage.raycastTarget = false;
            leverImage.enabled = true;
            ApplySprite(leverImage, LoadFirstSprite(InGameLeverTexturePath), true);

            GameFlowImageSlot imageSlot = lever.GetComponent<GameFlowImageSlot>();
            if (imageSlot == null)
            {
                imageSlot = lever.gameObject.AddComponent<GameFlowImageSlot>();
            }

            imageSlot.Bind("battle/spin-lever", leverImage);

            SlotLeverView leverView = lever.GetComponent<SlotLeverView>();
            if (leverView == null)
            {
                leverView = lever.gameObject.AddComponent<SlotLeverView>();
            }

            leverView.Bind(leverImage, LoadSprites(InGameLeverTexturePath));

            RunBattleController controller = prefabRoot.GetComponentInChildren<RunBattleController>(true);
            if (controller != null)
            {
                var controllerSO = new UnityEditor.SerializedObject(controller);
                SerializedProperty leverProperty = controllerSO.FindProperty("_spinLeverView");

                if (leverProperty != null)
                {
                    leverProperty.objectReferenceValue = leverView;
                    controllerSO.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(prefabRoot);
            EditorUtility.SetDirty(lever.gameObject);
            return leverView;
        }

        private static int PatchLoadedRunBattleSceneInstances(out GameObject selectedLever)
        {
            selectedLever = null;
            int count = 0;
            RunBattleController[] controllers = UnityEngine.Object.FindObjectsByType<RunBattleController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int index = 0; index < controllers.Length; index++)
            {
                RunBattleController controller = controllers[index];

                if (controller == null || EditorUtility.IsPersistent(controller))
                {
                    continue;
                }

                Scene scene = controller.gameObject.scene;

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                SlotLeverView leverView = PatchRunBattleLever(controller.gameObject);

                if (leverView == null)
                {
                    continue;
                }

                selectedLever = leverView.gameObject;
                EditorSceneManager.MarkSceneDirty(scene);
                count++;
            }

            return count;
        }

        private static void ConfigureSpinLeverTransform(RectTransform lever, RectTransform spinButton)
        {
            if (lever == null)
            {
                return;
            }

            lever.anchorMin = spinButton != null ? spinButton.anchorMin : new Vector2(0.5f, 0.5f);
            lever.anchorMax = spinButton != null ? spinButton.anchorMax : new Vector2(0.5f, 0.5f);
            lever.pivot = new Vector2(0.5f, 0.5f);
            lever.sizeDelta = new Vector2(96f, 170f);
            lever.localScale = Vector3.one;
            lever.localRotation = Quaternion.identity;
            lever.anchoredPosition = spinButton != null
                ? spinButton.anchoredPosition + new Vector2((spinButton.sizeDelta.x * 0.5f) + 58f, 18f)
                : new Vector2(193f, -542f);
        }

        private static RectTransform FindRectTransform(GameObject root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            RectTransform[] transforms = root.GetComponentsInChildren<RectTransform>(true);

            for (int index = 0; index < transforms.Length; index++)
            {
                if (transforms[index].gameObject.name == objectName)
                {
                    return transforms[index];
                }
            }

            return null;
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
            RunMapEdgeDefinition edge)
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
            Image backgroundImage = AddImageSlot(background.gameObject, "scene-background", BackgroundColor);
            ApplySprite(backgroundImage, LoadFirstSprite(BackgroundOutsideTexturePath), false);
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

        private static RectTransform CreateSpriteImage(
            Transform parent,
            string name,
            string slotId,
            string spritePath,
            Vector2 position,
            Vector2 size,
            bool preserveAspect)
        {
            RectTransform transform = CreateRect(name, parent, position, size);
            Image image = AddImageSlot(transform.gameObject, slotId, Color.white);
            image.raycastTarget = false;
            ApplySprite(image, LoadFirstSprite(spritePath), preserveAspect);
            return transform;
        }

        private static Button CreateSpriteButton(
            Transform parent,
            string name,
            string slotId,
            string spritePath,
            Vector2 position,
            Vector2 size)
        {
            RectTransform transform = CreateRect(name, parent, position, size);
            Image image = AddImageSlot(transform.gameObject, slotId, Color.white);
            ApplySprite(image, LoadFirstSprite(spritePath), true);
            image.raycastTarget = true;
            Button button = transform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
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

        private static Image CreateVerticalFillBar(
            Transform parent,
            string name,
            string baseSlotId,
            string fillSlotId,
            Vector2 position,
            Vector2 size,
            Color32 color,
            Sprite sprite = null)
        {
            RectTransform bar = CreateRect(name, parent, position, size);
            bar.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            RectTransform fill = CreateRect($"{name} Fill", bar, Vector2.zero, new Vector2(size.x, size.y));
            fill.anchorMin = new Vector2(0.5f, 0f);
            fill.anchorMax = new Vector2(0.5f, 0f);
            fill.pivot = new Vector2(0.5f, 0f);
            fill.anchoredPosition = Vector2.zero;
            Image image = AddImageSlot(fill.gameObject, fillSlotId, color);
            ApplySprite(image, sprite, true);
            image.raycastTarget = false;
            return image;
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

        private static void PatchSpinViewBindings(GameObject prefabRoot)
        {
            var manager = prefabRoot.GetComponentInChildren<SlotPresentationManager>(true);
            if (manager == null)
            {
                UnityEngine.Debug.LogError(
                    "[SlotRogue] SlotPresentationManager was not found.");
                return;
            }

            var spinView = manager.gameObject.GetComponent<SlotCellSpinView>();
            if (spinView == null)
            {
                spinView = manager.gameObject.AddComponent<SlotCellSpinView>();
            }

            Image[] cellIcons = new Image[SlotSpinResult.CellCount];
            Image[] allImages = prefabRoot.GetComponentsInChildren<Image>(true);

            foreach (Image img in allImages)
            {
                for (int i = 0; i < SlotSpinResult.CellCount; i++)
                {
                    if (img.gameObject.name == $"Slot Cell Icon {i:00}")
                    {
                        cellIcons[i] = img;
                        break;
                    }
                }
            }

            int found = 0;
            foreach (var icon in cellIcons) if (icon != null) found++;
            if (found == 0)
            {
                UnityEngine.Debug.LogWarning(
                    "[SlotRogue] Could not find any 'Slot Cell Icon XX' objects. " +
                    "Check that slot cell icon names use 'Slot Cell Icon 00' through 'Slot Cell Icon 14'.");
            }

            Sprite[] sprites = LoadSprites(SlotIconTexturePath);
            Sprite[] spinSprites = LoadSprites(SlotIconAnimatedTexturePath);

            spinView.Bind(cellIcons, sprites, spinSprites);

            var managerSO = new UnityEditor.SerializedObject(manager);
            managerSO.FindProperty("_slotCellSpinView").objectReferenceValue = spinView;
            managerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEngine.Debug.Log(
                $"[SlotRogue] SlotCellSpinView patched. Cell icons: {found}/{SlotSpinResult.CellCount}, " +
                $"symbol sprites: {sprites.Length}, spin sprites: {spinSprites.Length}.");
        }
    }
}
