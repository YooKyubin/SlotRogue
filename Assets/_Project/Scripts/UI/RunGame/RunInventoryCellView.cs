using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunInventoryCellView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [FormerlySerializedAs("_tmpText")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [FormerlySerializedAs("_text")]
        [SerializeField] private Text _legacyText;
        [SerializeField] private GameObject _highlight;

        private bool _missingReferenceErrorLogged;

        internal Button Button => _button;

        internal Image Icon => _icon;

        internal TMP_Text NameText => _nameText;

        internal TMP_Text DescriptionText => _descriptionText;

        internal Text LegacyText => _legacyText;

        internal GameObject Highlight => _highlight;

        internal void ValidateRequiredReferences()
        {
            bool hasText = _nameText != null ||
                _descriptionText != null ||
                _legacyText != null;

            if (_missingReferenceErrorLogged ||
                (_icon != null && hasText))
            {
                return;
            }

            _missingReferenceErrorLogged = true;
            Debug.LogError(
                "[RunInventoryCellView] Inventory cell references must be wired in the inspector. " +
                $"Missing: {BuildMissingReferenceSummary()}",
                this);
        }

        private string BuildMissingReferenceSummary()
        {
            var missing = new System.Collections.Generic.List<string>();
            if (_icon == null) missing.Add("Icon");
            if (_nameText == null &&
                _descriptionText == null &&
                _legacyText == null)
            {
                missing.Add("Name Text or Description Text");
            }

            return missing.Count > 0 ? string.Join(", ", missing) : "None";
        }
    }
}
