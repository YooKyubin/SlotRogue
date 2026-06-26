using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// 로비의 임시 디버그 버튼(튜토리얼 시작/스킵/초기화, 광고 구매 초기화) UI 생성·배선만 담당하는
    /// 순수 헬퍼입니다. "무엇을 하는가"는 소유자(GameStartSceneRoot)가 전달한 콜백이 결정합니다.
    /// 출시 전 제거 대상인 임시 UI이므로 런타임 생성을 허용합니다.
    /// </summary>
    public sealed class LobbyDebugButtons
    {
        private static readonly Color ButtonColor = new(0.14f, 0.14f, 0.18f, 0.92f);
        private static readonly Color ButtonTextColor = new(1f, 0.82f, 0.38f, 1f);

        private Button _tutorialStartButton;
        private Button _tutorialSkipButton;
        private Button _tutorialResetButton;
        private Button _adsResetButton;

        private Action _onTutorialStart;
        private Action _onTutorialSkip;
        private Action _onTutorialReset;
        private Action _onAdsReset;

        public void Install(
            Canvas canvas,
            Action onTutorialStart,
            Action onTutorialSkip,
            Action onTutorialReset,
            Action onAdsReset)
        {
            if (canvas == null)
            {
                Debug.LogWarning(
                    "[LobbyDebugButtons] Temporary reset buttons require a Canvas.");
                return;
            }

            _onTutorialStart = onTutorialStart;
            _onTutorialSkip = onTutorialSkip;
            _onTutorialReset = onTutorialReset;
            _onAdsReset = onAdsReset;

            Transform existingHost = canvas.transform.Find("Temporary Reset Buttons");
            RectTransform host = existingHost as RectTransform;
            if (host == null)
            {
                host = CreateRect("Temporary Reset Buttons", canvas.transform);
                host.anchorMin = new Vector2(0.04f, 0.03f);
                host.anchorMax = new Vector2(0.86f, 0.14f);
                host.offsetMin = Vector2.zero;
                host.offsetMax = Vector2.zero;
            }

            _tutorialStartButton = EnsureButton(
                host,
                "Temporary Tutorial Start Button",
                "튜토리얼 시작",
                new Vector2(0f, 0f),
                new Vector2(0.235f, 1f));
            _tutorialSkipButton = EnsureButton(
                host,
                "Temporary Tutorial Skip Button",
                "튜토리얼 스킵",
                new Vector2(0.255f, 0f),
                new Vector2(0.49f, 1f));
            _tutorialResetButton = EnsureButton(
                host,
                "Temporary Tutorial Reset Button",
                "튜토리얼 초기화",
                new Vector2(0.51f, 0f),
                new Vector2(0.745f, 1f));
            _adsResetButton = EnsureButton(
                host,
                "Temporary Ads Reset Button",
                "광고 구매 초기화",
                new Vector2(0.765f, 0f),
                new Vector2(1f, 1f));

            Subscribe();
        }

        public void Dispose()
        {
            _tutorialStartButton?.onClick.RemoveListener(HandleTutorialStart);
            _tutorialSkipButton?.onClick.RemoveListener(HandleTutorialSkip);
            _tutorialResetButton?.onClick.RemoveListener(HandleTutorialReset);
            _adsResetButton?.onClick.RemoveListener(HandleAdsReset);
        }

        private void Subscribe()
        {
            Dispose();

            _tutorialStartButton?.onClick.AddListener(HandleTutorialStart);
            _tutorialSkipButton?.onClick.AddListener(HandleTutorialSkip);
            _tutorialResetButton?.onClick.AddListener(HandleTutorialReset);
            _adsResetButton?.onClick.AddListener(HandleAdsReset);
        }

        private void HandleTutorialStart() => _onTutorialStart?.Invoke();

        private void HandleTutorialSkip() => _onTutorialSkip?.Invoke();

        private void HandleTutorialReset() => _onTutorialReset?.Invoke();

        private void HandleAdsReset() => _onAdsReset?.Invoke();

        private static Button EnsureButton(
            RectTransform parent,
            string objectName,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Transform existing = parent.Find(objectName);
            RectTransform buttonRect = existing as RectTransform;
            if (buttonRect == null)
            {
                buttonRect = CreateRect(objectName, parent);
            }

            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image image = buttonRect.GetComponent<Image>();
            if (image == null)
            {
                image = buttonRect.gameObject.AddComponent<Image>();
            }

            image.color = ButtonColor;
            image.raycastTarget = true;

            Button button = buttonRect.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRect.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = image;

            Text text = buttonRect.GetComponentInChildren<Text>(includeInactive: true);
            if (text == null)
            {
                RectTransform textRect = CreateRect($"{objectName} Text", buttonRect);
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 0f);
                textRect.offsetMax = new Vector2(-8f, 0f);
                text = textRect.gameObject.AddComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }

            Font font = Resources.Load<Font>("Galmuri11-Bold");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            text.font = font;
            text.fontSize = 24;
            text.color = ButtonTextColor;
            text.text = label;

            return button;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }
    }
}
