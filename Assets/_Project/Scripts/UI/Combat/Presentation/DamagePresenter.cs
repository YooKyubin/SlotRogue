using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class DamagePresenter : CombatPresenterBase
    {
        private const float HudTweenDuration = 0.35f;
        private const float EffectStubDuration = 0.08f;

        public DamagePresenter(CombatPresentationHost host)
            : base(host)
        {
        }

        public override async UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.EffectApplied ||
                combatEvent.Effect.Kind != CombatEffectKind.Damage)
            {
                return;
            }

            int damageDealt = combatEvent.ApplyResult.DamageDealt;
            UniTask hudTween = TweenTargetHpAsync(combatEvent, viewModel, HudTweenDuration, cancellationToken);
            UniTask vfxStub = EffectStubDelayAsync(EffectStubDuration, Host, cancellationToken);
            UniTask sfxStub = EffectStubDelayAsync(EffectStubDuration, Host, cancellationToken);
            UniTask floatingStub = ShowFloatingDamageAsync(
                damageDealt,
                context.IsCritical,
                combatEvent.TargetParticipantId,
                combatEvent.IsPlayerParticipant,
                cancellationToken);

            await UniTask.WhenAll(hudTween, vfxStub, sfxStub, floatingStub);

            viewModel.ApplyParticipantSnapshot(
                combatEvent.TargetParticipantId,
                combatEvent.TargetAfter,
                combatEvent.IsPlayerParticipant);
            RefreshHUD();
        }

        private async UniTask ShowFloatingDamageAsync(
            int amount,
            bool isCritical,
            CombatParticipantId targetParticipantId,
            bool isPlayerTarget,
            CancellationToken cancellationToken)
        {
            string prefix = isCritical ? "[CRIT] " : string.Empty;
            string targetLabel = isPlayerTarget ? "Player" : $"Monster#{targetParticipantId.Value}";
            Debug.Log($"[Presentation] {prefix}Floating damage {amount} -> {targetLabel}");

            if (amount <= 0)
            {
                return;
            }

            if (Host.FloatingDamageTextPrefab == null)
            {
                Debug.LogWarning("[Presentation] FloatingDamageText prefab is not assigned.");
                return;
            }

            if (Host.FloatingTextRoot == null)
            {
                Debug.LogWarning("[Presentation] FloatingTextRoot is not assigned.");
                return;
            }

            RectTransform anchor = Host.ResolveDamageAnchor(targetParticipantId, isPlayerTarget);
            if (anchor == null)
            {
                Debug.LogWarning($"[Presentation] Missing {(isPlayerTarget ? "player" : "monster")} damage anchor.");
                return;
            }

            FloatingDamageTextView damageText = Object.Instantiate(
                Host.FloatingDamageTextPrefab,
                Host.FloatingTextRoot);
            if (damageText.transform is RectTransform textTransform &&
                Host.FloatingTextRoot is RectTransform floatingRoot)
            {
                AlignFloatingTextToAnchor(textTransform, anchor, floatingRoot);
            }

            CombatAnchorKind anchorKind = isPlayerTarget ? CombatAnchorKind.Player : CombatAnchorKind.Monster;
            await damageText.Play(amount, isCritical, anchorKind, cancellationToken);
        }

        private static void AlignFloatingTextToAnchor(
            RectTransform floatingText,
            RectTransform anchor,
            RectTransform floatingRoot)
        {
            Canvas floatingCanvas = floatingRoot.GetComponentInParent<Canvas>();
            Camera floatingCamera = null;
            if (floatingCanvas != null && floatingCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                floatingCamera = floatingCanvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                ResolveAnchorCamera(anchor),
                worldCenter);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    floatingRoot,
                    screenPoint,
                    floatingCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            floatingText.anchorMin = new Vector2(0.5f, 0.5f);
            floatingText.anchorMax = new Vector2(0.5f, 0.5f);
            floatingText.pivot = new Vector2(0.5f, 0.5f);
            floatingText.anchoredPosition = localPoint;
        }

        private static Camera ResolveAnchorCamera(RectTransform anchor)
        {
            Canvas anchorCanvas = anchor.GetComponentInParent<Canvas>();
            if (anchorCanvas != null)
            {
                if (anchorCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return null;
                }

                return anchorCanvas.worldCamera;
            }

            return Camera.main;
        }
    }
}
