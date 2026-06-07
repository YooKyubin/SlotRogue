using System;
using System.Collections.Generic;
using SlotRogue.Relics.Data;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics
{
    /// <summary>
    /// 유물 조건 판정과 효과 실행에 필요한 모든 입력/출력을 담는 전달 객체.
    /// 슬롯/전투 연결 계층이 입력(족보, 풀, 플레이어/적 상태)을 채워 <c>RelicSystem.Execute</c>에 넘기고,
    /// 누적된 출력(피해 보너스/회복/방어/골드/상태이상 요청 등)을 읽어 기존 전투 시스템에 반영한다.
    /// </summary>
    public sealed class RelicContext
    {
        public RelicContext(Random rng = null)
        {
            Rng = rng ?? new Random();
            Damage = new DamageContext();
            StatusApplications = new List<RelicStatusApplication>();
            _enemyStatuses = new HashSet<RelicStatusType>();
        }

        // ---- 공용 ----
        public Random Rng { get; }
        public DamageContext Damage { get; }

        // ---- 입력(연결 계층이 매 트리거 전에 갱신) ----

        /// <summary>이번 턴에 발동한 족보들. 패턴 기반 조건/효과가 참조한다.</summary>
        public IReadOnlyList<SlotPatternMatch> Patterns { get; set; } = Array.Empty<SlotPatternMatch>();

        /// <summary>슬롯 풀의 심볼 구성(풀 조건용). 없으면 풀 조건은 발동하지 않는다.</summary>
        public IReadOnlyList<SlotSymbolType> PoolSymbols { get; set; }

        public int PlayerCurrentHp { get; set; }
        public int PlayerMaxHp { get; set; }
        public int PlayerGold { get; set; }

        /// <summary>적의 현재 독 스택(PerStack 반복용). 기존 전투 상태에서 주입.</summary>
        public int EnemyPoisonStacks { get; set; }

        /// <summary>OnAfterDamage 시점에 이번 공격으로 적에게 가한 피해(회복 비율 계산용).</summary>
        public int LastDamageDealt { get; set; }

        /// <summary>플레이어가 사망 판정되었는지(OnPlayerDeath/부활 처리용).</summary>
        public bool PlayerIsDead { get; set; }

        /// <summary>현재 실행 중인 트리거 타이밍.</summary>
        public RelicTriggerTiming CurrentTiming { get; set; }

        private readonly HashSet<RelicStatusType> _enemyStatuses;

        /// <summary>적이 현재 가진 상태이상을 연결 계층이 설정한다(기존 상태이상 시스템에서 스냅샷).</summary>
        public void SetEnemyStatuses(IEnumerable<RelicStatusType> statuses)
        {
            _enemyStatuses.Clear();
            if (statuses == null)
            {
                return;
            }

            foreach (RelicStatusType status in statuses)
            {
                _enemyStatuses.Add(status);
            }
        }

        public void SetEnemyStatus(RelicStatusType status, bool active)
        {
            if (status == RelicStatusType.None)
            {
                return;
            }

            if (active)
            {
                _enemyStatuses.Add(status);
            }
            else
            {
                _enemyStatuses.Remove(status);
            }
        }

        public bool HasEnemyStatus(RelicStatusType status)
        {
            return status != RelicStatusType.None && _enemyStatuses.Contains(status);
        }

        // ---- 출력(효과 실행으로 누적, 연결 계층이 반영) ----
        public int HealAccumulated { get; set; }
        public int ShieldAccumulated { get; set; }
        public int GoldAccumulated { get; set; }
        public int PlayerDamageAccumulated { get; set; }
        public int NextTurnBaseDamageAccumulated { get; set; }
        public int RespinTicketsGained { get; set; }
        public int PotionsGained { get; set; }
        public int EnemyAttackReduction { get; set; }
        public int RemoveRandomSymbolCount { get; set; }
        public int CurseSymbolCount { get; set; }
        public bool ReviveRequested { get; set; }

        /// <summary>유물이 적에게 걸어달라고 요청한 상태이상 목록(연결 계층이 기존 시스템으로 적용).</summary>
        public List<RelicStatusApplication> StatusApplications { get; }

        /// <summary>한 번의 트리거 실행에서 누적되는 출력 값을 초기화한다(다음 트리거 전 호출).</summary>
        public void ResetAccumulators()
        {
            HealAccumulated = 0;
            ShieldAccumulated = 0;
            GoldAccumulated = 0;
            PlayerDamageAccumulated = 0;
            NextTurnBaseDamageAccumulated = 0;
            RespinTicketsGained = 0;
            PotionsGained = 0;
            EnemyAttackReduction = 0;
            RemoveRandomSymbolCount = 0;
            CurseSymbolCount = 0;
            ReviveRequested = false;
            StatusApplications.Clear();
        }
    }
}
