using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyFormationSlotView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Root")]
        [SerializeField] private Transform _root;
        [SerializeField] private Transform _shakeGroup;

        [Header("Visual")]
        [SerializeField] private EnemyCombatVisualHostView _combatVisualHost;

        [Header("Selection")]
        [SerializeField] private GameObject _selectionIndicator;

        [Header("HUD")]
        [SerializeField] private EnemyHealthHudView _healthHudView;

        [Header("Combat Anchors")]
        [SerializeField] private RectTransform _damageAnchor;
        [SerializeField] private Collider2D _clickCollider;

        [Header("Status Effects")]
        [SerializeField] private EnemyStatusEffectListView _statusEffectListView;

        [Header("Intent")]
        [SerializeField] private EnemyIntentListView _intentListView;

        private UnityAction _clickHandler;
        private bool _interactable = true;
        private Sprite _portraitSprite;
        private bool _deathPresented;

        public Transform Root => _root != null ? _root : transform;

        public Transform ShakeGroup => _shakeGroup;

        public Canvas HudRoot => _healthHudView != null ? _healthHudView.HudRoot : null;

        public RectTransform DamageAnchor => _damageAnchor;

        public ShieldGaugeView ShieldGauge => _healthHudView != null ? _healthHudView.ShieldGauge : null;

        public Sprite PortraitSprite => _portraitSprite;

        public void SetCombatVisualPrefab(GameObject combatVisualPrefab)
        {
            if (_combatVisualHost.TrySetCombatVisualPrefab(combatVisualPrefab))
            {
                ResetDeathPresentation();
            }
        }

        public void SetPortraitSprite(Sprite portraitSprite)
        {
            _portraitSprite = portraitSprite;
        }

        public void ClearCombatVisual()
        {
            _combatVisualHost.ClearCombatVisual();
        }

        public UniTask PlayCombatVisualActionUntilEffectPointAsync(
            string actionName,
            CancellationToken cancellationToken)
        {
            return _combatVisualHost.PlayActionUntilEffectPointAsync(actionName, cancellationToken);
        }

        public UniTask WaitCombatVisualActionCompletedAsync(CancellationToken cancellationToken)
        {
            return _combatVisualHost.WaitForActionCompletedAsync(cancellationToken);
        }

        public UniTask ShowCombatDamageVFXAsync(CombatDamageVFXRequest request, CancellationToken cancellationToken)
        {
            return _combatVisualHost.ShowDamageVFXAsync(request, cancellationToken);
        }

        public async UniTask PlayDeathAsync(CancellationToken cancellationToken)
        {
            if (_deathPresented)
            {
                return;
            }

            _deathPresented = true;
            SetInteractable(false);
            if (_intentListView != null)
            {
                _intentListView.gameObject.SetActive(false);
            }

            await _combatVisualHost.PlayDeathAsync(cancellationToken);

            HideDeathPresentation();
        }

        /// <summary>
        /// 슬롯의 표시 수명주기를 전환한다.
        /// 새 전투 비주얼의 최초 Idle 재생은 Root가 활성화된 뒤에만 시작한다.
        /// </summary>
        public void SetPresentationActive(bool active)
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(active);
            }

            if (!active)
            {
                SetSelected(false);
            }

            if (active && _deathPresented)
            {
                HideDeathPresentation();
            }

            _combatVisualHost.SetPresentationActive(active);
        }

        public void SetHealthHud(string hpText, int currentHp, int maxHp, int shield)
        {
            if (_deathPresented)
            {
                return;
            }

            _healthHudView?.Render(hpText, currentHp, maxHp, shield);
        }

        public UniTask WaitHpFillAsync(CancellationToken cancellationToken)
        {
            return _healthHudView != null
                ? _healthHudView.WaitHpFillAsync(cancellationToken)
                : UniTask.CompletedTask;
        }

        public void SetStatusEffects(IReadOnlyList<StatusEffectViewData> statuses)
        {
            if (_deathPresented)
            {
                HideDeathPresentation();
                return;
            }

            _statusEffectListView.SetStatusEffects(statuses);
        }

        public UniTask AddStatusAsync(StatusEffectViewData status, CancellationToken cancellationToken)
        {
            return _statusEffectListView.AddStatusAsync(status, cancellationToken);
        }

        public UniTask UpdateStatusValueAsync(StatusEffectViewData status, CancellationToken cancellationToken)
        {
            return _statusEffectListView.UpdateStatusValueAsync(status, cancellationToken);
        }

        public UniTask PlayStatusActivationAsync(
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return _statusEffectListView.PlayStatusActivationAsync(kind, cancellationToken);
        }

        public UniTask RemoveStatusAsync(StatusEffectKind kind, CancellationToken cancellationToken)
        {
            return _statusEffectListView.RemoveStatusAsync(kind, cancellationToken);
        }

        public void SetUpcomingActions(IReadOnlyList<EnemyUpcomingActionViewData> upcomingActions)
        {
            if (_deathPresented)
            {
                HideDeathPresentation();
                return;
            }

            _intentListView.Render(upcomingActions);
        }

        public void SetSelected(bool selected)
        {
            if (_selectionIndicator != null)
            {
                _selectionIndicator.SetActive(selected && !_deathPresented);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_deathPresented)
            {
                _interactable = false;
                if (_clickCollider != null)
                {
                    _clickCollider.enabled = false;
                }

                return;
            }

            _interactable = interactable;
            if (_clickCollider != null)
            {
                _clickCollider.enabled = interactable;
            }
        }

        public void SetClickHandler(UnityAction action)
        {
            _clickHandler = action;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable)
            {
                return;
            }

            if (_clickHandler == null || _clickCollider == null || !_clickCollider.enabled)
            {
                return;
            }

            _clickHandler.Invoke();
        }

        private void ResetDeathPresentation()
        {
            _deathPresented = false;
            _healthHudView?.PrepareForReuse();
            _combatVisualHost.ResetPresentation();
            _healthHudView?.SetVisible(true);

            if (_statusEffectListView != null)
            {
                _statusEffectListView.gameObject.SetActive(true);
            }
        }

        private void HideDeathPresentation()
        {
            SetSelected(false);

            _combatVisualHost.SetVisible(false);
            _healthHudView?.SetVisible(false);

            if (_statusEffectListView != null)
            {
                _statusEffectListView.gameObject.SetActive(false);
            }

            if (_intentListView != null)
            {
                _intentListView.gameObject.SetActive(false);
            }
        }

    }
}
