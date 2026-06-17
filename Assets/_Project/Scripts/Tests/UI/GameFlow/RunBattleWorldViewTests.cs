using NUnit.Framework;
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

                Assert.That(slotViews[0].CombatVisualPrefab, Is.Null);
                Assert.That(slotViews[1].CombatVisualPrefab, Is.SameAs(combatVisualPrefab));
                Assert.That(slotViews[1].CombatVisualInstance, Is.Not.Null);
                Assert.That(slotViews[1].CombatVisualInstance.name, Does.StartWith(combatVisualPrefab.name));
                Assert.That(slotViews[1].CombatVisualInstance.transform.parent, Is.SameAs(slotViews[1].VisualRoot));
                Assert.That(slotViews[1].CombatVisualInstance.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(slotViews[1].CombatVisualInstance.transform.localRotation, Is.EqualTo(Quaternion.identity));
                var firstVisual = slotViews[1].CombatVisual as TestEnemyCombatVisual;
                Assert.That(firstVisual, Is.Not.Null);
                Assert.That(firstVisual.IdleCallCount, Is.EqualTo(1));
                Assert.That(firstVisual.AttackCallCount, Is.EqualTo(0));
                slotViews[1].PlayCombatVisualAttack();
                Assert.That(firstVisual.AttackCallCount, Is.EqualTo(1));

                GameObject firstInstance = slotViews[1].CombatVisualInstance;
                worldView.SetEnemyCombatVisualPrefab(formationSlot: 1, replacementPrefab);

                Assert.That(firstInstance == null, Is.True);
                Assert.That(slotViews[1].CombatVisualPrefab, Is.SameAs(replacementPrefab));
                Assert.That(slotViews[1].CombatVisualInstance, Is.Not.Null);
                Assert.That(slotViews[1].CombatVisualInstance.name, Does.StartWith(replacementPrefab.name));
                Assert.That(slotViews[1].VisualRoot.childCount, Is.EqualTo(1));
                var replacementVisual = slotViews[1].CombatVisual as TestEnemyCombatVisual;
                Assert.That(replacementVisual, Is.Not.Null);
                Assert.That(replacementVisual.IdleCallCount, Is.EqualTo(1));
                Assert.That(replacementVisual.AttackCallCount, Is.EqualTo(0));

                GameObject replacementInstance = slotViews[1].CombatVisualInstance;
                worldView.ClearEnemyCombatVisualPrefabs();

                Assert.That(replacementInstance == null, Is.True);
                Assert.That(slotViews[0].CombatVisualPrefab, Is.Null);
                Assert.That(slotViews[0].CombatVisualInstance, Is.Null);
                Assert.That(slotViews[1].CombatVisualPrefab, Is.Null);
                Assert.That(slotViews[1].CombatVisualInstance, Is.Null);
                Assert.That(slotViews[1].CombatVisual, Is.Null);
                Assert.That(slotViews[1].VisualRoot.childCount, Is.EqualTo(0));
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

        private sealed class TestEnemyCombatVisual : MonoBehaviour, IEnemyCombatVisual
        {
            public int IdleCallCount { get; private set; }

            public int AttackCallCount { get; private set; }

            public void PlayIdle()
            {
                IdleCallCount++;
            }

            public void PlayAttack()
            {
                AttackCallCount++;
            }
        }
    }
}
