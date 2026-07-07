using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunBottomPanelView : ViewComponentBase
    {
        [SerializeField] private Button _descriptionButton;
        [SerializeField] private Button _relicInventoryButton;

        private Button _subscribedDescriptionButton;
        private Button _subscribedRelicInventoryButton;
        private bool _presenterBound;
        private bool _reportedMissingReferences;

        public event Action DescriptionOpenRequested;

        public event Action RelicInventoryOpenRequested;

        private void Awake()
        {
            EnsureRuntimeLayout();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        public void Bind(IRunGameFlow presenter)
        {
            if (presenter == null || _presenterBound)
            {
                return;
            }

            EnsureRuntimeLayout();
            DescriptionOpenRequested += presenter.HandleDescriptionOpenRequested;
            RelicInventoryOpenRequested += presenter.HandleInventoryOpenRequested;
            _presenterBound = true;
        }

        public bool EnsureRuntimeLayout()
        {
            if (!HasRequiredReferences())
            {
                if (!_reportedMissingReferences)
                {
                    Debug.LogError(
                        "[RunBottomPanelView] Bottom panel buttons must be wired in the inspector. " +
                        $"Missing: {BuildMissingReferenceSummary()}");
                    _reportedMissingReferences = true;
                }

                return false;
            }

            PrepareButton(_descriptionButton);
            PrepareButton(_relicInventoryButton);
            SubscribeButtons();
            return true;
        }

        private bool HasRequiredReferences()
        {
            return _descriptionButton != null &&
                _relicInventoryButton != null;
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _descriptionButton != null, "Description Button");
            AppendMissing(builder, _relicInventoryButton != null, "Relic Inventory Button");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private void SubscribeButtons()
        {
            if (_subscribedDescriptionButton == _descriptionButton &&
                _subscribedRelicInventoryButton == _relicInventoryButton)
            {
                return;
            }

            UnsubscribeButtons();
            _descriptionButton?.onClick.AddListener(HandleDescriptionClicked);
            _relicInventoryButton?.onClick.AddListener(HandleRelicInventoryClicked);
            _subscribedDescriptionButton = _descriptionButton;
            _subscribedRelicInventoryButton = _relicInventoryButton;
        }

        private void UnsubscribeButtons()
        {
            if (_subscribedDescriptionButton != null)
            {
                _subscribedDescriptionButton.onClick.RemoveListener(HandleDescriptionClicked);
            }

            if (_subscribedRelicInventoryButton != null)
            {
                _subscribedRelicInventoryButton.onClick.RemoveListener(HandleRelicInventoryClicked);
            }

            _subscribedDescriptionButton = null;
            _subscribedRelicInventoryButton = null;
        }

        private void HandleDescriptionClicked()
        {
            DescriptionOpenRequested?.Invoke();
        }

        private void HandleRelicInventoryClicked()
        {
            RelicInventoryOpenRequested?.Invoke();
        }

        private static void PrepareButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                button.targetGraphic = image;
            }
        }
    }
}
