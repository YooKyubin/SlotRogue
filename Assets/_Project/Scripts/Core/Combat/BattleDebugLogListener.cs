using UnityEngine;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleDebugLogListener : MonoBehaviour
    {
        [SerializeField] private BattleBootstrap _bootstrap;

        private void OnEnable()
        {
            if (_bootstrap?.Presenter == null)
            {
                return;
            }

            BattlePresenter presenter = _bootstrap.Presenter;
            presenter.PlayerHpChanged += OnPlayerHpChanged;
            presenter.MonsterHpChanged += OnMonsterHpChanged;
            presenter.MonsterActionExecuted += OnMonsterActionExecuted;
            presenter.BattleEnded += OnBattleEnded;
        }

        private void OnDisable()
        {
            if (_bootstrap?.Presenter == null)
            {
                return;
            }

            BattlePresenter presenter = _bootstrap.Presenter;
            presenter.PlayerHpChanged -= OnPlayerHpChanged;
            presenter.MonsterHpChanged -= OnMonsterHpChanged;
            presenter.MonsterActionExecuted -= OnMonsterActionExecuted;
            presenter.BattleEnded -= OnBattleEnded;
        }

        private static void OnPlayerHpChanged(int currentHp, int maxHp)
        {
            Debug.Log($"[Battle] Player HP: {currentHp}/{maxHp}");
        }

        private static void OnMonsterHpChanged(int currentHp, int maxHp)
        {
            Debug.Log($"[Battle] Monster HP: {currentHp}/{maxHp}");
        }

        private static void OnMonsterActionExecuted(MonsterAction action)
        {
            Debug.Log($"[Battle] Monster action: {action.Kind} (atk={action.RawAttack}, def={action.DefendValue})");
        }

        private static void OnBattleEnded(BattleEndReason reason)
        {
            Debug.Log($"[Battle] Ended: {reason}");
        }
    }
}
