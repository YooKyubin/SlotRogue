using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotRogue.UI.Common
{
    /// <summary>
    /// 코드로 임시 UI(팝업 등)를 빠르게 조립하기 위한 런타임 UI 빌더 헬퍼입니다.
    /// 정식 UI는 에디터에서 저작하는 것이 원칙이며, 이 헬퍼는 아직 씬에 저작되지 않은
    /// 화면을 임시로 띄우기 위한 용도입니다.
    /// </summary>
    internal static class RuntimeUiKit
    {
        private static Font _font;

        internal static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _font;
            }
        }

        /// <summary>ScreenSpaceOverlay 캔버스를 만들고 EventSystem 존재를 보장합니다.</summary>
        internal static Canvas CreateOverlayCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(
                name,
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        internal static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        /// <summary>부모를 가득 채우는 반투명 배경(클릭 차단용).</summary>
        internal static Image CreateBackdrop(Transform parent, Color color)
        {
            var go = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        /// <summary>중앙 정렬 패널.</summary>
        internal static RectTransform CreatePanel(Transform parent, Color color, Vector2 size)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            go.GetComponent<Image>().color = color;
            return rt;
        }

        internal static Text CreateText(
            Transform parent,
            string text,
            int fontSize,
            TextAnchor anchor,
            Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            var t = go.GetComponent<Text>();
            t.font = DefaultFont;
            t.text = text ?? string.Empty;
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        internal static Button CreateButton(
            Transform parent,
            string label,
            Color background,
            Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            go.GetComponent<Image>().color = background;

            var button = go.GetComponent<Button>();
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            Text label_t = CreateText(go.transform, label, 30, TextAnchor.MiddleCenter, Color.white);
            RectTransform lrt = label_t.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            return button;
        }

        /// <summary>RectTransform을 부모 기준 절대 위치/크기로 배치합니다(중앙 앵커).</summary>
        internal static void Place(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }
    }
}
