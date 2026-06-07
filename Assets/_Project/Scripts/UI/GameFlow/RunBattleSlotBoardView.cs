using System;
using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleSlotBoardView : MonoBehaviour
    {
        private static readonly Color PatternHitColor = new(1f, 0.82f, 0.23f, 1f);

        [SerializeField] private Text[] _slotCells;

        private Color[] _slotCellDefaultColors;
        private bool _hasCachedDefaults;

        public void Bind(Text[] slotCells)
        {
            _slotCells = slotCells;
        }

        public void Render(RunBattleScreenState state)
        {
            RenderSlotCells(state.SlotCells);
            RenderOutcome(state.SlotOutcome);
        }

        private void RenderSlotCells(string[] values)
        {
            if (_slotCells == null || values == null)
            {
                return;
            }

            int count = Mathf.Min(_slotCells.Length, values.Length);
            for (int index = 0; index < count; index++)
            {
                if (_slotCells[index] != null)
                {
                    _slotCells[index].text = values[index];
                }
            }
        }

        private void RenderOutcome(RunBattleSlotOutcomeState outcome)
        {
            CacheDefaultsIfNeeded();
            ResetSlotCellColors();

            if (!outcome.HasPattern ||
                outcome.Row < 0 ||
                outcome.Row >= SlotSpinResult.Rows ||
                outcome.MatchLength <= 0 ||
                _slotCells == null)
            {
                return;
            }

            int firstColumn = Mathf.Clamp(outcome.StartColumn, 0, SlotSpinResult.Columns - 1);
            int endColumn = Mathf.Min(SlotSpinResult.Columns, firstColumn + outcome.MatchLength);

            for (int column = firstColumn; column < endColumn; column++)
            {
                int index = SlotSpinResult.ToIndex(column, outcome.Row);
                if (index >= 0 && index < _slotCells.Length && _slotCells[index] != null)
                {
                    _slotCells[index].color = PatternHitColor;
                }
            }
        }

        private void CacheDefaultsIfNeeded()
        {
            if (_hasCachedDefaults)
            {
                return;
            }

            if (_slotCells == null)
            {
                _slotCellDefaultColors = Array.Empty<Color>();
            }
            else
            {
                _slotCellDefaultColors = new Color[_slotCells.Length];
                for (int index = 0; index < _slotCells.Length; index++)
                {
                    _slotCellDefaultColors[index] = _slotCells[index] != null
                        ? _slotCells[index].color
                        : Color.white;
                }
            }

            _hasCachedDefaults = true;
        }

        private void ResetSlotCellColors()
        {
            if (_slotCells == null)
            {
                return;
            }

            for (int index = 0; index < _slotCells.Length; index++)
            {
                if (_slotCells[index] == null)
                {
                    continue;
                }

                _slotCells[index].color = _slotCellDefaultColors != null && index < _slotCellDefaultColors.Length
                    ? _slotCellDefaultColors[index]
                    : Color.white;
            }
        }
    }
}
