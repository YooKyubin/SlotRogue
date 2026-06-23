using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunTutorialOverlayView : MonoBehaviour
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
            ResolveSceneReferences();

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

        private void ResolveSceneReferences()
        {
            _panelRoot ??= FindDeepChild(transform, "Tutorial Overlay Panel") as RectTransform;
            _bodyText ??= FindChildComponent<Text>("Tutorial Overlay Body");
            _bodyTmpText ??= FindChildComponent<TMP_Text>("Tutorial Overlay Body");
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

        private static void AppendMissing(
            System.Text.StringBuilder builder,
            bool hasReference,
            string label)
        {
            if (hasReference)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(label);
        }
    }
}
