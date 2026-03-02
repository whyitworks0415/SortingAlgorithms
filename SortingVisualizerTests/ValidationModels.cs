using SortingVisualizerApp.Core;

internal sealed class ValidationSuiteResult
{
    public required string Name { get; init; }
    public required int Runs { get; init; }
    public required List<string> Failures { get; init; }
    public required List<string> Notes { get; init; }
}

internal sealed class ExecutionLimits
{
    public long MaxEvents { get; init; } = 25_000_000;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(15);
}

internal sealed class AlgorithmExecutionTrace
{
    public required int[] FinalState { get; init; }
    public required bool DoneSeen { get; init; }
    public required long ProcessedEvents { get; init; }
    public required long Comparisons { get; init; }
    public required long Swaps { get; init; }
    public required long Writes { get; init; }
    public required bool TimedOut { get; init; }
    public required bool EventLimitExceeded { get; init; }
    public required string? Error { get; init; }
    public required IReadOnlyList<SortEvent> Events { get; init; }
}
