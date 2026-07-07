using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SlotRogue.Slot.Data
{
    /// <summary>
    /// 런(run)별 심볼 출현 확률 테이블. 심볼 <b>종류</b>는 6종 고정이고,
    /// 각 슬롯 칸은 심볼별 <b>확률값 / 전체 확률값</b> 확률로 독립 추첨합니다.
    ///
    /// 인스턴스는 런 내내 유지(식별자 불변)되며, 새 런 시작 시 <see cref="Reset"/>로
    /// 확률값만 초기화합니다.
    /// </summary>
    public sealed class SlotSymbolPool
    {
        // 기획상 "심볼 풀"이라는 물리적 가방은 쓰지 않는다. 이 값들은 한 칸에 어떤 심볼이
        // 나올지 정하는 상대 확률값이며, 보상은 이 값을 올리거나 내린다.

        // 클로버핏식 상대 가중치(×10 정수 스케일): 체리·레몬 1.3, 종·클로버 1.0, 다이아 0.8, 세븐 0.5.
        // (우리 게임엔 '보물' 심볼이 없어 다이아가 0.8 자리.) 실제 %는 가중치/총합으로 동적 계산된다.

        /// <summary>체리 시작 가중치(=1.3).</summary>
        public const int DefaultCherryProbabilityPercent = 13;

        /// <summary>레몬 시작 가중치(=1.3).</summary>
        public const int DefaultLemonProbabilityPercent = 13;

        /// <summary>클로버 시작 가중치(=1.0).</summary>
        public const int DefaultCloverProbabilityPercent = 10;

        /// <summary>종 시작 가중치(=1.0).</summary>
        public const int DefaultBellProbabilityPercent = 10;

        /// <summary>다이아 시작 가중치(=0.8).</summary>
        public const int DefaultDiamondProbabilityPercent = 8;

        /// <summary>7 시작 가중치(=0.5).</summary>
        public const int DefaultSevenProbabilityPercent = 5;

        private static readonly SlotSymbolType[] AllSymbols =
        {
            SlotSymbolType.Cherry,
            SlotSymbolType.Seven,
            SlotSymbolType.Diamond,
            SlotSymbolType.Bell,
            SlotSymbolType.Clover,
            SlotSymbolType.Lemon,
        };

        private static readonly SlotSymbolType[] ProbabilityDisplaySymbols =
        {
            SlotSymbolType.Cherry,
            SlotSymbolType.Lemon,
            SlotSymbolType.Clover,
            SlotSymbolType.Bell,
            SlotSymbolType.Diamond,
            SlotSymbolType.Seven,
        };

        private readonly Dictionary<SlotSymbolType, int> _weights = new();

        public SlotSymbolPool()
        {
            Reset();
        }

        /// <summary>모든 심볼을 같은 가중치로 시작하는 테이블(테스트/디버그용).</summary>
        public SlotSymbolPool(int initialWeightPerSymbol)
        {
            ResetUniform(initialWeightPerSymbol);
        }

        /// <summary>추첨 대상이 되는 모든 심볼 종류(고정).</summary>
        public static IReadOnlyList<SlotSymbolType> Symbols => AllSymbols;

        /// <summary>UI 확률 표시용 고정 순서.</summary>
        public static IReadOnlyList<SlotSymbolType> ProbabilityDisplayOrder =>
            ProbabilityDisplaySymbols;

        /// <summary>현재 테이블의 총 확률값. 기본 시작값은 100입니다.</summary>
        public int TotalWeight
        {
            get
            {
                int total = 0;
                foreach (SlotSymbolType symbol in AllSymbols) total += GetWeight(symbol);
                return total;
            }
        }

        /// <summary>기존 저장/테스트 호환용 별칭. 새 코드는 <see cref="TotalWeight"/>를 사용한다.</summary>
        public int Total => TotalWeight;

        public int GetWeight(SlotSymbolType symbol) =>
            _weights.TryGetValue(symbol, out int weight) ? weight : 0;

        /// <summary>기존 저장/테스트 호환용 별칭. 새 코드는 <see cref="GetWeight"/>를 사용한다.</summary>
        public int GetCount(SlotSymbolType symbol) => GetWeight(symbol);

        public static int DefaultWeightFor(SlotSymbolType symbol) =>
            symbol switch
            {
                SlotSymbolType.Cherry => DefaultCherryProbabilityPercent,
                SlotSymbolType.Lemon => DefaultLemonProbabilityPercent,
                SlotSymbolType.Clover => DefaultCloverProbabilityPercent,
                SlotSymbolType.Bell => DefaultBellProbabilityPercent,
                SlotSymbolType.Diamond => DefaultDiamondProbabilityPercent,
                SlotSymbolType.Seven => DefaultSevenProbabilityPercent,
                _ => 0,
            };

        /// <summary>기존 저장/테스트 호환용 별칭. 새 코드는 <see cref="DefaultWeightFor"/>를 사용한다.</summary>
        public static int DefaultCountFor(SlotSymbolType symbol) => DefaultWeightFor(symbol);

        public double ProbabilityOf(SlotSymbolType symbol)
        {
            int total = TotalWeight;
            return total > 0
                ? GetWeight(symbol) / (double)total
                : 1d / AllSymbols.Length;
        }

        /// <summary>새 런 시작 시 호출. 심볼 확률값을 기본 시작값으로 되돌립니다.</summary>
        public void Reset()
        {
            foreach (SlotSymbolType symbol in AllSymbols)
            {
                _weights[symbol] = DefaultWeightFor(symbol);
            }
        }

        /// <summary>모든 심볼을 같은 가중치로 되돌립니다(테스트/디버그용).</summary>
        public void ResetUniform(int initialWeightPerSymbol)
        {
            int start = Math.Max(0, initialWeightPerSymbol);
            foreach (SlotSymbolType symbol in AllSymbols) _weights[symbol] = start;
        }

        /// <summary>보상 등으로 특정 심볼 가중치를 늘립니다(음수면 감소, 0 미만 방지).</summary>
        public void AddWeight(SlotSymbolType symbol, int amount)
        {
            if (amount == 0) return;
            _weights[symbol] = Math.Max(0, GetWeight(symbol) + amount);
        }

        /// <summary>기존 저장/테스트 호환용 별칭. 새 코드는 <see cref="AddWeight"/>를 사용한다.</summary>
        public void Add(SlotSymbolType symbol, int amount)
        {
            AddWeight(symbol, amount);
        }

        /// <summary>
        /// 심볼 가중치를 절반으로 줄입니다("덜 나온다" 계열 보상용). 완전히 사라지지 않도록 최소 1을 유지합니다.
        /// </summary>
        public void HalveWeight(SlotSymbolType symbol)
        {
            int current = GetWeight(symbol);
            if (current <= 1)
            {
                return;
            }

            _weights[symbol] = Math.Max(1, current / 2);
        }

        /// <summary>심볼 가중치를 지정값으로 설정합니다(0 미만 방지). 저장된 런 복원에 사용합니다.</summary>
        public void SetWeight(SlotSymbolType symbol, int weight)
        {
            _weights[symbol] = Math.Max(0, weight);
        }

        /// <summary>기존 저장/테스트 호환용 별칭. 새 코드는 <see cref="SetWeight"/>를 사용한다.</summary>
        public void SetCount(SlotSymbolType symbol, int count)
        {
            SetWeight(symbol, count);
        }

        /// <summary>
        /// 한 칸에 들어갈 심볼 하나를 가중치 기반으로 뽑습니다. <paramref name="exclude"/>는 제외합니다.
        /// 전체 가중치가 0이거나 전부 제외되면 균등 폴백합니다.
        /// </summary>
        public SlotSymbolType Draw(Random random, ISet<SlotSymbolType> exclude = null)
        {
            if (random == null) random = new Random();

            int total = 0;
            foreach (SlotSymbolType symbol in AllSymbols)
            {
                if (exclude != null && exclude.Contains(symbol)) continue;
                total += GetWeight(symbol);
            }

            if (total <= 0)
            {
                return AllSymbols[random.Next(AllSymbols.Length)];
            }

            int roll = random.Next(total);
            foreach (SlotSymbolType symbol in AllSymbols)
            {
                if (exclude != null && exclude.Contains(symbol)) continue;
                roll -= GetWeight(symbol);
                if (roll < 0) return symbol;
            }

            return AllSymbols[AllSymbols.Length - 1]; // 부동소수점 없는 정수 합이라 도달하지 않음(안전망)
        }

        public string BuildSummary()
        {
            var builder = new StringBuilder();
            foreach (SlotSymbolType symbol in AllSymbols)
            {
                builder
                    .Append(symbol)
                    .Append(' ')
                    .Append((ProbabilityOf(symbol) * 100d).ToString("0.#", CultureInfo.InvariantCulture))
                    .Append("% (p")
                    .Append(GetWeight(symbol))
                    .Append(")   ");
            }
            return builder.ToString().TrimEnd();
        }
    }
}
