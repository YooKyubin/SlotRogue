// DEV ONLY: This component is for editor/manual combat checks and is not used by the actual in-game flow.
using SlotRogue.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleStatusEffectDebugButton : MonoBehaviour
    {
        [SerializeField] private RunBattleCompositionRoot _compositionRoot;
        [SerializeField] private Button _button;

        [Header("Status Effect")]
        [SerializeField] private bool _useRecommendedDefaults = true;
        [SerializeField] private StatusEffectKind _statusEffectKind = StatusEffectKind.Burn;
        [SerializeField] private int _duration = 3;
        [SerializeField] private int _magnitude = 2;
        [SerializeField] private StatusStackMode _stackMode = StatusStackMode.Refresh;

        [Header("Optional Damage")]
        [SerializeField] private bool _includeDamage;
        [SerializeField] private int _damage;
        [SerializeField] private int _attackCount = 1;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(ApplyStatusTurn);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(ApplyStatusTurn);
            }
        }

        public void ApplyStatusTurn()
        {
            RunBattleCompositionRoot root = ResolveCompositionRoot();
            if (root == null)
            {
                Debug.LogError("[RunBattleStatusEffectDebugButton] RunBattleCompositionRoot is missing.");
                return;
            }
            Debug.Log("ApplyStatusTurn");

            root.DevApplyStatusTurn(
                _statusEffectKind,
                _duration,
                _magnitude,
                _stackMode,
                _includeDamage,
                _damage,
                _attackCount);
        }

        private void Reset()
        {
            _compositionRoot = GetComponentInParent<RunBattleCompositionRoot>();
            ApplyRecommendedDefaults();
        }

        private void OnValidate()
        {
            if (_useRecommendedDefaults)
            {
                ApplyRecommendedDefaults();
            }

            _duration = Mathf.Max(0, _duration);
            _magnitude = Mathf.Max(0, _magnitude);
            _damage = Mathf.Max(0, _damage);
            _attackCount = Mathf.Max(1, _attackCount);
        }

        private RunBattleCompositionRoot ResolveCompositionRoot()
        {
            if (_compositionRoot != null)
            {
                return _compositionRoot;
            }

            _compositionRoot = GetComponentInParent<RunBattleCompositionRoot>();
            return _compositionRoot;
        }

        private void ApplyRecommendedDefaults()
        {
            switch (_statusEffectKind)
            {
                case StatusEffectKind.Burn:
                    _duration = 3;
                    _magnitude = 2;
                    _stackMode = StatusStackMode.Refresh;
                    break;
                case StatusEffectKind.Freeze:
                    _duration = 1;
                    _magnitude = 0;
                    _stackMode = StatusStackMode.Refresh;
                    break;
                case StatusEffectKind.Poison:
                    _duration = 0;
                    _magnitude = 1;
                    _stackMode = StatusStackMode.Stack;
                    break;
            }
        }
    }
}
