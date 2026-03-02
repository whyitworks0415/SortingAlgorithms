using System.Numerics;
using ImGuiNET;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Rendering;

public sealed class NetworkViewRenderer : IViewRenderer
{
    public VisualizationMode Mode => VisualizationMode.Network;

    public void Draw(SimulationFrameState state)
    {
        if (!state.VisualEnabled)
        {
            return;
        }

        var network = state.Network;
        if (network.WireCount <= 1)
        {
            return;
        }

        var drawList = ImGui.GetBackgroundDrawList();
        var margin = 24.0f;
        var topOffset = 52.0f;
        var left = margin;
        var right = Math.Max(left + 120.0f, state.ViewportWidth - margin);
        var top = topOffset;
        var bottom = Math.Max(top + 120.0f, state.ViewportHeight - margin);

        var rectMin = new Vector2(left, top);
        var rectMax = new Vector2(right, bottom);
        drawList.AddRect(rectMin, rectMax, PackColor(200, 200, 200, 120), 0.0f, ImDrawFlags.None, 1.0f);

        var wireCount = Math.Max(2, network.WireCount);
        var plotWidth = Math.Max(1.0f, right - left);
        var plotHeight = Math.Max(1.0f, bottom - top);
        var wireStep = plotHeight / Math.Max(1, wireCount - 1);

        for (var wire = 0; wire < wireCount; wire++)
        {
            var y = top + wire * wireStep;
            drawList.AddLine(new Vector2(left, y), new Vector2(right, y), PackColor(160, 160, 160, 65), 1.0f);
        }

        var schedule = network.Schedule;
        if (schedule is null || schedule.StageCount == 0)
        {
            return;
        }

        var stageCount = schedule.StageCount;
        var currentStage = Math.Clamp(network.CurrentStage, 0, stageCount - 1);
        var stageToX = (Func<int, float>)(stage =>
        {
            if (stageCount <= 1)
            {
                return left;
            }

            return left + (stage / (float)(stageCount - 1)) * plotWidth;
        });

        var maxGuideLines = Math.Max(32, (int)(plotWidth / 4.0f));
        var guideStride = Math.Max(1, stageCount / maxGuideLines);
        for (var stage = 0; stage < stageCount; stage += guideStride)
        {
            var x = stageToX(stage);
            drawList.AddLine(new Vector2(x, top), new Vector2(x, bottom), PackColor(120, 120, 120, 35), 1.0f);
        }

        var totalComparators = 0;
        for (var i = 0; i < schedule.Stages.Count; i++)
        {
            totalComparators += schedule.Stages[i].Comparators.Count;
        }

        if (totalComparators <= 35_000 && stageCount <= 2_500)
        {
            DrawStageRange(drawList, schedule, 0, stageCount - 1, left, top, plotWidth, plotHeight, wireCount, PackColor(180, 180, 180, 55));
        }
        else
        {
            var window = Math.Max(12, (int)(plotWidth / 6.0f));
            var from = Math.Max(0, currentStage - window);
            var to = Math.Min(stageCount - 1, currentStage + window);
            DrawStageRange(drawList, schedule, from, to, left, top, plotWidth, plotHeight, wireCount, PackColor(180, 180, 180, 75));
        }

        var currentX = stageToX(currentStage);
        drawList.AddLine(new Vector2(currentX, top), new Vector2(currentX, bottom), PackColor(42, 173, 255, 230), 2.0f);

        var stageComparators = schedule.Stages[currentStage].Comparators;
        for (var i = 0; i < stageComparators.Count; i++)
        {
            var pair = stageComparators[i];
            if (pair.I < 0 || pair.I >= wireCount || pair.J < 0 || pair.J >= wireCount)
            {
                continue;
            }

            var y0 = top + pair.I * wireStep;
            var y1 = top + pair.J * wireStep;
            var key = PairKey(pair.I, pair.J);
            var color = network.SwapPairKeys.Contains(key)
                ? PackColor(42, 173, 255, 255)
                : PackColor(230, 230, 230, 220);
            drawList.AddLine(new Vector2(currentX, y0), new Vector2(currentX, y1), color, 2.0f);
        }
    }

    private static void DrawStageRange(
        ImDrawListPtr drawList,
        NetworkSchedule schedule,
        int fromStage,
        int toStage,
        float left,
        float top,
        float plotWidth,
        float plotHeight,
        int wireCount,
        uint color)
    {
        if (fromStage > toStage)
        {
            return;
        }

        var stageCount = schedule.StageCount;
        var wireStep = plotHeight / Math.Max(1, wireCount - 1);

        for (var stage = fromStage; stage <= toStage; stage++)
        {
            var x = stageCount <= 1
                ? left
                : left + (stage / (float)(stageCount - 1)) * plotWidth;
            var comparators = schedule.Stages[stage].Comparators;
            for (var i = 0; i < comparators.Count; i++)
            {
                var pair = comparators[i];
                if (pair.I < 0 || pair.I >= wireCount || pair.J < 0 || pair.J >= wireCount)
                {
                    continue;
                }

                var y0 = top + pair.I * wireStep;
                var y1 = top + pair.J * wireStep;
                drawList.AddLine(new Vector2(x, y0), new Vector2(x, y1), color, 1.0f);
            }
        }
    }

    public void Dispose()
    {
    }

    private static long PairKey(int i, int j)
    {
        if (i > j)
        {
            (i, j) = (j, i);
        }

        return ((long)i << 32) | (uint)j;
    }

    private static uint PackColor(byte r, byte g, byte b, byte a)
    {
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }
}
