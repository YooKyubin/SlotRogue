using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class MonsterView : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color EnemySlotColor = new(0.11f, 0.14f, 0.2f, 0.96f);
        private static readonly Color SelectedEnemySlotColor = new(0.45f, 0.26f, 0.12f, 0.96f);

        [SerializeField] private Transform _root;
        [SerializeField] private Transform _shakeGroup;
        [SerializeField] private SpriteRenderer _portrait;
        [SerializeField] private Canvas _hudRoot;
        [SerializeField] private Text _hudText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _statusBackground;
        [SerializeField] private RectTransform _damageAnchor;
        [SerializeField] private Text _placeholderText;
        [SerializeField] private Collider2D _clickCollider;

        private Action _clickHandler;
        private bool _interactable = true;
        private float _hpFillMaxWidth;
        private bool _hpFillLayoutInitialized;

        public Transform Root => _root != null ? _root : transform;

        public Transform ShakeGroup => _shakeGroup;

        public Canvas HudRoot => _hudRoot;

        public RectTransform DamageAnchor => _damageAnchor;

        public void Bind(
            Transform root,
            Transform shakeGroup,
            SpriteRenderer portrait,
            Canvas hudRoot,
            Text hudText,
            Image hpFill,
            Image statusBackground,
            RectTransform damageAnchor,
            Text placeholderText,
            Collider2D clickCollider)
        {
            _root = root;
            _shakeGroup = shakeGroup;
            _portrait = portrait;
            _hudRoot = hudRoot;
            _hudText = hudText;
            _hpFill = hpFill;
            _hpFillLayoutInitialized = false;
            _statusBackground = statusBackground;
            _damageAnchor = damageAnchor;
            _placeholderText = placeholderText;
            _clickCollider = clickCollider;
        }

        public void Render(RunBattleEnemySlotState state)
        {
            SetActive(state.Active);
            if (!state.Active)
            {
                return;
            }

            SetHud(state.Selected ? $"> {state.HudText}" : state.HudText);
            SetHpFill(state.Hp, state.MaxHp);
            SetSelected(state.Selected);
            SetInteractable(state.Interactable);
        }

        public void SetPortrait(Sprite sprite)
        {
            if (_portrait != null)
            {
                _portrait.sprite = sprite;
            }

            if (_placeholderText != null)
            {
                _placeholderText.gameObject.SetActive(sprite == null);
            }
        }

        public void SetClickHandler(Action action)
        {
            _clickHandler = action;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable || _clickHandler == null || _clickCollider == null || !_clickCollider.enabled)
            {
                return;
            }

            _clickHandler.Invoke();
        }

        private void SetActive(bool active)
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(active);
            }
        }

        private void SetHud(string value)
        {
            if (_hudText != null)
            {
                _hudText.text = value;
            }
        }

        private void SetHpFill(int current, int max)
        {
            if (_hpFill == null)
            {
                return;
            }

            RectTransform fillRect = _hpFill.rectTransform;
            RectTransform parent = fillRect.parent as RectTransform;
            if (!_hpFillLayoutInitialized)
            {
                float currentWidth = Mathf.Max(0f, fillRect.rect.width);
                _hpFillMaxWidth = currentWidth > 0f
                    ? currentWidth
                    : Mathf.Max(0f, fillRect.sizeDelta.x);

                float leftInset = 0f;
                if (parent != null)
                {
                    float parentWidth = parent.rect.width;
                    float pivotPosition = (parentWidth * fillRect.anchorMin.x) + fillRect.anchoredPosition.x;
                    leftInset = pivotPosition - (_hpFillMaxWidth * fillRect.pivot.x);
                }

                fillRect.anchorMin = new Vector2(0f, 0.5f);
                fillRect.anchorMax = new Vector2(0f, 0.5f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = new Vector2(leftInset, fillRect.anchoredPosition.y);
                _hpFillLayoutInitialized = true;
            }

            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            _hpFill.type = Image.Type.Simple;
            _hpFill.preserveAspect = false;
            fillRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                _hpFillMaxWidth * ratio);
        }

        private void SetSelected(bool selected)
        {
            if (_statusBackground != null)
            {
                _statusBackground.color = selected ? SelectedEnemySlotColor : EnemySlotColor;
            }
        }

        private void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (_clickCollider != null)
            {
                _clickCollider.enabled = interactable;
            }
        }
    }
}
