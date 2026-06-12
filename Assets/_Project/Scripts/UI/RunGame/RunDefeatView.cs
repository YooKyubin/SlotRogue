using System;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunDefeatView : MonoBehaviour, IRunGameView
    {
        private static readonly Color BackdropColor = new(0.04f, 0.01f, 0.02f, 0.96f);
        private static readonly Color PanelColor = new(0.18f, 0.04f, 0.06f, 0.98f);
        private static readonly Color ButtonColor = new(0.72f, 0.12f, 0.16f, 1f);

        [SerializeField] private Text _titleText;
        [SerializeField] private Text _summaryText;
        [SerializeField] private Button _newRunButton;
        [SerializeField] private Text _newRunButtonText;

        private bool _subscribed;

        public event Action NewRunRequested;

        private void OnDestroy()
        {
            UnsubscribeButton();
        }

        public void OnEnter()
        {
            EnsureRuntimeLayout();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void OnExit()
        {
            gameObject.SetActive(false);
        }

        public void Render(RunDefeatViewState state)
        {
            EnsureRuntimeLayout();
            SetText(_titleText, state.Title);
            SetText(_summaryText, state.Summary);
            SetText(_newRunButtonText, state.NewRunLabel);
        }

        public void EnsureRuntimeLayout()
        {
            if (_titleText == null || _summaryText == null || _newRunButton == null)
            {
                BuildRuntimeLayout();
            }

            SubscribeButton();
        }

        private void BuildRuntimeLayout()
        {
            Font font = Resources.Load<Font>("Galmuri11-Bold");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            RectTransform layout = CreateRect("Defeat Layout", transform);
            StretchOrFit(layout);
            Image backdrop = layout.gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;

            RectTransform panel = CreateRect("Defeat Panel", layout);
            SetAnchors(panel, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.75f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            _titleText = CreateText(
                "Defeat Title",
                panel,
                font,
                fontSize: 72,
                new Vector2(0.05f, 0.66f),
                new Vector2(0.95f, 0.92f));
            _titleText.color = new Color(1f, 0.42f, 0.42f, 1f);

            _summaryText = CreateText(
                "Defeat Summary",
                panel,
                font,
                fontSize: 38,
                new Vector2(0.08f, 0.30f),
                new Vector2(0.92f, 0.66f));

            RectTransform buttonRect = CreateRect("New Run Button", panel);
            SetAnchors(buttonRect, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.25f));
            Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = ButtonColor;
            _newRunButton = buttonRect.gameObject.AddComponent<Button>();
            _newRunButton.targetGraphic = buttonImage;

            _newRunButtonText = CreateText(
                "New Run Button Text",
                buttonRect,
                font,
                fontSize: 40,
                Vector2.zero,
                Vector2.one);
        }

        private void SubscribeButton()
        {
            if (_subscribed || _newRunButton == null)
            {
                return;
            }

            _newRunButton.onClick.AddListener(HandleNewRunClicked);
            _subscribed = true;
        }

        private void UnsubscribeButton()
        {
            if (!_subscribed || _newRunButton == null)
            {
                return;
            }

            _newRunButton.onClick.RemoveListener(HandleNewRunClicked);
            _subscribed = false;
        }

        private void HandleNewRunClicked()
        {
            NewRunRequested?.Invoke();
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            Font font,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rectTransform = CreateRect(objectName, parent);
            SetAnchors(rectTransform, anchorMin, anchorMax);

            Text text = rectTransform.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void StretchOrFit(RectTransform rectTransform)
        {
            if (rectTransform.parent is RectTransform)
            {
                SetAnchors(rectTransform, Vector2.zero, Vector2.one);
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(1080f, 1920f);
        }

        private static void SetAnchors(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
