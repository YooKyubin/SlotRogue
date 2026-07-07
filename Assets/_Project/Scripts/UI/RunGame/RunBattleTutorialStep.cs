using System;
using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.RunGame
{
    [Serializable]
    public sealed class RunBattleTutorialStep
    {
        [SerializeField] private RunBattleTutorialTargetKey _targetKey = RunBattleTutorialTargetKey.None;
        [SerializeField] private RunTutorialMessagePlacement _messagePlacement =
            RunTutorialMessagePlacement.Bottom;
        [SerializeField] private Vector2 _messageOffset;
        [SerializeField] private Vector2 _messageSize = Vector2.zero;
        [SerializeField, TextArea(2, 5)] private string _message;
        [SerializeField] private bool _showHand = true;

        public RunBattleTutorialStep(
            RunBattleTutorialTargetKey targetKey,
            string message,
            bool showHand = true,
            RunTutorialMessagePlacement messagePlacement = RunTutorialMessagePlacement.Bottom,
            Vector2 messageOffset = default,
            Vector2 messageSize = default)
        {
            _targetKey = targetKey;
            _messagePlacement = messagePlacement;
            _messageOffset = messageOffset;
            _messageSize = messageSize;
            _message = message ?? string.Empty;
            _showHand = showHand;
        }

        public RunBattleTutorialTargetKey TargetKey => _targetKey;

        public RunTutorialMessagePlacement MessagePlacement => _messagePlacement;

        public Vector2 MessageOffset => _messageOffset;

        public Vector2 MessageSize => _messageSize;

        public string Message => _message ?? string.Empty;

        public bool ShowHand => _showHand;
    }
}
