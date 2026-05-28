using SlotRogue.Slot.Data;
using SlotRogue.Slot.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.Slot.Views
{
    public sealed class SlotMachineView : MonoBehaviour
    {
        private void Awake()
        {
            _viewModel = new SlotMachineViewModel();
            CacheChildReferencesIfNeeded();
        }

        private void OnEnable()
        {
            if (_spinButton != null)
            {
                _spinButton.onClick.AddListener(HandleSpinClicked);
            }

            if (_viewModel != null)
            {
                _viewModel.StateChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (_spinButton != null)
            {
                _spinButton.onClick.RemoveListener(HandleSpinClicked);
            }

            if (_viewModel != null)
            {
                _viewModel.StateChanged -= Refresh;
            }
        }

        private void HandleSpinClicked()
        {
            _viewModel.Spin();
        }

        private void Refresh()
        {
            if (_spinButton != null)
            {
                _spinButton.interactable = _viewModel.CanSpin;
            }

            RefreshCells(_viewModel.CurrentSpinResult);

            if (_resultView != null)
            {
                _resultView.Display(
                    _viewModel.CurrentSpinResult,
                    _viewModel.CurrentPatternResult,
                    _viewModel.CurrentCalculationResult,
                    _viewModel.CurrentCombatRequest);
            }
        }

        private void RefreshCells(SlotSpinResult spinResult)
        {
            if (spinResult == null || _cells == null)
            {
                return;
            }

            int visibleCellCount = Mathf.Min(_cells.Length, SlotSpinResult.CellCount);

            for (int index = 0; index < visibleCellCount; index++)
            {
                if (_cells[index] != null)
                {
                    _cells[index].SetSymbol(spinResult.Symbols[index]);
                }
            }
        }

        private void CacheChildReferencesIfNeeded()
        {
            if (_spinButton == null)
            {
                _spinButton = GetComponentInChildren<Button>(true);
            }

            if (_resultView == null)
            {
                _resultView = GetComponentInChildren<SlotResultView>(true);
            }

            if (_cells == null || _cells.Length == 0)
            {
                _cells = GetComponentsInChildren<SlotCellView>(true);
            }
        }

        [SerializeField] private Button _spinButton;
        [SerializeField] private SlotCellView[] _cells;
        [SerializeField] private SlotResultView _resultView;

        private SlotMachineViewModel _viewModel;
    }
}
