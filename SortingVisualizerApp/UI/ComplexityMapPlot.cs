using System.Numerics;
using ImGuiNET;

namespace SortingVisualizerApp.UI;

public enum ComplexityPointShape
{
    Circle,
    Square,
    Diamond
}

public sealed class ComplexityMapVisualPoint
{
    public required string AlgorithmId { get; init; }
    public required string Label { get; init; }
    public required string Tooltip { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Size { get; init; }
    public required uint Color { get; init; }
    public required bool IsMeasured { get; init; }
    public required ComplexityPointShape Shape { get; init; }
}

public readonly record struct ComplexityMapPlotResult(
    string? HoveredAlgorithmId,
    string? ClickedAlgorithmId,
    Vector2 PlotMin,
    Vector2 PlotMax,
    bool HasValidPlot);

public static class ComplexityMapPlot
{
    public static ComplexityMapPlotResult Draw(
        string id,
        IReadOnlyList<ComplexityMapVisualPoint> points,
        Vector2 size,
        string xAxisLabel,
        string yAxisLabel)
    {
        if (!ImGui.BeginChild(id, size, ImGuiChildFlags.Borders))
        {
            ImGui.EndChild();
            return new ComplexityMapPlotResult(null, null, Vector2.Zero, Vector2.Zero, false);
        }

        var drawList = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();
        var width = Math.Max(220.0f, avail.X - 14.0f);
        var height = Math.Max(170.0f, avail.Y - 32.0f);

        var plotMin = cursor + new Vector2(8.0f, 8.0f);
        var plotMax = plotMin + new Vector2(width, height);

        drawList.AddRectFilled(plotMin, plotMax, PackColor(16, 16, 16, 255), 4.0f);
        drawList.AddRect(plotMin, plotMax, PackColor(75, 75, 75, 255), 4.0f);

        for (var i = 1; i <= 4; i++)
        {
            var x = plotMin.X + width * (i / 5.0f);
            var y = plotMin.Y + height * (i / 5.0f);
            drawList.AddLine(new Vector2(x, plotMin.Y), new Vector2(x, plotMax.Y), PackColor(70, 70, 70, 90), 1.0f);
            drawList.AddLine(new Vector2(plotMin.X, y), new Vector2(plotMax.X, y), PackColor(70, 70, 70, 90), 1.0f);
        }

        drawList.AddText(new Vector2(plotMin.X + 6.0f, plotMax.Y + 4.0f), PackColor(210, 210, 210, 255), $"X: {xAxisLabel}");
        drawList.AddText(new Vector2(plotMin.X + 6.0f, plotMax.Y + 20.0f), PackColor(210, 210, 210, 255), $"Y: {yAxisLabel}");

        var mouse = ImGui.GetIO().MousePos;
        var inside = mouse.X >= plotMin.X && mouse.X <= plotMax.X && mouse.Y >= plotMin.Y && mouse.Y <= plotMax.Y;
        var clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        ComplexityMapVisualPoint? hovered = null;
        var hoveredDistance = float.MaxValue;
        foreach (var point in points)
        {
            var p = new Vector2(
                plotMin.X + Math.Clamp(point.X, 0.0f, 1.0f) * width,
                plotMax.Y - Math.Clamp(point.Y, 0.0f, 1.0f) * height);

            var radius = Math.Clamp(point.Size, 3.0f, 12.0f);
            DrawPoint(drawList, p, radius, point.Color, point.Shape, point.IsMeasured);

            if (!inside)
            {
                continue;
            }

            var dx = mouse.X - p.X;
            var dy = mouse.Y - p.Y;
            var distSq = dx * dx + dy * dy;
            var hitRadius = radius + 3.0f;
            if (distSq <= hitRadius * hitRadius && distSq < hoveredDistance)
            {
                hovered = point;
                hoveredDistance = distSq;
            }
        }

        string? hoveredId = null;
        string? clickedId = null;
        if (hovered is not null)
        {
            hoveredId = hovered.AlgorithmId;
            if (clicked)
            {
                clickedId = hovered.AlgorithmId;
            }

            ImGui.BeginTooltip();
            ImGui.TextUnformatted(hovered.Label);
            ImGui.Separator();
            ImGui.TextWrapped(hovered.Tooltip);
            ImGui.EndTooltip();
        }

        ImGui.Dummy(new Vector2(width + 12.0f, height + 30.0f));
        ImGui.EndChild();

        return new ComplexityMapPlotResult(hoveredId, clickedId, plotMin, plotMax, true);
    }

    private static void DrawPoint(ImDrawListPtr drawList, Vector2 center, float radius, uint color, ComplexityPointShape shape, bool measured)
    {
        var alphaColor = measured ? color : (color & 0x00FFFFFF) | (0x88u << 24);
        var border = PackColor(235, 235, 235, measured ? (byte)180 : (byte)120);

        switch (shape)
        {
            case ComplexityPointShape.Square:
            {
                var min = new Vector2(center.X - radius, center.Y - radius);
                var max = new Vector2(center.X + radius, center.Y + radius);
                drawList.AddRectFilled(min, max, alphaColor, 2.0f);
                drawList.AddRect(min, max, border, 2.0f);
                break;
            }

            case ComplexityPointShape.Diamond:
            {
                var p0 = new Vector2(center.X, center.Y - radius);
                var p1 = new Vector2(center.X + radius, center.Y);
                var p2 = new Vector2(center.X, center.Y + radius);
                var p3 = new Vector2(center.X - radius, center.Y);
                drawList.AddQuadFilled(p0, p1, p2, p3, alphaColor);
                drawList.AddQuad(p0, p1, p2, p3, border, 1.0f);
                break;
            }

            default:
                drawList.AddCircleFilled(center, radius, alphaColor, 18);
                drawList.AddCircle(center, radius, border, 18, 1.0f);
                break;
        }
    }

    private static uint PackColor(byte r, byte g, byte b, byte a)
    {
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }
}
