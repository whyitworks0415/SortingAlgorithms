namespace SortingVisualizerApp.Core;

public readonly record struct SortOptions(
    int MaxValue,
    bool EmitExtendedEvents = true,
    int Parallelism = 1);
