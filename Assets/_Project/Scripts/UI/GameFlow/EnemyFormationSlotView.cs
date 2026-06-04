using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyFormationSlotView : MonoBehaviour
    {
        private static readonly Color EnemySlotColor = new Color(0.11f, 0.14f, 0.2f, 0.96f);
        private static readonly Color SelectedEnemySlotColor = new Color(0.45f, 0.26f, 0.12f, 0.96f);

        [SerializeField] private RectTransform _root;
        [SerializeField] private Button _button;
        [SerializeField] private GameFlowImageSlot _portrait;
        [SerializeField] private Text _hudText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _statusBackground;
        [SerializeField] private RectTransform _damageAnchor;
        [SerializeField] private Text _placeholderText;

        public RectTransform Root => _root != null ? _root : transform as RectTransform;

        public RectTransform DamageAnchor => _damageAnchor;

        public void Bind(
            RectTransform root,
            Button button,
            GameFlowImageSlot portrait,
            Text hudText,
            Image hpFill,
            Image statusBackground,
            RectTransform damageAnchor,
            Text placeholderText)
        {
            _root = root;
            _button = button;
            _portrait = portrait;
            _hudText = hudText;
            _hpFill = hpFill;
            _statusBackground = statusBackground;
            _damageAnchor = damageAnchor;
            _placeholderText = placeholderText;
        }

        public void SetPortrait(Sprite sprite)
        {
            if (sprite != null)
            {
                if (_portrait != null)
                {
                    _portrait.SetSprite(sprite);
                }

                if (_placeholderText != null)
                {
                    _placeholderText.gameObject.SetActive(false);
                }

                return;
            }

            if (_portrait != null)
            {
                _portrait.SetSprite(null);
            }

            if (_placeholderText != null)
            {
                _placeholderText.gameObject.SetActive(true);
            }
        }

        public void SetActive(bool active)
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(active);
            }
        }

        public void SetHud(string value)
        {
            if (_hudText != null)
            {
                _hudText.text = value;
            }
        }

        public void SetHpFill(int current, int max)
        {
            if (_hpFill == null)
            {
                return;
            }

            RectTransform parent = _hpFill.rectTransform.parent as RectTransform;
            float maxWidth = parent != null ? Mathf.Max(1f, parent.sizeDelta.x - 8f) : 1f;
            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            _hpFill.rectTransform.sizeDelta = new Vector2(maxWidth * ratio, _hpFill.rectTransform.sizeDelta.y);
        }

        public void SetSelected(bool selected)
        {
            if (_statusBackground != null)
            {
                _statusBackground.color = selected ? SelectedEnemySlotColor : EnemySlotColor;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        public void SetClickHandler(UnityAction action)
        {
            if (_button == null)
            {
                return;
            }

            _button.onClick.RemoveAllListeners();
            if (action != null)
            {
                _button.onClick.AddListener(action);
            }
        }
    }
}
