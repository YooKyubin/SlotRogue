using System;
using SlotRogue.UI.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    internal sealed class RunInventoryCell
    {
        private readonly GameObject _root;
        private readonly Button _button;
        private readonly Image _icon;
        private readonly TMP_Text _nameText;
        private readonly TMP_Text _descriptionText;
        private readonly Text _legacyText;
        private readonly GameObject _highlight;

        private RunInventoryCell(
            GameObject root,
            Button button,
            Image icon,
            TMP_Text nameText,
            TMP_Text descriptionText,
            Text legacyText,
            GameObject highlight)
        {
            _root = root;
            _button = button;
            _icon = icon;
            _nameText = nameText;
            _descriptionText = descriptionText;
            _legacyText = legacyText;
            _highlight = highlight;
        }

        internal static RunInventoryCell Resolve(GameObject root)
        {
            RunInventoryCellView view = root.GetComponent<RunInventoryCellView>();
            if (view == null)
            {
                Debug.LogError(
                    "[RunInventoryCell] RunInventoryCellView must be attached to the cell prefab root.",
                    root);
                return new RunInventoryCell(root, null, null, null, null, null, null);
            }

            view.ValidateRequiredReferences();

            return new RunInventoryCell(
                root,
                view.Button,
                view.Icon,
                view.NameText,
                view.DescriptionText,
                view.LegacyText,
                view.Highlight);
        }

        internal void SetActive(bool active)
        {
            if (_root != null)
            {
                _root.SetActive(active);
            }
        }

        internal void SetIcon(Sprite sprite)
        {
            if (_icon == null)
            {
                return;
            }

            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
        }

        internal void SetText(string value)
        {
            SetRelicText(value, string.Empty);
        }

        internal void SetRelicText(string name, string description)
        {
            string safeName = name ?? string.Empty;
            string safeDescription = description ?? string.Empty;

            if (_nameText != null && _descriptionText != null)
            {
                _nameText.text = safeName;
                _descriptionText.text = safeDescription;
                return;
            }

            string combined = string.IsNullOrEmpty(safeDescription)
                ? safeName
                : $"{safeName}\n{safeDescription}";

            if (_nameText != null)
            {
                _nameText.text = combined;
            }
            else if (_descriptionText != null)
            {
                _descriptionText.text = combined;
            }
            else if (_legacyText != null)
            {
                _legacyText.text = combined;
            }
        }

        internal void ApplyRelicDescriptionSpriteAsset(
            SlotSymbolTmpSpriteAssetBinder spriteAssetBinder)
        {
            if (spriteAssetBinder == null)
            {
                return;
            }

            if (_descriptionText != null)
            {
                spriteAssetBinder.ApplyTo(_descriptionText);
                return;
            }

            spriteAssetBinder.ApplyTo(_nameText);
        }

        internal void SetHighlight(bool on)
        {
            if (_highlight != null)
            {
                _highlight.SetActive(on);
            }
        }

        internal void SetClick(Action action)
        {
            if (_button == null)
            {
                return;
            }

            _button.onClick.RemoveAllListeners();
            if (action != null)
            {
                _button.onClick.AddListener(() => action());
            }
        }
    }
}
