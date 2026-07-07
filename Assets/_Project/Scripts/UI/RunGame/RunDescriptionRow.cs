using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    internal sealed class RunDescriptionRow
    {
        private readonly Image _icon;
        private readonly TMP_Text _titleText;
        private readonly TMP_Text _attackPowerText;
        private readonly TMP_Text _probabilityText;
        private readonly TMP_Text _multiplierText;

        private RunDescriptionRow(
            GameObject root,
            Image icon,
            TMP_Text titleText,
            TMP_Text attackPowerText,
            TMP_Text probabilityText,
            TMP_Text multiplierText)
        {
            Root = root;
            _icon = icon;
            _titleText = titleText;
            _attackPowerText = attackPowerText;
            _probabilityText = probabilityText;
            _multiplierText = multiplierText;
        }

        internal GameObject Root { get; }

        internal static RunDescriptionRow Resolve(GameObject root)
        {
            RunDescriptionRowView view = root.GetComponent<RunDescriptionRowView>();
            if (view == null)
            {
                Debug.LogError(
                    "[RunDescriptionRow] RunDescriptionRowView must be attached to the row prefab root.",
                    root);
                return new RunDescriptionRow(root, null, null, null, null, null);
            }

            view.ValidateRequiredReferences();
            return new RunDescriptionRow(
                root,
                view.Icon,
                view.TitleText,
                view.AttackPowerText,
                view.ProbabilityText,
                view.MultiplierText);
        }

        internal static RunDescriptionRow FromView(RunDescriptionRowView view)
        {
            if (view == null)
            {
                return new RunDescriptionRow(null, null, null, null, null, null);
            }

            view.ValidateRequiredReferences();
            return new RunDescriptionRow(
                view.gameObject,
                view.Icon,
                view.TitleText,
                view.AttackPowerText,
                view.ProbabilityText,
                view.MultiplierText);
        }

        internal void SetActive(bool active)
        {
            if (Root != null)
            {
                Root.SetActive(active);
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

        internal void SetTitle(string value) => SetTextValue(_titleText, value);

        internal void SetAttackPower(string value) =>
            SetTextValue(_attackPowerText, value);

        internal void SetProbability(string value) =>
            SetTextValue(_probabilityText, value);

        internal void SetMultiplier(string value) =>
            SetTextValue(_multiplierText, value);

        private static void SetTextValue(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
