using ImGuiNET;
using SortingVisualizerApp.Core;
using SortingVisualizerApp.UI;

namespace SortingVisualizerApp.App;

public sealed partial class VisualizerWindow
{
    private void DrawAnalysisPage()
    {
        var stats = GetCurrentStats();
        PanelTheme.SectionHeader("Realtime Stats");
        ImGui.TextUnformatted($"FPS: {_fps:0.0}");
        ImGui.TextUnformatted($"Elapsed: {stats.ElapsedMs:0.0} ms");
        ImGui.TextUnformatted($"Comparisons: {stats.Comparisons:N0}");
        ImGui.TextUnformatted($"Swaps: {stats.Swaps:N0}");
        ImGui.TextUnformatted($"Writes: {stats.Writes:N0}");
        ImGui.TextUnformatted($"Dropped compares: {stats.DroppedComparisons:N0}");
        ImGui.TextUnformatted($"Events/sec: {stats.EffectiveEventsPerSecond:0.0}");
        ImGui.TextUnformatted($"Cache hit/miss: {stats.CacheHits:N0}/{stats.CacheMisses:N0}");
        ImGui.TextUnformatted($"Parallel queue/tasks: {stats.ParallelQueueDepth}/{stats.ActiveParallelTasks}");
        ImGui.TextUnformatted($"Bad partitions: {stats.BadPartitions:N0}, Pivot quality: {stats.PivotQualityScore:0.000}");
        ImGui.TextUnformatted($"GPU used: {_lastGpuMetrics.UsedGpu} ({_lastGpuMetrics.Kind})");
        ImGui.TextUnformatted($"CPU sort: {_lastGpuMetrics.CpuSortMs:0.00} ms");
        ImGui.TextUnformatted($"GPU upload/dispatch/readback: {_lastGpuMetrics.UploadMs:0.00}/{_lastGpuMetrics.DispatchMs:0.00}/{_lastGpuMetrics.ReadbackMs:0.00} ms");
        ImGui.TextUnformatted($"GPU memory: {_lastGpuMetrics.GpuMemoryBytes / (1024.0 * 1024.0):0.00} MiB");
        ImGui.ProgressBar((float)Math.Clamp(_gpuProgress, 0.0, 1.0), new System.Numerics.Vector2(180, 0), $"{Math.Clamp(_gpuProgress, 0.0, 1.0) * 100.0:0}%");

        PanelTheme.SectionHeader("Side-by-Side Comparison");
        DrawComparisonSection();
        DrawComparisonHistoryTable();

        PanelTheme.SectionHeader("Benchmark");
        DrawBenchmarkPanel();

        PanelTheme.SectionHeader("Complexity Map");
        DrawComplexityMapSection();

        PanelTheme.SectionHeader("Growth / Summary");
        DrawAnalysisSection();
    }

    private void DrawBenchmarkPanel()
    {
        var benchmarkSeed = _benchmarkSeed;
        if (ImGui.InputInt("Benchmark seed", ref benchmarkSeed))
        {
            _benchmarkSeed = benchmarkSeed;
        }

        ImGui.Checkbox("Use favorites", ref _benchmarkUseFavorites);
        ImGui.Checkbox("Headless", ref _benchmarkHeadless);

        var running = _benchmarkTask is not null;
        if (!running)
        {
            if (PanelTheme.SecondaryButton("Run Benchmark", 160))
            {
                StartBenchmark();
            }
        }
        else
        {
            if (PanelTheme.SecondaryButton("Cancel Benchmark", 160))
            {
                _benchmarkCts?.Cancel();
            }
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Export Benchmark CSV", 190))
        {
            ExportBenchmarkCsv();
        }

        ImGui.TextWrapped(_benchmarkStatusText);
        if (_lastBenchmarkSuite is not null)
        {
            ImGui.TextUnformatted($"Rows: {_lastBenchmarkSuite.Results.Count}, warnings: {_lastBenchmarkSuite.Warnings.Count}");
            if (!string.IsNullOrWhiteSpace(_lastBenchmarkCsvPath))
            {
                ImGui.TextWrapped($"Last CSV: {_lastBenchmarkCsvPath}");
            }
        }
    }

    private void DrawComparisonHistoryTable()
    {
        if (_comparisonHistory.Count == 0)
        {
            ImGui.TextDisabled("No comparison records yet.");
            return;
        }

        if (!ImGui.BeginTable("comparison-history", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new System.Numerics.Vector2(0, 160)))
        {
            return;
        }

        ImGui.TableSetupColumn("Time");
        ImGui.TableSetupColumn("Left");
        ImGui.TableSetupColumn("Right");
        ImGui.TableSetupColumn("N/Dist");
        ImGui.TableSetupColumn("Left ms");
        ImGui.TableSetupColumn("Right ms");
        ImGui.TableHeadersRow();

        foreach (var row in _comparisonHistory.TakeLast(64).Reverse())
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(row.CreatedAtUtc.ToLocalTime().ToString("HH:mm:ss"));
            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(row.LeftAlgorithmName);
            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(row.RightAlgorithmName);
            ImGui.TableSetColumnIndex(3);
            ImGui.TextUnformatted($"{row.Size} / {row.Distribution}");
            ImGui.TableSetColumnIndex(4);
            ImGui.TextUnformatted($"{row.Left.ElapsedMs:0.0}");
            ImGui.TableSetColumnIndex(5);
            ImGui.TextUnformatted($"{row.Right.ElapsedMs:0.0}");
        }

        ImGui.EndTable();
    }
}
