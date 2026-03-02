namespace SortingVisualizerApp.Core;

public sealed record AlgorithmMetadata(
    string Id,
    string Name,
    string Category,
    AlgorithmImplementationStatus Status,
    string Description,
    string AverageComplexity,
    string WorstComplexity,
    bool? Stable,
    SupportedViews SupportedViews = SupportedViews.Bars,
    Func<ISortAlgorithm>? Factory = null,
    Func<IStringSortAlgorithm>? StringFactory = null,
    Func<ISpatialSortAlgorithm>? SpatialFactory = null,
    int? Difficulty = null)
{
    public bool IsImplemented => Factory is not null || StringFactory is not null || SpatialFactory is not null;
}
