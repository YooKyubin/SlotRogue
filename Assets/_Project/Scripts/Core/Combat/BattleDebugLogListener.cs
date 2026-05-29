using UnityEngine;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleDebugLogListener : MonoBehaviour
    {
        [SerializeField] private BattleBootstrap _bootstrap;

        private void OnEnable()
        {
            if (_bootstrap == null || _bootstrap.Presenter == null)
            {
                return;
            }

            BattlePresenter presenter = _bootstrap.Presenter;
            presenter.TurnReceived += OnTurnReceived;
            presenter.TurnCompleted += OnTurnCompleted;
        }

        private void OnDisable()
        {
            if (_bootstrap == null || _bootstrap.Presenter == null)
            {
                return;
            }

            BattlePresenter presenter = _bootstrap.Presenter;
            presenter.TurnReceived -= OnTurnReceived;
            presenter.TurnCompleted -= OnTurnCompleted;
        }

        private static void OnTurnReceived(TurnResult turnResult)
        {
            if (turnResult == null)
            {
                return;
            }

            foreach (CombatEvent combatEvent in turnResult.Events)
            {
                switch (combatEvent.Kind)
                {
                    case CombatEventKind.PlayerDamageToMonster:
                        Debug.Log($"[Battle] Player dealt {combatEvent.Amount} to monster");
                        break;
                    case CombatEventKind.MonsterDamageToPlayer:
                        Debug.Log($"[Battle] Monster dealt {combatEvent.Amount} to player");
                        break;
                    case CombatEventKind.MonsterActionExecuted:
                        MonsterAction action = combatEvent.MonsterAction;
                        Debug.Log($"[Battle] Monster action: {action.Kind} (atk={action.RawAttack}, def={action.DefendValue})");
                        break;
                    case CombatEventKind.PlayerHealed:
                        Debug.Log($"[Battle] Player healed {combatEvent.Amount}");
                        break;
                    case CombatEventKind.MonsterHealed:
                        Debug.Log($"[Battle] Monster healed {combatEvent.Amount}");
                        break;
                    case CombatEventKind.BattleEnded:
                        Debug.Log($"[Battle] Ended: {combatEvent.EndReason}");
                        break;
                }
            }
        }

        private static void OnTurnCompleted(BattleStateSnapshot snapshot)
        {
            Debug.Log($"[Battle] Turn end -> Player {snapshot.PlayerHp}/{snapshot.PlayerMaxHp}, Monster {snapshot.MonsterHp}/{snapshot.MonsterMaxHp}, PatternIndex {snapshot.PatternIndex}");
        }
    }
}
