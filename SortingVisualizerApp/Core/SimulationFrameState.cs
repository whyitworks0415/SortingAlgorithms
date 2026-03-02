using System.Numerics;

namespace SortingVisualizerApp.Core;

public sealed class SimulationFrameState
{
    public required VisualizationMode Mode { get; init; }
    public required bool VisualEnabled { get; init; }
    public required int ViewportWidth { get; init; }
    public required int ViewportHeight { get; init; }
    public required OverlayState Overlay { get; init; }

    public required BarsState Bars { get; init; }
    public required NetworkState Network { get; init; }
    public required ExternalState External { get; init; }
    public required GraphState Graph { get; init; }
    public required StringState String { get; init; }
    public required SpatialState Spatial { get; init; }
}

public sealed class OverlayState
{
    public int IntensityPercent { get; init; } = 80;
    public bool ShowRanges { get; init; } = true;
    public bool ShowPivot { get; init; } = true;
    public bool ShowBuckets { get; init; } = true;
    public bool ShowMemoryHeatmap { get; init; }
    public bool NormalizeHeatmapByMax { get; init; } = true;

    public float Intensity01 => Math.Clamp(IntensityPercent / 100.0f, 0.0f, 1.0f);
}

public sealed class BarsState
{
    public int[] Snapshot { get; init; } = Array.Empty<int>();
    public int Count { get; init; }
    public int MaxValue { get; init; }
    public SortEvent[] RecentEvents { get; init; } = Array.Empty<SortEvent>();
    public int[] MemoryAccess { get; init; } = Array.Empty<int>();
    public int MemoryAccessMax { get; init; }
}

public sealed class NetworkState
{
    public NetworkSchedule? Schedule { get; init; }
    public int WireCount { get; init; }
    public int CurrentStage { get; init; } = -1;
    public IReadOnlySet<long> SwapPairKeys { get; init; } = FrozenPairSet.Empty;
}

public sealed class ExternalState
{
    public ExternalRunSnapshot[] Runs { get; init; } = Array.Empty<ExternalRunSnapshot>();
    public ExternalMergeGroupSnapshot[] ActiveGroups { get; init; } = Array.Empty<ExternalMergeGroupSnapshot>();
}

public readonly record struct ExternalRunSnapshot(
    int RunId,
    int Start,
    int Length,
    int ReadCursor,
    int WriteCursor,
    bool IsOutputRun);

public readonly record struct ExternalMergeGroupSnapshot(
    int GroupId,
    int OutputRunId,
    int[] InputRunIds);

public sealed class GraphState
{
    public GraphNodeSnapshot[] Nodes { get; init; } = Array.Empty<GraphNodeSnapshot>();
    public GraphEdgeSnapshot[] Edges { get; init; } = Array.Empty<GraphEdgeSnapshot>();
    public int SelectedNode { get; init; } = -1;
}

public sealed class StringState
{
    public StringItem[] Items { get; init; } = Array.Empty<StringItem>();
    public int HighlightRowA { get; init; } = -1;
    public int HighlightRowB { get; init; } = -1;
    public int CurrentCharIndex { get; init; } = -1;
    public int[] BucketHistogram { get; init; } = Array.Empty<int>();
    public int[] MemoryAccess { get; init; } = Array.Empty<int>();
    public int MemoryAccessMax { get; init; }
}

public sealed class SpatialState
{
    public SpatialPoint[] Points { get; init; } = Array.Empty<SpatialPoint>();
    public uint[] Keys { get; init; } = Array.Empty<uint>();
    public int[] HighlightedIndices { get; init; } = Array.Empty<int>();
    public bool ShowOrder { get; init; }
    public bool ShowGrid { get; init; }
    public SpatialRegionHighlight? RegionHighlight { get; init; }
    public int[] MemoryAccess { get; init; } = Array.Empty<int>();
    public int MemoryAccessMax { get; init; }
}

public readonly record struct SpatialRegionHighlight(
    float X0,
    float Y0,
    float X1,
    float Y1);

public readonly record struct GraphNodeSnapshot(
    int NodeId,
    Vector2 Position,
    int InDegree,
    bool Emitted);

public readonly record struct GraphEdgeSnapshot(
    int From,
    int To,
    bool Active);

internal static class FrozenPairSet
{
    public static IReadOnlySet<long> Empty { get; } = new HashSet<long>();
}
