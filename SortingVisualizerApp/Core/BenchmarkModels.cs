namespace SortingVisualizerApp.Core;

public sealed class BenchmarkRequest
{
    public required IReadOnlyList<string> AlgorithmIds { get; init; }
    public int Size { get; init; } = 2048;
    public DistributionPreset Distribution { get; init; } = DistributionPreset.Random;
    public int Seed { get; init; } = 1337;
    public bool HeadlessMode { get; init; } = true;
    public long MaxEvents { get; init; } = 30_000_000;
    public TimeSpan TimeoutPerAlgorithm { get; init; } = TimeSpan.FromSeconds(20);
}

public sealed class BenchmarkResult
{
    public required string AlgorithmId { get; init; }
    public required string AlgorithmName { get; init; }
    public required int Size { get; init; }
    public required DistributionPreset Distribution { get; init; }
    public required int Seed { get; init; }
    public required double ElapsedMs { get; init; }
    public required long Comparisons { get; init; }
    public required long Swaps { get; init; }
    public required long Writes { get; init; }
    public required long ProcessedEvents { get; init; }
    public required bool Sorted { get; init; }
    public required bool MultisetPreserved { get; init; }
    public required bool Completed { get; init; }
    public required string? Error { get; init; }
}

public sealed class BenchmarkSuiteResult
{
    public required DateTime CreatedAtUtc { get; init; }
    public required BenchmarkRequest Request { get; init; }
    public required IReadOnlyList<BenchmarkResult> Results { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
}
