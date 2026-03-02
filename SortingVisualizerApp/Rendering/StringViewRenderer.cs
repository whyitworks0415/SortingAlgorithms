using System.Numerics;
using ImGuiNET;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Rendering;

public sealed class StringViewRenderer : IViewRenderer
{
    public VisualizationMode Mode => VisualizationMode.String;

    public void Draw(SimulationFrameState state)
    {
        if (!state.VisualEnabled)
        {
            return;
        }

        var data = state.String.Items;
        if (data.Length == 0)
        {
            return;
        }

        var drawList = ImGui.GetBackgroundDrawList();
        var left = 24.0f;
        var top = 52.0f;
        var right = Math.Max(left + 280.0f, state.ViewportWidth - 24.0f);
        var bottom = Math.Max(top + 220.0f, state.ViewportHeight - 24.0f);
        var rectMin = new Vector2(left, top);
        var rectMax = new Vector2(right, bottom);
        drawList.AddRect(rectMin, rectMax, PackColor(200, 200, 200, 120), 0.0f, ImDrawFlags.None, 1.0f);

        var histogramHeight = 110.0f;
        var rowsBottom = bottom - histogramHeight;
        drawList.AddLine(new Vector2(left, rowsBottom), new Vector2(right, rowsBottom), PackColor(150, 150, 150, 120), 1.0f);

        var lineHeight = 16.0f;
        var availableRows = Math.Max(1, (int)((rowsBottom - top - 8.0f) / lineHeight));
        var stride = Math.Max(1, (int)Math.Ceiling(data.Length / (double)availableRows));

        var drawRow = 0;
        var memory = state.String.MemoryAccess;
        var memoryMax = Math.Max(1, state.String.MemoryAccessMax);
        var memoryDenom = state.Overlay.NormalizeHeatmapByMax ? memoryMax : 16;

        for (var sourceIndex = 0; sourceIndex < data.Length && drawRow < availableRows; sourceIndex += stride)
        {
            var item = data[sourceIndex];
            var y = top + 4.0f + drawRow * lineHeight;
            if (state.Overlay.ShowMemoryHeatmap && sourceIndex < memory.Length)
            {
                var heat = Math.Clamp(memory[sourceIndex] / (float)Math.Max(1, memoryDenom), 0.0f, 1.0f);
                if (heat > 0.01f)
                {
                    drawList.AddRectFilled(
                        new Vector2(left + 2.0f, y - 1.0f),
                        new Vector2(right - 2.0f, y + lineHeight - 1.0f),
                        PackColor(245, 115, 70, (byte)Math.Clamp((int)(20 + heat * 92), 0, 140)),
                        0.0f,
                        ImDrawFlags.None);
                }
            }

            var color = sourceIndex == state.String.HighlightRowA || sourceIndex == state.String.HighlightRowB
                ? PackColor(42, 173, 255, 255)
                : PackColor(235, 235, 235, 230);

            drawList.AddText(new Vector2(left + 8.0f, y), PackColor(125, 125, 125, 220), $"{sourceIndex,5}");
            DrawStringWithCaret(drawList, new Vector2(left + 66.0f, y), item.Text, state.String.CurrentCharIndex, color);
            drawRow++;
        }

        DrawHistogram(drawList, state.String.BucketHistogram, left + 8.0f, rowsBottom + 12.0f, (right - left) - 16.0f, histogramHeight - 20.0f);
    }

    private static void DrawStringWithCaret(ImDrawListPtr drawList, Vector2 origin, string text, int charIndex, uint color)
    {
        if (string.IsNullOrEmpty(text))
        {
            drawList.AddText(origin, color, string.Empty);
            return;
        }

        if (charIndex < 0 || charIndex >= text.Length)
        {
            drawList.AddText(origin, color, text);
            return;
        }

        var prefix = text[..charIndex];
        var marker = text[charIndex].ToString();
        var suffix = charIndex + 1 < text.Length ? text[(charIndex + 1)..] : string.Empty;

        var prefixSize = ImGui.CalcTextSize(prefix);
        var markerSize = ImGui.CalcTextSize(marker);

        drawList.AddText(origin, color, prefix);
        drawList.AddRectFilled(
            new Vector2(origin.X + prefixSize.X - 1.0f, origin.Y - 1.0f),
            new Vector2(origin.X + prefixSize.X + markerSize.X + 1.0f, origin.Y + markerSize.Y + 1.0f),
            PackColor(42, 173, 255, 110),
            1.0f,
            ImDrawFlags.None);
        drawList.AddText(new Vector2(origin.X + prefixSize.X, origin.Y), PackColor(250, 250, 250, 255), marker);
        drawList.AddText(new Vector2(origin.X + prefixSize.X + markerSize.X, origin.Y), color, suffix);
    }

    private static void DrawHistogram(ImDrawListPtr drawList, int[] histogram, float x, float y, float width, float height)
    {
        if (histogram.Length == 0)
        {
            return;
        }

        var bins = Math.Min(histogram.Length, 64);
        var max = Math.Max(1, histogram.Take(bins).Max());
        var barWidth = Math.Max(1.0f, width / bins);

        for (var i = 0; i < bins; i++)
        {
            var value = histogram[i];
            if (value <= 0)
            {
                continue;
            }

            var h = (value / (float)max) * height;
            var x0 = x + i * barWidth;
            var y0 = y + (height - h);
            var x1 = x0 + Math.Max(1.0f, barWidth - 1.0f);
            var y1 = y + height;
            drawList.AddRectFilled(new Vector2(x0, y0), new Vector2(x1, y1), PackColor(210, 210, 210, 180), 0.0f, ImDrawFlags.None);
        }
    }

    public void Dispose()
    {
    }

    private static uint PackColor(byte r, byte g, byte b, byte a)
    {
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }
}
