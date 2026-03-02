namespace SortingVisualizerApp.Core;

public readonly record struct SortStatisticsSnapshot(
    long Comparisons,
    long Swaps,
    long Writes,
    long ProcessedEvents,
    double ElapsedMs,
    double EffectiveEventsPerSecond,
    bool IsRunning,
    bool IsPaused,
    bool IsCompleted,
    long DroppedComparisons = 0,
    long CacheHits = 0,
    long CacheMisses = 0,
    int ParallelQueueDepth = 0,
    int ActiveParallelTasks = 0,
    long BadPartitions = 0,
    double PivotQualityScore = 0.0);
