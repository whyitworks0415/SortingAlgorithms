namespace SortingVisualizerApp.Core;

public sealed class ComparisonAnalysisRecord
{
    public required DateTime CreatedAtUtc { get; init; }
    public required string LeftAlgorithmId { get; init; }
    public required string LeftAlgorithmName { get; init; }
    public required string RightAlgorithmId { get; init; }
    public required string RightAlgorithmName { get; init; }
    public required int Size { get; init; }
    public required DistributionPreset Distribution { get; init; }
    public required int Seed { get; init; }
    public required ComparisonSideSnapshot Left { get; init; }
    public required ComparisonSideSnapshot Right { get; init; }
}

public sealed class ComparisonSideSnapshot
{
    public required long Comparisons { get; init; }
    public required long Swaps { get; init; }
    public required long Writes { get; init; }
    public required long ProcessedEvents { get; init; }
    public required double ElapsedMs { get; init; }
    public required bool Completed { get; init; }
}
