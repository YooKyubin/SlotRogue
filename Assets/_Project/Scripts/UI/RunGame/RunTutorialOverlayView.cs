using System;
using SlotRogue.UI.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunTutorialOverlayView : ViewComponentBase, ITutorialOverlay
    {
        private const float TargetPanelGap = 28f;
        private const float PanelScreenPadding = 18f;

        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private Text _bodyText;
        [SerializeField] private TMP_Text _bodyTmpText;

        [Header("Spotlight")]
        [SerializeField] private RectTransform _spotlightRoot;
        [SerializeField] private RectTransform _topBlocker;
        [SerializeField] private RectTransform _bottomBlocker;
        [SerializeField] private RectTransform _leftBlocker;
        [SerializeField] private RectTransform _rightBlocker;
        [SerializeField] private RectTransform _highlightFrame;
        [SerializeField] private float _spotlightPadding = 20f;
        [SerializeField] private Vector2 _spotlightMinSize = new(56f, 56f);
        [SerializeField] private Color _generatedBlockerColor = new(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color _generatedFrameColor = new(1f, 0.82f, 0.22f, 0.2f);

        [Header("Hand Pointer")]
        [SerializeField] private Image _handImage;
        [SerializeField] private Sprite[] _handSprites = Array.Empty<Sprite>();
        [SerializeField] private Vector2 _handOffset = new(36f, -36f);
        [SerializeField] private Vector2 _handSize = new(56f, 56f);
        [SerializeField] private float _handFrameInterval = 0.18f;
        [SerializeField] private float _handPulseDistance = 8f;
        [SerializeField] private float _handPulseSpeed = 5f;

        [Header("Step Advance")]
        [SerializeField] private RectTransform _inputBlocker;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _nextButtonText;
        [SerializeField] private TMP_Text _nextButtonTmpText;
        [SerializeField] private string _nextButtonLabel = "다음";

        private readonly Vector3[] _targetWorldCorners = new Vector3[4];
        private readonly Vector2[] _targetLocalCorners = new Vector2[4];
        private bool _reportedMissingReferences;
        private RectTransform _spotlightTarget;
        private Canvas _canvas;
        private bool _showHand;
        private RunBattleTutorialStep _currentStep;
        private Action _pendingAdvance;
        private bool _advanceListenerWired;

        private void Awake()
        {
            EnsureRuntimeLayout();
            Hide();
        }

        private void LateUpdate()
        {
            if (_panelRoot == null ||
                !_panelRoot.gameObject.activeInHierarchy ||
                _currentStep == null)
            {
                return;
            }

            if (_spotlightTarget != null)
            {
                UpdateSpotlightLayout();
                return;
            }

            ApplyMessagePlacement(
                _currentStep,
                ResolveLayoutRootRect(),
                null,
                null);
        }

        private void OnDestroy()
        {
            if (_advanceListenerWired && _nextButton != null)
            {
                _nextButton.onClick.RemoveListener(HandleAdvanceClicked);
            }
        }

        public bool EnsureRuntimeLayout()
        {
            if (_panelRoot != null && HasBodyText())
            {
                return true;
            }

            if (!_reportedMissingReferences)
            {
                Debug.LogError(
                    "[RunTutorialOverlayView] Required tutorial overlay UI objects must be placed in the hierarchy. " +
                    $"Missing: {BuildMissingReferenceSummary()}");
                _reportedMissingReferences = true;
            }

            return false;
        }

        public void ShowMessage(string message)
        {
            if (!EnsureRuntimeLayout())
            {
                return;
            }

            SetMessage(message);
            ClearSpotlight();
            ClearAdvanceControls();
            BringToFront();
        }

        public void ShowNarration(string message, Action onAdvance)
        {
            ShowStep(
                null,
                new RunBattleTutorialStep(
                    RunBattleTutorialTargetKey.None,
                    message,
                    false,
                    RunTutorialMessagePlacement.Center),
                onAdvance);
        }

        public void ShowStep(
            RectTransform target,
            RunBattleTutorialStep step,
            Action onAdvance)
        {
            if (step == null)
            {
                onAdvance?.Invoke();
                return;
            }

            if (!EnsureRuntimeLayout())
            {
                onAdvance?.Invoke();
                return;
            }

            SetMessage(step.Message);
            _currentStep = step;
            _pendingAdvance = onAdvance;
            _spotlightTarget = target;
            _showHand = step.ShowHand;
            SetBodyRaycast(false);

            if (target == null || !EnsureSpotlightReferences())
            {
                _spotlightTarget = null;
                _showHand = false;
                SetSpotlightActive(false);
                ApplyMessagePlacement(step, ResolveLayoutRootRect(), null, null);
            }
            else
            {
                SetSpotlightActive(true);
                UpdateSpotlightLayout();
            }

            EnsureAdvanceControls();
            BringToFront();
        }

        public void ShowSpotlight(RectTransform target, string message, bool showHand)
        {
            if (!EnsureRuntimeLayout())
            {
                return;
            }

            SetMessage(message);
            _currentStep = null;
            _spotlightTarget = target;
            _showHand = showHand;
            ClearAdvanceControls();

            if (target == null || !EnsureSpotlightReferences())
            {
                ClearSpotlight();
                BringToFront();
                return;
            }

            SetSpotlightActive(true);
            UpdateSpotlightLayout();
            BringToFront();
        }

        public void Hide()
        {
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(false);
            }

            ClearSpotlight();
            ClearAdvanceControls();
            gameObject.SetActive(false);
        }

        private void ClearAdvanceControls()
        {
            _pendingAdvance = null;
            SetActive(_inputBlocker, false);
            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(false);
            }
        }

        private bool EnsureAdvanceControls()
        {
            EnsureInputBlocker();
            if (!EnsureNextButton())
            {
                return false;
            }

            if (!_advanceListenerWired)
            {
                _nextButton.onClick.AddListener(HandleAdvanceClicked);
                _advanceListenerWired = true;
            }

            ConfigureNextButtonText();
            SetActive(_inputBlocker, true);
            _nextButton.gameObject.SetActive(true);
            return true;
        }

        private bool EnsureInputBlocker()
        {
            if (_inputBlocker != null)
            {
                return true;
            }

            RectTransform parent = transform as RectTransform;
            if (parent == null)
            {
                return false;
            }

            var generated = new GameObject(
                "Tutorial Input Blocker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            generated.transform.SetParent(parent, false);

            _inputBlocker = generated.GetComponent<RectTransform>();
            _inputBlocker.anchorMin = Vector2.zero;
            _inputBlocker.anchorMax = Vector2.one;
            _inputBlocker.pivot = new Vector2(0.5f, 0.5f);
            _inputBlocker.offsetMin = Vector2.zero;
            _inputBlocker.offsetMax = Vector2.zero;

            var image = generated.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
            generated.SetActive(false);
            return true;
        }

        private bool EnsureNextButton()
        {
            if (_nextButton == null)
            {
                if (_panelRoot == null)
                {
                    return false;
                }

                var generated = new GameObject(
                    "Tutorial Next Button",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                generated.transform.SetParent(_panelRoot, false);

                RectTransform rectTransform = generated.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(1f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 0f);
                rectTransform.pivot = new Vector2(1f, 0f);
                rectTransform.anchoredPosition = new Vector2(-18f, 12f);
                rectTransform.sizeDelta = new Vector2(120f, 44f);
                rectTransform.localScale = Vector3.one;

                var image = generated.GetComponent<Image>();
                image.color = new Color(0.08f, 0.09f, 0.11f, 0.92f);
                image.raycastTarget = true;

                _nextButton = generated.GetComponent<Button>();
                _nextButton.transition = Selectable.Transition.ColorTint;

                var textObject = new GameObject(
                    "Tutorial Next Button Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                textObject.transform.SetParent(generated.transform, false);

                RectTransform textTransform = textObject.GetComponent<RectTransform>();
                textTransform.anchorMin = Vector2.zero;
                textTransform.anchorMax = Vector2.one;
                textTransform.offsetMin = Vector2.zero;
                textTransform.offsetMax = Vector2.zero;

                _nextButtonText = textObject.GetComponent<Text>();
                _nextButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_nextButtonText.font == null)
                {
                    _nextButtonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                _nextButtonText.alignment = TextAnchor.MiddleCenter;
                _nextButtonText.color = Color.white;
                _nextButtonText.fontSize = 22;
                _nextButtonText.raycastTarget = false;
                generated.SetActive(false);
            }

            if (_nextButtonText == null && _nextButton != null)
            {
                _nextButtonText = _nextButton.GetComponentInChildren<Text>(true);
            }

            if (_nextButtonTmpText == null && _nextButton != null)
            {
                _nextButtonTmpText = _nextButton.GetComponentInChildren<TMP_Text>(true);
            }

            return _nextButton != null;
        }

        private void ConfigureNextButtonText()
        {
            string label = string.IsNullOrWhiteSpace(_nextButtonLabel)
                ? "다음"
                : _nextButtonLabel;

            if (_nextButtonText != null)
            {
                _nextButtonText.text = label;
                _nextButtonText.raycastTarget = false;
            }

            if (_nextButtonTmpText != null)
            {
                _nextButtonTmpText.text = label;
                _nextButtonTmpText.raycastTarget = false;
            }
        }

        private void HandleAdvanceClicked()
        {
            Action advance = _pendingAdvance;
            _pendingAdvance = null;
            SetActive(_inputBlocker, false);
            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(false);
            }

            advance?.Invoke();
        }

        private bool HasBodyText()
        {
            return _bodyText != null || _bodyTmpText != null;
        }

        private void SetMessage(string message)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(true);
            }

            if (_bodyText != null)
            {
                _bodyText.text = message ?? string.Empty;
            }

            if (_bodyTmpText != null)
            {
                _bodyTmpText.text = message ?? string.Empty;
            }
        }

        private void SetBodyRaycast(bool raycastTarget)
        {
            if (_bodyText != null)
            {
                _bodyText.raycastTarget = raycastTarget;
            }

            if (_bodyTmpText != null)
            {
                _bodyTmpText.raycastTarget = raycastTarget;
            }
        }

        private void BringToFront()
        {
            transform.SetAsLastSibling();
            _topBlocker?.SetAsFirstSibling();
            _bottomBlocker?.SetAsFirstSibling();
            _leftBlocker?.SetAsFirstSibling();
            _rightBlocker?.SetAsFirstSibling();
            _inputBlocker?.SetAsLastSibling();
            _highlightFrame?.SetAsLastSibling();
            _handImage?.rectTransform.SetAsLastSibling();
            _panelRoot?.SetAsLastSibling();
            _nextButton?.transform.SetAsLastSibling();
        }

        private void ClearSpotlight()
        {
            _spotlightTarget = null;
            _showHand = false;
            _currentStep = null;
            SetSpotlightActive(false);
        }

        private bool EnsureSpotlightReferences()
        {
            if (_spotlightRoot == null)
            {
                _spotlightRoot = transform as RectTransform;
            }

            if (_spotlightRoot == null)
            {
                return false;
            }

            _topBlocker ??= CreateGeneratedImage("Tutorial Spotlight Top", _generatedBlockerColor, true);
            _bottomBlocker ??= CreateGeneratedImage("Tutorial Spotlight Bottom", _generatedBlockerColor, true);
            _leftBlocker ??= CreateGeneratedImage("Tutorial Spotlight Left", _generatedBlockerColor, true);
            _rightBlocker ??= CreateGeneratedImage("Tutorial Spotlight Right", _generatedBlockerColor, true);
            _highlightFrame ??= CreateGeneratedImage("Tutorial Spotlight Highlight", _generatedFrameColor, false);

            SetImageRaycast(_topBlocker, true);
            SetImageRaycast(_bottomBlocker, true);
            SetImageRaycast(_leftBlocker, true);
            SetImageRaycast(_rightBlocker, true);
            SetImageRaycast(_highlightFrame, false);
            EnsureHandImage();

            return _topBlocker != null &&
                _bottomBlocker != null &&
                _leftBlocker != null &&
                _rightBlocker != null;
        }

        private RectTransform CreateGeneratedImage(
            string objectName,
            Color color,
            bool raycastTarget)
        {
            var generated = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            generated.transform.SetParent(_spotlightRoot, false);

            var rectTransform = generated.GetComponent<RectTransform>();
            var image = generated.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            generated.SetActive(false);
            return rectTransform;
        }

        private static void SetImageRaycast(RectTransform target, bool raycastTarget)
        {
            if (target != null && target.TryGetComponent(out Image image))
            {
                image.raycastTarget = raycastTarget;
            }
        }

        private void SetSpotlightActive(bool active)
        {
            SetActive(_topBlocker, active);
            SetActive(_bottomBlocker, active);
            SetActive(_leftBlocker, active);
            SetActive(_rightBlocker, active);
            SetActive(_highlightFrame, active && _highlightFrame != null);
            SetActive(_handImage, active && _showHand && CanShowHand());
        }

        private void UpdateSpotlightLayout()
        {
            RectTransform layoutRoot = ResolveLayoutRoot();
            if (_spotlightTarget == null || layoutRoot == null || !EnsureSpotlightReferences())
            {
                SetSpotlightActive(false);
                ApplyMessagePlacement(_currentStep, ResolveLayoutRootRect(), null, null);
                return;
            }

            if (!TryGetTargetLocalBounds(
                _spotlightTarget,
                layoutRoot,
                out Vector2 targetMin,
                out Vector2 targetMax))
            {
                SetSpotlightActive(false);
                ApplyMessagePlacement(_currentStep, layoutRoot.rect, null, null);
                return;
            }

            Rect rootRect = layoutRoot.rect;
            float padding = Mathf.Max(0f, _spotlightPadding);
            targetMin -= new Vector2(padding, padding);
            targetMax += new Vector2(padding, padding);

            float minWidth = Mathf.Max(1f, _spotlightMinSize.x);
            float minHeight = Mathf.Max(1f, _spotlightMinSize.y);
            ExpandToMinSize(ref targetMin, ref targetMax, minWidth, minHeight);

            targetMin.x = Mathf.Clamp(targetMin.x, rootRect.xMin, rootRect.xMax);
            targetMin.y = Mathf.Clamp(targetMin.y, rootRect.yMin, rootRect.yMax);
            targetMax.x = Mathf.Clamp(targetMax.x, rootRect.xMin, rootRect.xMax);
            targetMax.y = Mathf.Clamp(targetMax.y, rootRect.yMin, rootRect.yMax);

            if (targetMax.x <= targetMin.x || targetMax.y <= targetMin.y)
            {
                SetSpotlightActive(false);
                ApplyMessagePlacement(_currentStep, rootRect, null, null);
                return;
            }

            SetRect(
                _topBlocker,
                rootRect,
                rootRect.xMin,
                targetMax.y,
                rootRect.width,
                rootRect.yMax - targetMax.y);
            SetRect(
                _bottomBlocker,
                rootRect,
                rootRect.xMin,
                rootRect.yMin,
                rootRect.width,
                targetMin.y - rootRect.yMin);
            SetRect(
                _leftBlocker,
                rootRect,
                rootRect.xMin,
                targetMin.y,
                targetMin.x - rootRect.xMin,
                targetMax.y - targetMin.y);
            SetRect(
                _rightBlocker,
                rootRect,
                targetMax.x,
                targetMin.y,
                rootRect.xMax - targetMax.x,
                targetMax.y - targetMin.y);
            SetRect(
                _highlightFrame,
                rootRect,
                targetMin.x,
                targetMin.y,
                targetMax.x - targetMin.x,
                targetMax.y - targetMin.y);
            UpdateHandLayout(rootRect, targetMin, targetMax);
            ApplyMessagePlacement(_currentStep, rootRect, targetMin, targetMax);
        }

        private RectTransform ResolveLayoutRoot()
        {
            if (_spotlightRoot == null)
            {
                _spotlightRoot = transform as RectTransform;
            }

            return _spotlightRoot;
        }

        private Rect ResolveLayoutRootRect()
        {
            RectTransform root = ResolveLayoutRoot();
            return root != null
                ? root.rect
                : new Rect(0f, 0f, Screen.width, Screen.height);
        }

        private void ApplyMessagePlacement(
            RunBattleTutorialStep step,
            Rect rootRect,
            Vector2? targetMin,
            Vector2? targetMax)
        {
            if (_panelRoot == null || step == null)
            {
                return;
            }

            switch (step.MessagePlacement)
            {
                case RunTutorialMessagePlacement.Top:
                    ApplyPresetMessagePlacement(
                        new Vector2(0.06f, 0.82f),
                        new Vector2(0.94f, 0.965f),
                        step.MessageOffset,
                        step.MessageSize);
                    break;
                case RunTutorialMessagePlacement.Center:
                    ApplyPresetMessagePlacement(
                        new Vector2(0.08f, 0.38f),
                        new Vector2(0.92f, 0.62f),
                        step.MessageOffset,
                        step.MessageSize);
                    break;
                case RunTutorialMessagePlacement.Left:
                    ApplyPresetMessagePlacement(
                        new Vector2(0.04f, 0.28f),
                        new Vector2(0.46f, 0.72f),
                        step.MessageOffset,
                        step.MessageSize);
                    break;
                case RunTutorialMessagePlacement.Right:
                    ApplyPresetMessagePlacement(
                        new Vector2(0.54f, 0.28f),
                        new Vector2(0.96f, 0.72f),
                        step.MessageOffset,
                        step.MessageSize);
                    break;
                case RunTutorialMessagePlacement.AboveTarget:
                case RunTutorialMessagePlacement.BelowTarget:
                case RunTutorialMessagePlacement.LeftOfTarget:
                case RunTutorialMessagePlacement.RightOfTarget:
                    ApplyTargetMessagePlacement(step, rootRect, targetMin, targetMax);
                    break;
                case RunTutorialMessagePlacement.Bottom:
                default:
                    ApplyPresetMessagePlacement(
                        new Vector2(0.06f, 0.035f),
                        new Vector2(0.94f, 0.18f),
                        step.MessageOffset,
                        step.MessageSize);
                    break;
            }
        }

        private void ApplyPresetMessagePlacement(
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offset,
            Vector2 requestedSize)
        {
            _panelRoot.anchorMin = anchorMin;
            _panelRoot.anchorMax = anchorMax;
            _panelRoot.pivot = new Vector2(0.5f, 0.5f);
            _panelRoot.anchoredPosition = offset;
            _panelRoot.sizeDelta = HasCustomSize(requestedSize)
                ? requestedSize
                : Vector2.zero;
            _panelRoot.localScale = Vector3.one;
        }

        private void ApplyTargetMessagePlacement(
            RunBattleTutorialStep step,
            Rect rootRect,
            Vector2? targetMin,
            Vector2? targetMax)
        {
            if (!targetMin.HasValue || !targetMax.HasValue)
            {
                ApplyPresetMessagePlacement(
                    new Vector2(0.06f, 0.035f),
                    new Vector2(0.94f, 0.18f),
                    step.MessageOffset,
                    step.MessageSize);
                return;
            }

            Vector2 min = targetMin.Value;
            Vector2 max = targetMax.Value;
            Vector2 center = (min + max) * 0.5f;
            Vector2 panelSize = ResolveMessageSize(step.MessageSize, rootRect);
            Vector2 desiredCenter = step.MessagePlacement switch
            {
                RunTutorialMessagePlacement.AboveTarget =>
                    new Vector2(center.x, max.y + TargetPanelGap + panelSize.y * 0.5f),
                RunTutorialMessagePlacement.BelowTarget =>
                    new Vector2(center.x, min.y - TargetPanelGap - panelSize.y * 0.5f),
                RunTutorialMessagePlacement.LeftOfTarget =>
                    new Vector2(min.x - TargetPanelGap - panelSize.x * 0.5f, center.y),
                RunTutorialMessagePlacement.RightOfTarget =>
                    new Vector2(max.x + TargetPanelGap + panelSize.x * 0.5f, center.y),
                _ => center,
            };

            desiredCenter += step.MessageOffset;
            desiredCenter = ClampPanelCenter(desiredCenter, rootRect, panelSize);

            _panelRoot.anchorMin = Vector2.zero;
            _panelRoot.anchorMax = Vector2.zero;
            _panelRoot.pivot = new Vector2(0.5f, 0.5f);
            _panelRoot.sizeDelta = panelSize;
            _panelRoot.anchoredPosition =
                new Vector2(desiredCenter.x - rootRect.xMin, desiredCenter.y - rootRect.yMin);
            _panelRoot.localScale = Vector3.one;
        }

        private static Vector2 ResolveMessageSize(Vector2 requestedSize, Rect rootRect)
        {
            if (HasCustomSize(requestedSize))
            {
                return requestedSize;
            }

            float width = Mathf.Clamp(rootRect.width * 0.78f, 280f, 760f);
            float height = Mathf.Clamp(rootRect.height * 0.14f, 130f, 210f);
            return new Vector2(width, height);
        }

        private static Vector2 ClampPanelCenter(
            Vector2 center,
            Rect rootRect,
            Vector2 panelSize)
        {
            float minX = rootRect.xMin + panelSize.x * 0.5f + PanelScreenPadding;
            float maxX = rootRect.xMax - panelSize.x * 0.5f - PanelScreenPadding;
            float minY = rootRect.yMin + panelSize.y * 0.5f + PanelScreenPadding;
            float maxY = rootRect.yMax - panelSize.y * 0.5f - PanelScreenPadding;

            center.x = minX <= maxX
                ? Mathf.Clamp(center.x, minX, maxX)
                : (rootRect.xMin + rootRect.xMax) * 0.5f;
            center.y = minY <= maxY
                ? Mathf.Clamp(center.y, minY, maxY)
                : (rootRect.yMin + rootRect.yMax) * 0.5f;
            return center;
        }

        private static bool HasCustomSize(Vector2 size)
        {
            return size.x > 0.1f && size.y > 0.1f;
        }

        private bool EnsureHandImage()
        {
            if (_handImage != null)
            {
                _handImage.raycastTarget = false;
                return true;
            }

            RectTransform layoutRoot = ResolveLayoutRoot();
            if (layoutRoot == null)
            {
                return false;
            }

            var generated = new GameObject(
                "Tutorial Hand Pointer",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            generated.transform.SetParent(layoutRoot, false);
            _handImage = generated.GetComponent<Image>();
            _handImage.raycastTarget = false;
            generated.SetActive(false);
            return true;
        }

        private bool CanShowHand()
        {
            return _handImage != null &&
                _handSprites != null &&
                _handSprites.Length > 0 &&
                _handSprites[0] != null;
        }

        private void UpdateHandLayout(
            Rect rootRect,
            Vector2 targetMin,
            Vector2 targetMax)
        {
            if (!_showHand || !EnsureHandImage() || !CanShowHand())
            {
                SetActive(_handImage, false);
                return;
            }

            RectTransform handTransform = _handImage.rectTransform;
            handTransform.gameObject.SetActive(true);
            handTransform.anchorMin = Vector2.zero;
            handTransform.anchorMax = Vector2.zero;
            handTransform.pivot = new Vector2(0.5f, 0.5f);
            handTransform.sizeDelta = _handSize;
            handTransform.localScale = Vector3.one;

            float pulse = Mathf.Sin(Time.unscaledTime * Mathf.Max(0.01f, _handPulseSpeed)) *
                Mathf.Max(0f, _handPulseDistance);
            Vector2 center = (targetMin + targetMax) * 0.5f;
            Vector2 localPosition = center + _handOffset + new Vector2(0f, pulse);
            handTransform.anchoredPosition =
                new Vector2(localPosition.x - rootRect.xMin, localPosition.y - rootRect.yMin);

            int spriteIndex = 0;
            if (_handSprites.Length > 1 && _handFrameInterval > 0f)
            {
                spriteIndex = Mathf.FloorToInt(Time.unscaledTime / _handFrameInterval) %
                    _handSprites.Length;
            }

            _handImage.sprite = _handSprites[spriteIndex] != null
                ? _handSprites[spriteIndex]
                : _handSprites[0];
            _handImage.SetNativeSize();
            handTransform.sizeDelta = _handSize;
        }

        private bool TryGetTargetLocalBounds(
            RectTransform target,
            RectTransform root,
            out Vector2 targetMin,
            out Vector2 targetMax)
        {
            targetMin = Vector2.zero;
            targetMax = Vector2.zero;

            if (target == null || root == null)
            {
                return false;
            }

            target.GetWorldCorners(_targetWorldCorners);
            Camera canvasCamera = ResolveCanvasCamera();

            for (int index = 0; index < _targetWorldCorners.Length; index++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                    canvasCamera,
                    _targetWorldCorners[index]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    root,
                    screenPoint,
                    canvasCamera,
                    out _targetLocalCorners[index]))
                {
                    return false;
                }
            }

            targetMin = _targetLocalCorners[0];
            targetMax = _targetLocalCorners[0];
            for (int index = 1; index < _targetLocalCorners.Length; index++)
            {
                targetMin = Vector2.Min(targetMin, _targetLocalCorners[index]);
                targetMax = Vector2.Max(targetMax, _targetLocalCorners[index]);
            }

            return true;
        }

        private Camera ResolveCanvasCamera()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            Canvas rootCanvas = _canvas != null ? _canvas.rootCanvas : null;
            return rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;
        }

        private static void ExpandToMinSize(
            ref Vector2 targetMin,
            ref Vector2 targetMax,
            float minWidth,
            float minHeight)
        {
            Vector2 center = (targetMin + targetMax) * 0.5f;
            float width = Mathf.Max(targetMax.x - targetMin.x, minWidth);
            float height = Mathf.Max(targetMax.y - targetMin.y, minHeight);
            var halfSize = new Vector2(width * 0.5f, height * 0.5f);
            targetMin = center - halfSize;
            targetMax = center + halfSize;
        }

        private static void SetRect(
            RectTransform target,
            Rect rootRect,
            float xMin,
            float yMin,
            float width,
            float height)
        {
            if (target == null)
            {
                return;
            }

            bool hasArea = width > 0.1f && height > 0.1f;
            target.gameObject.SetActive(hasArea);
            if (!hasArea)
            {
                return;
            }

            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.zero;
            target.pivot = Vector2.zero;
            target.anchoredPosition = new Vector2(xMin - rootRect.xMin, yMin - rootRect.yMin);
            target.sizeDelta = new Vector2(width, height);
            target.localScale = Vector3.one;
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _panelRoot != null, "Tutorial Overlay Panel");
            AppendMissing(builder, HasBodyText(), "Tutorial Overlay Body");
            return builder.Length > 0 ? builder.ToString() : "none";
        }
    }
}
