using ImGuiNET;
using SortingVisualizerApp.Core;
using SortingVisualizerApp.UI;

namespace SortingVisualizerApp.App;

public sealed partial class VisualizerWindow
{
    private void DrawViewPage()
    {
        PanelTheme.SectionHeader("View Mode");

        var modeNames = new[] { "Bars", "Network", "External", "Graph", "String", "Spatial" };
        var modes = new[]
        {
            VisualizationMode.Bars,
            VisualizationMode.Network,
            VisualizationMode.External,
            VisualizationMode.Graph,
            VisualizationMode.String,
            VisualizationMode.Spatial
        };

        var index = Array.IndexOf(modes, _visualizationMode);
        if (index < 0)
        {
            index = 0;
        }

        PanelTheme.LabeledRow("Mode", () =>
        {
            if (ImGui.Combo("##view-mode", ref index, modeNames, modeNames.Length))
            {
                SetVisualizationMode(modes[Math.Clamp(index, 0, modes.Length - 1)]);
            }
        });

        if (_registry.TryGet(_selectedAlgorithmId, out var meta) && !_visualizationMode.IsSupportedBy(meta.SupportedViews))
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.62f, 0.32f, 1f), "Selected algorithm does not support this view.");
            if (PanelTheme.SecondaryButton("Switch To Supported View", 220))
            {
                EnsureModeCompatibility(meta, setStatus: true);
            }
        }

        PanelTheme.SectionHeader("Visual Settings");
        var visualEnabled = _controls.VisualEnabled;
        if (ImGui.Checkbox("Visual Enabled", ref visualEnabled))
        {
            _controls.VisualEnabled = visualEnabled;
        }
        ImGui.Checkbox("Show HUD", ref _showHudOverlay);
        ImGui.Checkbox("Show Panel", ref _showSidePanel);
        ImGui.Checkbox("Panel Auto-hide (running)", ref _panelAutoHide);
        ImGui.Checkbox("Show Diagnostics", ref _showDiagnostics);

        var detail = (int)_controls.VisualDetail;
        PanelTheme.LabeledRow("Highlight Detail", () =>
        {
            if (PanelTheme.SliderIntWithInput("view-detail", ref detail, 1, 3, "%d", 1, 1))
            {
                _controls.VisualDetail = (DetailLevel)detail;
                if (_controls.LinkDetails)
                {
                    _controls.AudioDetail = _controls.VisualDetail;
                }
            }
        });

        PanelTheme.LabeledRow("Frame Event Budget", () =>
        {
            var budget = _controls.MaxVisualEventsPerFrame;
            if (PanelTheme.SliderIntWithInput("view-frame-budget", ref budget, 128, 4096, "%d", 32, 256))
            {
                _controls.MaxVisualEventsPerFrame = budget;
            }
        });

        PanelTheme.SectionHeader("Overlay");
        PanelTheme.LabeledRow("Overlay Intensity", () =>
        {
            var intensity = _controls.OverlayIntensity;
            if (PanelTheme.SliderIntWithInput("view-overlay-intensity", ref intensity, 0, 100, "%d", 1, 10))
            {
                _controls.OverlayIntensity = intensity;
            }
        });
        var showRanges = _controls.ShowRangesOverlay;
        if (ImGui.Checkbox("Show Ranges", ref showRanges))
        {
            _controls.ShowRangesOverlay = showRanges;
        }

        var showPivot = _controls.ShowPivotOverlay;
        if (ImGui.Checkbox("Show Pivot", ref showPivot))
        {
            _controls.ShowPivotOverlay = showPivot;
        }

        var showBuckets = _controls.ShowBucketsOverlay;
        if (ImGui.Checkbox("Show Buckets", ref showBuckets))
        {
            _controls.ShowBucketsOverlay = showBuckets;
        }

        var showMemoryHeatmap = _controls.ShowMemoryHeatmap;
        if (ImGui.Checkbox("Show Memory Heatmap", ref showMemoryHeatmap))
        {
            _controls.ShowMemoryHeatmap = showMemoryHeatmap;
        }

        var normalizeHeat = _controls.NormalizeHeatmapByMax;
        if (ImGui.Checkbox("Normalize Heatmap By Max", ref normalizeHeat))
        {
            _controls.NormalizeHeatmapByMax = normalizeHeat;
        }

        PanelTheme.LabeledRow("Cache Line", () =>
        {
            var lineSize = _controls.CacheLineSize;
            if (PanelTheme.SliderIntWithInput("view-cache-line", ref lineSize, 4, 256, "%d", 1, 16))
            {
                _controls.CacheLineSize = Math.Max(1, lineSize);
            }
        });

        if (PanelTheme.SecondaryButton("Reset Memory Counter", 220))
        {
            ResetCurrentMemoryCounters();
        }

        PanelTheme.SectionHeader("GPU");
        var gpuEnabled = _controls.GpuAccelerationEnabled;
        if (ImGui.Checkbox("Enable GPU Acceleration", ref gpuEnabled))
        {
            _controls.GpuAccelerationEnabled = gpuEnabled;
        }

        var compareCpuGpu = _controls.CompareCpuGpuTiming;
        if (ImGui.Checkbox("Compare CPU vs GPU Time", ref compareCpuGpu))
        {
            _controls.CompareCpuGpuTiming = compareCpuGpu;
        }

        var showGpuThread = _controls.ShowGpuThreadOverlay;
        if (ImGui.Checkbox("Show GPU Thread Overlay", ref showGpuThread))
        {
            _controls.ShowGpuThreadOverlay = showGpuThread;
        }

        var showBitonicGrid = _controls.ShowGpuBitonicStageGrid;
        if (ImGui.Checkbox("Show Bitonic Stage Grid", ref showBitonicGrid))
        {
            _controls.ShowGpuBitonicStageGrid = showBitonicGrid;
        }

        PanelTheme.SectionHeader("LOD");
        var lodBins = 0;
        var lodAsync = false;
        var lodQueue = 0;
        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            lodBins = _splitBarsRenderer?.LastLodBins ?? 0;
            lodAsync = _splitBarsRenderer?.UsedAsyncLod == true;
            lodQueue = _splitBarsRenderer?.LodQueueDepth ?? 0;
        }
        else if (IsBarsFamilyMode(_visualizationMode))
        {
            lodBins = _barsRenderer?.LastLodBins ?? 0;
            lodAsync = _barsRenderer?.UsedAsyncLod == true;
            lodQueue = _barsRenderer?.LodQueueDepth ?? 0;
        }
        else if (_visualizationMode == VisualizationMode.Spatial)
        {
            lodBins = _spatialRenderer?.LastLodBins ?? 0;
            lodAsync = _spatialRenderer?.UsedAsyncLod == true;
            lodQueue = _spatialRenderer?.LodQueueDepth ?? 0;
        }

        ImGui.TextUnformatted($"LOD bins: {lodBins}");
        ImGui.TextUnformatted($"LOD async: {(lodAsync ? "on" : "off")}");
        ImGui.TextUnformatted($"LOD worker queue: {lodQueue}");
        ImGui.TextUnformatted($"Heatmap effective: {_effectiveShowMemoryHeatmap} (requested={_controls.ShowMemoryHeatmap})");
        ImGui.TextUnformatted($"GPU runtime available: {_gpuSortService?.IsAvailable == true}");
        if (!string.IsNullOrWhiteSpace(_gpuSortService?.LastError))
        {
            ImGui.TextWrapped($"GPU error: {_gpuSortService?.LastError}");
        }
    }
}
