using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

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

            viewModel.ApplyParticipantSnapshot(combatEvent.IsPlayerParticipant, combatEvent.TargetAfter);
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

            if (Host.FloatingTextRoot == null || amount <= 0)
            {
                return;
            }

            var textObject = new GameObject("Floating Damage", typeof(RectTransform));
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.SetParent(Host.FloatingTextRoot, false);
            rectTransform.anchoredPosition = new Vector2(0f, isPlayerTarget ? -120f : 40f);
            rectTransform.sizeDelta = new Vector2(420f, 60f);

            var text = textObject.AddComponent<Text>();
            text.font = Host.DefaultFont;
            text.fontSize = isCritical ? 34 : 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = isCritical ? new Color32(255, 210, 64, 255) : new Color32(255, 120, 120, 255);
            text.text = $"{prefix}-{amount}";

            float elapsed = 0f;
            const float duration = 0.55f;
            Color startColor = text.color;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (text == null)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                rectTransform.anchoredPosition += new Vector2(0f, 40f * Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (textObject != null)
            {
                UnityEngine.Object.Destroy(textObject);
            }
        }
    }
}
