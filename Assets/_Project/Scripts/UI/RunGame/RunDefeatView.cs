using System;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunDefeatView : MonoBehaviour, IRunGameView
    {
        private static readonly Color BackdropColor = new(0.025f, 0.01f, 0.02f, 0.98f);
        private static readonly Color PanelColor = new(0.14f, 0.025f, 0.045f, 0.99f);
        private static readonly Color ButtonColor = new(0.78f, 0.12f, 0.18f, 1f);
        private static readonly Color ResultButtonColor = new(0.28f, 0.08f, 0.12f, 1f);

        [SerializeField] private RectTransform _layoutRoot;
        [SerializeField] private Text _titleText;

        [Header("Revive Offer")]
        [SerializeField] private GameObject _reviveOfferRoot;
        [SerializeField] private Image _monsterImage;
        [SerializeField] private Text _countdownText;
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Text _reviveButtonText;

        [Header("Run Result")]
        [SerializeField] private GameObject _resultRoot;
        [SerializeField] private Text _summaryText;
        [SerializeField] private Text _contributionText;
        [SerializeField] private Button _newRunButton;
        [SerializeField] private Text _newRunButtonText;
        [SerializeField] private Button _rankingButton;
        [SerializeField] private Text _rankingButtonText;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Text _homeButtonText;

        private Sprite _monsterPortrait;
        private bool _subscribed;

        public event Action RestartRequested;

        public event Action RankingRequested;

        public event Action HomeRequested;

        public event Action ReviveRequested;

        private void OnDestroy()
        {
            UnsubscribeButtons();
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

        public void SetMonsterPortrait(Sprite portrait)
        {
            _monsterPortrait = portrait;
            if (_monsterImage != null)
            {
                _monsterImage.sprite = portrait;
                _monsterImage.gameObject.SetActive(portrait != null);
            }
        }

        public void Render(RunDefeatViewState state)
        {
            EnsureRuntimeLayout();
            SetText(_titleText, state.Title);
            SetText(_countdownText, state.CountdownLabel);
            SetText(_summaryText, state.Summary);
            SetText(_contributionText, state.ContributionSummary);
            SetText(_newRunButtonText, state.RestartLabel);
            SetText(_rankingButtonText, state.RankingLabel);
            SetText(_homeButtonText, state.HomeLabel);
            SetText(_reviveButtonText, state.ReviveLabel);

            SetActive(_reviveOfferRoot, state.IsReviveOffer);
            SetActive(_resultRoot, state.IsResultVisible);

            if (_monsterImage != null)
            {
                _monsterImage.sprite = _monsterPortrait;
                _monsterImage.gameObject.SetActive(
                    state.IsReviveOffer && _monsterPortrait != null);
            }

            if (_reviveButton != null)
            {
                _reviveButton.gameObject.SetActive(state.IsReviveVisible);
                _reviveButton.interactable = state.CanRevive;
            }
        }

        public void EnsureRuntimeLayout()
        {
            ResolveSceneReferences();
            if (_layoutRoot == null)
            {
                BuildRuntimeLayout();
            }

            SubscribeButtons();
        }

        private void ResolveSceneReferences()
        {
            _layoutRoot ??= FindDeepChild(transform, "Defeat Layout") as RectTransform;
            _titleText ??= FindChildComponent<Text>("Defeat Title");
            _reviveOfferRoot ??= FindDeepChild(transform, "Revive Offer Root")?.gameObject;
            _monsterImage ??= FindChildComponent<Image>("Defeating Monster Image");
            _countdownText ??= FindChildComponent<Text>("Revive Countdown");
            _reviveButton ??= FindChildComponent<Button>("Revive Button");
            _reviveButtonText ??= FindChildComponent<Text>("Revive Button Text");
            _resultRoot ??= FindDeepChild(transform, "Run Result Root")?.gameObject;
            _summaryText ??= FindChildComponent<Text>("Defeat Summary");
            _contributionText ??= FindChildComponent<Text>("Relic Contribution Text");
            _newRunButton ??= FindChildComponent<Button>("New Run Button");
            _newRunButtonText ??= FindChildComponent<Text>("New Run Button Text");
            _rankingButton ??= FindChildComponent<Button>("Ranking Button");
            _rankingButtonText ??= FindChildComponent<Text>("Ranking Button Text");
            _homeButton ??= FindChildComponent<Button>("Home Button");
            _homeButtonText ??= FindChildComponent<Text>("Home Button Text");
        }

        private void BuildRuntimeLayout()
        {
            Font font = Resources.Load<Font>("Galmuri11-Bold");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            _layoutRoot = CreateRect("Defeat Layout", transform);
            StretchOrFit(_layoutRoot);
            Image backdrop = _layoutRoot.gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;

            RectTransform panel = CreateRect("Defeat Panel", _layoutRoot);
            SetAnchors(panel, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            _titleText = CreateText(
                "Defeat Title",
                panel,
                font,
                68,
                new Vector2(0.05f, 0.82f),
                new Vector2(0.95f, 0.96f));
            _titleText.color = new Color(1f, 0.36f, 0.42f, 1f);

            BuildReviveOffer(panel, font);
            BuildRunResult(panel, font);
            SetMonsterPortrait(_monsterPortrait);
        }

        private void BuildReviveOffer(RectTransform panel, Font font)
        {
            RectTransform root = CreateRect("Revive Offer Root", panel);
            SetAnchors(root, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.82f));
            _reviveOfferRoot = root.gameObject;

            RectTransform monsterFrame = CreateRect("Monster Frame", root);
            SetAnchors(monsterFrame, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.98f));
            Image monsterFrameImage = monsterFrame.gameObject.AddComponent<Image>();
            monsterFrameImage.color = new Color(0.04f, 0.04f, 0.07f, 0.96f);

            RectTransform monsterRect = CreateRect("Defeating Monster Image", monsterFrame);
            SetAnchors(monsterRect, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
            _monsterImage = monsterRect.gameObject.AddComponent<Image>();
            _monsterImage.color = Color.white;
            _monsterImage.preserveAspect = true;
            _monsterImage.raycastTarget = false;

            _countdownText = CreateText(
                "Revive Countdown",
                root,
                font,
                38,
                new Vector2(0.05f, 0.18f),
                new Vector2(0.95f, 0.29f));
            _countdownText.color = new Color(1f, 0.82f, 0.38f, 1f);

            CreateActionButton(
                "Revive Button",
                "Revive Button Text",
                root,
                font,
                ButtonColor,
                new Vector2(0.14f, 0.01f),
                new Vector2(0.86f, 0.16f),
                out _reviveButton,
                out _reviveButtonText);
        }

        private void BuildRunResult(RectTransform panel, Font font)
        {
            RectTransform root = CreateRect("Run Result Root", panel);
            SetAnchors(root, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.82f));
            _resultRoot = root.gameObject;

            _summaryText = CreateText(
                "Defeat Summary",
                root,
                font,
                34,
                new Vector2(0.04f, 0.78f),
                new Vector2(0.96f, 0.98f));

            Text heading = CreateText(
                "Relic Contribution Heading",
                root,
                font,
                30,
                new Vector2(0.04f, 0.69f),
                new Vector2(0.96f, 0.77f));
            heading.text = "RELIC CONTRIBUTION";
            heading.color = new Color(1f, 0.82f, 0.38f, 1f);

            _contributionText = CreateScrollableText(
                root,
                font,
                new Vector2(0.04f, 0.18f),
                new Vector2(0.96f, 0.68f));

            CreateActionButton(
                "New Run Button",
                "New Run Button Text",
                root,
                font,
                ResultButtonColor,
                new Vector2(0.01f, 0.01f),
                new Vector2(0.32f, 0.14f),
                out _newRunButton,
                out _newRunButtonText);
            CreateActionButton(
                "Ranking Button",
                "Ranking Button Text",
                root,
                font,
                ResultButtonColor,
                new Vector2(0.345f, 0.01f),
                new Vector2(0.655f, 0.14f),
                out _rankingButton,
                out _rankingButtonText);
            CreateActionButton(
                "Home Button",
                "Home Button Text",
                root,
                font,
                ResultButtonColor,
                new Vector2(0.68f, 0.01f),
                new Vector2(0.99f, 0.14f),
                out _homeButton,
                out _homeButtonText);
        }

        private static Text CreateScrollableText(
            Transform parent,
            Font font,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform viewport = CreateRect("Relic Contribution Viewport", parent);
            SetAnchors(viewport, anchorMin, anchorMax);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0.035f, 0.025f, 0.045f, 0.92f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = viewport;

            RectTransform content = CreateRect("Relic Contribution Text", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(-40f, 0f);

            Text text = content.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 25;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = content;
            return text;
        }

        private void SubscribeButtons()
        {
            if (_subscribed ||
                _newRunButton == null ||
                _rankingButton == null ||
                _homeButton == null ||
                _reviveButton == null)
            {
                return;
            }

            _newRunButton.onClick.AddListener(HandleRestartClicked);
            _rankingButton.onClick.AddListener(HandleRankingClicked);
            _homeButton.onClick.AddListener(HandleHomeClicked);
            _reviveButton.onClick.AddListener(HandleReviveClicked);
            _subscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (!_subscribed)
            {
                return;
            }

            _newRunButton?.onClick.RemoveListener(HandleRestartClicked);
            _rankingButton?.onClick.RemoveListener(HandleRankingClicked);
            _homeButton?.onClick.RemoveListener(HandleHomeClicked);
            _reviveButton?.onClick.RemoveListener(HandleReviveClicked);
            _subscribed = false;
        }

        private void HandleRestartClicked()
        {
            RestartRequested?.Invoke();
        }

        private void HandleRankingClicked()
        {
            RankingRequested?.Invoke();
        }

        private void HandleHomeClicked()
        {
            HomeRequested?.Invoke();
        }

        private void HandleReviveClicked()
        {
            ReviveRequested?.Invoke();
        }

        private T FindChildComponent<T>(string objectName) where T : Component
        {
            Transform child = FindDeepChild(transform, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindDeepChild(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindDeepChild(parent.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void CreateActionButton(
            string buttonName,
            string textName,
            Transform parent,
            Font font,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out Button button,
            out Text buttonText)
        {
            RectTransform buttonRect = CreateRect(buttonName, parent);
            SetAnchors(buttonRect, anchorMin, anchorMax);
            Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = color;
            button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            buttonText = CreateText(
                textName,
                buttonRect,
                font,
                26,
                Vector2.zero,
                Vector2.one);
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

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
