using SlotRogue.Data.GameFlow;
using SlotRogue.Relics.Data;
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

        public static string SelectedArtifactId { get; private set; } = string.Empty;

        public static bool HasStarterArtifact => !string.IsNullOrEmpty(SelectedArtifactId);

        // ── 유물(Relic) 인벤토리 (Artifact → Relic 통합 발판) ───────────────
        // 최종 목표는 시작 유물·전투 효과를 RelicDataSO / RelicSystem 중심으로 통합하는 것이다.
        // 현재는 RelicDataSO 에셋과 선택 UI가 아직 없어(에디터 작업 필요) 비어 있으며,
        // 기존 ArtifactDefinitionSO 경로(SelectedArtifactId)가 그대로 전투 효과를 담당한다.
        // 향후: 선택 UI가 RelicDataSO를 여기에 추가 → 전투에서 RelicSystem이 이 목록을 소비.
        private static readonly List<RelicDataSO> _relicInventory = new();

        /// <summary>[레거시] 구 RelicDataSO 인벤토리. v20.3 풀에서는 사용하지 않음(보존용).</summary>
        public static IReadOnlyList<RelicDataSO> SelectedRelics => _relicInventory;

        /// <summary>[레거시] 유물을 인벤토리에 추가합니다(중복 무시).</summary>
        public static void AddRelic(RelicDataSO relic)
        {
            EnsureRunStarted();
            if (relic != null && !_relicInventory.Contains(relic))
            {
                _relicInventory.Add(relic);
            }
        }

        // ── v20.3 유물 인벤토리(RelicDefinition) ─────────────────────────
        // 시작 유물 + 보상으로 획득한 유물을 누적한다. 전투마다 RelicEffectRunner가 소비한다.
        private static readonly List<RelicDefinition> _ownedRelics = new();

        /// <summary>이 런에서 보유한 v20.3 유물 목록(시작 유물 포함).</summary>
        public static IReadOnlyList<RelicDefinition> OwnedRelics => _ownedRelics;

        /// <summary>v20.3 유물을 보유 목록에 추가합니다(중복 허용 — 동일 유물 누적 가능).</summary>
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
            SelectedArtifactId = string.Empty;
            _relicInventory.Clear();
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

        public static void SelectArtifact(string artifactId)
        {
            EnsureRunStarted();
            SelectedArtifactId = artifactId ?? string.Empty;
        }

        /// <summary>[레거시] 구 시작 유물 선택. 런타임 미사용(시작 유물은 AddRelic 사용).</summary>
        [Obsolete("v20.3 레거시. 런타임 미사용 — 시작 유물은 RelicCatalog.Starters + AddRelic 사용.", false)]
        public static void SelectStarterArtifact(StarterArtifactId artifactId)
        {
#pragma warning disable CS0618 // 레거시 StarterArtifactCatalog 의도적 사용(보존용)
            ArtifactDefinitionSO so = StarterArtifactCatalog.Get(artifactId);
#pragma warning restore CS0618
            SelectArtifact(so?.ArtifactId ?? string.Empty);
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
