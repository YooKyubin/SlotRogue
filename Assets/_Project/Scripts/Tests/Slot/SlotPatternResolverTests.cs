using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
using UnityEngine;

namespace SlotRogue.Slot.Tests
{
    // 일부 회귀 테스트는 레거시(Obsolete) Resolve() 단일 패턴 동작을 의도적으로 검증한다.
#pragma warning disable CS0618
    public sealed class SlotPatternResolverTests
    {
        [Test]
        public void Resolve_WhenHorizontalRunExists_ReturnsBestPattern()
        {
            var spin = MakeSpin(new[]
            {
                S, S, S, C, G,
                G, G, G, G, C,
                E, C, K, H, E
            });
            var resolver = new SlotPatternResolver();

            SlotPatternResult result = resolver.Resolve(spin);

            Assert.That(result.HasMatch, Is.True);
            Assert.That(result.Symbol, Is.EqualTo(SlotSymbolType.Clover));
            Assert.That(result.Row, Is.EqualTo(1));
            Assert.That(result.MatchLength, Is.EqualTo(4));
        }

        [Test]
        public void Resolve_WhenNoRunExists_ReturnsNoMatch()
        {
            var spin = MakeSpin(new[]
            {
                S, H, E, C, G,
                G, K, S, H, E,
                E, C, G, K, S
            });
            var resolver = new SlotPatternResolver();

            SlotPatternResult result = resolver.Resolve(spin);

            Assert.That(result.HasMatch, Is.False);
        }

        [Test]
        public void ResolveAll_RandomSpin_DoesNotThrow()
        {
            var service = new SlotMachineService(new System.Random(42));
            var resolver = new SlotPatternResolver();

            for (int index = 0; index < 20; index++)
            {
                SlotSpinResult spin = service.Spin();
                Assert.DoesNotThrow(() => resolver.ResolveAll(spin));
            }
        }

        [Test]
        public void ResolveAll_HorizontalSm_Only()
        {
            var spin = MakeSpin(new[]
            {
                C, G, E, K, H,
                S, S, S, C, G,
                H, C, K, G, E
            });
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Definition.Rank, Is.EqualTo(SlotPatternRank.HorizontalSm));
            Assert.That(matches[0].Symbol, Is.EqualTo(SlotSymbolType.Cherry));
        }

        [Test]
        public void ResolveAll_HorizontalLg_SuppressesHorizontalSm()
        {
            var spin = MakeSpin(new[]
            {
                C, G, E, K, H,
                S, S, S, S, G,
                H, C, K, G, E
            });
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);

