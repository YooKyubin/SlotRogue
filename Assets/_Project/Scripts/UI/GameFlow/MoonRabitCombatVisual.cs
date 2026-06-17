using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    [RequireComponent(typeof(Animator))]
    public sealed class MoonRabitCombatVisual : MonoBehaviour, IEnemyCombatVisual
    {
        private const int BaseLayer = 0;
        private const float RestartFromBeginning = 0f;

        private static readonly int IdleStateHash = Animator.StringToHash("Idle");
        private static readonly int AttackStateHash = Animator.StringToHash("Attack");

        [SerializeField] private Animator _animator;

        public void PlayIdle()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            _animator.Play(IdleStateHash, BaseLayer, RestartFromBeginning);
        }

        public void PlayAttack()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            _animator.Play(AttackStateHash, BaseLayer, RestartFromBeginning);
        }

        private bool ValidateAnimator()
        {
            if (_animator != null)
            {
                return true;
            }

            Debug.LogError(
                "[MoonRabitCombatVisual] Animator reference is missing. " +
                "Assign the Animator used by the MoonRabit combat visual prefab.",
                this);
            return false;
        }
    }
}
