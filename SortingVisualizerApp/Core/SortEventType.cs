namespace SortingVisualizerApp.Core;

public enum SortEventType
{
    Compare,
    Swap,
    Write,
    MarkPivot,
    MarkRange,
    MarkBucket,
    MarkStage,
    MarkRun,
    TrieBuild,
    StageStart,
    StageEnd,
    MergeStart,
    MergeComplete,
    ParallelTaskStart,
    ParallelTaskEnd,
    ParallelQueueDepth,
    PartitionInfo,
    BadPartition,
    MarkStructure,
    Rotation,
    MergeTree,
    LevelHighlight,
    HeapBoundary,

    RunCreated,
    RunRead,
    RunWrite,
    MergeGroup,

    NodeSelected,
    InDegreeDecrement,
    NodeEmitted,

    CharCompare,
    CharIndex,
    BucketMove,
    PassStart,
    PassEnd,
    HistogramUpdate,

    PointKeyComputed,
    PointSwap,
    OrderUpdate,
    RegionHighlight,

    Done
}
