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
        [SerializeField] private TMP_Text _bodyTmpText;

        [Header("Panel Animation")]
        [SerializeField] private Image _panelImage;
        [SerializeField] private Sprite[] _panelAnimationFrames = Array.Empty<Sprite>();
        [SerializeField] private float _panelFrameInterval = 0.35f;

        [Header("Spotlight")]
        [SerializeField] private RectTransform _spotlightRoot;
        [SerializeField] private RectTransform _topBlocker;
        [SerializeField] private RectTransform _bottomBlocker;
        [SerializeField] private RectTransform _leftBlocker;
        [SerializeField] private RectTransform _rightBlocker;
        [SerializeField] private float _spotlightPadding = 20f;
        [SerializeField] private Vector2 _spotlightMinSize = new(56f, 56f);
        [SerializeField] private Color _generatedBlockerColor = new(0f, 0f, 0f, 0.72f);

        [Header("Step Advance")]
        [SerializeField] private RectTransform _inputBlocker;

        private readonly Vector3[] _targetWorldCorners = new Vector3[4];
        private readonly Vector2[] _targetLocalCorners = new Vector2[4];
        private bool _reportedMissingReferences;
        private RectTransform _spotlightTarget;
        private Canvas _canvas;
        private Button _inputBlockerButton;
        private RunBattleTutorialStep _currentStep;
        private Action _pendingAdvance;
        private bool _advanceListenerWired;
        private int _lastPanelFrameIndex = -1;
        private readonly SlotSymbolTmpSpriteAssetBinder _symbolSpriteAssetBinder = new();

        private void Awake()
        {
            EnsureRuntimeLayout();
            Hide();
        }

        private void LateUpdate()
        {
            RectTransform messagePanelRoot = ResolveMessagePanelRoot();
            if (messagePanelRoot == null ||
                !messagePanelRoot.gameObject.activeInHierarchy ||
                _currentStep == null)
            {
                UpdatePanelAnimation();
                return;
            }

            UpdatePanelAnimation();

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
            if (_advanceListenerWired && _inputBlockerButton != null)
            {
                _inputBlockerButton.onClick.RemoveListener(HandleAdvanceClicked);
            }

            _symbolSpriteAssetBinder.Dispose();
        }

        public bool EnsureRuntimeLayout()
        {
            ResolvePanelImage();

            if (ResolveMessagePanelRoot() != null && HasBodyText())
            {
                _symbolSpriteAssetBinder.ApplyTo(_bodyTmpText);
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
            SetBodyRaycast(false);

            if (target == null || !EnsureSpotlightReferences())
            {
                _spotlightTarget = null;
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
            RectTransform messagePanelRoot = ResolveMessagePanelRoot();
            if (messagePanelRoot != null)
            {
                messagePanelRoot.gameObject.SetActive(false);
            }

            ClearSpotlight();
            ClearAdvanceControls();
            // Keep the root active so a delayed Awake cannot re-hide the overlay
            // during ShowStep activation.
            _lastPanelFrameIndex = -1;
        }

        private void ClearAdvanceControls()
        {
            _pendingAdvance = null;
            SetActive(_inputBlocker, false);
        }

        private bool EnsureAdvanceControls()
        {
            if (!EnsureInputBlocker() || !EnsureInputBlockerButton())
            {
                return false;
            }

            if (!_advanceListenerWired)
            {
                _inputBlockerButton.onClick.AddListener(HandleAdvanceClicked);
                _advanceListenerWired = true;
            }

            SetActive(_inputBlocker, true);
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
                typeof(Image),
                typeof(Button));
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

        private bool EnsureInputBlockerButton()
        {
            if (_inputBlocker == null)
            {
                return false;
            }

            if (!_inputBlocker.TryGetComponent(out Image blockerImage))
            {
                blockerImage = _inputBlocker.gameObject.AddComponent<Image>();
            }

            blockerImage.color = new Color(0f, 0f, 0f, 0f);
            blockerImage.raycastTarget = true;

            if (_inputBlockerButton == null)
            {
                if (!_inputBlocker.TryGetComponent(out Button button))
                {
                    button = _inputBlocker.gameObject.AddComponent<Button>();
                }

                _inputBlockerButton = button;
            }

            _inputBlockerButton.transition = Selectable.Transition.None;
            _inputBlockerButton.targetGraphic = blockerImage;
            _inputBlockerButton.interactable = true;
            return true;
        }

        private void HandleAdvanceClicked()
        {
            Action advance = _pendingAdvance;
            _pendingAdvance = null;
            SetActive(_inputBlocker, false);

            advance?.Invoke();
        }

        private bool HasBodyText()
        {
            return _bodyTmpText != null;
        }

        private void ResolvePanelImage()
        {
            RectTransform messagePanelRoot = ResolveMessagePanelRoot();
            if (_panelImage == null && messagePanelRoot != null)
            {
                _panelImage = messagePanelRoot.GetComponent<Image>();
            }

            if (_panelImage != null)
            {
                _panelImage.raycastTarget = false;
            }
        }

        private bool CanAnimatePanel()
        {
            return _panelImage != null &&
                _panelAnimationFrames != null &&
                _panelAnimationFrames.Length > 0 &&
                _panelAnimationFrames[0] != null;
        }

        private void ApplyFirstPanelFrame()
        {
            if (!CanAnimatePanel())
            {
                return;
            }

            _panelImage.sprite = _panelAnimationFrames[0];
            _lastPanelFrameIndex = 0;
        }

        private void UpdatePanelAnimation()
        {
            if (!CanAnimatePanel())
            {
                return;
            }

            float interval = Mathf.Max(0.01f, _panelFrameInterval);
            int frameIndex = Mathf.FloorToInt(Time.unscaledTime / interval) %
                _panelAnimationFrames.Length;
            Sprite frame = _panelAnimationFrames[frameIndex] ??
                _panelAnimationFrames[0];
            if (frameIndex == _lastPanelFrameIndex && _panelImage.sprite == frame)
            {
                return;
            }

            _panelImage.sprite = frame;
            _lastPanelFrameIndex = frameIndex;
        }

        private void SetMessage(string message)
        {
            SetHierarchyActive(transform);

            RectTransform messagePanelRoot = ResolveMessagePanelRoot();
            if (messagePanelRoot != null)
            {
                messagePanelRoot.gameObject.SetActive(true);
            }

            ApplyFirstPanelFrame();

            if (_bodyTmpText != null)
            {
                _symbolSpriteAssetBinder.ApplyTo(_bodyTmpText);
                _bodyTmpText.text = message ?? string.Empty;
            }
        }

        private void SetBodyRaycast(bool raycastTarget)
        {
            if (_bodyTmpText != null)
            {
                _bodyTmpText.raycastTarget = raycastTarget;
            }
        }

        private static void SetHierarchyActive(Transform target)
        {
            if (target == null)
            {
                return;
            }

            SetHierarchyActive(target.parent);
            if (!target.gameObject.activeSelf)
            {
                target.gameObject.SetActive(true);
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
            ResolveMessagePanelRoot()?.SetAsLastSibling();
        }

        private void ClearSpotlight()
        {
            _spotlightTarget = null;
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

            SetImageRaycast(_topBlocker, true);
            SetImageRaycast(_bottomBlocker, true);
            SetImageRaycast(_leftBlocker, true);
            SetImageRaycast(_rightBlocker, true);

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
            if (ResolveMessagePanelRoot() == null || step == null)
            {
                return;
            }

            switch (step.MessagePlacement)
            {
                case RunTutorialMessagePlacement.Top:
                    ApplyPresetMessagePlacement(
                        new Vector2(0.06f, 0.82f),
                        new Vector2(0.94f, 0.965f),
                        step.MessageOffset);
                    break;
                case RunTutorialMessagePlacement.Center:
                    ApplyPresetMessagePlacement(
                        new Vector2(0.08f, 0.38f),
                        new Vector2(0.92f, 0.62f),
                        step.MessageOffset);
                    break;
                case RunTutorialMessagePlacement.Left:
                    ApplyPresetMessagePlacement(
                        new Vector2(0.04f, 0.28f),
                        new Vector2(0.46f, 0.72f),
                        step.MessageOffset);
                    break;
                case RunTutorialMessagePlacement.Right:
                    ApplyPresetMessagePlacement(
                        new Vector2(0.54f, 0.28f),
                        new Vector2(0.96f, 0.72f),
                        step.MessageOffset);
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
                        step.MessageOffset);
                    break;
            }
        }

        private void ApplyPresetMessagePlacement(
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offset)
        {
            RectTransform messagePanelRoot = ResolveMessagePanelRoot();
            if (messagePanelRoot == null)
            {
                return;
            }

            Vector2 anchorCenter = (anchorMin + anchorMax) * 0.5f;
            messagePanelRoot.anchorMin = anchorCenter;
            messagePanelRoot.anchorMax = anchorCenter;
            messagePanelRoot.pivot = new Vector2(0.5f, 0.5f);
            messagePanelRoot.anchoredPosition = offset;
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
                    step.MessageOffset);
                return;
            }

            RectTransform messagePanelRoot = ResolveMessagePanelRoot();
            if (messagePanelRoot == null)
            {
                return;
            }

            Vector2 min = targetMin.Value;
            Vector2 max = targetMax.Value;
            Vector2 center = (min + max) * 0.5f;
            Vector2 panelSize = ResolveMessagePanelVisualSize(messagePanelRoot);
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

            messagePanelRoot.anchorMin = Vector2.zero;
            messagePanelRoot.anchorMax = Vector2.zero;
            messagePanelRoot.pivot = new Vector2(0.5f, 0.5f);
            messagePanelRoot.anchoredPosition =
                new Vector2(desiredCenter.x - rootRect.xMin, desiredCenter.y - rootRect.yMin);
        }

        private RectTransform ResolveMessagePanelRoot()
        {
            if (_panelRoot == null)
            {
                return null;
            }

            if (_panelRoot.transform != transform)
            {
                return _panelRoot;
            }

            // Legacy scene wiring uses _panelRoot as the overlay activation root.
            // In that case, move the actual message panel child instead of the whole overlay.
            if (_bodyTmpText != null &&
                _bodyTmpText.rectTransform != null &&
                _bodyTmpText.rectTransform.parent is RectTransform bodyParent &&
                bodyParent != _panelRoot)
            {
                return bodyParent;
            }

            return _panelRoot;
        }

        private static Vector2 ResolveMessagePanelVisualSize(RectTransform messagePanelRoot)
        {
            Rect rect = messagePanelRoot.rect;
            Vector3 scale = messagePanelRoot.localScale;
            return new Vector2(
                Mathf.Max(1f, Mathf.Abs(rect.width * scale.x)),
                Mathf.Max(1f, Mathf.Abs(rect.height * scale.y)));
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
            AppendMissing(builder, ResolveMessagePanelRoot() != null, "Tutorial Overlay Panel");
            AppendMissing(builder, HasBodyText(), "Tutorial Overlay Body");
            return builder.Length > 0 ? builder.ToString() : "none";
        }
    }
}
