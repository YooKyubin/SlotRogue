using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class GameFlowOptionView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private RewardSlotRarity _rarity;

        private bool _missingReferenceErrorLogged;

        public Button Button => _button;

        private void Awake()
        {
            ValidateRequiredReferences();
        }

        public void Bind(
            Button button,
            TMP_Text titleText,
            TMP_Text descriptionText)
        {
            _button = button;
            _titleText = titleText;
            _descriptionText = descriptionText;
        }

        public void SetText(string title, string description)
        {
            ValidateRequiredReferences();

            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = description;
            }

        }

        public void SetRarity(RewardRarity rarity)
        {
            if (_rarity != null)
            {
                _rarity.Apply(rarity);
            }
        }

        public void SetDescriptionSpriteAsset(TMP_SpriteAsset spriteAsset)
        {
            ValidateRequiredReferences();

            if (_descriptionText == null || _descriptionText.spriteAsset == spriteAsset)
            {
                return;
            }

            _descriptionText.spriteAsset = spriteAsset;
            _descriptionText.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        }

        private void ValidateRequiredReferences()
        {
            if (_missingReferenceErrorLogged ||
                (_button != null &&
                 _titleText != null &&
                 _descriptionText != null &&
                 _rarity != null))
            {
                return;
            }

            _missingReferenceErrorLogged = true;
            Debug.LogError(
                "[GameFlowOptionView] Option view references must be wired in the inspector. " +
                $"Missing: {BuildMissingReferenceSummary()}");
        }

        private string BuildMissingReferenceSummary()
        {
            var missing = new System.Collections.Generic.List<string>();
            if (_button == null) missing.Add("Button");
            if (_titleText == null) missing.Add("Title Text");
            if (_descriptionText == null) missing.Add("Description Text");
            if (_rarity == null) missing.Add("Rarity");
            return missing.Count > 0 ? string.Join(", ", missing) : "None";
        }
    }
}