            Assert.That(HasRank(matches, SlotPatternRank.HorizontalLg), Is.True, "Horizontal L should fire.");
            Assert.That(HasRank(matches, SlotPatternRank.HorizontalSm), Is.False, "Horizontal x3 should be suppressed.");
        }

        [Test]
        public void ResolveAll_HorizontalXL_SuppressesLgAndSm()
        {
            var spin = MakeSpin(new[]
            {
                C, G, E, K, H,
                S, S, S, S, S,
                H, C, K, G, E
            });
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);

            Assert.That(HasRank(matches, SlotPatternRank.HorizontalXL), Is.True, "Horizontal XL should fire.");
            Assert.That(HasRank(matches, SlotPatternRank.HorizontalLg), Is.False, "Horizontal L should be suppressed.");
            Assert.That(HasRank(matches, SlotPatternRank.HorizontalSm), Is.False, "Horizontal x3 should be suppressed.");
        }

        [Test]
        public void ResolveAll_Zig_SuppressesContainedDiagonals()
        {
            var spin = MakeSpin(new[]
            {
                C, C, S, C, C,
                C, S, C, S, C,
                S, C, C, C, S
            });
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);

            Assert.That(HasRank(matches, SlotPatternRank.Zig), Is.True, "Zig should fire.");
            Assert.That(HasDiag(matches, "diag-bs2"), Is.False, "Backslash diagonal should be suppressed by Zig.");
            Assert.That(HasDiag(matches, "diag-s0"), Is.False, "Slash diagonal should be suppressed by Zig.");
        }

        [Test]
        public void ResolveAll_AllCherry_ZigAndZagSuppressedByGroundAndHeaven()
        {
            SlotSpinResult spin = AllSame(SlotSymbolType.Cherry);
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);

            Assert.That(HasRank(matches, SlotPatternRank.Zig), Is.False, "Zig should be suppressed by Ground.");
            Assert.That(HasRank(matches, SlotPatternRank.Zag), Is.False, "Zag should be suppressed by Heaven.");
        }

        [Test]
        public void ResolveAll_Ground_SuppressesZigAndBottomXL()
        {
            SlotSymbolType[] symbols =
            {
                C, C, S, C, C,
                C, S, C, S, C,
                S, S, S, S, S
            };
            var spin = MakeSpin(symbols);
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);

            Assert.That(HasRank(matches, SlotPatternRank.Ground), Is.True, "Ground should fire.");
            Assert.That(FindXLRow(matches, 2), Is.Null, "Bottom row Horizontal XL should be suppressed.");
        }

        [Test]
        public void ResolveAll_Heaven_SuppressesZagAndTopXL()
        {
            SlotSymbolType[] symbols =
            {
                S, S, S, S, S,
                C, S, C, S, C,
                C, C, S, C, C
            };
            var spin = MakeSpin(symbols);
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);

            Assert.That(HasRank(matches, SlotPatternRank.Heaven), Is.True, "Heaven should fire.");
            Assert.That(FindXLRow(matches, 0), Is.Null, "Top row Horizontal XL should be suppressed.");
        }

        [Test]
        public void ResolveAll_Eye_SuppressesVerticalCol1AndCol3()
        {
            SlotSymbolType[] symbols =
            {
                C, S, S, S, C,
                S, S, C, S, S,
                C, S, S, S, C
            };
            var spin = MakeSpin(symbols);
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);

            Assert.That(HasRank(matches, SlotPatternRank.Eye), Is.True, "Eye should fire.");
            Assert.That(HasVerticalCol(matches, 1), Is.False, "Column 2 vertical should be suppressed.");
            Assert.That(HasVerticalCol(matches, 3), Is.False, "Column 4 vertical should be suppressed.");
        }

        [Test]
        public void ResolveAll_Jackpot_FiresAllExpectedPatterns()
        {
            SlotSpinResult spin = AllSame(SlotSymbolType.Cherry);
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);
            string log = BuildMatchLog(matches);

            Assert.That(HasVerticalCol(matches, 0), Is.True, $"Column 1 vertical\n{log}");
            Assert.That(HasVerticalCol(matches, 2), Is.True, $"Column 3 vertical\n{log}");
            Assert.That(HasVerticalCol(matches, 4), Is.True, $"Column 5 vertical\n{log}");
            Assert.That(HasVerticalCol(matches, 1), Is.False, $"Column 2 vertical suppressed\n{log}");
            Assert.That(HasVerticalCol(matches, 3), Is.False, $"Column 4 vertical suppressed\n{log}");

            Assert.That(FindXLRow(matches, 1), Is.Not.Null, $"Row 2 Horizontal XL\n{log}");
            Assert.That(FindXLRow(matches, 0), Is.Null, $"Row 1 Horizontal XL suppressed\n{log}");
            Assert.That(FindXLRow(matches, 2), Is.Null, $"Row 3 Horizontal XL suppressed\n{log}");

            Assert.That(HasDiag(matches, "diag-bs1"), Is.True, $"Center backslash diagonal\n{log}");
            Assert.That(HasDiag(matches, "diag-s1"), Is.True, $"Center slash diagonal\n{log}");
            Assert.That(HasDiag(matches, "diag-bs0"), Is.False, $"Left backslash diagonal suppressed\n{log}");
            Assert.That(HasDiag(matches, "diag-bs2"), Is.False, $"Right backslash diagonal suppressed\n{log}");
            Assert.That(HasDiag(matches, "diag-s0"), Is.False, $"Left slash diagonal suppressed\n{log}");
            Assert.That(HasDiag(matches, "diag-s2"), Is.False, $"Right slash diagonal suppressed\n{log}");

            Assert.That(HasRank(matches, SlotPatternRank.Ground), Is.True, $"Ground\n{log}");
            Assert.That(HasRank(matches, SlotPatternRank.Heaven), Is.True, $"Heaven\n{log}");
            Assert.That(HasRank(matches, SlotPatternRank.Eye), Is.True, $"Eye\n{log}");
            Assert.That(HasRank(matches, SlotPatternRank.Jackpot), Is.True, $"Jackpot\n{log}");

            Assert.That(HasRank(matches, SlotPatternRank.Zig), Is.False, $"Zig suppressed\n{log}");
            Assert.That(HasRank(matches, SlotPatternRank.Zag), Is.False, $"Zag suppressed\n{log}");
            Assert.That(HasRank(matches, SlotPatternRank.HorizontalSm), Is.False, $"Horizontal x3 suppressed\n{log}");
            Assert.That(HasRank(matches, SlotPatternRank.HorizontalLg), Is.False, $"Horizontal L suppressed\n{log}");
            Assert.That(matches[matches.Count - 1].Definition.Rank, Is.EqualTo(SlotPatternRank.Jackpot), $"Jackpot should be last\n{log}");
        }

        [Test]
        public void ResolveAll_DefaultCatalogAsset_FiresExpectedJackpotFlow()
        {
            SlotPatternCatalogAsset catalog = SlotPatternCatalogAsset.CreateDefaultCatalog();

            try
            {
                SlotPatternCatalog.SetRuntimeCatalogOverride(catalog);

                SlotSpinResult spin = AllSame(SlotSymbolType.Cherry);
                IReadOnlyList<SlotPatternMatch> matches = Resolve(spin);
                string log = BuildMatchLog(matches);

                Assert.That(FindXLRow(matches, 1), Is.Not.Null, $"Row 2 Horizontal XL\n{log}");
                Assert.That(HasRank(matches, SlotPatternRank.Ground), Is.True, $"Ground\n{log}");
                Assert.That(HasRank(matches, SlotPatternRank.Heaven), Is.True, $"Heaven\n{log}");
                Assert.That(HasRank(matches, SlotPatternRank.Eye), Is.True, $"Eye\n{log}");
                Assert.That(matches[matches.Count - 1].Definition.Rank, Is.EqualTo(SlotPatternRank.Jackpot), $"Jackpot should be last\n{log}");
            }
            finally
            {
                SlotPatternCatalog.ClearRuntimeCatalogOverride();
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void JackpotMatch_RepeatIndex_ProducesCorrectTitle()
        {
            SlotSpinResult spin = AllSame(SlotSymbolType.Clover);
            IReadOnlyList<SlotPatternMatch> matches = Resolve(spin, jackpotRepeatIndex: 0);

            Assert.That(GetJackpot(matches).PresentationTitle, Is.EqualTo("잭팟"));
            Assert.That(MakeJackpot(1).PresentationTitle, Is.EqualTo("슈퍼 잭팟"));
            Assert.That(MakeJackpot(2).PresentationTitle, Is.EqualTo("메가 잭팟"));
            Assert.That(MakeJackpot(3).PresentationTitle, Is.EqualTo("울트라 잭팟"));
            Assert.That(MakeJackpot(4).PresentationTitle, Is.EqualTo("얼티밋 잭팟"));
            Assert.That(MakeJackpot(5).PresentationTitle, Is.EqualTo("잭팟 X2"));
            Assert.That(MakeJackpot(6).PresentationTitle, Is.EqualTo("잭팟 X3"));
            Assert.That(MakeJackpot(7).PresentationTitle, Is.EqualTo("잭팟 X4"));
        }

        [Test]
        public void Spin_WithLuck15_ProducesJackpot()
        {
            var service = new SlotMachineService(new System.Random(1));
            var resolver = new SlotPatternResolver();

            SlotSpinResult spin = service.Spin(luck: 15);
            IReadOnlyList<SlotPatternMatch> matches = resolver.ResolveAll(spin);

            Assert.That(HasRank(matches, SlotPatternRank.Jackpot), Is.True);
        }

        [Test]
        public void Spin_WithLuck0_IsFullyRandom()
        {
            var service = new SlotMachineService(new System.Random(999));

            for (int index = 0; index < 10; index++)
            {
                Assert.DoesNotThrow(() => service.Spin(luck: 0));
            }
        }

        private static IReadOnlyList<SlotPatternMatch> Resolve(
            SlotSpinResult spin,
            int jackpotRepeatIndex = 0)
        {
            return new SlotPatternResolver().ResolveAll(spin, jackpotRepeatIndex);
        }

        private static SlotSpinResult MakeSpin(SlotSymbolType[] symbols)
        {
            return new SlotSpinResult(symbols);
        }

        private static SlotSpinResult AllSame(SlotSymbolType symbol)
        {
            var symbols = new SlotSymbolType[SlotSpinResult.CellCount];

            for (int index = 0; index < symbols.Length; index++)
            {
                symbols[index] = symbol;
            }

            return new SlotSpinResult(symbols);
        }

        private static bool HasRank(IReadOnlyList<SlotPatternMatch> matches, SlotPatternRank rank)
        {
            foreach (SlotPatternMatch match in matches)
            {
                if (match.Definition.Rank == rank)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasVerticalCol(IReadOnlyList<SlotPatternMatch> matches, int column)
        {
            foreach (SlotPatternMatch match in matches)
            {
                if (match.Definition.Rank == SlotPatternRank.Vertical &&
                    match.Definition.PatternId == $"vertical-col{column}")
                {
                    return true;
                }
            }

            return false;
        }

        private static SlotPatternMatch FindXLRow(IReadOnlyList<SlotPatternMatch> matches, int row)
        {
            foreach (SlotPatternMatch match in matches)
            {
                if (match.Definition.Rank != SlotPatternRank.HorizontalXL)
                {
                    continue;
                }

                if (match.MatchedCells.Count > 0 && match.MatchedCells[0].Row == row)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool HasDiag(IReadOnlyList<SlotPatternMatch> matches, string id)
        {
            foreach (SlotPatternMatch match in matches)
            {
                if (match.Definition.PatternId == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static SlotPatternMatch GetJackpot(IReadOnlyList<SlotPatternMatch> matches)
        {
            foreach (SlotPatternMatch match in matches)
            {
                if (match.Definition.IsJackpot)
                {
                    return match;
                }
            }

            return null;
        }

        private static SlotPatternMatch MakeJackpot(int repeatIndex)
        {
            var definition = new SlotPatternDefinition(
                "jackpot", "잭팟", 10, 10.0f, SlotPatternRank.Jackpot, true,
                System.Array.Empty<SlotCell>());
            return new SlotPatternMatch(definition, SlotSymbolType.Cherry, new List<SlotCell>(), 1, repeatIndex);
        }

        private static string BuildMatchLog(IReadOnlyList<SlotPatternMatch> matches)
        {
            var names = new List<string>();

            foreach (SlotPatternMatch match in matches)
            {
                names.Add(match.PresentationTitle);
            }

            return string.Join("\n", names);
        }

        private const SlotSymbolType S = SlotSymbolType.Cherry;
        private const SlotSymbolType H = SlotSymbolType.Seven;
        private const SlotSymbolType E = SlotSymbolType.Diamond;
        private const SlotSymbolType C = SlotSymbolType.Bell;
        private const SlotSymbolType G = SlotSymbolType.Clover;
        private const SlotSymbolType K = SlotSymbolType.Lemon;
    }
#pragma warning restore CS0618
}
