using System.Collections.Generic;
using SlotRogue.Relics.Pool;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 런 동안 보유한 v23 유물 목록을 관리합니다(시작 유물 + 보상 누적).
    /// 도메인 상태만 담당하며 RNG/보상 추첨/프레젠테이션은 포함하지 않습니다.
    /// 인스턴스 클래스라 단위 테스트에서 격리해 검증할 수 있습니다.
    /// </summary>
    public sealed class RelicInventory
    {
        private readonly List<RelicDefinition> _relics = new();

        /// <summary>보유 유물 목록(시작 유물 포함).</summary>
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

        /// <summary>유물을 보유 목록에 추가합니다(중복 허용 — 동일 유물 누적 가능).</summary>
        public void Add(RelicDefinition relic)
        {
            if (relic != null)
            {
                _relics.Add(relic);
            }
        }

        public void Clear()
        {
            _relics.Clear();
        }
    }
}
