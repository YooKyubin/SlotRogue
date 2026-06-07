using System;
using System.Collections.Generic;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.RunGame.ViewModels
{
    /// <summary>
    /// 시작 유물 선택 화면의 상태와 커맨드를 담습니다.
    /// MonoBehaviour가 아닌 순수 C# 클래스입니다.
    /// </summary>
    public sealed class StartRelicSelectViewModel
    {
        // ── 상태 ────────────────────────────────────────────────────────

        public IReadOnlyList<ArtifactDefinitionSO> Artifacts { get; }
        public string Summary { get; private set; }

        // ── 이벤트 ──────────────────────────────────────────────────────

        /// <summary>유물이 선택되었을 때 발행합니다. (artifactId)</summary>
        public event Action<string> ArtifactSelected;

        // ── 생성 ────────────────────────────────────────────────────────

        public StartRelicSelectViewModel()
        {
            Artifacts = StarterArtifactCatalog.All;
            Refresh();
        }

        // ── 커맨드 ──────────────────────────────────────────────────────

        /// <summary>View가 유물 버튼 클릭 시 호출합니다.</summary>
        public void SelectArtifact(string artifactId)
        {
            GameFlowSession.SelectArtifact(artifactId);
            ArtifactSelected?.Invoke(artifactId);
        }

        /// <summary>화면 진입 시 View가 최신 데이터로 갱신하도록 호출합니다.</summary>
        public void Refresh()
        {
            Summary = GameFlowSession.BuildSummary();
        }
    }
}
