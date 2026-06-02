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
            bool isPlayerTarget,
            CancellationToken cancellationToken)
        {
            string prefix = isCritical ? "[CRIT] " : string.Empty;
            string targetLabel = isPlayerTarget ? "Player" : "Monster";
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

            RectTransform anchor = isPlayerTarget ? Host.PlayerDamageAnchor : Host.MonsterDamageAnchor;
            if (anchor == null)
            {
                Debug.LogWarning($"[Presentation] Missing {(isPlayerTarget ? "player" : "monster")} damage anchor.");
                return;
            }

            FloatingDamageTextView damageText = Object.Instantiate(
                Host.FloatingDamageTextPrefab,
                Host.FloatingTextRoot);
            RectTransform textTransform = damageText.transform as RectTransform;
            if (textTransform != null)
            {
                textTransform.anchoredPosition = anchor.anchoredPosition;
            }

            CombatAnchorKind anchorKind = isPlayerTarget ? CombatAnchorKind.Player : CombatAnchorKind.Monster;
            await damageText.Play(amount, isCritical, anchorKind, cancellationToken);
        }
    }
}
