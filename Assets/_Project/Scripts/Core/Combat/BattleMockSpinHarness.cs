using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleMockSpinHarness : MonoBehaviour
    {
        [SerializeField] private BattleBootstrap _bootstrap;
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _passButton;
        [SerializeField] private Button _defendButton;
        [SerializeField] private int _mockAttack = 5;
        [SerializeField] private int _mockDefense = 4;

        private void Awake()
        {
            if (_attackButton != null)
            {
                _attackButton.onClick.AddListener(OnAttackClicked);
            }

            if (_passButton != null)
            {
                _passButton.onClick.AddListener(OnPassClicked);
            }

            if (_defendButton != null)
            {
                _defendButton.onClick.AddListener(OnDefendClicked);
            }
        }

        private void OnDestroy()
        {
            if (_attackButton != null)
            {
                _attackButton.onClick.RemoveListener(OnAttackClicked);
            }

            if (_passButton != null)
            {
                _passButton.onClick.RemoveListener(OnPassClicked);
            }

            if (_defendButton != null)
            {
                _defendButton.onClick.RemoveListener(OnDefendClicked);
            }
        }

        private void OnAttackClicked()
        {
            ApplySpin(new CombatSpinOutcome(_mockAttack, 0));
        }

        private void OnPassClicked()
        {
            ApplySpin(new CombatSpinOutcome(0, 0));
        }

        private void OnDefendClicked()
        {
            ApplySpin(new CombatSpinOutcome(0, _mockDefense));
        }

        private void ApplySpin(CombatSpinOutcome outcome)
        {
            if (_bootstrap == null)
            {
                Debug.LogWarning("[BattleMockSpinHarness] Bootstrap is null.", this);
                return;
            }

            ISpinCombatConsumer consumer = _bootstrap.CombatConsumer;
            if (consumer == null)
            {
                Debug.LogWarning("[BattleMockSpinHarness] Battle system is not ready.", this);
                return;
            }

            consumer.OnSpinResolved(outcome);
        }
    }
}
