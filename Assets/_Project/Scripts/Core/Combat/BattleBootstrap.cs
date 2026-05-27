using SlotRogue.Data.Combat;
using UnityEngine;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleBootstrap : MonoBehaviour
    {
        [SerializeField] private MonsterDefinition _monsterDefinition;
        [SerializeField] private int _playerMaxHp = 30;

        private BattleResolver _resolver;
        private BattlePresenter _presenter;

        public BattleResolver Resolver => _resolver;

        public BattlePresenter Presenter => _presenter;

        public ISpinCombatConsumer CombatConsumer => _resolver;

        private void Awake()
        {
            if (_monsterDefinition == null)
            {
                Debug.LogError("[BattleBootstrap] MonsterDefinition is not assigned.", this);
                return;
            }

            _resolver = new BattleResolver(_monsterDefinition, _playerMaxHp);
            _presenter = new BattlePresenter(_resolver, _monsterDefinition);
        }

        private void OnDestroy()
        {
            _presenter?.Unsubscribe();
        }
    }
}
