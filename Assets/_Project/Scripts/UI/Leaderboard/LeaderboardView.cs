using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Leaderboard
{
    public sealed class LeaderboardView : MonoBehaviour
    {
        private static readonly Color OverlayColor = new(0.025f, 0.035f, 0.07f, 0.97f);
        private static readonly Color PanelColor = new(0.08f, 0.12f, 0.22f, 1f);
        private static readonly Color ButtonColor = new(0.18f, 0.42f, 0.72f, 1f);
        private static readonly Color AccentColor = new(0.95f, 0.76f, 0.2f, 1f);

        private Button _openButton;
        private GameObject _panel;
        private Text _titleText;
        private InputField _nameInput;
        private Button _saveProfileButton;
        private Text _saveProfileButtonText;
        private Button _refreshButton;
        private Button _closeButton;
        private GameObject _entriesViewport;
        private Text _entriesText;
        private Text _statusText;
        private bool _subscribed;
        private bool _showLauncher = true;

        public event Action OpenRequested;

        public event Action CloseRequested;

        public event Action RefreshRequested;

        public event Action<string> PlayerProfileSubmitted;

        public static LeaderboardView CreateRuntime(Transform canvasTransform)
        {
            var host = new GameObject("LeaderboardView", typeof(RectTransform));
            RectTransform hostRect = (RectTransform)host.transform;
            hostRect.SetParent(canvasTransform, false);
            Stretch(hostRect);

            LeaderboardView view = host.AddComponent<LeaderboardView>();
            view.EnsureRuntimeLayout();
            return view;
        }

        public void EnsureRuntimeLayout()
        {
            if (_openButton == null || _panel == null)
            {
                BuildRuntimeLayout();
            }

            SubscribeButtons();
        }

        public void SetLauncherVisible(bool isVisible)
        {
            _showLauncher = isVisible;
            if (_openButton != null && (_panel == null || !_panel.activeSelf))
            {
                _openButton.gameObject.SetActive(_showLauncher);
            }
        }

        public void Render(LeaderboardViewState state)
        {
            EnsureRuntimeLayout();
            bool isProfileRequired = state?.IsProfileRequired == true;
            bool isVisible = state?.IsVisible == true && !isProfileRequired;

            _panel.SetActive(isVisible);
            _openButton.gameObject.SetActive(
                !isVisible &&
                !isProfileRequired &&
                _showLauncher);

            if (!isVisible || state == null)
            {
                return;
            }

            transform.SetAsLastSibling();
            if (_titleText != null)
            {
                _titleText.text = "SLOT ROGUE LEADERBOARD";
            }

            if (_nameInput != null && !_nameInput.isFocused)
            {
                _nameInput.text = state.PlayerName;
            }

            if (_entriesText != null)
            {
                _entriesText.text = BuildEntriesText(state.Entries);
            }

            if (_saveProfileButtonText != null)
            {
                _saveProfileButtonText.text = "SAVE";
            }

            if (_refreshButton != null)
            {
                _refreshButton.gameObject.SetActive(true);
            }

            if (_closeButton != null)
            {
                _closeButton.gameObject.SetActive(true);
            }

            if (_entriesViewport != null)
            {
                _entriesViewport.SetActive(true);
            }

            if (_statusText != null)
            {
                _statusText.text = state.StatusMessage;
            }

            SetInteractable(_saveProfileButton, !state.IsLoading);
            SetInteractable(_refreshButton, !state.IsLoading);
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        private void BuildRuntimeLayout()
        {
            Font font = FindFont(transform.root);
            RemoveStaleRuntimeLayout();

            _openButton = CreateButton(
                "Leaderboard Open Button",
                transform,
                font,
                "LEADERBOARD",
                new Vector2(0.66f, 0.91f),
                new Vector2(0.96f, 0.98f));

            RectTransform panelRect = CreateRect("Leaderboard Panel", transform);
            Stretch(panelRect);
            Image overlay = panelRect.gameObject.AddComponent<Image>();
            overlay.color = OverlayColor;
            _panel = panelRect.gameObject;

            RectTransform contentPanel = CreateRect("Leaderboard Content", panelRect);
            SetAnchors(
                contentPanel,
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.92f));
            Image panelImage = contentPanel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            _titleText = CreateText(
                "Leaderboard Title",
                contentPanel,
                font,
                48,
                TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.88f),
                new Vector2(0.65f, 0.98f));
            _titleText.text = "SLOT ROGUE LEADERBOARD";
            _titleText.color = AccentColor;

            _refreshButton = CreateButton(
                "Refresh Button",
                contentPanel,
                font,
                "REFRESH",
                new Vector2(0.67f, 0.90f),
                new Vector2(0.82f, 0.97f));
            _closeButton = CreateButton(
                "Close Button",
                contentPanel,
                font,
                "X",
                new Vector2(0.84f, 0.90f),
                new Vector2(0.94f, 0.97f));

            _nameInput = CreateInputField(
                "Nickname Input",
                contentPanel,
                font,
                new Vector2(0.06f, 0.79f),
                new Vector2(0.70f, 0.87f));
            _saveProfileButton = CreateButton(
                "Save Profile Button",
                contentPanel,
                font,
                "SAVE",
                new Vector2(0.72f, 0.79f),
                new Vector2(0.94f, 0.87f));
            _saveProfileButtonText =
                _saveProfileButton.GetComponentInChildren<Text>(includeInactive: true);

            _entriesText = CreateScrollableEntries(
                contentPanel,
                font,
                new Vector2(0.07f, 0.12f),
                new Vector2(0.93f, 0.76f));
            _entriesViewport = _entriesText.transform.parent.gameObject;

            _statusText = CreateText(
                "Leaderboard Status",
                contentPanel,
                font,
                25,
                TextAnchor.MiddleCenter,
                new Vector2(0.07f, 0.03f),
                new Vector2(0.93f, 0.11f));
            _statusText.color = new Color(0.75f, 0.82f, 0.94f, 1f);

            _panel.SetActive(false);
        }

        private void RemoveStaleRuntimeLayout()
        {
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                Transform child = transform.GetChild(index);
                if (child.name != "Leaderboard Open Button" &&
                    child.name != "Leaderboard Panel")
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void SubscribeButtons()
        {
            if (_subscribed)
            {
                return;
            }

            _openButton.onClick.AddListener(HandleOpenClicked);
            _saveProfileButton.onClick.AddListener(HandleSaveProfileClicked);
            _refreshButton.onClick.AddListener(HandleRefreshClicked);
            _closeButton.onClick.AddListener(HandleCloseClicked);
            _subscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_openButton != null)
            {
                _openButton.onClick.RemoveListener(HandleOpenClicked);
            }

            if (_saveProfileButton != null)
            {
                _saveProfileButton.onClick.RemoveListener(HandleSaveProfileClicked);
            }

            if (_refreshButton != null)
            {
                _refreshButton.onClick.RemoveListener(HandleRefreshClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            _subscribed = false;
        }

        private void HandleOpenClicked()
        {
            OpenRequested?.Invoke();
        }

        private void HandleSaveProfileClicked()
        {
            PlayerProfileSubmitted?.Invoke(_nameInput?.text ?? string.Empty);
        }

        private void HandleRefreshClicked()
        {
            RefreshRequested?.Invoke();
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private static string BuildEntriesText(IReadOnlyList<LeaderboardEntryData> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int index = 0; index < entries.Count; index++)
            {
                LeaderboardEntryData entry = entries[index];
                string marker = entry.IsCurrentPlayer ? "* " : string.Empty;
                string relics = entry.RelicIds.Count == 0
                    ? "-"
                    : string.Join(", ", entry.RelicIds);

                builder.Append(marker);
                builder.Append('#');
                builder.Append(entry.Rank);
                builder.Append(' ');
                builder.Append(entry.PlayerName);
                builder.Append("\n   SCORE ");
                builder.Append(Math.Round(entry.Score));
                builder.Append("  WAVE ");
                builder.Append(entry.Wave);
                builder.Append("  RELICS ");
                builder.Append(relics);

                if (index < entries.Count - 1)
                {
                    builder.Append('\n');
                    builder.Append('\n');
                }
            }

            return builder.ToString();
        }

        private static Font FindFont(Transform root)
        {
            Text existingText = root != null
                ? root.GetComponentInChildren<Text>(includeInactive: true)
                : null;
            if (existingText != null && existingText.font != null)
            {
                return existingText.font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static InputField CreateInputField(
            string objectName,
            Transform parent,
            Font font,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rectTransform = CreateRect(objectName, parent);
            SetAnchors(rectTransform, anchorMin, anchorMax);

            Image background = rectTransform.gameObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.04f, 0.08f, 1f);

            InputField input = rectTransform.gameObject.AddComponent<InputField>();
            Text text = CreateText(
                "Text",
                rectTransform,
                font,
                28,
                TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0f),
                new Vector2(0.96f, 1f));
            Text placeholder = CreateText(
                "Placeholder",
                rectTransform,
                font,
                28,
                TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0f),
                new Vector2(0.96f, 1f));
            placeholder.text = "Nickname";
            placeholder.color = new Color(0.55f, 0.6f, 0.7f, 1f);

            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = background;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 50;
            return input;
        }

        private static Text CreateScrollableEntries(
            Transform parent,
            Font font,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform viewportRect = CreateRect("Leaderboard Entries Viewport", parent);
            SetAnchors(viewportRect, anchorMin, anchorMax);

            Image viewportImage = viewportRect.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewportRect.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            ScrollRect scrollRect = viewportRect.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 48f;
            scrollRect.viewport = viewportRect;

            RectTransform contentRect = CreateRect("Leaderboard Entries", viewportRect);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            Text text = contentRect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 27;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            ContentSizeFitter fitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            return text;
        }

        private static Button CreateButton(
            string objectName,
            Transform parent,
            Font font,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rectTransform = CreateRect(objectName, parent);
            SetAnchors(rectTransform, anchorMin, anchorMax);

            Image image = rectTransform.gameObject.AddComponent<Image>();
            image.color = ButtonColor;

            Button button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText(
                $"{objectName} Text",
                rectTransform,
                font,
                26,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one);
            text.text = label;
            return button;
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rectTransform = CreateRect(objectName, parent);
            SetAnchors(rectTransform, anchorMin, anchorMax);

            Text text = rectTransform.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetAnchors(rectTransform, Vector2.zero, Vector2.one);
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

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }

    }
}
