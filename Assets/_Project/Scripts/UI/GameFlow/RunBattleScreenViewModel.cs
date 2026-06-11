using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleScreenViewModel
    {
        public const int DefaultSlotCellCount = 15;
        public const int DefaultEnemySlotCount = 3;

        private readonly string[] _slotCells;
        private readonly RunBattleEnemySlotState[] _enemySlots;

        private string _statusText = string.Empty;
        private string _slotResultText = string.Empty;
        private string _attackResultText = string.Empty;
        private string _playerHudText = string.Empty;
        private string _enemyIntentText = string.Empty;
        private int _playerHp;
        private int _playerMaxHp = 1;
        private int _playerShield;
        private int _playerShieldMax = 1;
        private RunBattleActionMode _actionMode = RunBattleActionMode.Spin;
        private bool _spinInteractable = true;
        private RunBattleSlotOutcomeState _slotOutcome = RunBattleSlotOutcomeState.None;
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

            State = CreateState();
        }

        public event Action<RunBattleScreenState> Changed;

        public RunBattleScreenState State { get; private set; }

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

        public void SetBattleText(
            string statusText,
            string slotResultText,
            string attackResultText,
            string enemyIntentText)
        {
            _statusText = statusText ?? string.Empty;
            _slotResultText = slotResultText ?? string.Empty;
            _attackResultText = attackResultText ?? string.Empty;
            _enemyIntentText = enemyIntentText ?? string.Empty;
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
            int matchLength)
        {
            _slotOutcome = new RunBattleSlotOutcomeState(
                hasPattern,
                row,
                startColumn,
                matchLength);
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
            EnemyUpcomingActionViewData[] upcomingActions = null)
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
            State = CreateState();
            Changed?.Invoke(State);
        }

        private RunBattleScreenState CreateState()
        {
            return new RunBattleScreenState(
                _slotCells,
                _enemySlots,
                _statusText,
                _slotResultText,
                _attackResultText,
                _playerHudText,
                _enemyIntentText,
                _playerHp,
                _playerMaxHp,
                _playerShield,
                _playerShieldMax,
                _actionMode,
                _spinInteractable,
                _slotOutcome);
        }
    }

    public sealed class RunBattleScreenState
    {
        private readonly string[] _slotCells;
        private readonly RunBattleEnemySlotState[] _enemySlots;

        internal RunBattleScreenState(
            string[] slotCells,
            RunBattleEnemySlotState[] enemySlots,
            string statusText,
            string slotResultText,
            string attackResultText,
            string playerHudText,
            string enemyIntentText,
            int playerHp,
            int playerMaxHp,
            int playerShield,
            int playerShieldMax,
            RunBattleActionMode actionMode,
            bool spinInteractable,
            RunBattleSlotOutcomeState slotOutcome)
        {
            _slotCells = Clone(slotCells);
            _enemySlots = Clone(enemySlots);
            StatusText = statusText ?? string.Empty;
            SlotResultText = slotResultText ?? string.Empty;
            AttackResultText = attackResultText ?? string.Empty;
            PlayerHudText = playerHudText ?? string.Empty;
            EnemyIntentText = enemyIntentText ?? string.Empty;
            PlayerHp = playerHp;
            PlayerMaxHp = playerMaxHp;
            PlayerShield = playerShield;
            PlayerShieldMax = playerShieldMax;
            ActionMode = actionMode;
            SpinInteractable = spinInteractable;
            SlotOutcome = slotOutcome;
        }

        public string[] SlotCells => Clone(_slotCells);

        public RunBattleEnemySlotState[] EnemySlots => Clone(_enemySlots);

        public string StatusText { get; }

        public string SlotResultText { get; }

        public string AttackResultText { get; }

        public string PlayerHudText { get; }

        public string EnemyIntentText { get; }

        public int PlayerHp { get; }

        public int PlayerMaxHp { get; }

        public int PlayerShield { get; }

        public int PlayerShieldMax { get; }

        public RunBattleActionMode ActionMode { get; }

        public bool SpinInteractable { get; }

        public RunBattleSlotOutcomeState SlotOutcome { get; }

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
            EnemyUpcomingActionViewData[] upcomingActions = null)
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

        private static EnemyUpcomingActionViewData[] Clone(EnemyUpcomingActionViewData[] source)
        {
            if (source == null)
            {
                return Array.Empty<EnemyUpcomingActionViewData>();
            }

            var copy = new EnemyUpcomingActionViewData[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }

    public readonly struct RunBattleSlotOutcomeState
    {
        public static readonly RunBattleSlotOutcomeState None =
            new(false, row: -1, startColumn: -1, matchLength: 0);

        public RunBattleSlotOutcomeState(
            bool hasPattern,
            int row,
            int startColumn,
            int matchLength)
        {
            HasPattern = hasPattern;
            Row = row;
            StartColumn = startColumn;
            MatchLength = matchLength;
        }

        public bool HasPattern { get; }

        public int Row { get; }

        public int StartColumn { get; }

        public int MatchLength { get; }
    }
}
