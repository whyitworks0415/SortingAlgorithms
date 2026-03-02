namespace SortingVisualizerApp.Core;

public enum GpuSortKind
{
    None,
    Bitonic,
    RadixLsd
}

public readonly record struct GpuExecutionMetrics(
    GpuSortKind Kind,
    bool UsedGpu,
    double CpuSortMs,
    double UploadMs,
    double DispatchMs,
    double ReadbackMs,
    int DispatchCount,
    int WorkGroupCount,
    int StageCount,
    long GpuMemoryBytes,
    double Progress01,
    string Message)
{
    public static GpuExecutionMetrics Empty => new(
        Kind: GpuSortKind.None,
        UsedGpu: false,
        CpuSortMs: 0,
        UploadMs: 0,
        DispatchMs: 0,
        ReadbackMs: 0,
        DispatchCount: 0,
        WorkGroupCount: 0,
        StageCount: 0,
        GpuMemoryBytes: 0,
        Progress01: 0,
        Message: "idle");
}
