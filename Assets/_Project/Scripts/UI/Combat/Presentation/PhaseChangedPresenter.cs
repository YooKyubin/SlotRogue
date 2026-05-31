using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class PhaseChangedPresenter : CombatPresenterBase
    {
        private const float BannerDuration = 1f;

        public PhaseChangedPresenter(CombatPresentationHost host)
            : base(host)
        {
        }

        public override async UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.PhaseChanged)
            {
                return;
            }

            string message = combatEvent.Phase switch
            {
                BattlePhase.Resolving => "플레이어(전투) 턴 시작",
                BattlePhase.EnemyTurn => "몬스터 턴 시작",
                BattlePhase.PlayerTurn => "플레이어(룰렛) 턴 시작",
                _ => null,
            };

            if (message == null)
            {
                return;
            }

            await ShowTurnBannerAsync(message, cancellationToken);
        }

        private async UniTask ShowTurnBannerAsync(string message, CancellationToken cancellationToken)
        {
            if (Host.FloatingTextRoot == null)
            {
                await CombatPresentationTweens.DelayAsync(BannerDuration, Host.LinkTarget, cancellationToken);
                return;
            }

            var bannerObject = new GameObject("Turn Banner", typeof(RectTransform));
            RectTransform rectTransform = bannerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(Host.FloatingTextRoot, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 180f);
            rectTransform.sizeDelta = new Vector2(700f, 80f);

            var text = bannerObject.AddComponent<Text>();
            text.font = Host.DefaultFont;
            text.fontSize = 40;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color32(255, 230, 140, 255);
            text.text = message;

            try
            {
                await CombatPresentationTweens.DelayAsync(BannerDuration, Host.LinkTarget, cancellationToken);
            }
            finally
            {
                if (bannerObject != null)
                {
                    UnityEngine.Object.Destroy(bannerObject);
                }
            }
        }
    }
}
