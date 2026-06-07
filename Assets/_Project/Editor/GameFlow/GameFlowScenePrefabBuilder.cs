using System;
using System.Collections.Generic;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;
using SlotRogue.Slot.Data;
using SlotRogue.UI.Combat.Presentation;
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
        private const string WorldPrefabFolder = "Assets/_Project/Prefabs/World/GameFlow";
        private const string EnemyFormationSlotPrefabName = "EnemyFormationSlot";
        private const string MonsterViewPrefabName = "MonsterView";
        private const string FloatingDamageTextPrefabName = "FloatingDamageTextView";
        private const int FormationSlotCount = 3;
        private const float FormationWorldSpacing = 2.7f;
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

        private const string InGameLeverTexturePath = "Assets/_Project/Resources/Textures/UI/Ingame_lever.png";
        private const string InGameSlotAnimationTexturePath = "Assets/_Project/Resources/Textures/UI/Ingame_Slot_ani.png";
        private const string SlotIconAnimationTexturePath = "Assets/_Project/Resources/Textures/UI/icon_slot_ani.png";

        [MenuItem("SlotRogue/Game Flow/Migrate Run Battle Hierarchy In Place (Preserve UI)")]
        [MenuItem("SlotRogue/Game Flow/Reorganize Run Battle Hierarchy (Keep Layout)")]
        public static void ReorganizeRunBattleHierarchy()
        {
            bool changed = false;
            string prefabPath = $"{PrefabFolder}/RunBattleView.prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset != null)
            {
                using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                {
                    changed |= ReorganizeRunBattleViewRoot(scope.prefabContentsRoot);
                    changed |= ApplyStrictRunBattleMvvmInPlace(
                        scope.prefabContentsRoot,
                        new[] { scope.prefabContentsRoot },
                        createCompositionRoot: false,
                        compositionParent: null);
                }
            }

            foreach (Scene scene in GetOpenRunBattleScenes())
            {
                changed |= ReorganizeRunBattleScene(scene);
            }

            changed |= ReorganizeClosedRunBattleSceneAsset();

            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("[SlotRogue] RunBattle hierarchy reorganized.");
            }
            else
            {
                UnityEngine.Debug.Log("[SlotRogue] RunBattle hierarchy already matches the organizer layout.");
            }
        }

        private static bool ReorganizeRunBattleScene(Scene scene)
        {
            bool changed = false;
            GameObject sceneRoot = FindRootGameObject(scene, "RunBattleSceneRoot");

            if (sceneRoot == null)
            {
                sceneRoot = new GameObject("RunBattleSceneRoot");
                SceneManager.MoveGameObjectToScene(sceneRoot, scene);
                changed = true;
            }

            Transform composition = EnsureOrganizerGroup(sceneRoot.transform, "00_Composition", 0, ref changed);
            Transform systems = EnsureOrganizerGroup(sceneRoot.transform, "10_Systems", 1, ref changed);
            Transform world = EnsureOrganizerGroup(sceneRoot.transform, "20_World", 2, ref changed);
            Transform ui = EnsureOrganizerGroup(sceneRoot.transform, "30_UI", 3, ref changed);

            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                GameObject root = roots[index];

                if (root == sceneRoot)
                {
                    continue;
                }

                if (root.GetComponent<Camera>() != null || root.GetComponent<EventSystem>() != null)
                {
                    changed |= MoveTransform(root.transform, systems);
                    continue;
                }

                if (HasRunBattleScreen(root))
                {
                    changed |= MoveTransform(root.transform, ui);
                    changed |= ReorganizeRunBattleViewRoot(root, composition, systems, world);
                    continue;
                }

                if (root.name == "BattleArenaRoot")
                {
                    changed |= MoveTransform(root.transform, world);
                }
            }

            changed |= ApplyStrictRunBattleMvvmInPlace(
                sceneRoot,
                scene.GetRootGameObjects(),
                createCompositionRoot: true,
                compositionParent: composition);
            EditorSceneManager.MarkSceneDirty(scene);
            return changed;
        }

        private static bool ReorganizeClosedRunBattleSceneAsset()
        {
            string scenePath = $"{SceneFolder}/RunBattle.unity";
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                return false;
            }

            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene openScene = SceneManager.GetSceneAt(index);
                if (openScene.isLoaded && openScene.path == scenePath)
                {
                    return false;
                }
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            bool changed = ReorganizeRunBattleScene(scene);
            if (changed)
            {
                EditorSceneManager.SaveScene(scene);
            }

            EditorSceneManager.CloseScene(scene, removeScene: true);
            return changed;
        }

        private static bool HasRunBattleScreen(GameObject root)
        {
            return root != null &&
                (root.name == "RunBattleView" ||
                root.GetComponentInChildren<RunBattleScreenView>(true) != null ||
                FindDeepChild(root.transform, "Run Battle Root") != null);
        }

        private static bool ReorganizeRunBattleViewRoot(GameObject root)
        {
            return ReorganizeRunBattleViewRoot(
                root,
                compositionDestination: null,
                systemsDestination: null,
                worldDestination: null);
        }

        private static bool ReorganizeRunBattleViewRoot(
            GameObject root,
            Transform compositionDestination,
            Transform systemsDestination,
            Transform worldDestination)
        {
            if (root == null)
            {
                return false;
            }

            bool changed = false;
            Transform rootTransform = root.transform;
            changed |= FlattenNestedRunBattleSceneGroups(
                rootTransform,
                compositionDestination,
                systemsDestination,
                worldDestination);

            Transform battleRoot = FindDeepChild(rootTransform, "Run Battle Root");
            if (battleRoot != null)
            {
                changed |= ReorganizeRunBattleUiRoot(battleRoot);
            }

            return changed;
        }

        private static bool FlattenNestedRunBattleSceneGroups(
            Transform viewRoot,
            Transform compositionDestination,
            Transform systemsDestination,
            Transform worldDestination)
        {
            bool changed = false;
            changed |= MoveGroupChildrenOrDelete(viewRoot, "00_Composition", compositionDestination);
            changed |= MoveGroupChildrenOrDelete(viewRoot, "10_Systems", systemsDestination);
            changed |= MoveGroupChildrenOrDelete(viewRoot, "20_World", worldDestination);

            Transform nestedUi = FindDirectChild(viewRoot, "30_UI");
            if (nestedUi != null)
            {
                changed |= MoveAllChildren(nestedUi, viewRoot);
                UnityEngine.Object.DestroyImmediate(nestedUi.gameObject, true);
                changed = true;
            }

            return changed;
        }

        private static bool MoveGroupChildrenOrDelete(
            Transform parent,
            string groupName,
            Transform destination)
        {
            Transform group = FindDirectChild(parent, groupName);
            if (group == null)
            {
                return false;
            }

            if (destination != null)
            {
                MoveAllChildren(group, destination);
            }

            UnityEngine.Object.DestroyImmediate(group.gameObject, true);
            return true;
        }

        private static bool MoveAllChildren(Transform source, Transform destination)
        {
            bool changed = false;
            while (source.childCount > 0)
            {
                Transform child = source.GetChild(0);
                changed |= MoveTransform(child, destination);
            }

            return changed;
        }

        private static bool ReorganizeRunBattleUiRoot(Transform battleRoot)
        {
            bool changed = false;
            Transform background = EnsureOrganizerGroup(battleRoot, "00_Background", 0, ref changed);
            Transform playerHud = EnsureOrganizerGroup(battleRoot, "10_PlayerHUD", 1, ref changed);
            Transform battlefield = EnsureOrganizerGroup(battleRoot, "20_Battlefield", 2, ref changed);
            Transform slotMachine = EnsureOrganizerGroup(battleRoot, "30_SlotMachine", 3, ref changed);
            Transform actions = EnsureOrganizerGroup(battleRoot, "40_Actions", 4, ref changed);
            Transform bottomHud = EnsureOrganizerGroup(battleRoot, "50_BottomHUD", 5, ref changed);
            Transform presentationOverlay = EnsureOrganizerGroup(battleRoot, "90_PresentationOverlay", 6, ref changed);

            changed |= MoveNamedChild(battleRoot, "Inside Top", background);
            changed |= MoveNamedChild(battleRoot, "Inside Bottom", background);
            changed |= MoveNamedChild(battleRoot, "Currency HUD", playerHud);
            changed |= MoveNamedChild(battleRoot, "Pause Button", playerHud);
            changed |= MoveNamedChild(battleRoot, "Player Status HUD", playerHud);
            changed |= MoveNamedChild(battleRoot, "Wave HUD", playerHud);
            changed |= MoveNamedChild(battleRoot, "Settings HUD", playerHud);
            changed |= MoveNamedChild(battleRoot, "Battle Arena Image", battlefield);
            changed |= MoveNamedChild(battleRoot, "Enemy Intent Panel", battlefield);
            changed |= MoveNamedChild(battleRoot, "Slot Machine Panel", slotMachine);
            changed |= MoveNamedChild(battleRoot, "Attack Result Panel", actions);
            changed |= MoveNamedChild(battleRoot, "Spin Button", actions);
            changed |= MoveNamedChild(battleRoot, "Spin Lever", actions);
            changed |= MoveNamedChild(battleRoot, "Claim Reward Button", actions);
            changed |= MoveNamedChild(battleRoot, "Return To Start Button", actions);
            changed |= MoveNamedChild(battleRoot, "Next Attack Panel", actions);
            changed |= MoveNamedChild(battleRoot, "Battle Status Panel", bottomHud);
            changed |= MoveNamedChild(battleRoot, "Energy Panel", bottomHud);
            changed |= MoveNamedChild(battleRoot, "Credits Panel", bottomHud);
            changed |= MoveNamedChild(battleRoot, "Presentation Overlay", presentationOverlay);
            changed |= MoveNamedChild(battleRoot, "Slot Presentation Layer", presentationOverlay);

            return changed;
        }

        private sealed class RunBattleMigrationBindings
        {
            public Text[] SlotCells = Array.Empty<Text>();
            public Image[] SlotCellIcons = Array.Empty<Image>();
            public Text StatusText;
            public Text SlotResultText;
            public Text AttackResultText;
            public Text PlayerHudText;
            public Text EnemyIntentText;
            public Image PlayerHpFill;
            public Image PlayerShieldFill;
            public EnemyFormationSlotView[] FormationSlots = Array.Empty<EnemyFormationSlotView>();
            public MonsterView[] MonsterViews = Array.Empty<MonsterView>();
            public Button SpinButton;
            public Button ContinueButton;
            public Button RestartButton;
            public RectTransform FloatingTextRoot;
            public RectTransform PlayerDamageAnchor;
            public FloatingDamageTextView FloatingDamageTextPrefab;
            public MonsterDefinition MonsterDefinition;
            public SlotLeverView SpinLeverView;
            public SlotMachineFrameView SlotMachineFrameView;
            public SlotPresentationManager SlotPresentationManager;
        }

        private static bool ApplyStrictRunBattleMvvmInPlace(
            GameObject root,
            GameObject[] searchRoots,
            bool createCompositionRoot,
            Transform compositionParent)
        {
            if (root == null)
            {
                return false;
            }

            RunBattleScreenView existingScreenView =
                root.GetComponentInChildren<RunBattleScreenView>(true);

            if (existingScreenView == null && !HasRunBattleScreen(root))
            {
                return false;
            }

            bool changed = false;
            RunBattleMigrationBindings bindings = CaptureRunBattleBindings(root, searchRoots);

            GameObject screenObject = ResolveRunBattleScreenObject(root, searchRoots, existingScreenView);
            RunBattleScreenView screenView = EnsureComponent<RunBattleScreenView>(screenObject, ref changed);

            RunBattlePlayerHudView playerHudView = EnsureComponent<RunBattlePlayerHudView>(
                ResolveGameObject(searchRoots, "Player Status HUD", screenObject),
                ref changed);
            SetObjectField(playerHudView, "_hudText", bindings.PlayerHudText, ref changed);
            SetObjectField(playerHudView, "_hpFill", bindings.PlayerHpFill, ref changed);
            SetObjectField(playerHudView, "_shieldFill", bindings.PlayerShieldFill, ref changed);

            RunBattleStatusView statusView = EnsureComponent<RunBattleStatusView>(
                ResolveGameObject(searchRoots, "Battle Status Panel", screenObject),
                ref changed);
            SetObjectField(statusView, "_statusText", bindings.StatusText, ref changed);
            SetObjectField(statusView, "_enemyIntentText", bindings.EnemyIntentText, ref changed);

            GameObject slotMachineObject = ResolveGameObject(searchRoots, "Slot Machine Panel", screenObject);
            RunBattleSlotBoardView slotBoardView = EnsureComponent<RunBattleSlotBoardView>(
                slotMachineObject,
                ref changed);
            SetObjectArrayField(slotBoardView, "_slotCells", bindings.SlotCells, ref changed);
            SlotMachineFrameView slotMachineFrameView =
                bindings.SlotMachineFrameView != null
                    ? bindings.SlotMachineFrameView
                    : EnsureComponent<SlotMachineFrameView>(slotMachineObject, ref changed);
            BindSlotMachineFrameView(slotMachineFrameView, ref changed);

            SlotCellSpinView slotCellSpinView =
                FindFirstComponent<SlotCellSpinView>(searchRoots);
            BindSlotCellSpinView(slotCellSpinView, bindings.SlotCellIcons, ref changed);

            GameObject actionObject =
                ResolveGameObject(searchRoots, "Attack Result Panel", null) ??
                ResolveGameObject(searchRoots, "Attack Power HUD", screenObject);
            RunBattleActionView actionView = EnsureComponent<RunBattleActionView>(
                actionObject,
                ref changed);
            SetObjectField(actionView, "_slotResultText", bindings.SlotResultText, ref changed);
            SetObjectField(actionView, "_attackResultText", bindings.AttackResultText, ref changed);
            SetObjectField(actionView, "_spinButton", bindings.SpinButton, ref changed);
            SetObjectField(actionView, "_continueButton", bindings.ContinueButton, ref changed);
            SetObjectField(actionView, "_restartButton", bindings.RestartButton, ref changed);

            RunBattlePresentationOverlayView overlayView =
                EnsureComponent<RunBattlePresentationOverlayView>(
                    ResolveGameObject(searchRoots, "Presentation Overlay", screenObject),
                    ref changed);
            SetObjectField(overlayView, "_floatingTextRoot", bindings.FloatingTextRoot, ref changed);
            SetObjectField(overlayView, "_playerDamageAnchor", bindings.PlayerDamageAnchor, ref changed);

            EnemyFormationView enemyFormationView = null;
            RunBattleWorldView worldView = null;
            GameObject worldObject =
                ResolveGameObject(searchRoots, "BattleArenaRoot", null) ??
                ResolveGameObject(searchRoots, "Formation Slots Root", null) ??
                ResolveGameObject(searchRoots, "FormationSlotsRoot", null);

            if (worldObject != null)
            {
                worldView = EnsureComponent<RunBattleWorldView>(worldObject, ref changed);
                GameObject formationObject =
                    ResolveGameObject(searchRoots, "Formation Slots Root", null) ??
                    ResolveGameObject(searchRoots, "FormationSlotsRoot", worldObject);
                enemyFormationView = EnsureComponent<EnemyFormationView>(formationObject, ref changed);
                if (bindings.MonsterViews.Length > 0)
                {
                    SetObjectArrayField(
                        enemyFormationView,
                        "_monsterViews",
                        bindings.MonsterViews,
                        ref changed);
                    SetObjectArrayField(
                        enemyFormationView,
                        "_formationSlotViews",
                        Array.Empty<EnemyFormationSlotView>(),
                        ref changed);
                }
                else
                {
                    SetObjectArrayField(
                        enemyFormationView,
                        "_formationSlotViews",
                        bindings.FormationSlots,
                        ref changed);
                    SetObjectArrayField(
                        enemyFormationView,
                        "_monsterViews",
                        Array.Empty<MonsterView>(),
                        ref changed);
                }

                SetObjectField(worldView, "_battleShakeRoot", worldObject.transform, ref changed);
                SetObjectField(worldView, "_enemyFormationView", enemyFormationView, ref changed);
            }

            SetObjectField(screenView, "_playerHudView", playerHudView, ref changed);
            SetObjectField(screenView, "_statusView", statusView, ref changed);
            SetObjectField(screenView, "_slotBoardView", slotBoardView, ref changed);
            SetObjectField(screenView, "_actionView", actionView, ref changed);
            SetObjectField(screenView, "_presentationOverlayView", overlayView, ref changed);
            SetObjectField(screenView, "_worldView", worldView, ref changed);

            if (createCompositionRoot)
            {
                GameObject compositionObject = ResolveGameObject(searchRoots, "RunBattleCompositionRoot", null);
                if (compositionObject == null)
                {
                    compositionParent ??= ResolveTransform(searchRoots, "00_Composition", root.transform);
                    compositionObject = new GameObject("RunBattleCompositionRoot");
                    compositionObject.transform.SetParent(compositionParent, false);
                    changed = true;
                }
                else if (compositionParent != null)
                {
                    changed |= MoveTransform(compositionObject.transform, compositionParent);
                }

                RunBattleCompositionRoot compositionRoot =
                    EnsureComponent<RunBattleCompositionRoot>(compositionObject, ref changed);
                SetObjectField(compositionRoot, "_view", screenView, ref changed);
                SetObjectField(
                    compositionRoot,
                    "_floatingDamageTextPrefab",
                    bindings.FloatingDamageTextPrefab ?? EnsureFloatingDamageTextPrefab(),
                    ref changed);
                SetObjectField(compositionRoot, "_monsterDefinition", bindings.MonsterDefinition, ref changed);
                SetObjectField(compositionRoot, "_spinLeverView", bindings.SpinLeverView, ref changed);
                SetObjectField(compositionRoot, "_slotMachineFrameView", slotMachineFrameView, ref changed);
                SetObjectField(
                    compositionRoot,
                    "_slotPresentationManager",
                    bindings.SlotPresentationManager,
                    ref changed);
            }
            else
            {
                changed |= RemoveCompositionRootObjects(root);
            }

            return changed;
        }

        private static RunBattleMigrationBindings CaptureRunBattleBindings(
            GameObject root,
            GameObject[] searchRoots)
        {
            RunBattleCompositionRoot compositionRoot = FindFirstComponent<RunBattleCompositionRoot>(searchRoots);
            var bindings = new RunBattleMigrationBindings
            {
                SlotCells = FindSlotCellTexts(searchRoots),
                SlotCellIcons = FindSlotCellIconImages(searchRoots),
                StatusText = FindComponentByName<Text>(searchRoots, "Battle Status Text"),
                SlotResultText = FindComponentByName<Text>(searchRoots, "Next Attack Text"),
                AttackResultText =
                    FindComponentByName<Text>(searchRoots, "Attack Power Text") ??
                    FindComponentByName<Text>(searchRoots, "Attack Result Value"),
                PlayerHudText = FindComponentByName<Text>(searchRoots, "Player HUD Values"),
                EnemyIntentText = FindComponentByName<Text>(searchRoots, "Enemy Intent Text"),
                PlayerHpFill =
                    FindImageSlotById(searchRoots, "battle/player-hp-fill") ??
                    FindComponentByName<Image>(searchRoots, "Player HP Bar Fill"),
                PlayerShieldFill =
                    FindImageSlotById(searchRoots, "battle/player-shield-fill") ??
                    FindComponentByName<Image>(searchRoots, "Player Shield Bar Fill"),
                FormationSlots = FindFormationSlots(searchRoots),
                MonsterViews = FindMonsterViews(searchRoots),
                SpinButton = FindComponentByName<Button>(searchRoots, "Spin Button"),
                ContinueButton = FindComponentByName<Button>(searchRoots, "Claim Reward Button"),
                RestartButton = FindComponentByName<Button>(searchRoots, "Return To Start Button"),
                FloatingTextRoot =
                    ResolveTransform(searchRoots, "Presentation Overlay", root.transform) as RectTransform,
                PlayerDamageAnchor =
                    ResolveTransform(searchRoots, "player-damage-anchor", null) as RectTransform ??
                    ResolveTransform(searchRoots, "Player Damage Anchor", null) as RectTransform,
                FloatingDamageTextPrefab =
                    ReadObject<FloatingDamageTextView>(compositionRoot, "_floatingDamageTextPrefab"),
                MonsterDefinition = ReadObject<MonsterDefinition>(compositionRoot, "_monsterDefinition"),
                SpinLeverView = FindComponentByName<SlotLeverView>(searchRoots, "Spin Lever"),
                SlotMachineFrameView = FindFirstComponent<SlotMachineFrameView>(searchRoots),
                SlotPresentationManager =
                    FindFirstComponent<SlotPresentationManager>(searchRoots)
            };

            return bindings;
        }

        private static bool RemoveCompositionRootObjects(GameObject root)
        {
            bool changed = false;
            RunBattleCompositionRoot[] compositionRoots =
                root.GetComponentsInChildren<RunBattleCompositionRoot>(true);
            for (int index = 0; index < compositionRoots.Length; index++)
            {
                GameObject gameObject = compositionRoots[index].gameObject;
                UnityEngine.Object.DestroyImmediate(gameObject, true);
                changed = true;
            }

            return changed;
        }

        private static GameObject ResolveRunBattleScreenObject(
            GameObject root,
            GameObject[] searchRoots,
            RunBattleScreenView existingScreenView)
        {
            if (existingScreenView != null)
            {
                return existingScreenView.gameObject;
            }

            Canvas canvas = FindFirstComponent<Canvas>(searchRoots);
            return canvas != null ? canvas.gameObject : root;
        }

        private static T EnsureComponent<T>(GameObject gameObject, ref bool changed)
            where T : Component
        {
            if (gameObject == null)
            {
                return null;
            }

            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            changed = true;
            component = gameObject.AddComponent<T>();
            EditorUtility.SetDirty(gameObject);
            return component;
        }

        private static void BindSlotMachineFrameView(
            SlotMachineFrameView slotMachineFrameView,
            ref bool changed)
        {
            if (slotMachineFrameView == null)
            {
                return;
            }

            Image animationImage = EnsureSlotMachineAnimationImage(slotMachineFrameView.transform as RectTransform, ref changed);
            Sprite[] sprites = LoadSprites(InGameSlotAnimationTexturePath);
            SetObjectField(slotMachineFrameView, "_animationImage", animationImage, ref changed);
            SetObjectArrayField(slotMachineFrameView, "_slotMachineSprites", sprites, ref changed);
            slotMachineFrameView.SetIdleImmediate();
            EditorUtility.SetDirty(slotMachineFrameView);

            if (animationImage != null)
            {
                EditorUtility.SetDirty(animationImage);
            }
        }

        private static Image EnsureSlotMachineAnimationImage(RectTransform slotMachine, ref bool changed)
        {
            if (slotMachine == null)
            {
                return null;
            }

            Transform existing = FindDirectChild(slotMachine, SlotMachineFrameView.AnimationImageName);
            RectTransform animationTransform = existing as RectTransform;
            if (animationTransform == null)
            {
                animationTransform = CreateRect(
                    SlotMachineFrameView.AnimationImageName,
                    slotMachine,
                    Vector2.zero,
                    Vector2.zero);
                changed = true;
            }

            if (animationTransform.GetSiblingIndex() != 0)
            {
                animationTransform.SetAsFirstSibling();
                changed = true;
            }

            changed |= SetSlotMachineAnimationRect(animationTransform, slotMachine);

            Image image = animationTransform.GetComponent<Image>();
            if (image == null)
            {
                image = animationTransform.gameObject.AddComponent<Image>();
                changed = true;
            }

            if (image.raycastTarget)
            {
                image.raycastTarget = false;
                changed = true;
            }

            if (image.preserveAspect)
            {
                image.preserveAspect = false;
                changed = true;
            }

            if (image.color != Color.white)
            {
                image.color = Color.white;
                changed = true;
            }

            return image;
        }

        private static bool SetSlotMachineAnimationRect(
            RectTransform rectTransform,
            RectTransform slotMachine)
        {
            bool changed = false;
            Vector2 centerAnchor = new(0.5f, 0.5f);
            if (rectTransform.anchorMin != centerAnchor)
            {
                rectTransform.anchorMin = centerAnchor;
                changed = true;
            }

            if (rectTransform.anchorMax != centerAnchor)
            {
                rectTransform.anchorMax = centerAnchor;
                changed = true;
            }

            if (rectTransform.anchoredPosition != Vector2.zero)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                changed = true;
            }

            Vector2 targetSize = SlotMachineFrameView.ResolveAnimationImageSize(ResolveRectSize(slotMachine));
            if (rectTransform.sizeDelta != targetSize)
            {
                rectTransform.sizeDelta = targetSize;
                changed = true;
            }

            if (rectTransform.pivot != new Vector2(0.5f, 0.5f))
            {
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                changed = true;
            }

            if (rectTransform.localScale != Vector3.one)
            {
                rectTransform.localScale = Vector3.one;
                changed = true;
            }

            return changed;
        }

        private static Vector2 ResolveRectSize(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return new Vector2(SlotMachineFrameView.BaseFrameWidth, SlotMachineFrameView.BaseFrameHeight);
            }

            Vector2 size = rectTransform.rect.size;
            if (size.x > 0f && size.y > 0f)
            {
                return size;
            }

            size = rectTransform.sizeDelta;
            return size.x > 0f && size.y > 0f
                ? size
                : new Vector2(SlotMachineFrameView.BaseFrameWidth, SlotMachineFrameView.BaseFrameHeight);
        }

        private static void BindSlotCellSpinView(
            SlotCellSpinView slotCellSpinView,
            Image[] slotCellIcons,
            ref bool changed)
        {
            if (slotCellSpinView == null)
            {
                return;
            }

            if (slotCellIcons != null && slotCellIcons.Length == SlotSpinResult.CellCount)
            {
                SetObjectArrayField(slotCellSpinView, "_cellIcons", slotCellIcons, ref changed);
            }

            Sprite[] spinSprites = LoadSprites(SlotIconAnimationTexturePath);
            if (spinSprites.Length > 0)
            {
                SetObjectArrayField(slotCellSpinView, "_spinSymbolSprites", spinSprites, ref changed);
            }

            slotCellSpinView.EnsureReferences();
            EditorUtility.SetDirty(slotCellSpinView);
        }

        private static GameObject ResolveGameObject(
            GameObject[] searchRoots,
            string objectName,
            GameObject fallback)
        {
            Transform transform = ResolveTransform(searchRoots, objectName, null);
            return transform != null ? transform.gameObject : fallback;
        }

        private static Transform ResolveTransform(
            GameObject[] searchRoots,
            string objectName,
            Transform fallback)
        {
            if (searchRoots != null)
            {
                for (int index = 0; index < searchRoots.Length; index++)
                {
                    if (searchRoots[index] == null)
                    {
                        continue;
                    }

                    Transform found = FindDeepChild(searchRoots[index].transform, objectName);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return fallback;
        }

        private static T FindFirstComponent<T>(GameObject[] searchRoots)
            where T : Component
        {
            if (searchRoots == null)
            {
                return null;
            }

            for (int index = 0; index < searchRoots.Length; index++)
            {
                if (searchRoots[index] == null)
                {
                    continue;
                }

                T component = searchRoots[index].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static T FindComponentByName<T>(
            GameObject[] searchRoots,
            string objectName)
            where T : Component
        {
            Transform transform = ResolveTransform(searchRoots, objectName, null);
            return transform != null ? transform.GetComponent<T>() : null;
        }

        private static Image FindImageSlotById(GameObject[] searchRoots, string slotId)
        {
            if (searchRoots == null)
            {
                return null;
            }

            for (int rootIndex = 0; rootIndex < searchRoots.Length; rootIndex++)
            {
                if (searchRoots[rootIndex] == null)
                {
                    continue;
                }

                GameFlowImageSlot[] slots =
                    searchRoots[rootIndex].GetComponentsInChildren<GameFlowImageSlot>(true);
                for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                {
                    if (slots[slotIndex].SlotId == slotId)
                    {
                        return slots[slotIndex].Image != null
                            ? slots[slotIndex].Image
                            : slots[slotIndex].GetComponent<Image>();
                    }
                }
            }

            return null;
        }

        private static Text[] FindSlotCellTexts(GameObject[] searchRoots)
        {
            Text[] cells = new Text[SlotSpinResult.CellCount];
            bool foundAny = false;
            for (int index = 0; index < cells.Length; index++)
            {
                cells[index] = FindComponentByName<Text>(searchRoots, $"Slot Cell Text {index:00}");
                foundAny |= cells[index] != null;
            }

            return foundAny ? cells : Array.Empty<Text>();
        }

        private static Image[] FindSlotCellIconImages(GameObject[] searchRoots)
        {
            var icons = new List<IndexedSlotCellIcon>(SlotSpinResult.CellCount);
            if (searchRoots != null)
            {
                for (int index = 0; index < searchRoots.Length; index++)
                {
                    if (searchRoots[index] == null)
                    {
                        continue;
                    }

                    CollectSlotCellIconImages(searchRoots[index].transform, icons);
                }
            }

            if (icons.Count == 0)
            {
                return Array.Empty<Image>();
            }

            icons.Sort((left, right) => left.Index.CompareTo(right.Index));
            var cells = new Image[Math.Min(SlotSpinResult.CellCount, icons.Count)];
            for (int index = 0; index < cells.Length; index++)
            {
                cells[index] = icons[index].Image;
            }

            return cells;
        }

        private static void CollectSlotCellIconImages(
            Transform parent,
            List<IndexedSlotCellIcon> icons)
        {
            if (parent == null)
            {
                return;
            }

            if (TryGetSlotCellIconIndex(parent.name, out int slotIndex))
            {
                Image image = parent.GetComponent<Image>();
                if (image != null)
                {
                    icons.Add(new IndexedSlotCellIcon(slotIndex, image));
                }
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                CollectSlotCellIconImages(parent.GetChild(index), icons);
            }
        }

        private static bool TryGetSlotCellIconIndex(string objectName, out int index)
        {
            index = -1;

            const string baseName = "Slot Cell Icon";
            if (objectName == baseName)
            {
                index = 0;
                return true;
            }

            if (string.IsNullOrEmpty(objectName) ||
                !objectName.StartsWith(baseName + " (", StringComparison.Ordinal) ||
                !objectName.EndsWith(")", StringComparison.Ordinal))
            {
                return false;
            }

            int startIndex = baseName.Length + 2;
            int length = objectName.Length - startIndex - 1;
            if (length <= 0)
            {
                return false;
            }

            return int.TryParse(objectName.Substring(startIndex, length), out index);
        }

        private readonly struct IndexedSlotCellIcon
        {
            public IndexedSlotCellIcon(int index, Image image)
            {
                Index = index;
                Image = image;
            }

            public int Index { get; }
            public Image Image { get; }
        }

        private static EnemyFormationSlotView[] FindFormationSlots(GameObject[] searchRoots)
        {
            var slots = new List<EnemyFormationSlotView>();
            if (searchRoots != null)
            {
                for (int index = 0; index < searchRoots.Length; index++)
                {
                    if (searchRoots[index] == null)
                    {
                        continue;
                    }

                    slots.AddRange(searchRoots[index].GetComponentsInChildren<EnemyFormationSlotView>(true));
                }
            }

            slots.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return slots.ToArray();
        }

        private static MonsterView[] FindMonsterViews(GameObject[] searchRoots)
        {
            var monsterViews = new List<MonsterView>();
            if (searchRoots != null)
            {
                for (int index = 0; index < searchRoots.Length; index++)
                {
                    if (searchRoots[index] == null)
                    {
                        continue;
                    }

                    monsterViews.AddRange(searchRoots[index].GetComponentsInChildren<MonsterView>(true));
                }
            }

            monsterViews.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return monsterViews.ToArray();
        }

        private static T ReadObject<T>(Component component, string propertyName)
            where T : UnityEngine.Object
        {
            if (component == null)
            {
                return null;
            }

            var serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static void SetObjectField(
            Component component,
            string propertyName,
            UnityEngine.Object value,
            ref bool changed)
        {
            if (component == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            changed = true;
        }

        private static void SetObjectArrayField<T>(
            Component component,
            string propertyName,
            T[] values,
            ref bool changed)
            where T : UnityEngine.Object
        {
            if (component == null)
            {
                return;
            }

            values ??= Array.Empty<T>();
            var serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return;
            }

            bool arrayChanged = property.arraySize != values.Length;
            if (!arrayChanged)
            {
                for (int index = 0; index < values.Length; index++)
                {
                    if (property.GetArrayElementAtIndex(index).objectReferenceValue != values[index])
                    {
                        arrayChanged = true;
                        break;
                    }
                }
            }

            if (!arrayChanged)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            changed = true;
        }

        private static GameObject FindRootGameObject(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == name)
                {
                    return roots[index];
                }
            }

            return null;
        }

        private static Transform EnsureOrganizerGroup(
            Transform parent,
            string name,
            int siblingIndex,
            ref bool changed)
        {
            Transform group = FindDirectChild(parent, name);
            if (group == null)
            {
                GameObject groupObject = parent is RectTransform
                    ? new GameObject(name, typeof(RectTransform))
                    : new GameObject(name);
                group = groupObject.transform;
                group.SetParent(parent, false);
                ConfigureOrganizerTransform(group);
                changed = true;
            }

            int targetSiblingIndex = Mathf.Clamp(siblingIndex, 0, parent.childCount - 1);
            if (group.GetSiblingIndex() != targetSiblingIndex)
            {
                group.SetSiblingIndex(targetSiblingIndex);
                changed = true;
            }

            return group;
        }

        private static void ConfigureOrganizerTransform(Transform group)
        {
            group.localPosition = Vector3.zero;
            group.localRotation = Quaternion.identity;
            group.localScale = Vector3.one;

            if (group is RectTransform rectTransform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private static bool MoveNamedChild(Transform searchRoot, string childName, Transform newParent)
        {
            Transform child = FindDeepChild(searchRoot, childName);
            if (child == null)
            {
                return false;
            }

            return MoveTransform(child, newParent);
        }

        private static bool MoveDirectChild(Transform searchRoot, string childName, Transform newParent)
        {
            Transform child = FindDirectChild(searchRoot, childName);
            if (child == null)
            {
                return false;
            }

            return MoveTransform(child, newParent);
        }

        private static bool MoveTransform(Transform child, Transform newParent)
        {
            if (child == null || newParent == null || child == newParent)
            {
                return false;
            }

            if (child.parent == newParent || IsAncestor(child, newParent))
            {
                return false;
            }

            child.SetParent(newParent, false);
            return true;
        }

        private static bool IsAncestor(Transform possibleAncestor, Transform child)
        {
            Transform current = child.parent;
            while (current != null)
            {
                if (current == possibleAncestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
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
            BuildAllInternal();
        }

        private static void BuildAllInternal()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(WorldPrefabFolder);

            BuildGameStart();
            BuildArtifactSelection();
            BuildRunMap();
            BuildRunBattle();
            BuildRunReward();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("SlotRogue/Game Flow/Rebuild Run Battle UI Prefab Only")]
        public static void BuildRunBattleOnly()
        {
            BuildRunBattleOnlyInternal();
        }

        private static void BuildRunBattleOnlyInternal()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(WorldPrefabFolder);

            BuildRunBattle();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildAllFromCommandLine()
        {
            BuildAll();
        }

        private static bool EnsureSceneSwitchAllowed()
        {
            return Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        private static bool ShouldBuildGeneratedAsset(string prefabName, string sceneName)
        {
            string prefabPath = $"{PrefabFolder}/{prefabName}.prefab";
            string scenePath = $"{SceneFolder}/{sceneName}.unity";
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
            bool sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null;

            if (!prefabExists && !sceneExists)
            {
                return EnsureSceneSwitchAllowed();
            }

            UnityEngine.Debug.LogWarning(
                $"[SlotRogue] Skipped {prefabName}/{sceneName} rebuild because existing assets are preserved. " +
                "Delete the target assets manually first if you intentionally want to regenerate them.");
            return false;
        }

        private static void BuildGameStart()
        {
            if (!ShouldBuildGeneratedAsset("GameStartView", "GameStart"))
            {
                return;
            }

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
            if (!ShouldBuildGeneratedAsset("StartArtifactSelectionView", "StartArtifactSelection"))
            {
                return;
            }

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
            if (!ShouldBuildGeneratedAsset("RunRewardView", "RunReward"))
            {
                return;
            }

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
            if (!ShouldBuildGeneratedAsset("RunMapView", "RunMap"))
            {
                return;
            }

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
            string prefabPath = $"{PrefabFolder}/RunBattleView.prefab";
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;

            if (prefabExists)
            {
                ReorganizeRunBattleHierarchy();
                UnityEngine.Debug.Log(
                    "[SlotRogue] Existing RunBattleView.prefab was migrated in place. " +
                    "Sprites, references, transforms, and user-added children were preserved.");
                return;
            }

            BuildRunBattleFresh();
        }

        private static void BuildRunBattleFresh()
        {
            if (!EnsureSceneSwitchAllowed())
            {
                return;
            }

            var prefabRoot = new GameObject("RunBattleView");

            var compositionObject = new GameObject("RunBattleCompositionRoot");
            compositionObject.transform.SetParent(prefabRoot.transform, false);
            RunBattleCompositionRoot compositionRoot = compositionObject.AddComponent<RunBattleCompositionRoot>();

            GameObject canvas = CreateCanvasRoot("Run Battle UI");
            canvas.transform.SetParent(prefabRoot.transform, false);
            MoveEventSystemToRoot(canvas.transform, prefabRoot.transform);
            var screenView = canvas.AddComponent<RunBattleScreenView>();
            RectTransform root = CreateRootPanel(canvas.transform, "Run Battle Root");

            Text[] slotCells = new Text[SlotSpinResult.CellCount];
            CreateBattleTopHud(root, out Text playerHud, out Image playerHp, out Image playerShield);
            EnsureMonsterViewPrefab();
            CreateBattleArena(root, out Text enemyIntent);
            RunBattleWorldView worldView = CreateBattleWorldFormationOnly(prefabRoot.transform);
            CreateSlotMachine(root, slotCells, out SlotMachineFrameView slotMachineFrameView);
            CreateBattleActionRow(root, out Text resultValue, out Button spinButton, out Button continueButton, out Button restartButton, out Text slotResult);
            CreateBattleBottomRow(root, out Text statusText);
            RectTransform presentationOverlay = CreatePresentationOverlay(canvas.transform);
            RectTransform playerDamageAnchor = CreateDamageAnchor(
                presentationOverlay,
                "player-damage-anchor",
                new Vector2(0f, -120f));
            presentationOverlay.SetAsLastSibling();

            FloatingDamageTextView floatingDamageTextPrefab = EnsureFloatingDamageTextPrefab();

            RunBattlePlayerHudView playerHudView = root.gameObject.AddComponent<RunBattlePlayerHudView>();
            playerHudView.Bind(playerHud, playerHp, playerShield);

            RunBattleStatusView statusView = root.gameObject.AddComponent<RunBattleStatusView>();
            statusView.Bind(statusText, enemyIntent);

            RunBattleSlotBoardView slotBoardView = root.gameObject.AddComponent<RunBattleSlotBoardView>();
            slotBoardView.Bind(slotCells);

            RunBattleActionView actionView = root.gameObject.AddComponent<RunBattleActionView>();
            actionView.Bind(slotResult, resultValue, spinButton, continueButton, restartButton);

            RunBattlePresentationOverlayView overlayView =
                presentationOverlay.gameObject.AddComponent<RunBattlePresentationOverlayView>();
            overlayView.Bind(presentationOverlay, playerDamageAnchor);

            screenView.Bind(
                playerHudView,
                statusView,
                slotBoardView,
                actionView,
                overlayView,
                worldView);

            //compositionRoot.Bind(
            //    screenView,
            //    floatingDamageTextPrefab,
            //    null,
            //    slotMachineFrameView,
            //    null);

            ConfigureRunBattleRaycasts(canvas.transform);
            SavePrefabAndScene(prefabRoot, "RunBattleView", "RunBattle");
        }

        private static void CreateBattleBackdrop(RectTransform root)
        {
            RectTransform top = CreatePanel(
                root,
                "Inside Top",
                "battle/background/inside-top",
                new Color32(21, 24, 34, 255),
                new Vector2(0f, 465f),
                new Vector2(860f, 790f));
            CreateOverlayText(
                top,
                "Inside Top Label",
                "RUN BATTLE",
                24,
                TextAnchor.UpperCenter,
                new RectOffset(22, 22, 20, 720));

            CreatePanel(
                root,
                "Inside Bottom",
                "battle/background/inside-bottom",
                new Color32(19, 21, 29, 255),
                new Vector2(0f, -530f),
                new Vector2(860f, 680f));
        }

        private static void CreateBattlefieldOverlay(
            RectTransform root,
            out Text enemyIntent)
        {
            RectTransform intentPanel = CreatePanel(
                root,
                "Enemy Intent Panel",
                "battle/enemy-intent-panel",
                PanelColor,
                new Vector2(0f, 62f),
                new Vector2(620f, 86f));
            enemyIntent = CreateOverlayText(
                intentPanel,
                "Enemy Intent Text",
                "ENEMY INTENT: -",
                24,
                TextAnchor.MiddleCenter,
                new RectOffset(16, 16, 10, 10));
        }

        private static RunBattleWorldView CreateBattleWorld(Transform parent)
        {
            var worldObject = new GameObject("RunBattleWorldView");
            worldObject.transform.SetParent(parent, false);

            var worldView = worldObject.AddComponent<RunBattleWorldView>();

            var shakeRoot = new GameObject("BattleShakeRoot");
            shakeRoot.transform.SetParent(worldObject.transform, false);
            shakeRoot.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            CreateBattleWorldBackground(shakeRoot.transform);

            var formationRoot = new GameObject("EnemyFormationView");
            formationRoot.transform.SetParent(shakeRoot.transform, false);
            formationRoot.transform.localPosition = Vector3.zero;
            EnemyFormationView formationView = formationRoot.AddComponent<EnemyFormationView>();

            string monsterPrefabPath = $"{WorldPrefabFolder}/{MonsterViewPrefabName}.prefab";
            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(monsterPrefabPath);
            var monsterViews = new MonsterView[FormationSlotCount];

            for (int index = 0; index < FormationSlotCount; index++)
            {
                Vector3 slotPosition = ResolveEnemyWorldSlotPosition(index, FormationSlotCount);
                GameObject monsterObject = monsterPrefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefab, formationRoot.transform)
                    : CreateMonsterView(formationRoot.transform, index, slotPosition).gameObject;

                monsterObject.name = $"MonsterView {index + 1}";
                monsterObject.transform.localPosition = slotPosition;

                monsterViews[index] = monsterObject.GetComponent<MonsterView>();
                if (monsterViews[index] == null)
                {
                    monsterViews[index] = monsterObject.AddComponent<MonsterView>();
                }
            }

            formationView.Bind(monsterViews);
            worldView.Bind(shakeRoot.transform, formationView);
            return worldView;
        }

        private static RunBattleWorldView CreateBattleWorldFormationOnly(Transform parent)
        {
            var arenaRoot = new GameObject("BattleArenaRoot");
            arenaRoot.transform.SetParent(parent, false);
            arenaRoot.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            RunBattleWorldView worldView = arenaRoot.AddComponent<RunBattleWorldView>();

            var formationRoot = new GameObject("FormationSlotsRoot");
            formationRoot.transform.SetParent(arenaRoot.transform, false);
            formationRoot.transform.localPosition = Vector3.zero;
            EnemyFormationView formationView = formationRoot.AddComponent<EnemyFormationView>();

            string monsterPrefabPath = $"{WorldPrefabFolder}/{MonsterViewPrefabName}.prefab";
            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(monsterPrefabPath);
            var monsterViews = new MonsterView[FormationSlotCount];

            for (int index = 0; index < FormationSlotCount; index++)
            {
                Vector3 slotPosition = ResolveEnemyWorldSlotPosition(index, FormationSlotCount);
                GameObject monsterObject = monsterPrefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefab, formationRoot.transform)
                    : CreateMonsterView(formationRoot.transform, index, slotPosition).gameObject;

                monsterObject.name = $"Formation Slot {index + 1}";
                monsterObject.transform.localPosition = slotPosition;

                monsterViews[index] = monsterObject.GetComponent<MonsterView>();
                if (monsterViews[index] == null)
                {
                    monsterViews[index] = monsterObject.AddComponent<MonsterView>();
                }
            }

            formationView.Bind(monsterViews);
            worldView.Bind(arenaRoot.transform, formationView);
            return worldView;
        }

        private static void CreateBattleWorldBackground(Transform parent)
        {
            Canvas backgroundCanvas = CreateWorldCanvas(
                parent,
                "Battle Background Canvas",
                new Vector3(0f, 0.15f, 0.25f),
                new Vector2(900f, 690f),
                sortingOrder: 0);
            RectTransform backgroundRoot = backgroundCanvas.GetComponent<RectTransform>();
            RectTransform arena = CreatePanel(
                backgroundRoot,
                "Battle Background View",
                "battle/world-background",
                ArenaColor,
                Vector2.zero,
                new Vector2(900f, 690f));
            CreateOverlayText(
                arena,
                "Battle Background Label",
                "ALIEN FRONTIER",
                30,
                TextAnchor.UpperCenter,
                new RectOffset(22, 22, 24, 610));
        }

        private static void EnsureMonsterViewPrefab()
        {
            string prefabPath = $"{WorldPrefabFolder}/{MonsterViewPrefabName}.prefab";
            MonsterView monsterView = CreateMonsterView(parent: null, index: 0, position: Vector3.zero);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(monsterView.gameObject, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(monsterView.gameObject);
            }
        }

        private static MonsterView CreateMonsterView(
            Transform parent,
            int index,
            Vector3 position)
        {
            var root = new GameObject($"{MonsterViewPrefabName} {index + 1}");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            root.transform.localPosition = position;
            var clickCollider = root.AddComponent<BoxCollider2D>();
            clickCollider.size = new Vector2(2.35f, 3.45f);
            clickCollider.offset = new Vector2(0f, 0.05f);

            var shakeGroup = new GameObject("ShakeGroup");
            shakeGroup.transform.SetParent(root.transform, false);
            shakeGroup.transform.localPosition = Vector3.zero;

            var portraitObject = new GameObject("Portrait", typeof(SpriteRenderer));
            portraitObject.transform.SetParent(shakeGroup.transform, false);
            portraitObject.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            portraitObject.transform.localScale = new Vector3(1.75f, 2.25f, 1f);
            SpriteRenderer portrait = portraitObject.GetComponent<SpriteRenderer>();
            portrait.color = MonsterColor;
            portrait.sortingOrder = 10;

            Canvas hudRoot = CreateWorldCanvas(
                shakeGroup.transform,
                "HudRoot",
                new Vector3(0f, -1.35f, 0f),
                new Vector2(260f, 126f),
                sortingOrder: 20);
            RectTransform hudTransform = hudRoot.GetComponent<RectTransform>();
            Text placeholder = CreateText(
                hudTransform,
                "Portrait Placeholder Text",
                "MONSTER\nSPRITE SLOT",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 48f),
                new Vector2(245f, 58f));
            RectTransform statusPanel = CreatePanel(
                hudTransform,
                "Status Panel",
                "battle/monster/status-panel",
                PanelColor,
                new Vector2(0f, -28f),
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
                "battle/monster/hp-bar",
                "battle/monster/hp-fill",
                new Vector2(0f, -22f),
                new Vector2(205f, 22f),
                HpColor);

            var damageAnchorObject = new GameObject("Damage Anchor", typeof(RectTransform));
            RectTransform damageAnchor = damageAnchorObject.GetComponent<RectTransform>();
            damageAnchor.SetParent(root.transform, false);
            damageAnchor.localPosition = new Vector3(0f, 1.75f, 0f);
            damageAnchor.sizeDelta = Vector2.zero;

            var monsterView = root.AddComponent<MonsterView>();
            monsterView.Bind(
                root.transform,
                shakeGroup.transform,
                portrait,
                hudRoot,
                hudText,
                hpFill,
                statusBackground,
                damageAnchor,
                placeholder,
                clickCollider);
            return monsterView;
        }

        private static FloatingDamageTextView EnsureFloatingDamageTextPrefab()
        {
            string prefabPath = $"{PrefabFolder}/{FloatingDamageTextPrefabName}.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                FloatingDamageTextView existingView = existingPrefab.GetComponent<FloatingDamageTextView>();
                if (existingView != null)
                {
                    return existingView;
                }
            }

            var root = new GameObject(
                FloatingDamageTextPrefabName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            RectTransform rectTransform = root.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(240f, 72f);

            Text text = root.GetComponent<Text>();
            ConfigureText(text, "-0", 48, TextAnchor.MiddleCenter);
            text.raycastTarget = false;

            FloatingDamageTextView view = root.AddComponent<FloatingDamageTextView>();
            var serializedView = new SerializedObject(view);
            serializedView.FindProperty("_text").objectReferenceValue = text;
            serializedView.FindProperty("_rectTransform").objectReferenceValue = rectTransform;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return prefab != null ? prefab.GetComponent<FloatingDamageTextView>() : null;
        }

        private static SlotLeverView CreateSpinLever(RectTransform root, Button spinButton)
        {
            RectTransform lever = CreateRect("Spin Lever", root, new Vector2(160f, -560f), new Vector2(96f, 170f));

            if (spinButton != null)
            {
                RectTransform spinButtonRect = spinButton.transform as RectTransform;
                if (spinButtonRect != null)
                {
                    lever.anchorMin = spinButtonRect.anchorMin;
                    lever.anchorMax = spinButtonRect.anchorMax;
                    lever.pivot = new Vector2(0.5f, 0.5f);
                    lever.anchoredPosition = spinButtonRect.anchoredPosition + new Vector2(160f, 10f);
                    lever.SetSiblingIndex(spinButtonRect.GetSiblingIndex() + 1);
                }
            }

            Image leverImage = AddImageSlot(lever.gameObject, "battle/spin-lever", Color.white);
            leverImage.raycastTarget = false;
            leverImage.preserveAspect = true;

            Sprite[] sprites = LoadSprites(InGameLeverTexturePath);
            if (sprites != null && sprites.Length > 0)
            {
                leverImage.sprite = sprites[0];
            }

            SlotLeverView leverView = lever.gameObject.AddComponent<SlotLeverView>();
            leverView.Bind(leverImage, sprites);
            return leverView;
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
            string prefabPath = $"{WorldPrefabFolder}/{EnemyFormationSlotPrefabName}.prefab";
            EnemyFormationSlotView slot = CreateEnemyFormationSlot(parent: null, index: 0, position: Vector3.zero);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(slot.gameObject, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(slot.gameObject);
            }
        }

        private static void CreateBattleArena(
            RectTransform root,
            out Text enemyIntent)
        {
            RectTransform arena = CreatePanel(root, "Battle Arena Image", "battle/arena", ArenaColor, new Vector2(0f, 380f), new Vector2(860f, 650f));
            CreateOverlayText(arena, "Arena Background Label", "ALIEN FRONTIER", 23, TextAnchor.UpperCenter, new RectOffset(22, 22, 20, 560));

            enemyIntent = CreateOverlayText(arena, "Enemy Intent Text", "ENEMY INTENT: -", 24, TextAnchor.LowerCenter, new RectOffset(36, 36, 560, 22));
        }

        private static void CreateBattleWorldArena(
            Transform parent,
            out EnemyFormationSlotView[] formationSlots)
        {
            var arenaRoot = new GameObject("BattleArenaRoot");
            arenaRoot.transform.SetParent(parent, false);
            arenaRoot.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            var formationRoot = new GameObject("FormationSlotsRoot");
            formationRoot.transform.SetParent(arenaRoot.transform, false);
            formationRoot.transform.localPosition = Vector3.zero;

            string slotPrefabPath = $"{WorldPrefabFolder}/{EnemyFormationSlotPrefabName}.prefab";
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(slotPrefabPath);
            formationSlots = new EnemyFormationSlotView[FormationSlotCount];

            for (int index = 0; index < FormationSlotCount; index++)
            {
                Vector3 slotPosition = ResolveEnemyWorldSlotPosition(index, FormationSlotCount);
                GameObject slotObject = slotPrefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, formationRoot.transform)
                    : CreateEnemyFormationSlot(formationRoot.transform, index, slotPosition).gameObject;

                slotObject.name = $"Formation Slot {index + 1}";
                slotObject.transform.localPosition = slotPosition;

                formationSlots[index] = slotObject.GetComponent<EnemyFormationSlotView>();
                if (formationSlots[index] == null)
                {
                    formationSlots[index] = slotObject.AddComponent<EnemyFormationSlotView>();
                }
            }
        }

        private static EnemyFormationSlotView CreateEnemyFormationSlot(
            Transform parent,
            int index,
            Vector3 position)
        {
            var root = new GameObject($"Formation Slot {index + 1}");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            root.transform.localPosition = position;
            var clickCollider = root.AddComponent<BoxCollider2D>();
            clickCollider.size = new Vector2(2.35f, 3.45f);
            clickCollider.offset = new Vector2(0f, 0.05f);

            var shakeGroup = new GameObject("ShakeGroup");
            shakeGroup.transform.SetParent(root.transform, false);
            shakeGroup.transform.localPosition = Vector3.zero;

            var portraitObject = new GameObject("Portrait", typeof(SpriteRenderer));
            portraitObject.transform.SetParent(shakeGroup.transform, false);
            portraitObject.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            portraitObject.transform.localScale = new Vector3(1.75f, 2.25f, 1f);
            SpriteRenderer portrait = portraitObject.GetComponent<SpriteRenderer>();
            portrait.color = MonsterColor;
            portrait.sortingOrder = 10;

            Canvas hudRoot = CreateWorldCanvas(shakeGroup.transform, "HudRoot", new Vector3(0f, -1.35f, 0f), new Vector2(260f, 126f), sortingOrder: 20);
            RectTransform hudTransform = hudRoot.GetComponent<RectTransform>();
            Text placeholder = CreateText(
                hudTransform,
                "Portrait Placeholder Text",
                "MONSTER\nSPRITE SLOT",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 48f),
                new Vector2(245f, 58f));
            RectTransform statusPanel = CreatePanel(
                hudTransform,
                "Status Panel",
                $"battle/monster-{index + 1}-status-panel",
                PanelColor,
                new Vector2(0f, -28f),
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

            var damageAnchorObject = new GameObject("Damage Anchor", typeof(RectTransform));
            RectTransform damageAnchor = damageAnchorObject.GetComponent<RectTransform>();
            damageAnchor.SetParent(root.transform, false);
            damageAnchor.localPosition = new Vector3(0f, 1.75f, 0f);
            damageAnchor.sizeDelta = Vector2.zero;

            var slotView = root.AddComponent<EnemyFormationSlotView>();
            slotView.Bind(
                root.transform,
                shakeGroup.transform,
                portrait,
                hudRoot,
                hudText,
                hpFill,
                statusBackground,
                damageAnchor,
                placeholder,
                clickCollider);
            return slotView;
        }

        private static Canvas CreateWorldCanvas(
            Transform parent,
            string name,
            Vector3 position,
            Vector2 size,
            int sortingOrder)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = position;
            canvasObject.transform.localScale = Vector3.one * 0.01f;

            RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = size;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            return canvas;
        }

        private static void ConfigureRunBattleRaycasts(Transform canvasTransform)
        {
            Graphic[] graphics = canvasTransform.GetComponentsInChildren<Graphic>(true);
            for (int index = 0; index < graphics.Length; index++)
            {
                Graphic graphic = graphics[index];
                graphic.raycastTarget = graphic.GetComponent<Button>() != null;
            }
        }

        private static void MoveEventSystemToRoot(Transform canvasTransform, Transform rootTransform)
        {
            EventSystem eventSystem = canvasTransform.GetComponentInChildren<EventSystem>(true);
            if (eventSystem != null)
            {
                eventSystem.transform.SetParent(rootTransform, false);
            }
        }

        private static Vector3 ResolveEnemyWorldSlotPosition(int index, int slotCount)
        {
            if (slotCount <= 1)
            {
                return Vector3.zero;
            }

            float spacing = slotCount <= 2 ? 3f : FormationWorldSpacing;
            float startX = -(slotCount - 1) * spacing * 0.5f;
            return new Vector3(startX + (index * spacing), 0f, 0f);
        }

        private static void CreateSlotMachine(
            RectTransform root,
            Text[] slotCells,
            out SlotMachineFrameView slotMachineFrameView)
        {
            RectTransform slotMachine = CreatePanel(root, "Slot Machine Panel", "battle/slot-machine-panel", SlotBoardColor, new Vector2(0f, -205f), new Vector2(860f, 430f));
            slotMachineFrameView = slotMachine.gameObject.AddComponent<SlotMachineFrameView>();
            bool animationImageChanged = false;
            Image animationImage = EnsureSlotMachineAnimationImage(slotMachine, ref animationImageChanged);
            Sprite[] slotMachineSprites = LoadSprites(InGameSlotAnimationTexturePath);
            slotMachineFrameView.Bind(animationImage, slotMachineSprites);
            if (animationImageChanged)
            {
                EditorUtility.SetDirty(slotMachine);
            }

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
            Image image = AddImageSlot(fill.gameObject, fillSlotId, color);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
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

        private static void SavePrefabAndScene(
            GameObject canvas,
            string prefabName,
            string sceneName,
            bool createCamera = true)
        {
            string prefabPath = $"{PrefabFolder}/{prefabName}.prefab";
            string scenePath = $"{SceneFolder}/{sceneName}.unity";
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
            bool sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null;

            if (prefabExists || sceneExists)
            {
                UnityEngine.Debug.LogWarning(
                    $"[SlotRogue] Preserved existing {prefabName}/{sceneName}; generated replacement was discarded.");
                UnityEngine.Object.DestroyImmediate(canvas);
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(canvas, prefabPath);
            UnityEngine.Object.DestroyImmediate(canvas);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            if (createCamera)
            {
                CreateMainCamera();
            }

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static GameObject CreateMainCamera(Transform parent = null)
        {
            var cameraObject = new GameObject("Main Camera");
            if (parent != null)
            {
                cameraObject.transform.SetParent(parent, false);
            }

            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<Physics2DRaycaster>();
            cameraObject.AddComponent<AudioListener>();
            return cameraObject;
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
