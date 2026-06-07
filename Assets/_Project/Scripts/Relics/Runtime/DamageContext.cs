using UnityEngine;

namespace SlotRogue.Relics
{
    /// <summary>
    /// 한 번의 플레이어 공격에 대한 피해 계산 상태. 유물 효과가 각 항목을 누적 변경하고
    /// <see cref="ComputeFinal"/>에서 최종 피해를 산출한다.
    /// <para>finalDamage = (baseDamage + patternDamage + flatBonusDamage) * damageMultiplier</para>
    /// <para>치명타 발생 시 critDamageMultiplier를 추가로 곱한다.</para>
    /// </summary>
    public sealed class DamageContext
    {
        public int BaseDamage;
        public int PatternDamage;
        public int FlatBonusDamage;
        public float DamageMultiplier = 1f;
        public float CritChance;
        public float CritDamageMultiplier = 2f;

        public int FinalDamage { get; private set; }
        public bool IsCritical { get; private set; }

        /// <summary>새 공격 계산을 위해 항목을 초기값으로 되돌린다.</summary>
        public void Reset(int baseDamage = 0, int patternDamage = 0)
        {
            BaseDamage = baseDamage;
            PatternDamage = patternDamage;
            FlatBonusDamage = 0;
            DamageMultiplier = 1f;
            CritChance = 0f;
            CritDamageMultiplier = 2f;
            FinalDamage = 0;
            IsCritical = false;
        }

        /// <summary>현재 누적 상태로 최종 피해를 계산해 <see cref="FinalDamage"/>에 반영하고 반환한다.</summary>
        public int ComputeFinal(System.Random rng)
        {
            float raw = (BaseDamage + PatternDamage + FlatBonusDamage) * DamageMultiplier;

            IsCritical = CritChance > 0f && rng != null && rng.NextDouble() < CritChance;
            if (IsCritical)
            {
                raw *= CritDamageMultiplier;
            }

            FinalDamage = Mathf.Max(0, Mathf.RoundToInt(raw));
            return FinalDamage;
        }
    }
}
