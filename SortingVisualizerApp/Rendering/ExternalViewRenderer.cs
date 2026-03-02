using System.Numerics;
using ImGuiNET;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Rendering;

public sealed class ExternalViewRenderer : IViewRenderer
{
    public VisualizationMode Mode => VisualizationMode.External;

    public void Draw(SimulationFrameState state)
    {
        if (!state.VisualEnabled)
        {
            return;
        }

        var runs = state.External.Runs;
        if (runs.Length == 0)
        {
            return;
        }

        var drawList = ImGui.GetBackgroundDrawList();
        var left = 24.0f;
        var right = Math.Max(left + 160.0f, state.ViewportWidth - 24.0f);
        var top = 52.0f;
        var bottom = Math.Max(top + 160.0f, state.ViewportHeight - 24.0f);

        var rectMin = new Vector2(left, top);
        var rectMax = new Vector2(right, bottom);
        drawList.AddRect(rectMin, rectMax, PackColor(200, 200, 200, 120), 0.0f, ImDrawFlags.None, 1.0f);

        var height = bottom - top;
        var splitY = top + height * 0.42f;
        drawList.AddLine(new Vector2(left, splitY), new Vector2(right, splitY), PackColor(155, 155, 155, 80), 1.0f);

        var totalLength = Math.Max(1, runs.Max(static run => run.Start + run.Length));
        var width = Math.Max(1.0f, right - left);

        foreach (var run in runs)
        {
            var x0 = left + run.Start / (float)totalLength * width;
            var x1 = left + (run.Start + run.Length) / (float)totalLength * width;
            x1 = Math.Max(x1, x0 + 2.0f);

            var y0 = run.IsOutputRun ? splitY + 16.0f : top + 16.0f;
            var y1 = run.IsOutputRun ? bottom - 22.0f : splitY - 16.0f;
            if (y1 <= y0)
            {
                continue;
            }

            drawList.AddRectFilled(new Vector2(x0, y0), new Vector2(x1, y1), PackColor(240, 240, 240, 40), 0.0f, ImDrawFlags.None);
            drawList.AddRect(new Vector2(x0, y0), new Vector2(x1, y1), PackColor(220, 220, 220, 150), 0.0f, ImDrawFlags.None, 1.0f);

            if (run.ReadCursor >= 0)
            {
                var cursorX = x0 + Math.Clamp(run.ReadCursor / (float)Math.Max(1, run.Length), 0.0f, 1.0f) * (x1 - x0);
                drawList.AddLine(new Vector2(cursorX, y0), new Vector2(cursorX, y1), PackColor(175, 175, 175, 200), 1.0f);
            }

            if (run.WriteCursor >= 0)
            {
                var cursorX = x0 + Math.Clamp(run.WriteCursor / (float)Math.Max(1, run.Length), 0.0f, 1.0f) * (x1 - x0);
                drawList.AddLine(new Vector2(cursorX, y0), new Vector2(cursorX, y1), PackColor(42, 173, 255, 230), 2.0f);
            }
        }

        foreach (var group in state.External.ActiveGroups)
        {
            var output = runs.FirstOrDefault(run => run.RunId == group.OutputRunId);
            if (output.RunId != group.OutputRunId)
            {
                continue;
            }

            var outCenterX = left + (output.Start + output.Length * 0.5f) / totalLength * width;
            var outY = splitY + 16.0f;

            for (var i = 0; i < group.InputRunIds.Length; i++)
            {
                var inputId = group.InputRunIds[i];
                var input = runs.FirstOrDefault(run => run.RunId == inputId);
                if (input.RunId != inputId)
                {
                    continue;
                }

                var inCenterX = left + (input.Start + input.Length * 0.5f) / totalLength * width;
                var inY = splitY - 16.0f;
                drawList.AddLine(new Vector2(inCenterX, inY), new Vector2(outCenterX, outY), PackColor(42, 173, 255, 180), 1.2f);
            }
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
