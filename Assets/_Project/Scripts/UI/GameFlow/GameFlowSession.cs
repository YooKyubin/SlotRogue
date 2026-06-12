using SlotRogue.Data.GameFlow;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using System;
using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public static class GameFlowSession
    {
        private const int DefaultPlayerMaxHp = 30;

        // ── 무한모드 진행 (Infinite Mode) ───────────────────────────────
        // v1 출시 스코프. 맵/노드 없이 인덱스 기반으로 전투를 반복하며,
        // 등급은 CurrentBattleNumber로부터 생성합니다.

        /// <summary>일반 전투 승리 시 자동 회복량. (엘리트/보스는 유물로만 보상)</summary>
        private const int NormalWinHeal = 4;

        /// <summary>엘리트 등장 주기. 기획 미정 — 플레이테스트로 조정.</summary>
        public static int ElitePeriod { get; set; } = 5;

        /// <summary>보스 등장 주기. ElitePeriod보다 우선합니다. 기획 미정 — 플레이테스트로 조정.</summary>
        public static int BossPeriod { get; set; } = 10;

        /// <summary>
        /// 무한모드 여부. true면 전투는 맵 노드 대신 등급(Tier) 기반으로 적을 구성합니다.
        /// 맵 API는 그대로 두고 이 플래그로만 우회합니다(스토리모드 재연결 대비).
        /// </summary>
        public static bool IsInfiniteMode { get; private set; }

        /// <summary>현재 진행 중인 전투 번호(1-base).</summary>
        public static int CurrentBattleNumber { get; private set; }

        /// <summary>
        /// 런 동안 유지되는 가변 심볼 풀(가방). 슬롯은 이 풀의 개수에 비례해 심볼을 뽑습니다.
        /// 인스턴스는 불변(식별자 유지)이며 새 런 시작 시 개수만 Reset합니다.
        /// </summary>
        public static SlotSymbolPool SlotPool { get; } = new SlotSymbolPool();

        /// <summary>무한모드 전투 표시용 제목.</summary>
        public static string CurrentEncounterTitle => CurrentTier switch
        {
            EncounterTier.Boss => "BOSS",
            EncounterTier.Elite => "ELITE",
            _ => $"BATTLE {CurrentBattleNumber}",
        };

        /// <summary>현재 전투의 등급.</summary>
        public static EncounterTier CurrentTier => GetTierForBattle(CurrentBattleNumber);

        /// <summary>현재(직전) 전투가 유물 보상을 주는가 (엘리트/보스만).</summary>
        public static bool CurrentBattleGrantsArtifact =>
            CurrentTier is EncounterTier.Elite or EncounterTier.Boss;

        /// <summary>전투 번호로부터 등급을 결정합니다. 보스가 엘리트보다 우선합니다.</summary>
        public static EncounterTier GetTierForBattle(int battleNumber)
        {
            if (battleNumber <= 0) return EncounterTier.Normal;
            if (BossPeriod > 0 && battleNumber % BossPeriod == 0) return EncounterTier.Boss;
            if (ElitePeriod > 0 && battleNumber % ElitePeriod == 0) return EncounterTier.Elite;
            return EncounterTier.Normal;
        }

        /// <summary>다음 전투로 진행합니다. 보상/회복 처리가 끝난 뒤 호출합니다.</summary>
        public static void AdvanceToNextBattle()
        {
            EnsureRunStarted();
            CurrentBattleNumber++;
        }

        /// <summary>
        /// 무한모드 전투 승리 처리. HP를 반영하고 승리 수를 올린 뒤,
        /// 일반 전투면 소량 회복합니다. (엘리트/보스 보상은 유물 선택에서 별도 처리)
        /// </summary>
        public static void CompleteInfiniteVictory(int remainingPlayerHp)
        {
            CompleteBattleVictory(remainingPlayerHp);

            if (CurrentTier == EncounterTier.Normal)
            {
                PlayerCurrentHp = Math.Min(PlayerCurrentHp + NormalWinHeal, PlayerMaxHp);
            }
        }

        public static bool HasRun { get; private set; }

        public static int PlayerMaxHp { get; private set; }

        public static int PlayerCurrentHp { get; private set; }

        public static int BattleIndex { get; private set; }

        public static int Victories { get; private set; }

        public static int RewardsClaimed { get; private set; }

        public static int DamageBonus { get; private set; }

        public static int DefenseBonus { get; private set; }

        // ── v23 유물 인벤토리 ────────────────────────────────────────────
        // 시작 유물 + 보상으로 획득한 유물을 누적한다. 전투마다 RelicTurnResolver가 소비한다.
        private static readonly List<RelicDefinition> _ownedRelics = new();

        /// <summary>이 런에서 보유한 v23 유물 목록(시작 유물 포함).</summary>
        public static IReadOnlyList<RelicDefinition> OwnedRelics => _ownedRelics;

        public static bool HasStarterRelic
        {
            get
            {
                for (int index = 0; index < _ownedRelics.Count; index++)
                {
                    if (_ownedRelics[index].IsStarter)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>시작 유물은 런에 하나만 유지합니다.</summary>
        public static bool SelectStarterRelic(RelicDefinition relic)
        {
            EnsureRunStarted();
            if (relic == null || !relic.IsStarter || !relic.Phase1)
            {
                return false;
            }

            for (int index = _ownedRelics.Count - 1; index >= 0; index--)
            {
                if (_ownedRelics[index].IsStarter)
                {
                    _ownedRelics.RemoveAt(index);
                }
            }

            _ownedRelics.Insert(0, relic);
            return true;
        }

        /// <summary>v23 유물을 보유 목록에 추가합니다(중복 허용 — 동일 유물 누적 가능).</summary>
        public static void AddRelic(RelicDefinition relic)
        {
            EnsureRunStarted();
            if (relic != null)
            {
                _ownedRelics.Add(relic);
            }
        }

        public static void StartNewRun()
        {
            HasRun = true;
            IsInfiniteMode = true; // v1: 무한모드. 스토리모드 재개 시 별도 진입점에서 false 설정.
            PlayerMaxHp = DefaultPlayerMaxHp;
            PlayerCurrentHp = DefaultPlayerMaxHp;
            BattleIndex = 0;
            CurrentBattleNumber = 1;
            Victories = 0;
            RewardsClaimed = 0;
            DamageBonus = 0;
            DefenseBonus = 0;
            _ownedRelics.Clear();
            SlotPool.Reset();
        }

        public static void EnsureRunStarted()
        {
            if (!HasRun)
            {
                StartNewRun();
            }
        }

        public static void CompleteBattleVictory(int remainingPlayerHp)
        {
            EnsureRunStarted();
            PlayerCurrentHp = Math.Max(1, Math.Min(remainingPlayerHp, PlayerMaxHp));
            Victories++;
        }

        public static void CompleteBattleDefeat()
        {
            HasRun = false;
        }

        public static void ApplyReward(RunRewardType rewardType)
        {
            EnsureRunStarted();

            switch (rewardType)
            {
                case RunRewardType.Heal:
                    PlayerCurrentHp = Math.Min(PlayerCurrentHp + 8, PlayerMaxHp);
                    break;
                case RunRewardType.DamageBonus:
                    DamageBonus += 2;
                    break;
                case RunRewardType.DefenseBonus:
                    DefenseBonus += 2;
                    break;
                case RunRewardType.MaxHpUp:
                    PlayerMaxHp += 5;
                    PlayerCurrentHp = Math.Min(PlayerCurrentHp + 5, PlayerMaxHp);
                    break;
                case RunRewardType.BigHeal:
                    PlayerCurrentHp = Math.Min(PlayerCurrentHp + 16, PlayerMaxHp);
                    break;
                case RunRewardType.GreaterDamage:
                    DamageBonus += 4;
                    break;
                case RunRewardType.GreaterDefense:
                    DefenseBonus += 4;
                    break;
                case RunRewardType.FullHeal:
                    PlayerCurrentHp = PlayerMaxHp;
                    break;
                default:
                    break;
            }

            RewardsClaimed++;
        }

        /// <summary>슬롯 풀에 심볼을 추가하는 보상을 적용합니다.</summary>
        public static void ApplySymbolReward(SlotSymbolType symbol, int amount)
        {
            EnsureRunStarted();
            SlotPool.Add(symbol, amount);
            RewardsClaimed++;
        }

        /// <summary>유물 보상 선택을 보상 카운트에 반영합니다(유물 추가는 AddRelic이 담당).</summary>
        public static void MarkRewardClaimed()
        {
            EnsureRunStarted();
            RewardsClaimed++;
        }

        public static string BuildSummary()
        {
            return
                $"HP {PlayerCurrentHp}/{PlayerMaxHp}\n" +
                $"진입 전투: {BattleIndex}\n" +
                $"승리: {Victories}\n" +
                $"보상: {RewardsClaimed}\n" +
                $"현재 전투: {CurrentBattleNumber}\n" +
                $"전투 등급: {CurrentTier}\n" +
                $"보유 유물: {BuildRelicSummary()}\n" +
                $"슬롯 풀: {SlotPool.BuildSummary()}";
        }

        private static string BuildRelicSummary()
        {
            if (_ownedRelics.Count == 0)
            {
                return "없음";
            }

            var builder = new System.Text.StringBuilder();
            for (int index = 0; index < _ownedRelics.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(_ownedRelics[index].Name);
            }

            return builder.ToString();
        }
    }
}
