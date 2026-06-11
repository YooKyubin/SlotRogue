using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// [레거시] 구 시작 유물(Artifact) 카탈로그. <b>런타임 미사용</b>.
    /// v20.3에서 시작 유물은 <c>SlotRogue.Relics.Pool.RelicCatalog.Starters</c>로 대체되었다.
    /// 컴파일 유지를 위해 보존하며, 신규 코드에서 호출하지 말 것.
    /// </summary>
    [System.Obsolete("v20.3 레거시. 런타임 미사용 — 시작 유물은 RelicCatalog.Starters 사용.", false)]
    public static class StarterArtifactCatalog
    {
        public static IReadOnlyList<ArtifactDefinitionSO> All => GetStarterArtifacts();

        public static ArtifactDefinitionSO Get(StarterArtifactId id)
        {
            string artifactId = ToArtifactId(id);

            if (string.IsNullOrEmpty(artifactId))
            {
                return null;
            }

            ArtifactDefinitionSO found = FindInCatalog(artifactId);
            return found != null ? found : GetFallback(id);
        }

        public static ArtifactDefinitionSO GetById(string artifactId)
        {
            if (string.IsNullOrEmpty(artifactId))
            {
                return null;
            }

            ArtifactDefinitionSO found = FindInCatalog(artifactId);

            if (found != null)
            {
                return found;
            }

            foreach (StarterArtifactId id in System.Enum.GetValues(typeof(StarterArtifactId)))
            {
                if (ToArtifactId(id) == artifactId)
                {
                    return GetFallback(id);
                }
            }

            return null;
        }

        private static IReadOnlyList<ArtifactDefinitionSO> GetStarterArtifacts()
        {
            ArtifactCatalogSO catalog = ArtifactCatalogSO.Load();

            if (catalog != null)
            {
                return catalog.GetByCategory(ArtifactCategory.Starter);
            }

            return CreateFallbackAll();
        }

        private static ArtifactDefinitionSO FindInCatalog(string artifactId)
        {
            ArtifactCatalogSO catalog = ArtifactCatalogSO.Load();
            return catalog != null ? catalog.GetById(artifactId) : null;
        }

        // 카탈로그 에셋이 없을 때만 쓰는 폴백. SO는 id마다 한 번만 생성해 재사용한다
        // (매 호출 CreateInstance 시 발생하던 인스턴스 누수 방지).
        private static ArtifactDefinitionSO GetFallback(StarterArtifactId id)
        {
            if (id == StarterArtifactId.None)
            {
                return null;
            }

            if (_fallbackCache.TryGetValue(id, out ArtifactDefinitionSO cached) && cached != null)
            {
                return cached;
            }

            ArtifactDefinitionSO created = CreateFallback(id);
            _fallbackCache[id] = created;
            return created;
        }

        private static string ToArtifactId(StarterArtifactId id) => id switch
        {
            StarterArtifactId.Cherry => "cherry",
            StarterArtifactId.Grape => "grape",
            StarterArtifactId.Seven => "seven",
            StarterArtifactId.Lemon => "lemon",
            StarterArtifactId.Bell => "bell",
            StarterArtifactId.Clover => "clover",
            _ => string.Empty
        };

        private static ArtifactDefinitionSO CreateFallback(StarterArtifactId id) => id switch
        {
            StarterArtifactId.Cherry => ArtifactDefinitionSO.Create(
                "cherry", "체리",
                "체리 아이콘 3개 이상 매치 시 피해 +5.",
                ArtifactCategory.Starter, SlotSymbolType.Cherry, 3,
                ArtifactEffectKind.BonusDamage, bonusAmount: 5),
            StarterArtifactId.Grape => ArtifactDefinitionSO.Create(
                "grape", "포도",
                "포도 아이콘 3개 이상 매치 시 회복 +4.",
                ArtifactCategory.Starter, SlotSymbolType.Diamond, 3,
                ArtifactEffectKind.BonusHeal, bonusAmount: 4),
            StarterArtifactId.Seven => ArtifactDefinitionSO.Create(
                "seven", "세븐",
                "세븐 아이콘 3개 이상 매치 시 방어 +6.",
                ArtifactCategory.Starter, SlotSymbolType.Seven, 3,
                ArtifactEffectKind.BonusDefense, bonusAmount: 6),
            StarterArtifactId.Lemon => ArtifactDefinitionSO.Create(
                "lemon", "레몬",
                "레몬 아이콘 3개 이상 매치 시 화염 부여 (3턴, 턴당 피해 2).",
                ArtifactCategory.Starter, SlotSymbolType.Lemon, 3,
                ArtifactEffectKind.ApplyBurn, statusDuration: 3, statusMagnitude: 2,
                statusStackBehavior: StatusStackBehavior.Refresh),
            StarterArtifactId.Bell => ArtifactDefinitionSO.Create(
                "bell", "종",
                "종 아이콘 3개 이상 매치 시 빙결 부여 (적 행동 1턴 스킵).",
                ArtifactCategory.Starter, SlotSymbolType.Bell, 3,
                ArtifactEffectKind.ApplyFreeze, statusDuration: 1,
                statusStackBehavior: StatusStackBehavior.Refresh),
            StarterArtifactId.Clover => ArtifactDefinitionSO.Create(
                "clover", "네잎클로버",
                "네잎클로버 아이콘 3개 이상 매치 시 독 스택 +1 (스택당 매 턴 피해, 최대 5).",
                ArtifactCategory.Starter, SlotSymbolType.Clover, 3,
                ArtifactEffectKind.ApplyPoison, statusMagnitude: 1,
                statusStackBehavior: StatusStackBehavior.Stack),
            _ => null
        };

        private static IReadOnlyList<ArtifactDefinitionSO> CreateFallbackAll()
        {
            return new[]
            {
                GetFallback(StarterArtifactId.Cherry),
                GetFallback(StarterArtifactId.Grape),
                GetFallback(StarterArtifactId.Seven),
                GetFallback(StarterArtifactId.Lemon),
                GetFallback(StarterArtifactId.Bell),
                GetFallback(StarterArtifactId.Clover),
            };
        }

        private static readonly Dictionary<StarterArtifactId, ArtifactDefinitionSO> _fallbackCache = new();
    }
}
