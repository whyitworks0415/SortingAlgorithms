using System.Numerics;
using ImGuiNET;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Rendering;

public sealed class GraphViewRenderer : IViewRenderer
{
    public VisualizationMode Mode => VisualizationMode.Graph;

    public void Draw(SimulationFrameState state)
    {
        if (!state.VisualEnabled)
        {
            return;
        }

        var graph = state.Graph;
        if (graph.Nodes.Length == 0)
        {
            return;
        }

        var drawList = ImGui.GetBackgroundDrawList();
        var left = 24.0f;
        var top = 52.0f;
        var right = Math.Max(left + 220.0f, state.ViewportWidth - 24.0f);
        var bottom = Math.Max(top + 220.0f, state.ViewportHeight - 24.0f);

        var rectMin = new Vector2(left, top);
        var rectMax = new Vector2(right, bottom);
        drawList.AddRect(rectMin, rectMax, PackColor(200, 200, 200, 120), 0.0f, ImDrawFlags.None, 1.0f);

        var width = Math.Max(1.0f, right - left);
        var height = Math.Max(1.0f, bottom - top);
        var radius = Math.Clamp(10.0f - graph.Nodes.Length * 0.02f, 3.0f, 9.0f);

        for (var i = 0; i < graph.Edges.Length; i++)
        {
            var edge = graph.Edges[i];
            if (edge.From < 0 || edge.From >= graph.Nodes.Length || edge.To < 0 || edge.To >= graph.Nodes.Length)
            {
                continue;
            }

            var fromPos = ToScreen(graph.Nodes[edge.From].Position, left, top, width, height);
            var toPos = ToScreen(graph.Nodes[edge.To].Position, left, top, width, height);
            var color = edge.Active
                ? PackColor(42, 173, 255, 220)
                : PackColor(180, 180, 180, 110);
            drawList.AddLine(fromPos, toPos, color, edge.Active ? 1.8f : 1.0f);
        }

        for (var i = 0; i < graph.Nodes.Length; i++)
        {
            var node = graph.Nodes[i];
            var p = ToScreen(node.Position, left, top, width, height);
            var isSelected = node.NodeId == graph.SelectedNode;
            var fill = node.Emitted
                ? PackColor(230, 230, 230, 255)
                : (isSelected ? PackColor(42, 173, 255, 255) : PackColor(100, 100, 100, 240));
            var stroke = isSelected ? PackColor(255, 255, 255, 255) : PackColor(30, 30, 30, 255);

            drawList.AddCircleFilled(p, radius, fill, 16);
            drawList.AddCircle(p, radius, stroke, 16, 1.0f);

            var indegreeText = node.InDegree.ToString();
            var textOffset = new Vector2(p.X - 4.0f, p.Y - 5.0f);
            drawList.AddText(textOffset, PackColor(10, 10, 10, 255), indegreeText);
        }
    }

    public void Dispose()
    {
    }

    private static Vector2 ToScreen(Vector2 uv, float left, float top, float width, float height)
    {
        return new Vector2(left + uv.X * width, top + uv.Y * height);
    }

    private static uint PackColor(byte r, byte g, byte b, byte a)
    {
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }
}
