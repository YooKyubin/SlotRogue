using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunTutorialOverlayView : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.04f, 0.035f, 0.055f, 0.94f);
        private static readonly Color BorderColor = new(0.95f, 0.68f, 0.24f, 1f);

        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private Text _bodyText;

        public static RunTutorialOverlayView CreateRuntime(Transform canvasTransform)
        {
            var hostObject = new GameObject("RunTutorialOverlayView", typeof(RectTransform));
            var rectTransform = (RectTransform)hostObject.transform;
            rectTransform.SetParent(canvasTransform, false);
            Stretch(rectTransform);

            RunTutorialOverlayView view = hostObject.AddComponent<RunTutorialOverlayView>();
            view.EnsureRuntimeLayout();
            return view;
        }

        private void Awake()
        {
            EnsureRuntimeLayout();
        }

        public void EnsureRuntimeLayout()
        {
            ResolveSceneReferences();

            if (_panelRoot == null)
            {
                BuildRuntimeLayout();
            }
        }

        public void ShowMessage(string message)
        {
            EnsureRuntimeLayout();

            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(true);
            }

            if (_bodyText != null)
            {
                _bodyText.text = message ?? string.Empty;
            }

            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(false);
            }
        }

        private void ResolveSceneReferences()
        {
            _panelRoot ??= FindDeepChild(transform, "Tutorial Overlay Panel") as RectTransform;
            _bodyText ??= FindChildComponent<Text>("Tutorial Overlay Body");
        }

        private void BuildRuntimeLayout()
        {
            Font font = Resources.Load<Font>("Galmuri11-Bold");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            _panelRoot = CreateRect("Tutorial Overlay Panel", transform);
            SetAnchors(_panelRoot, new Vector2(0.06f, 0.035f), new Vector2(0.94f, 0.18f));

            Image border = _panelRoot.gameObject.AddComponent<Image>();
            border.color = BorderColor;
            border.raycastTarget = false;

            RectTransform body = CreateRect("Tutorial Overlay Body Panel", _panelRoot);
            SetAnchors(body, new Vector2(0.01f, 0.08f), new Vector2(0.99f, 0.92f));
            Image bodyImage = body.gameObject.AddComponent<Image>();
            bodyImage.color = PanelColor;
            bodyImage.raycastTarget = false;

            _bodyText = CreateText(
                "Tutorial Overlay Body",
                body,
                font,
                25,
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.92f),
                TextAnchor.MiddleLeft);
            _bodyText.raycastTarget = false;

            _panelRoot.gameObject.SetActive(false);
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

        private static Text CreateText(
            string objectName,
            Transform parent,
            Font font,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAnchor alignment)
        {
            RectTransform rectTransform = CreateRect(objectName, parent);
            SetAnchors(rectTransform, anchorMin, anchorMax);

            Text text = rectTransform.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
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
    }
}
