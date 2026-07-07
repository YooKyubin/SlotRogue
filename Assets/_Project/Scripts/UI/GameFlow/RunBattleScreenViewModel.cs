using System;
using System.Collections.Generic;
using R3;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;
using SlotRogue.UI.Combat.Presentation;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleScreenViewModel
    {
        public const int DefaultSlotCellCount = 15;
        public const int DefaultEnemySlotCount = 3;

        private readonly string[] _slotCells;
        private readonly RunBattleEnemySlotState[] _enemySlots;
        private readonly ReactiveProperty<RunBattleScreenState> _state;

        private string _slotResultText = string.Empty;
        private string _attackResultText = string.Empty;
        private string _playerHudText = string.Empty;
        private RunBattleRelicShopState _relicShop = RunBattleRelicShopState.Empty;
        private RunBattleSwapState _swapState = RunBattleSwapState.Disabled;
        private int _playerHp;
        private int _playerMaxHp = 1;
        private int _playerShield;
        private int _playerShieldMax = 1;
        private RunBattleActionMode _actionMode = RunBattleActionMode.Spin;
        private bool _spinInteractable = true;
        private RunBattleSlotOutcomeState _slotOutcome = RunBattleSlotOutcomeState.None;
        private int _runCoins;
        private int _batchDepth;
        private bool _hasPendingPublish;

        public RunBattleScreenViewModel(int slotCellCount = DefaultSlotCellCount, int enemySlotCount = DefaultEnemySlotCount)
        {
            _slotCells = new string[Math.Max(0, slotCellCount)];
            for (int index = 0; index < _slotCells.Length; index++)
            {
                _slotCells[index] = "-";
            }

            _enemySlots = new RunBattleEnemySlotState[Math.Max(0, enemySlotCount)];
            for (int index = 0; index < _enemySlots.Length; index++)
            {
                _enemySlots[index] = RunBattleEnemySlotState.Hidden(index);
            }

            _state = new ReactiveProperty<RunBattleScreenState>(CreateState());
        }

        // 화면 상태는 R3 ReactiveProperty로 노출한다(ADR-0019/0020). 구독 즉시 현재 값을
        // 1회 흘려보내므로 별도 Changed 이벤트 + 초기 Render 호출이 필요 없다.
        public ReadOnlyReactiveProperty<RunBattleScreenState> State => _state;

        public void Batch(Action update)
        {
            if (update == null)
            {
                return;
            }

            _batchDepth++;
            try
            {
                update();
            }
            finally
            {
                _batchDepth--;
                if (_batchDepth == 0 && _hasPendingPublish)
                {
                    Publish();
                }
            }
        }

        public void SetSlotCells(string[] values)
        {
            int count = values != null ? Math.Min(_slotCells.Length, values.Length) : 0;
            for (int index = 0; index < _slotCells.Length; index++)
            {
                _slotCells[index] = index < count && !string.IsNullOrEmpty(values[index])
                    ? values[index]
                    : "-";
            }

            RequestPublish();
        }

        public void SetRelicShop(RunBattleRelicShopState relicShop)
        {
            _relicShop = relicShop ?? RunBattleRelicShopState.Empty;
            RequestPublish();
        }

        public void SetBattleText(
            string slotResultText,
            string attackResultText)
        {
            _slotResultText = slotResultText ?? string.Empty;
            _attackResultText = attackResultText ?? string.Empty;
            RequestPublish();
        }

        public void SetPlayerHud(
            string hudText,
            int hp,
            int maxHp,
            int shield,
            int shieldMax)
        {
            _playerHudText = hudText ?? string.Empty;
            _playerHp = Math.Max(0, hp);
            _playerMaxHp = Math.Max(1, maxHp);
            _playerShield = Math.Max(0, shield);
            _playerShieldMax = Math.Max(1, shieldMax);
            RequestPublish();
        }

        public void SetSlotOutcome(
            bool hasPattern,
            int row,
            int startColumn,
            int matchLength,
            IReadOnlyList<int> highlightedCellIndices = null,
            IReadOnlyList<SlotSymbolType?> highlightedCellSymbols = null)
        {
            _slotOutcome = new RunBattleSlotOutcomeState(
                hasPattern,
                row,
                startColumn,
                matchLength,
                highlightedCellIndices,
                highlightedCellSymbols);
            RequestPublish();
        }

        public void SetSwapState(
            bool interactable,
            int swapsRemaining,
            int selectedCellIndex)
        {
            _swapState = new RunBattleSwapState(
                interactable,
                swapsRemaining,
                selectedCellIndex);
            RequestPublish();
        }

        public void ClearEnemySlots()
        {
            for (int index = 0; index < _enemySlots.Length; index++)
            {
                _enemySlots[index] = RunBattleEnemySlotState.Hidden(index);
            }

            RequestPublish();
        }

        public void SetEnemySlot(
            int slotIndex,
            CombatParticipantId participantId,
            string hudText,
            int hp,
            int maxHp,
            int shield,
            bool selected,
            bool interactable,
            StatusEffectViewData[] statuses = null,
            IReadOnlyList<EnemyUpcomingActionViewData> upcomingActions = null)
        {
            if (slotIndex < 0 || slotIndex >= _enemySlots.Length)
            {
                return;
            }

            _enemySlots[slotIndex] = new RunBattleEnemySlotState(
                slotIndex,
                participantId,
                active: true,
                hudText ?? string.Empty,
                Math.Max(0, hp),
                Math.Max(1, maxHp),
                Math.Max(0, shield),
                selected,
                interactable,
                statuses,
                upcomingActions);
            RequestPublish();
        }

        public void SetActionMode(RunBattleActionMode mode, bool spinInteractable)
        {
            _actionMode = mode;
            _spinInteractable = spinInteractable;
            RequestPublish();
        }

        public void SetSpinInteractable(bool interactable)
        {
            _spinInteractable = interactable;
            RequestPublish();
        }

        public void SetRunCoins(int coins)
        {
            _runCoins = Math.Max(0, coins);
            RequestPublish();
        }

        private void RequestPublish()
        {
            if (_batchDepth > 0)
            {
                _hasPendingPublish = true;
                return;
            }

            Publish();
        }

        private void Publish()
        {
            _hasPendingPublish = false;
            _state.Value = CreateState();
        }

        private RunBattleScreenState CreateState()
        {
            return new RunBattleScreenState(
                _slotCells,
                _enemySlots,
                _slotResultText,
                _attackResultText,
                _playerHudText,
                _playerHp,
                _playerMaxHp,
                _playerShield,
                _playerShieldMax,
                _actionMode,
                _spinInteractable,
                _slotOutcome,
                _swapState,
                _relicShop,
                _runCoins);
        }
    }

    public sealed class RunBattleScreenState
    {
        private readonly string[] _slotCells;
        private readonly RunBattleEnemySlotState[] _enemySlots;

        internal RunBattleScreenState(
            string[] slotCells,
            RunBattleEnemySlotState[] enemySlots,
            string slotResultText,
            string attackResultText,
            string playerHudText,
            int playerHp,
            int playerMaxHp,
            int playerShield,
            int playerShieldMax,
            RunBattleActionMode actionMode,
            bool spinInteractable,
            RunBattleSlotOutcomeState slotOutcome,
            RunBattleSwapState swapState,
            RunBattleRelicShopState relicShop,
            int runCoins)
        {
            _slotCells = Clone(slotCells);
            _enemySlots = Clone(enemySlots);
            RunCoins = Math.Max(0, runCoins);
            SlotResultText = slotResultText ?? string.Empty;
            AttackResultText = attackResultText ?? string.Empty;
            PlayerHudText = playerHudText ?? string.Empty;
            PlayerHp = playerHp;
            PlayerMaxHp = playerMaxHp;
            PlayerShield = playerShield;
            PlayerShieldMax = playerShieldMax;
            ActionMode = actionMode;
            SpinInteractable = spinInteractable;
            SlotOutcome = slotOutcome;
            Swap = swapState;
            RelicShop = relicShop ?? RunBattleRelicShopState.Empty;
        }

        public string[] SlotCells => Clone(_slotCells);

        public RunBattleEnemySlotState[] EnemySlots => Clone(_enemySlots);

        public string SlotResultText { get; }

        public string AttackResultText { get; }

        public string PlayerHudText { get; }

        public int PlayerHp { get; }

        public int PlayerMaxHp { get; }

        public int PlayerShield { get; }

        public int PlayerShieldMax { get; }

        public RunBattleActionMode ActionMode { get; }

        public bool SpinInteractable { get; }

        public RunBattleSlotOutcomeState SlotOutcome { get; }

        public RunBattleSwapState Swap { get; }

        public RunBattleRelicShopState RelicShop { get; }

        public int RunCoins { get; }

        private static string[] Clone(string[] source)
        {
            if (source == null)
            {
                return Array.Empty<string>();
            }

            var copy = new string[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static RunBattleEnemySlotState[] Clone(RunBattleEnemySlotState[] source)
        {
            if (source == null)
            {
                return Array.Empty<RunBattleEnemySlotState>();
            }

            var copy = new RunBattleEnemySlotState[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }

    public readonly struct RunBattleEnemySlotState
    {
        private readonly StatusEffectViewData[] _statuses;
        private readonly EnemyUpcomingActionViewData[] _upcomingActions;

        public RunBattleEnemySlotState(
            int slotIndex,
            CombatParticipantId participantId,
            bool active,
            string hudText,
            int hp,
            int maxHp,
            int shield,
            bool selected,
            bool interactable,
            StatusEffectViewData[] statuses = null,
            IReadOnlyList<EnemyUpcomingActionViewData> upcomingActions = null)
        {
            SlotIndex = slotIndex;
            ParticipantId = participantId;
            Active = active;
            HudText = hudText ?? string.Empty;
            Hp = hp;
            MaxHp = maxHp;
            Shield = shield;
            Selected = selected;
            Interactable = interactable;
            _statuses = Clone(statuses);
            _upcomingActions = Clone(upcomingActions);
        }

        public int SlotIndex { get; }

        public CombatParticipantId ParticipantId { get; }

        public bool Active { get; }

        public string HudText { get; }

        public int Hp { get; }

        public int MaxHp { get; }

        public int Shield { get; }

        public bool Selected { get; }

        public bool Interactable { get; }

        public StatusEffectViewData[] Statuses => Clone(_statuses);

        public EnemyUpcomingActionViewData[] UpcomingActions => Clone(_upcomingActions);

        public static RunBattleEnemySlotState Hidden(int slotIndex)
        {
            return new RunBattleEnemySlotState(
                slotIndex,
                default,
                active: false,
                string.Empty,
                hp: 0,
                maxHp: 1,
                shield: 0,
                selected: false,
                interactable: false,
                statuses: null,
                upcomingActions: null);
        }

        private static StatusEffectViewData[] Clone(StatusEffectViewData[] source)
        {
            if (source == null)
            {
                return Array.Empty<StatusEffectViewData>();
            }

            var copy = new StatusEffectViewData[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static EnemyUpcomingActionViewData[] Clone(IReadOnlyList<EnemyUpcomingActionViewData> source)
        {
            if (source == null)
            {
                return Array.Empty<EnemyUpcomingActionViewData>();
            }

            var copy = new EnemyUpcomingActionViewData[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }

    public readonly struct RunBattleSlotOutcomeState
    {
        public static readonly RunBattleSlotOutcomeState None =
            new(false, row: -1, startColumn: -1, matchLength: 0);

        private readonly int[] _highlightedCellIndices;

        public RunBattleSlotOutcomeState(
            bool hasPattern,
            int row,
            int startColumn,
            int matchLength,
            IReadOnlyList<int> highlightedCellIndices = null,
            IReadOnlyList<SlotSymbolType?> highlightedCellSymbols = null)
        {
            Row = row;
            StartColumn = startColumn;
            MatchLength = matchLength;
            _highlightedCellIndices = CopyHighlightedCellIndices(
                highlightedCellIndices,
                hasPattern,
                row,
                startColumn,
                matchLength);
            HighlightedCellSymbols = CopyHighlightedCellSymbols(highlightedCellSymbols);
            HasPattern = hasPattern || _highlightedCellIndices.Length > 0;
        }

        public bool HasPattern { get; }

        public int Row { get; }

        public int StartColumn { get; }

        public int MatchLength { get; }

        public int[] HighlightedCellIndices => Clone(_highlightedCellIndices);

        public SlotSymbolType?[] HighlightedCellSymbols { get; }

        private static int[] CopyHighlightedCellIndices(
            IReadOnlyList<int> source,
            bool hasPattern,
            int row,
            int startColumn,
            int matchLength)
        {
            if (source != null && source.Count > 0)
            {
                var result = new List<int>(source.Count);
                for (int index = 0; index < source.Count; index++)
                {
                    int cellIndex = source[index];
                    if (SlotSpinResult.IsValidIndex(cellIndex) && !result.Contains(cellIndex))
                    {
                        result.Add(cellIndex);
                    }
                }

                return result.ToArray();
            }

            if (!hasPattern ||
                row < 0 ||
                row >= SlotSpinResult.Rows ||
                matchLength <= 0)
            {
                return Array.Empty<int>();
            }

            int firstColumn = Math.Max(0, Math.Min(SlotSpinResult.Columns - 1, startColumn));
            int endColumn = Math.Min(SlotSpinResult.Columns, firstColumn + matchLength);
            var cells = new List<int>(Math.Max(0, endColumn - firstColumn));

            for (int column = firstColumn; column < endColumn; column++)
            {
                cells.Add(SlotSpinResult.ToIndex(column, row));
            }

            return cells.ToArray();
        }

        private static int[] Clone(int[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<int>();
            }

            var copy = new int[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static SlotSymbolType?[] CopyHighlightedCellSymbols(
            IReadOnlyList<SlotSymbolType?> source)
        {
            var result = new SlotSymbolType?[SlotSpinResult.CellCount];
            if (source == null)
            {
                return result;
            }

            int count = Math.Min(source.Count, result.Length);
            for (int index = 0; index < count; index++)
            {
                result[index] = source[index];
            }

            return result;
        }
    }

    public readonly struct RunBattleSwapState
    {
        public static readonly RunBattleSwapState Disabled =
            new(interactable: false, swapsRemaining: 0, selectedCellIndex: -1);

        public RunBattleSwapState(
            bool interactable,
            int swapsRemaining,
            int selectedCellIndex)
        {
            Interactable = interactable;
            SwapsRemaining = Math.Max(0, swapsRemaining);
            SelectedCellIndex = SlotSpinResult.IsValidIndex(selectedCellIndex) ? selectedCellIndex : -1;
        }

        public bool Interactable { get; }

        public int SwapsRemaining { get; }

        public int SelectedCellIndex { get; }

        public bool HasSelection => SelectedCellIndex >= 0;

        public bool CanSelectCells => Interactable && SwapsRemaining > 0;

        public bool IsSelected(int cellIndex) => cellIndex == SelectedCellIndex;
    }

    public sealed class RunBattleRelicShopState
    {
        public static readonly RunBattleRelicShopState Empty =
            new(false, Array.Empty<RunBattleRelicShopOfferState>(), 0, 0, false, false, false);

        private readonly RunBattleRelicShopOfferState[] _offers;

        public RunBattleRelicShopState(
            bool visible,
            IReadOnlyList<RunBattleRelicShopOfferState> offers,
            int runCoins,
            int rerollCost,
            bool canReroll,
            bool canUseShop,
            bool canOpenShop)
        {
            Visible = visible;
            _offers = Copy(offers);
            RunCoins = Math.Max(0, runCoins);
            RerollCost = Math.Max(0, rerollCost);
            CanReroll = canReroll;
            CanUseShop = canUseShop;
            CanOpenShop = canOpenShop;
        }

        public bool Visible { get; }

        public IReadOnlyList<RunBattleRelicShopOfferState> Offers => _offers;

        public int RunCoins { get; }

        public int RerollCost { get; }

        public bool CanReroll { get; }

        public bool CanUseShop { get; }

        public bool CanOpenShop { get; }

        private static RunBattleRelicShopOfferState[] Copy(
            IReadOnlyList<RunBattleRelicShopOfferState> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<RunBattleRelicShopOfferState>();
            }

            var copy = new RunBattleRelicShopOfferState[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }

    public readonly struct RunBattleRelicShopOfferState
    {
        public RunBattleRelicShopOfferState(
            string relicId,
            string name,
            string grade,
            RewardRarity rarity,
            string description,
            string iconKey,
            int cost,
            bool purchased,
            bool canPurchase)
        {
            RelicId = relicId ?? string.Empty;
            Name = name ?? string.Empty;
            Grade = grade ?? string.Empty;
            Rarity = rarity;
            Description = description ?? string.Empty;
            IconKey = iconKey ?? string.Empty;
            Cost = Math.Max(0, cost);
            Purchased = purchased;
            CanPurchase = canPurchase;
        }

        public string RelicId { get; }

        public string Name { get; }

        public string Grade { get; }

        public RewardRarity Rarity { get; }

        public string Description { get; }

        public string IconKey { get; }

        public int Cost { get; }

        public bool Purchased { get; }

        public bool CanPurchase { get; }
    }
}
