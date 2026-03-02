using OpenTK.Graphics.OpenGL4;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Rendering;

public sealed class SplitBarsRenderer : IDisposable
{
    private readonly BarRenderer _leftRenderer = new();
    private readonly BarRenderer _rightRenderer = new();

    public int LastLodBins => _leftRenderer.LastLodBins + _rightRenderer.LastLodBins;
    public bool UsedAsyncLod => _leftRenderer.UsedAsyncLod || _rightRenderer.UsedAsyncLod;
    public int LodQueueDepth => _leftRenderer.LodQueueDepth + _rightRenderer.LodQueueDepth;
    public float LastBarWidthPx => (_leftRenderer.LastBarWidthPx + _rightRenderer.LastBarWidthPx) * 0.5f;
    public float LastNominalBarWidthPx => (_leftRenderer.LastNominalBarWidthPx + _rightRenderer.LastNominalBarWidthPx) * 0.5f;
    public int LastVisibleCount => _leftRenderer.LastVisibleCount + _rightRenderer.LastVisibleCount;
    public BarsRenderMode LastRenderMode =>
        _leftRenderer.LastRenderMode == BarsRenderMode.BarsLOD || _rightRenderer.LastRenderMode == BarsRenderMode.BarsLOD
            ? BarsRenderMode.BarsLOD
            : BarsRenderMode.BarsRaw;

    public void Draw(
        int[] leftData,
        int leftCount,
        int leftMaxValue,
        int[] leftMemoryAccess,
        int leftMemoryAccessMax,
        ReadOnlySpan<SortEvent> leftEvents,
        int[] rightData,
        int rightCount,
        int rightMaxValue,
        int[] rightMemoryAccess,
        int rightMemoryAccessMax,
        ReadOnlySpan<SortEvent> rightEvents,
        int viewportWidth,
        int viewportHeight,
        bool visualEnabled,
        float overlayIntensity,
        bool showRanges,
        bool showPivot,
        bool showBuckets,
        bool showMemoryHeatmap,
        bool normalizeHeatmapByMax)
    {
        if (!visualEnabled || viewportWidth <= 1 || viewportHeight <= 1)
        {
            return;
        }

        var leftWidth = viewportWidth / 2;
        var rightWidth = Math.Max(1, viewportWidth - leftWidth);

        GL.Viewport(0, 0, leftWidth, viewportHeight);
        _leftRenderer.Render(leftData, leftCount, leftMaxValue, leftMemoryAccess, leftMemoryAccessMax, leftEvents, leftWidth, viewportHeight, visualEnabled, overlayIntensity, showRanges, showPivot, showBuckets, showMemoryHeatmap, normalizeHeatmapByMax);

        GL.Viewport(leftWidth, 0, rightWidth, viewportHeight);
        _rightRenderer.Render(rightData, rightCount, rightMaxValue, rightMemoryAccess, rightMemoryAccessMax, rightEvents, rightWidth, viewportHeight, visualEnabled, overlayIntensity, showRanges, showPivot, showBuckets, showMemoryHeatmap, normalizeHeatmapByMax);

        GL.Viewport(0, 0, viewportWidth, viewportHeight);
    }

    public void Dispose()
    {
        _leftRenderer.Dispose();
        _rightRenderer.Dispose();
    }
}
