using System;
using SlotRogue.Data.GameFlow;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.RunGame.ViewModels
{
    /// <summary>
    /// 맵 화면의 상태와 커맨드를 담습니다.
    /// 순수 C# 클래스입니다.
    /// </summary>
    public sealed class RunMapViewModel
    {
        // ── 상태 ────────────────────────────────────────────────────────

        public RunMapGraphDefinition Graph => GameFlowSession.CurrentMapGraph;
        public RunMapNodeDefinition CurrentNode => GameFlowSession.CurrentMapNode;
        public string Summary { get; private set; }

        // ── 이벤트 ──────────────────────────────────────────────────────

        /// <summary>선택 가능한 노드를 클릭했을 때 발행합니다. (nodeId)</summary>
        public event Action<string> NodeSelected;

        // ── 생성 ────────────────────────────────────────────────────────

        public RunMapViewModel()
        {
            Refresh();
        }

        // ── 쿼리 ────────────────────────────────────────────────────────

        public bool IsNodeSelectable(string nodeId) =>
            GameFlowSession.IsMapNodeSelectable(nodeId);

        public bool IsNodeVisited(string nodeId) =>
            GameFlowSession.IsMapNodeVisited(nodeId);

        public RunMapNodeDefinition[] GetNextChoices() =>
            GameFlowSession.GetNextMapChoices();

        // ── 커맨드 ──────────────────────────────────────────────────────

        /// <summary>
        /// View가 노드 버튼 클릭 시 호출합니다.
        /// 선택 가능한 노드일 때만 NodeSelected 이벤트를 발행합니다.
        /// </summary>
        public void SelectNode(string nodeId)
        {
            if (!GameFlowSession.SelectNextMapNode(nodeId)) return;

            NodeSelected?.Invoke(nodeId);
        }

        /// <summary>화면 진입 시 최신 데이터로 갱신합니다.</summary>
        public void Refresh()
        {
            Summary = BuildSummary();
        }

        // ── 내부 ────────────────────────────────────────────────────────

        private static string BuildSummary()
        {
            RunMapNodeDefinition current = GameFlowSession.CurrentMapNode;
            RunMapNodeDefinition[] choices = GameFlowSession.GetNextMapChoices();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"현재 위치: {current.DisplayName}  (F{current.Floor})");
            sb.AppendLine($"다음 노드: {choices.Length}개");

            for (int i = 0; i < choices.Length; i++)
                sb.AppendLine($"  - {choices[i].DisplayName} ({choices[i].NodeType})");

            if (choices.Length == 0) sb.AppendLine("  - 루트 클리어");

            sb.AppendLine();
            sb.Append(GameFlowSession.BuildSummary());
            return sb.ToString();
        }
    }
}
