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

        public RectTransform DamageAnchor
        {
            get
            {
                EnsureReferences();
                return _damageAnchor;
            }
        }

        private void Awake()
        {
            EnsureReferences();
        }

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

        public void EnsureReferences()
        {
            if (_root == null)
            {
                _root = transform as RectTransform;
            }

            if (_button == null)
            {
                _button = GetComponent<Button>();
                if (_button == null)
                {
                    _button = GetComponentInChildren<Button>(true);
                }
            }

            if (_portrait == null)
            {
                _portrait = FindImageSlot("Portrait", "portrait");
            }

            if (_hudText == null)
            {
                _hudText = FindText("HUD Text");
            }

            if (_hpFill == null)
            {
                _hpFill = FindImage("HP Bar Fill");
            }

            if (_statusBackground == null)
            {
                _statusBackground = FindImage("Status Panel");
            }

            if (_damageAnchor == null)
            {
                _damageAnchor = FindRectTransform("Damage Anchor");
                if (_damageAnchor == null)
                {
                    _damageAnchor = FindRectTransform("DamageAnchor");
                }
            }

            if (_placeholderText == null)
            {
                _placeholderText = FindText("Portrait Placeholder Text");
            }
        }

        public void SetPortrait(Sprite sprite)
        {
            EnsureReferences();

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
            EnsureReferences();

            if (_hudText != null)
            {
                _hudText.text = value;
            }
        }

        public void SetHpFill(int current, int max)
        {
            EnsureReferences();

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
            EnsureReferences();

            if (_statusBackground != null)
            {
                _statusBackground.color = selected ? SelectedEnemySlotColor : EnemySlotColor;
            }
        }

        public void SetInteractable(bool interactable)
        {
            EnsureReferences();

            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        public void SetClickHandler(UnityAction action)
        {
            EnsureReferences();

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

        private GameFlowImageSlot FindImageSlot(string objectName, string slotIdFragment)
        {
            Transform byName = FindDeepChild(transform, objectName);
            if (byName != null && byName.TryGetComponent(out GameFlowImageSlot imageSlot))
            {
                return imageSlot;
            }

            GameFlowImageSlot[] slots = GetComponentsInChildren<GameFlowImageSlot>(true);
            for (int index = 0; index < slots.Length; index++)
            {
                GameFlowImageSlot slot = slots[index];
                if (slot != null &&
                    !string.IsNullOrEmpty(slot.SlotId) &&
                    slot.SlotId.ToLowerInvariant().Contains(slotIdFragment))
                {
                    return slot;
                }
            }

            return null;
        }

        private Text FindText(string objectName)
        {
            Transform child = FindDeepChild(transform, objectName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private Image FindImage(string objectName)
        {
            Transform child = FindDeepChild(transform, objectName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private RectTransform FindRectTransform(string objectName)
        {
            Transform child = FindDeepChild(transform, objectName);
            return child as RectTransform;
        }

        private static Transform FindDeepChild(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDeepChild(root.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
