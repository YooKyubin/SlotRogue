using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunTutorialOverlayView : ViewComponentBase, ITutorialOverlay
    {
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private Text _bodyText;
        [SerializeField] private TMP_Text _bodyTmpText;

        private bool _reportedMissingReferences;

        private void Awake()
        {
            EnsureRuntimeLayout();
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

            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(false);
            }
        }

        private bool HasBodyText()
        {
            return _bodyText != null || _bodyTmpText != null;
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
