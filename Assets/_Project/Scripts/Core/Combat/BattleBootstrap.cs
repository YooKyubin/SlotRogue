using SlotRogue.Data.Combat;
using UnityEngine;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleBootstrap : MonoBehaviour
    {
        [SerializeField] private MonsterDefinition _monsterDefinition;
        [SerializeField] private int _playerMaxHp = 30;

        public BattleResolver Resolver { get; private set; }
        public BattlePresenter Presenter { get; private set; }
        private CombatPipelineConsumer _combatConsumer;

        public ISpinCombatConsumer CombatConsumer => _combatConsumer;

        private void Awake()
        {
            if (_monsterDefinition == null)
            {
                Debug.LogError("[BattleBootstrap] MonsterDefinition is not assigned.", this);
                return;
            }

            Resolver = new BattleResolver(_monsterDefinition, _playerMaxHp);
            Presenter = new BattlePresenter();
            _combatConsumer = new CombatPipelineConsumer(Resolver, Presenter);
        }
    }
}
