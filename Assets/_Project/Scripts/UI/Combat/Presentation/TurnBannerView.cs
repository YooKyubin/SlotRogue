using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class TurnBannerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _bannerRoot;
        [SerializeField] private Font _defaultFont;
        [SerializeField] private GameObject _linkTarget;

        public async UniTask ShowTurnBannerAsync(
            string message,
            float duration,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (_bannerRoot == null)
            {
                await CombatPresentationTweens.DelayAsync(duration, ResolveLinkTarget(), cancellationToken);
                return;
            }

            GameObject bannerObject = new("Turn Banner", typeof(RectTransform));
            RectTransform rectTransform = bannerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(_bannerRoot, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 180f);
            rectTransform.sizeDelta = new Vector2(700f, 80f);

            Text text = bannerObject.AddComponent<Text>();
            text.font = ResolveFont();
            text.fontSize = 40;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color32(255, 230, 140, 255);
            text.text = message;

            try
            {
                await CombatPresentationTweens.DelayAsync(duration, ResolveLinkTarget(), cancellationToken);
            }
            finally
            {
                if (bannerObject != null)
                {
                    Destroy(bannerObject);
                }
            }
        }

        private GameObject ResolveLinkTarget()
        {
            return _linkTarget != null ? _linkTarget : gameObject;
        }

        private Font ResolveFont()
        {
            if (_defaultFont != null)
            {
                return _defaultFont;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
