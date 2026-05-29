using System;
using System.Collections;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatTimelineController : MonoBehaviour
    {
        [SerializeField] private BattleBootstrap _bootstrap;
        [SerializeField] private float _eventStepDelaySeconds = 0.2f;
        [SerializeField] private float _battleEndDelaySeconds = 0.4f;

        public event Action<CombatEvent> TimelineEventPlayed;

        public event Action<BattleStateSnapshot> FinalStateApplied;

        private Coroutine _playRoutine;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            if (_bootstrap != null && _bootstrap.Presenter != null)
            {
                _bootstrap.Presenter.TurnReceived -= OnTurnReceived;
            }
        }

        private void TrySubscribe()
        {
            if (_bootstrap == null || _bootstrap.Presenter == null)
            {
                return;
            }

            _bootstrap.Presenter.TurnReceived -= OnTurnReceived;
            _bootstrap.Presenter.TurnReceived += OnTurnReceived;
        }

        private void OnTurnReceived(TurnResult result)
        {
            if (result == null)
            {
                return;
            }

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _playRoutine = StartCoroutine(PlayTurnRoutine(result));
        }

        private IEnumerator PlayTurnRoutine(TurnResult result)
        {
            for (int index = 0; index < result.Events.Count; index++)
            {
                CombatEvent combatEvent = result.Events[index];
                TimelineEventPlayed?.Invoke(combatEvent);
                yield return CreateEventDelay(combatEvent);
            }

            FinalStateApplied?.Invoke(result.FinalState);
            _playRoutine = null;
        }

        private YieldInstruction CreateEventDelay(CombatEvent combatEvent)
        {
            float delay = combatEvent.Kind == CombatEventKind.BattleEnded
                ? _battleEndDelaySeconds
                : _eventStepDelaySeconds;

            if (delay <= 0f)
            {
                return null;
            }

            return new WaitForSeconds(delay);
        }
    }
}
