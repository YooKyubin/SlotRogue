using System;

using SlotRogue.Slot.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotRogue.Slot.Views
{
    public sealed class SlotDevSceneBootstrap : MonoBehaviour
    {
        private const float RootWidth = 760f;
        private const float RootHeight = 1120f;
        private const float CellWidth = 128f;
        private const float CellHeight = 88f;
        private const int FontSizeTitle = 42;
        private const int FontSizeBody = 26;
        private const int FontSizeCell = 22;
        private const string InputSystemUiInputModuleTypeName =
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        private void Awake()
        {
            CreateEventSystemIfNeeded();
            CreateSlotUi();
        }

        private static void CreateEventSystemIfNeeded()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            Type inputModuleType = Type.GetType(InputSystemUiInputModuleTypeName);

            if (inputModuleType != null)
            {
                eventSystemObject.AddComponent(inputModuleType);
                return;
            }

            SlotDebugLog.Info("InputSystemUIInputModule type was not found. Slot UI input may need an EventSystem module.");
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void CreateSlotUi()
        {
            Font font = GetDefaultFont();

            GameObject canvasObject = new GameObject("Slot Dev Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform root = CreateRectTransform("SlotMachine", canvasObject.transform);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(RootWidth, RootHeight);
            root.anchoredPosition = Vector2.zero;

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            Text titleText = CreateText("Title", root, font, "Dev Slot", FontSizeTitle, TextAnchor.MiddleCenter);
            AddLayoutElement(titleText.gameObject, 70f);

            RectTransform grid = CreateRectTransform("Slot Grid", root);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(CellWidth, CellHeight);
            gridLayout.spacing = new Vector2(12f, 12f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            AddLayoutElement(grid.gameObject, 300f);

            for (int index = 0; index < 15; index++)
            {
                CreateCell(grid, font);
            }

            Button spinButton = CreateButton("Spin Button", root, font, "SPIN");
            AddLayoutElement(spinButton.gameObject, 82f);

            RectTransform resultRoot = CreateRectTransform("Slot Result", root);
            var resultLayout = resultRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            resultLayout.spacing = 8f;
            resultLayout.childControlHeight = false;
            resultLayout.childControlWidth = true;
            resultLayout.childForceExpandHeight = false;
            resultLayout.childForceExpandWidth = true;
            AddLayoutElement(resultRoot.gameObject, 460f);

            Text symbolsText = CreateText("Symbols Text", resultRoot, font, "Symbols: -", FontSizeBody, TextAnchor.MiddleLeft);
            Text patternText = CreateText("Pattern Text", resultRoot, font, "Pattern: -", FontSizeBody, TextAnchor.MiddleLeft);
            Text damageText = CreateText("Damage Text", resultRoot, font, "Damage: 0", FontSizeBody, TextAnchor.MiddleLeft);
            Text attackText = CreateText("Attack Count Text", resultRoot, font, "Attack Count: 0", FontSizeBody, TextAnchor.MiddleLeft);
            Text healText = CreateText("Heal Text", resultRoot, font, "Heal: 0", FontSizeBody, TextAnchor.MiddleLeft);
            Text criticalText = CreateText("Critical Text", resultRoot, font, "Critical: False", FontSizeBody, TextAnchor.MiddleLeft);
            Text requestText = CreateText(
                "Combat Request Text",
                resultRoot,
                font,
                "Combat Request: attack=0, defense=0",
                FontSizeBody,
                TextAnchor.MiddleLeft);

            var resultView = resultRoot.gameObject.AddComponent<SlotResultView>();
            resultView.Bind(symbolsText, patternText, damageText, attackText, healText, criticalText, requestText);

            root.gameObject.AddComponent<SlotMachineView>();
        }

        private static void CreateCell(RectTransform parent, Font font)
        {
            RectTransform cell = CreateRectTransform("Slot Cell", parent);
            var image = cell.gameObject.AddComponent<Image>();
            image.color = new Color32(36, 40, 52, 255);

            Text text = CreateText("Symbol Text", cell, font, "-", FontSizeCell, TextAnchor.MiddleCenter);
            RectTransform textTransform = text.rectTransform;
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = Vector2.zero;
            textTransform.offsetMax = Vector2.zero;

            var cellView = cell.gameObject.AddComponent<SlotCellView>();
            cellView.Bind(text);
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label)
        {
            RectTransform buttonTransform = CreateRectTransform(name, parent);
            var image = buttonTransform.gameObject.AddComponent<Image>();
            image.color = new Color32(222, 176, 77, 255);

            var button = buttonTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText("Button Text", buttonTransform, font, label, 30, TextAnchor.MiddleCenter);
            RectTransform textTransform = text.rectTransform;
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = Vector2.zero;
            textTransform.offsetMax = Vector2.zero;

            return button;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            string value,
            int fontSize,
            TextAnchor alignment)
        {
            RectTransform transform = CreateRectTransform(name, parent);
            var text = transform.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            AddLayoutElement(text.gameObject, 44f);

            return text;
        }

        private static RectTransform CreateRectTransform(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;

            return rectTransform;
        }

        private static void AddLayoutElement(GameObject gameObject, float preferredHeight)
        {
            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
