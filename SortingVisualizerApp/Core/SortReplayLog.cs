namespace SortingVisualizerApp.Core;

public sealed record SortReplayLog(
    string AlgorithmId,
    DateTime CreatedUtc,
    int[] InitialData,
    SortEvent[] Events);
