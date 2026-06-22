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

                    slotView.Bind(
                        slot.transform,
                        shakeGroup: null,
                        hudRoot: null,
                        hudText: null,
                        hpFill: null,
                        statusBackground: null,
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
                    slotViews[index].Bind(
                        slot.transform,
                        shakeGroup: null,
                        hudRoot: null,
                        hudText: null,
                        hpFill: null,
                        statusBackground: null,
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
                worldView.Render(viewModel.State);
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
                    typeof(Image),
                    typeof(GameFlowImageSlot));
                fillObject.transform.SetParent(gauge.transform, false);
                Image fill = fillObject.GetComponent<Image>();
                fillObject.GetComponent<GameFlowImageSlot>().Bind("battle/player-hp-fill", fill);

                var viewModel = new RunBattleScreenViewModel(slotCellCount: 0, enemySlotCount: 0);
                viewModel.SetPlayerHud("15/30", hp: 15, maxHp: 30, shield: 0, shieldMax: 30);

                view.Render(viewModel.State);

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

                view.Bind(
                    root.transform,
                    shakeGroup: null,
                    hudRoot: null,
                    hudText: null,
                    hpFill: fill,
                    statusBackground: null,
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
        public void ActionViewRender_ResolvesAttackPowerText()
        {
            var root = new GameObject("UI_HUDCanvas");
            try
            {
                RunBattleActionView view = root.AddComponent<RunBattleActionView>();
                var textObject = new GameObject(
                    "Attack Power Text",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                textObject.transform.SetParent(root.transform, false);
                Text attackPowerText = textObject.GetComponent<Text>();

                var viewModel = new RunBattleScreenViewModel(slotCellCount: 0, enemySlotCount: 0);
                viewModel.SetBattleText("slot", "ATK 18");

                view.Render(viewModel.State);

                Assert.That(attackPowerText.text, Is.EqualTo("ATK 18"));
            }
            finally
            {
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

        private static readonly FieldInfo CurrentPlaybackField =
            typeof(EnemyAnimatorCombatVisual).GetField(
                "_currentPlayback",
                BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
