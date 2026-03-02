using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Rendering;

public sealed class BarsViewRenderer : IViewRenderer
{
    private readonly BarRenderer _barRenderer = new();

    public VisualizationMode Mode => VisualizationMode.Bars;
    public int LastLodBins => _barRenderer.LastLodBins;
    public bool UsedAsyncLod => _barRenderer.UsedAsyncLod;
    public int LodQueueDepth => _barRenderer.LodQueueDepth;
    public float LastBarWidthPx => _barRenderer.LastBarWidthPx;
    public float LastNominalBarWidthPx => _barRenderer.LastNominalBarWidthPx;
    public BarsRenderMode LastRenderMode => _barRenderer.LastRenderMode;
    public int LastVisibleCount => _barRenderer.LastVisibleCount;

    public void Draw(SimulationFrameState state)
    {
        _barRenderer.Render(
            state.Bars.Snapshot,
            state.Bars.Count,
            state.Bars.MaxValue,
            state.Bars.MemoryAccess,
            state.Bars.MemoryAccessMax,
            state.Bars.RecentEvents,
            state.ViewportWidth,
            state.ViewportHeight,
            state.VisualEnabled,
            state.Overlay.Intensity01,
            state.Overlay.ShowRanges,
            state.Overlay.ShowPivot,
            state.Overlay.ShowBuckets,
            state.Overlay.ShowMemoryHeatmap,
            state.Overlay.NormalizeHeatmapByMax);
    }

    public void Dispose()
    {
        _barRenderer.Dispose();
    }
}
