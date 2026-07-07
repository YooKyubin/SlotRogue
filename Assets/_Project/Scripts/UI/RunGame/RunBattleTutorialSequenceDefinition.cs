using System;
using UnityEngine;

namespace SlotRogue.UI.RunGame
{
    [CreateAssetMenu(
        fileName = "RunBattleTutorialSequence",
        menuName = "SlotRogue/Tutorial/Run Battle Tutorial Sequence")]
    public sealed class RunBattleTutorialSequenceDefinition : ScriptableObject
    {
        [SerializeField] private RunBattleTutorialStep[] _steps = Array.Empty<RunBattleTutorialStep>();
        [SerializeField, TextArea(2, 4)] private string _completionMessage;

        public string CompletionMessage => _completionMessage ?? string.Empty;

        public int StepCount => _steps?.Length ?? 0;

        public bool TryGetStep(
            int index,
            out RunBattleTutorialStep step)
        {
            if (_steps == null || index < 0 || index >= _steps.Length || _steps[index] == null)
            {
                step = null;
                return false;
            }

            step = _steps[index];
            return true;
        }

        public void ConfigureForRuntime(
            RunBattleTutorialStep[] steps,
            string completionMessage)
        {
            _steps = steps ?? Array.Empty<RunBattleTutorialStep>();
            _completionMessage = completionMessage ?? string.Empty;
        }
    }
}
