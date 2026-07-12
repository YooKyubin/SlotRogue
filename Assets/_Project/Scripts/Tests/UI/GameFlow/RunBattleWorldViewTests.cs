using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.UI.GameFlow;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class RunBattleWorldViewTests
    {
        [Test]
        public void EnsureReferences_WithFormationSlotChildren_BindsEnemyFormationView()
        {
            var root = new GameObject("BattleArenaRoot");
            try
            {
                RunBattleWorldView worldView = root.AddComponent<RunBattleWorldView>();
                var formationRoot = new GameObject("FormationSlotsRoot");
                formationRoot.transform.SetParent(root.transform, false);

                for (int index = 0; index < 3; index++)
                {
                    var slot = new GameObject($"Formation Slot {index + 1}");
                    slot.transform.SetParent(formationRoot.transform, false);
                    EnemyFormationSlotView slotView = slot.AddComponent<EnemyFormationSlotView>();

                    var damageAnchorObject = new GameObject("Damage Anchor", typeof(RectTransform));
                    RectTransform damageAnchor = damageAnchorObject.GetComponent<RectTransform>();
                    damageAnchor.SetParent(slot.transform, false);

                    ConfigureEnemyFormationSlot(
                        slotView,
                        root: slot.transform,
                        shakeGroup: null,
                        hudRoot: null,
                        hudText: null,
                        hpFill: null,
                        shieldGauge: null,
                        damageAnchor: damageAnchor,
                        clickCollider: null);
                }

                Assert.That(worldView.EnsureReferences(), Is.True);
                Assert.That(worldView.EnemyFormationView, Is.Not.Null);
                Assert.That(worldView.EnemyFormationView.SlotCount, Is.EqualTo(3));
                Assert.That(worldView.GetEnemyDamageAnchor(1), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetEnemyCombatVisualPrefab_InstantiatesPrefabUnderFormationSlotVisualRoot()
        {
            var root = new GameObject("BattleArenaRoot");
            var combatVisualPrefab = new GameObject("Enemy Combat Visual Prefab");
            var replacementPrefab = new GameObject("Enemy Combat Visual Replacement Prefab");
            combatVisualPrefab.AddComponent<TestEnemyCombatVisual>();
            replacementPrefab.AddComponent<TestEnemyCombatVisual>();
            try
            {
                RunBattleWorldView worldView = root.AddComponent<RunBattleWorldView>();
                var formationRoot = new GameObject("FormationSlotsRoot");
                formationRoot.transform.SetParent(root.transform, false);

                EnemyFormationSlotView[] slotViews = new EnemyFormationSlotView[2];
                for (int index = 0; index < slotViews.Length; index++)
                {
                    var slot = new GameObject($"Formation Slot {index + 1}");
                    slot.transform.SetParent(formationRoot.transform, false);
                    var visualRoot = new GameObject("VisualRoot");
                    visualRoot.transform.SetParent(slot.transform, false);

                    slotViews[index] = slot.AddComponent<EnemyFormationSlotView>();
                    ConfigureEnemyFormationSlot(
                        slotViews[index],
                        root: slot.transform,
                        shakeGroup: null,
                        hudRoot: null,
                        hudText: null,
                        hpFill: null,
                        shieldGauge: null,
                        damageAnchor: null,
                        clickCollider: null,
                        visualRoot: visualRoot.transform);
                }

                worldView.EnsureReferences();
                worldView.SetEnemyCombatVisualPrefab(formationSlot: 1, combatVisualPrefab);

                Assert.That(slotViews[0].transform.Find("VisualRoot").childCount, Is.EqualTo(0));
                Transform boundVisualRoot = slotViews[1].transform.Find("VisualRoot");
                Assert.That(boundVisualRoot.childCount, Is.EqualTo(1));
                Transform firstInstanceTransform = boundVisualRoot.GetChild(0);
                Assert.That(firstInstanceTransform.name, Does.StartWith(combatVisualPrefab.name));
                Assert.That(firstInstanceTransform.parent, Is.SameAs(boundVisualRoot));
                Assert.That(firstInstanceTransform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(firstInstanceTransform.localRotation, Is.EqualTo(Quaternion.identity));
                var firstVisual = firstInstanceTransform.GetComponent<TestEnemyCombatVisual>();
                Assert.That(firstVisual, Is.Not.Null);
                Assert.That(firstVisual.IdleCallCount, Is.EqualTo(1));
                Assert.That(firstVisual.ActionCallCount, Is.EqualTo(0));
                var viewModel = new RunBattleScreenViewModel(slotCellCount: 0, enemySlotCount: 2);
                viewModel.SetEnemySlot(
                    slotIndex: 0,
                    new CombatParticipantId(100),
                    "Enemy 0",
                    hp: 10,
                    maxHp: 10,
                    shield: 0,
                    selected: false,
                    interactable: true);
                viewModel.SetEnemySlot(
                    slotIndex: 1,
                    new CombatParticipantId(101),
                    "Enemy 1",
                    hp: 10,
                    maxHp: 10,
                    shield: 0,
                    selected: false,
                    interactable: true);
                worldView.Render(viewModel.State.CurrentValue);
                worldView.PlayEnemyCombatVisualActionUntilEffectPointAsync(
                        new CombatParticipantId(101),
                        "Defend",
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                Assert.That(firstVisual.ActionCallCount, Is.EqualTo(1));
                Assert.That(firstVisual.LastActionName, Is.EqualTo("Defend"));
                Assert.That(slotViews[0].transform.Find("VisualRoot").childCount, Is.EqualTo(0));

                GameObject firstInstance = firstInstanceTransform.gameObject;
                worldView.SetEnemyCombatVisualPrefab(formationSlot: 1, replacementPrefab);

                Assert.That(firstInstance == null, Is.True);
                Assert.That(boundVisualRoot.childCount, Is.EqualTo(1));
                Transform replacementInstanceTransform = boundVisualRoot.GetChild(0);
                Assert.That(replacementInstanceTransform.name, Does.StartWith(replacementPrefab.name));
                var replacementVisual = replacementInstanceTransform.GetComponent<TestEnemyCombatVisual>();
                Assert.That(replacementVisual, Is.Not.Null);
                Assert.That(replacementVisual.IdleCallCount, Is.EqualTo(1));
                Assert.That(replacementVisual.ActionCallCount, Is.EqualTo(0));

                GameObject replacementInstance = replacementInstanceTransform.gameObject;
                worldView.ClearEnemyCombatVisualPrefabs();

                Assert.That(replacementInstance == null, Is.True);
                Assert.That(slotViews[0].transform.Find("VisualRoot").childCount, Is.EqualTo(0));
                Assert.That(boundVisualRoot.childCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(replacementPrefab);
                Object.DestroyImmediate(combatVisualPrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerHudRender_ResolvesVerticalHpSlotAndUpdatesFill()
        {
            var root = new GameObject("UI_HUDCanvas");
            try
            {
                RunBattlePlayerHudView view = root.AddComponent<RunBattlePlayerHudView>();
                var gauge = new GameObject("Player HP Gauge", typeof(RectTransform));
                RectTransform gaugeRect = gauge.GetComponent<RectTransform>();
                gaugeRect.SetParent(root.transform, false);
                gaugeRect.sizeDelta = new Vector2(72f, 364f);

                var fillObject = new GameObject(
                    "Player HP Gauge Fill",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                fillObject.transform.SetParent(gauge.transform, false);
                Image fill = fillObject.GetComponent<Image>();

                var viewModel = new RunBattleScreenViewModel(slotCellCount: 0, enemySlotCount: 0);
                viewModel.SetPlayerHud("15/30", hp: 15, maxHp: 30, shield: 0, shieldMax: 30);

                view.Render(viewModel.State.CurrentValue);

                Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
                Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Vertical));
                Assert.That(fill.fillOrigin, Is.EqualTo((int)Image.OriginVertical.Bottom));
                Assert.That(fill.fillAmount, Is.EqualTo(0.5f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerHudRender_HidesShieldGaugeAndCentersHpWhenShieldIsZero()
        {
            var root = new GameObject("UI_HUDCanvas");
            try
            {
                RunBattlePlayerHudView view = root.AddComponent<RunBattlePlayerHudView>();
                (RectTransform hpGauge, _) = CreatePlayerHudGauge(
                    root.transform,
                    "Player HP Gauge",
                    "Player HP Gauge Fill",
                    childYOffset: -6f);
                (RectTransform shieldGauge, _) = CreatePlayerHudGauge(
                    root.transform,
                    "Player Shield Gauge",
                    "Player Shield Gauge Fill",
                    childYOffset: 6f);

                var viewModel = new RunBattleScreenViewModel(slotCellCount: 0, enemySlotCount: 0);
                viewModel.SetPlayerHud("12/30", hp: 12, maxHp: 30, shield: 0, shieldMax: 30);

                view.Render(viewModel.State.CurrentValue);

                Assert.That(shieldGauge.gameObject.activeSelf, Is.False);
                Assert.That(hpGauge.anchoredPosition.y, Is.EqualTo(6f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerHudRender_ShowsShieldGaugeAndRestoresHpPositionWhenShieldIsPositive()
        {
            var root = new GameObject("UI_HUDCanvas");
            try
            {
                RunBattlePlayerHudView view = root.AddComponent<RunBattlePlayerHudView>();
                (RectTransform hpGauge, _) = CreatePlayerHudGauge(
                    root.transform,
                    "Player HP Gauge",
                    "Player HP Gauge Fill",
                    childYOffset: -6f);
                (RectTransform shieldGauge, Image shieldFill) = CreatePlayerHudGauge(
                    root.transform,
                    "Player Shield Gauge",
                    "Player Shield Gauge Fill",
                    childYOffset: 6f);

                var viewModel = new RunBattleScreenViewModel(slotCellCount: 0, enemySlotCount: 0);
                viewModel.SetPlayerHud("12/30", hp: 12, maxHp: 30, shield: 0, shieldMax: 30);
                view.Render(viewModel.State.CurrentValue);

                viewModel.SetPlayerHud("12/30", hp: 12, maxHp: 30, shield: 8, shieldMax: 20);
                view.Render(viewModel.State.CurrentValue);

                Assert.That(shieldGauge.gameObject.activeSelf, Is.True);
                Assert.That(hpGauge.anchoredPosition.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(shieldFill.fillAmount, Is.EqualTo(0.4f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyHpRender_ResizesFromFixedLeftEdgeWithoutSprite()
        {
            var root = new GameObject("Enemy");
            try
            {
                EnemyFormationSlotView view = root.AddComponent<EnemyFormationSlotView>();
                var bar = new GameObject("HP Bar", typeof(RectTransform));
                RectTransform barRect = bar.GetComponent<RectTransform>();
                barRect.SetParent(root.transform, false);
                barRect.sizeDelta = new Vector2(170f, 22f);

                var fillObject = new GameObject(
                    "HP Bar Fill",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                RectTransform fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.SetParent(bar.transform, false);
                fillRect.sizeDelta = new Vector2(160f, 14f);
                fillRect.anchorMin = new Vector2(0.5f, 0.5f);
                fillRect.anchorMax = new Vector2(0.5f, 0.5f);
                fillRect.pivot = new Vector2(0.5f, 0.5f);
                Image fill = fillObject.GetComponent<Image>();

                ConfigureEnemyFormationSlot(
                    view,
                    root: root.transform,
                    shakeGroup: null,
                    hudRoot: null,
                    hudText: null,
                    hpFill: fill,
                    shieldGauge: null,
                    damageAnchor: null,
                    clickCollider: null);

                view.SetHpFill(current: 5, max: 10);

                Assert.That(fill.type, Is.EqualTo(Image.Type.Simple));
                Assert.That(fillRect.anchorMin.x, Is.EqualTo(0f));
                Assert.That(fillRect.anchorMax.x, Is.EqualTo(0f));
                Assert.That(fillRect.pivot.x, Is.EqualTo(0f));
                Assert.That(fillRect.anchoredPosition.x, Is.EqualTo(5f).Within(0.001f));
                Assert.That(fillRect.rect.width, Is.EqualTo(80f).Within(0.001f));

                view.SetHpFill(current: 10, max: 10);

                Assert.That(fillRect.anchoredPosition.x, Is.EqualTo(5f).Within(0.001f));
                Assert.That(fillRect.rect.width, Is.EqualTo(160f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyShieldRender_SwapsHpFillAndFrameSprites()
        {
            var root = new GameObject("Enemy");
            Sprite normalFill = CreateTestSprite("Normal Fill");
            Sprite shieldedFill = CreateTestSprite("Shielded Fill");
            Sprite normalFrame = CreateTestSprite("Normal Frame");
            Sprite shieldedFrame = CreateTestSprite("Shielded Frame");
            try
            {
                EnemyFormationSlotView view = root.AddComponent<EnemyFormationSlotView>();

                var fillObject = new GameObject(
                    "HP Bar Fill",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                fillObject.transform.SetParent(root.transform, false);
                Image fill = fillObject.GetComponent<Image>();
                fill.sprite = normalFill;

                var frameObject = new GameObject(
                    "HP Bar Frame",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                frameObject.transform.SetParent(root.transform, false);
                Image frame = frameObject.GetComponent<Image>();
                frame.sprite = normalFrame;

                ConfigureEnemyFormationSlot(
                    view,
                    root: root.transform,
                    shakeGroup: null,
                    hudRoot: null,
                    hudText: null,
                    hpFill: fill,
                    hpBarFrame: frame,
                    shieldGauge: null,
                    damageAnchor: null,
                    clickCollider: null);
                SetPrivateField(view, "_shieldedHpFillSprite", shieldedFill);
                SetPrivateField(view, "_shieldedHpBarFrameSprite", shieldedFrame);

                view.SetShield(6);

                Assert.That(fill.sprite, Is.SameAs(shieldedFill));
                Assert.That(frame.sprite, Is.SameAs(shieldedFrame));

                view.SetShield(0);

                Assert.That(fill.sprite, Is.SameAs(normalFill));
                Assert.That(frame.sprite, Is.SameAs(normalFrame));
            }
            finally
            {
                DestroySpriteWithTexture(shieldedFrame);
                DestroySpriteWithTexture(normalFrame);
                DestroySpriteWithTexture(shieldedFill);
                DestroySpriteWithTexture(normalFill);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemySlotDeathPresentation_RemainsHiddenAcrossRenderUpdatesUntilVisualRebind()
        {
            var root = new GameObject("Enemy");
            var combatVisualPrefab = new GameObject("Enemy Combat Visual Prefab");
            combatVisualPrefab.AddComponent<TestEnemyCombatVisual>();
            try
            {
                EnemyFormationSlotView view = root.AddComponent<EnemyFormationSlotView>();
                var visualRoot = new GameObject("VisualRoot");
                visualRoot.transform.SetParent(root.transform, false);
                var hudRootObject = new GameObject("HUD Root", typeof(Canvas));
                hudRootObject.transform.SetParent(root.transform, false);
                Canvas hudRoot = hudRootObject.GetComponent<Canvas>();
                var hudTextObject = new GameObject("HUD Text", typeof(Text));
                hudTextObject.transform.SetParent(hudRootObject.transform, false);
                Text hudText = hudTextObject.GetComponent<Text>();
                var statusRootObject = new GameObject("Status Root", typeof(RectTransform));
                RectTransform statusRoot = statusRootObject.GetComponent<RectTransform>();
                statusRoot.SetParent(root.transform, false);
                var intentRoot = new GameObject("Intent Root");
                intentRoot.transform.SetParent(root.transform, false);
                var collider = root.AddComponent<BoxCollider2D>();

                ConfigureEnemyFormationSlot(
                    view,
                    root: root.transform,
                    shakeGroup: null,
                    hudRoot: hudRoot,
                    hudText: hudText,
                    hpFill: null,
                    shieldGauge: null,
                    damageAnchor: null,
                    clickCollider: collider,
                    statusEffectRoot: statusRoot,
                    statusEffectIconPrefab: null,
                    intentRoot: intentRoot.transform,
                    intentIconPrefab: null,
                    visualRoot: visualRoot.transform);

                view.PlayDeathAsync(CancellationToken.None).GetAwaiter().GetResult();
                view.SetPresentationActive(true);
                view.SetHud("Should Not Render");
                view.SetUpcomingActions(new[]
                {
                    new EnemyUpcomingActionViewData(EnemyUpcomingActionKind.Damage, 4),
                });
                view.SetInteractable(true);

                Assert.That(hudRootObject.activeSelf, Is.False);
                Assert.That(statusRootObject.activeSelf, Is.False);
                Assert.That(intentRoot.activeSelf, Is.False);
                Assert.That(collider.enabled, Is.False);
                Assert.That(hudText.text, Is.Not.EqualTo("Should Not Render"));

                view.SetCombatVisualPrefab(combatVisualPrefab);
                view.SetHud("Alive Again");
                view.SetInteractable(true);

                Assert.That(hudRootObject.activeSelf, Is.True);
                Assert.That(statusRootObject.activeSelf, Is.True);
                Assert.That(visualRoot.transform.childCount, Is.EqualTo(1));
                Assert.That(collider.enabled, Is.True);
                Assert.That(hudText.text, Is.EqualTo("Alive Again"));
            }
            finally
            {
                Object.DestroyImmediate(combatVisualPrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task ActionAnimationCompleted_DetachesPlaybackBeforeSignalingContinuation()
        {
            var root = new GameObject("Enemy Visual");
            try
            {
                var visual = root.AddComponent<EnemyAnimatorCombatVisual>();
                var completedPlayback = new EnemyActionPlaybackState();
                var nextPlayback = new EnemyActionPlaybackState();
                SetCurrentPlayback(visual, completedPlayback);

                Task continuation = UniTask.Create(async () =>
                    {
                        await completedPlayback.WaitForActionCompletedAsync(CancellationToken.None);
                        SetCurrentPlayback(visual, nextPlayback);
                    })
                    .AsTask();

                InvokePrivate(visual, "OnActionAnimationCompleted");
                await continuation;

                Assert.That(GetCurrentPlayback(visual), Is.SameAs(nextPlayback));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task CancelCurrentPlayback_DetachesPlaybackBeforeSignalingContinuation()
        {
            var root = new GameObject("Enemy Visual");
            try
            {
                var visual = root.AddComponent<EnemyAnimatorCombatVisual>();
                var canceledPlayback = new EnemyActionPlaybackState();
                var nextPlayback = new EnemyActionPlaybackState();
                SetCurrentPlayback(visual, canceledPlayback);

                Task continuation = UniTask.Create(async () =>
                    {
                        try
                        {
                            await canceledPlayback.WaitForActionCompletedAsync(CancellationToken.None);
                        }
                        catch (System.OperationCanceledException)
                        {
                            SetCurrentPlayback(visual, nextPlayback);
                        }
                    })
                    .AsTask();

                InvokePrivate(visual, "CancelCurrentPlayback");
                await continuation;

                Assert.That(GetCurrentPlayback(visual), Is.SameAs(nextPlayback));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ActionAnimationCompleted_DuplicateEventWithoutCurrentPlaybackDoesNothing()
        {
            var root = new GameObject("Enemy Visual");
            try
            {
                var visual = root.AddComponent<EnemyAnimatorCombatVisual>();
                SetCurrentPlayback(visual, new EnemyActionPlaybackState());

                InvokePrivate(visual, "OnActionAnimationCompleted");
                InvokePrivate(visual, "OnActionAnimationCompleted");

                Assert.That(GetCurrentPlayback(visual), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyActionPlaybackState_WaitForActionCompletedCompletesWhenMarkedBeforeWait()
        {
            using var playback = new EnemyActionPlaybackState();

            playback.MarkActionCompleted();

            Assert.DoesNotThrowAsync(async () =>
                await playback.WaitForActionCompletedAsync(CancellationToken.None).AsTask());
        }

        [Test]
        public void EnemyActionPlaybackState_WaitForActionCompletedCancelsWhenPlaybackIsCanceled()
        {
            using var playback = new EnemyActionPlaybackState();

            playback.Cancel();

            Assert.ThrowsAsync<System.OperationCanceledException>(async () =>
                await playback.WaitForActionCompletedAsync(CancellationToken.None).AsTask());
        }

        private sealed class TestEnemyCombatVisual : MonoBehaviour, IEnemyCombatVisual
        {
            public int IdleCallCount { get; private set; }

            public int ActionCallCount { get; private set; }

            public string LastActionName { get; private set; }

            public void PlayIdle()
            {
                IdleCallCount++;
            }

            public UniTask PlayActionUntilEffectPointAsync(
                string actionName,
                CancellationToken cancellationToken)
            {
                ActionCallCount++;
                LastActionName = actionName;
                return UniTask.CompletedTask;
            }

            public UniTask WaitForActionCompletedAsync(CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }
        }

        private static void SetCurrentPlayback(
            EnemyAnimatorCombatVisual visual,
            EnemyActionPlaybackState playback)
        {
            CurrentPlaybackField.SetValue(visual, playback);
        }

        private static EnemyActionPlaybackState GetCurrentPlayback(EnemyAnimatorCombatVisual visual)
        {
            return (EnemyActionPlaybackState)CurrentPlaybackField.GetValue(visual);
        }

        private static void InvokePrivate(EnemyAnimatorCombatVisual visual, string methodName)
        {
            typeof(EnemyAnimatorCombatVisual)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(visual, parameters: null);
        }

        private static void SetPrivateField(
            EnemyFormationSlotView view,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(EnemyFormationSlotView)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            field.SetValue(view, value);
        }

        private static void ConfigureEnemyFormationSlot(
            EnemyFormationSlotView view,
            Transform root,
            Transform shakeGroup,
            Canvas hudRoot,
            Text hudText,
            Image hpFill,
            ShieldGaugeView shieldGauge,
            RectTransform damageAnchor,
            Collider2D clickCollider,
            Image hpBarFrame = null,
            RectTransform statusEffectRoot = null,
            GameObject statusEffectIconPrefab = null,
            Transform intentRoot = null,
            EnemyIntentIconView intentIconPrefab = null,
            Transform visualRoot = null)
        {
            SetPrivateField(view, "_root", root);
            SetPrivateField(view, "_shakeGroup", shakeGroup);
            SetPrivateField(view, "_visualRoot", visualRoot);
            SetPrivateField(view, "_hudRoot", hudRoot);
            SetPrivateField(view, "_hudText", hudText);
            SetPrivateField(view, "_hpFill", hpFill);
            SetPrivateField(view, "_hpBarFrame", hpBarFrame);
            SetPrivateField(view, "_shieldGauge", shieldGauge);
            SetPrivateField(view, "_damageAnchor", damageAnchor);
            SetPrivateField(view, "_clickCollider", clickCollider);
            SetPrivateField(view, "_statusEffectRoot", statusEffectRoot);
            SetPrivateField(view, "_statusEffectIconPrefab", statusEffectIconPrefab);
            SetPrivateField(view, "_intentRoot", intentRoot);
            SetPrivateField(view, "_intentIconPrefab", intentIconPrefab);
        }

        private static Sprite CreateTestSprite(string name)
        {
            var texture = new Texture2D(2, 2);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            sprite.name = name;
            return sprite;
        }

        private static void DestroySpriteWithTexture(Sprite sprite)
        {
            Texture2D texture = sprite != null ? sprite.texture : null;
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        private static readonly FieldInfo CurrentPlaybackField =
            typeof(EnemyAnimatorCombatVisual).GetField(
                "_currentPlayback",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static (RectTransform Gauge, Image Fill) CreatePlayerHudGauge(
            Transform parent,
            string gaugeName,
            string fillName,
            float childYOffset)
        {
            var gauge = new GameObject(gaugeName, typeof(RectTransform));
            RectTransform gaugeRect = gauge.GetComponent<RectTransform>();
            gaugeRect.SetParent(parent, false);
            gaugeRect.anchoredPosition = Vector2.zero;
            gaugeRect.sizeDelta = new Vector2(100f, 18f);

            var icon = new GameObject($"{gaugeName} Icon", typeof(RectTransform));
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.SetParent(gauge.transform, false);
            iconRect.anchoredPosition = new Vector2(0f, childYOffset);

            var fillObject = new GameObject(
                fillName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.SetParent(gauge.transform, false);
            fillRect.anchoredPosition = new Vector2(0f, childYOffset);
            Image fill = fillObject.GetComponent<Image>();

            return (gaugeRect, fill);
        }
    }
}
