using System;
using System.Collections.Generic;
using System.Text;

namespace SlotRogue.Slot.Data
{
    /// <summary>
    /// 런(run)별 가변 심볼 풀(가방). 심볼 <b>종류</b>는 6종 고정이고,
    /// 심볼별 <b>개수</b>를 늘려 빌드를 쌓습니다. 슬롯은 개수에 비례한 가중 추첨으로 심볼을 뽑습니다.
    ///
    /// 인스턴스는 런 내내 유지(식별자 불변)되며, 새 런 시작 시 <see cref="Reset"/>로 개수만 초기화합니다.
    /// </summary>
    public sealed class SlotSymbolPool
    {
        /// <summary>심볼당 시작 개수. 기획 미정 — 플레이테스트로 조정.</summary>
        public const int DefaultCountPerSymbol = 4;

        private static readonly SlotSymbolType[] AllSymbols =
        {
            SlotSymbolType.Cherry,
            SlotSymbolType.Seven,
            SlotSymbolType.Diamond,
            SlotSymbolType.Bell,
            SlotSymbolType.Clover,
            SlotSymbolType.Lemon,
        };

        private readonly Dictionary<SlotSymbolType, int> _counts = new();

        public SlotSymbolPool() : this(DefaultCountPerSymbol)
        {
        }

        public SlotSymbolPool(int initialPerSymbol)
        {
            Reset(initialPerSymbol);
        }

        /// <summary>풀에 존재하는 모든 심볼 종류(고정).</summary>
        public static IReadOnlyList<SlotSymbolType> Symbols => AllSymbols;

        /// <summary>현재 풀의 총 심볼 개수.</summary>
        public int Total
        {
            get
            {
                int total = 0;
                foreach (SlotSymbolType symbol in AllSymbols) total += GetCount(symbol);
                return total;
            }
        }

        public int GetCount(SlotSymbolType symbol) =>
            _counts.TryGetValue(symbol, out int count) ? count : 0;

        /// <summary>새 런 시작 시 호출. 모든 심볼 개수를 시작값으로 되돌립니다.</summary>
        public void Reset(int initialPerSymbol = DefaultCountPerSymbol)
        {
            int start = Math.Max(0, initialPerSymbol);
            foreach (SlotSymbolType symbol in AllSymbols) _counts[symbol] = start;
        }

        /// <summary>보상 등으로 특정 심볼 개수를 늘립니다(음수면 감소, 0 미만 방지).</summary>
        public void Add(SlotSymbolType symbol, int amount)
        {
            if (amount == 0) return;
            _counts[symbol] = Math.Max(0, GetCount(symbol) + amount);
        }

        /// <summary>
        /// 개수에 비례한 가중 추첨으로 심볼 하나를 뽑습니다. <paramref name="exclude"/>는 제외합니다.
        /// 풀이 비었거나 전부 제외되면 균등 폴백합니다.
        /// </summary>
        public SlotSymbolType Draw(Random random, ISet<SlotSymbolType> exclude = null)
        {
            if (random == null) random = new Random();

            int total = 0;
            foreach (SlotSymbolType symbol in AllSymbols)
            {
                if (exclude != null && exclude.Contains(symbol)) continue;
                total += GetCount(symbol);
            }

            if (total <= 0)
            {
                return AllSymbols[random.Next(AllSymbols.Length)];
            }

            int roll = random.Next(total);
            foreach (SlotSymbolType symbol in AllSymbols)
            {
                if (exclude != null && exclude.Contains(symbol)) continue;
                roll -= GetCount(symbol);
                if (roll < 0) return symbol;
            }

            return AllSymbols[AllSymbols.Length - 1]; // 부동소수점 없는 정수 합이라 도달하지 않음(안전망)
        }

        public string BuildSummary()
        {
            var builder = new StringBuilder();
            foreach (SlotSymbolType symbol in AllSymbols)
            {
                builder.Append(symbol).Append(' ').Append(GetCount(symbol)).Append("   ");
            }
            return builder.ToString().TrimEnd();
        }
    }
}
