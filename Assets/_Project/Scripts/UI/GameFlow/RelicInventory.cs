using System.Collections.Generic;
using SlotRogue.Relics.Pool;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 런 동안 보유한 유물 목록을 관리합니다(보상/상점 누적). 도메인 상태만 담당하며
    /// RNG/보상 추첨/프레젠테이션은 포함하지 않습니다. 인스턴스 클래스라 단위 테스트에서 격리 검증 가능.
    /// 소멸·웨이브(ConsumableWaves) 유물은 남은 웨이브를 추적해 <see cref="TickWaveLifetimes"/>에서 만료 제거합니다.
    /// </summary>
    public sealed class RelicInventory
    {
        private readonly List<RelicDefinition> _relics = new();

        /// <summary>소멸·웨이브 유물의 남은 웨이브 수(유물 id → 잔여). v29는 유물당 1개라 id 키로 충분.</summary>
        private readonly Dictionary<string, int> _wavesRemaining = new();

        /// <summary>보유 유물 목록.</summary>
        public IReadOnlyList<RelicDefinition> Owned => _relics;

        public bool HasStarter
        {
            get
            {
                for (int index = 0; index < _relics.Count; index++)
                {
                    if (_relics[index].IsStarter)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 시작 유물을 선택합니다. 런에는 시작 유물을 하나만 유지하므로 기존 시작 유물을 교체합니다.
        /// Starter/Phase1 유물이 아니면 아무 변경 없이 false를 반환합니다.
        /// </summary>
        public bool SelectStarter(RelicDefinition relic)
        {
            if (relic == null || !relic.IsStarter || !relic.Phase1)
            {
                return false;
            }

            for (int index = _relics.Count - 1; index >= 0; index--)
            {
                if (_relics[index].IsStarter)
                {
                    _relics.RemoveAt(index);
                }
            }

            _relics.Insert(0, relic);
            return true;
        }

        /// <summary>유물을 보유 목록에 추가합니다(중복 허용). 소멸·웨이브 유물이면 남은 웨이브를 초기화합니다.</summary>
        public void Add(RelicDefinition relic)
        {
            if (relic == null)
            {
                return;
            }

            _relics.Add(relic);

            RelicSpec spec = RelicSpecCatalog.GetById(relic.Id);
            if (spec != null &&
                spec.Lifetime.Kind == RelicLifetimeKind.ConsumableWaves &&
                spec.Lifetime.Amount > 0)
            {
                _wavesRemaining[relic.Id] = spec.Lifetime.Amount;
            }
        }

        /// <summary>
        /// 웨이브(전투)가 하나 지날 때 호출. 소멸·웨이브 유물의 남은 웨이브를 1 줄이고,
        /// 0이 된 유물은 보유 목록에서 제거합니다(알아서 슬롯을 비움).
        /// </summary>
        public void TickWaveLifetimes()
        {
            if (_wavesRemaining.Count == 0)
            {
                return;
            }

            var keys = new List<string>(_wavesRemaining.Keys);
            for (int index = 0; index < keys.Count; index++)
            {
                string relicId = keys[index];
                int remaining = _wavesRemaining[relicId] - 1;
                if (remaining <= 0)
                {
                    _wavesRemaining.Remove(relicId);
                    RemoveById(relicId);
                }
                else
                {
                    _wavesRemaining[relicId] = remaining;
                }
            }
        }

        public void Clear()
        {
            _relics.Clear();
            _wavesRemaining.Clear();
        }

        private void RemoveById(string relicId)
        {
            for (int index = _relics.Count - 1; index >= 0; index--)
            {
                if (_relics[index] != null && _relics[index].Id == relicId)
                {
                    _relics.RemoveAt(index);
                }
            }
        }
    }
}
