namespace SortingVisualizerApp.Core;

public enum ComplexityMapXAxisMetric
{
    AvgTimeComplexity = 0,
    MeasuredElapsedMs = 1,
    Comparisons = 2,
    Writes = 3
}

public enum ComplexityMapYAxisMetric
{
    ExtraMemoryComplexity = 0,
    MeasuredElapsedMs = 1,
    Swaps = 2,
    Comparisons = 3
}

public enum ComplexityMapColorMode
{
    Category = 0,
    SupportedView = 1,
    Status = 2
}

public enum ComplexityMapSizeMetric
{
    Fixed = 0,
    ElapsedMs = 1,
    SwapsPlusWrites = 2
}

public enum ComplexityDifficultyMode
{
    Static = 0,
    Dynamic = 1
}

public enum ComplexityStableFilter
{
    All = 0,
    StableOnly = 1,
    UnstableOnly = 2
}

public sealed class ComplexityMeasuredStats
{
    public required string AlgorithmId { get; init; }
    public required int SampleCount { get; init; }
    public required double ElapsedMs { get; init; }
    public required double Comparisons { get; init; }
    public required double Swaps { get; init; }
    public required double Writes { get; init; }
}

public sealed class ComplexityMapFilter
{
    public string Search { get; init; } = string.Empty;
    public IReadOnlySet<string>? Categories { get; init; }
    public SupportedViews ViewMask { get; init; } = SupportedViews.All;
    public bool IncludeStatusA { get; init; } = true;
    public bool IncludeStatusB { get; init; } = true;
    public ComplexityStableFilter StableFilter { get; init; } = ComplexityStableFilter.All;
    public int DifficultyMin { get; init; } = 1;
    public int DifficultyMax { get; init; } = 5;
}

public sealed class ComplexityMapBuildRequest
{
    public required IReadOnlyList<AlgorithmMetadata> Algorithms { get; init; }
    public required IReadOnlyDictionary<string, ComplexityMeasuredStats> MeasuredLookup { get; init; }
    public required ComplexityMapFilter Filter { get; init; }
    public IReadOnlyDictionary<string, int>? DifficultyOverrides { get; init; }
    public ComplexityMapXAxisMetric XAxis { get; init; } = ComplexityMapXAxisMetric.AvgTimeComplexity;
    public ComplexityMapYAxisMetric YAxis { get; init; } = ComplexityMapYAxisMetric.ExtraMemoryComplexity;
    public ComplexityMapSizeMetric SizeMetric { get; init; } = ComplexityMapSizeMetric.Fixed;
    public ComplexityDifficultyMode DifficultyMode { get; init; } = ComplexityDifficultyMode.Static;
}

public sealed class ComplexityMapPoint
{
    public required AlgorithmMetadata Metadata { get; init; }
    public required ComplexityMeasuredStats? Measured { get; init; }
    public required double XNormalized { get; init; }
    public required double YNormalized { get; init; }
    public required double SizeNormalized { get; init; }
    public required double XRaw { get; init; }
    public required double YRaw { get; init; }
    public required double SizeRaw { get; init; }
    public required bool XUsesMeasured { get; init; }
    public required bool YUsesMeasured { get; init; }
    public required bool SizeUsesMeasured { get; init; }
    public required int StaticDifficulty { get; init; }
    public required int DynamicDifficulty { get; init; }
    public required int EffectiveDifficulty { get; init; }
    public bool IsMeasured => Measured is not null;
}

public sealed class ComplexityMapBuildResult
{
    public required IReadOnlyList<ComplexityMapPoint> Points { get; init; }
    public required int SourceCount { get; init; }
    public required int FilteredCount { get; init; }
    public required int MeasuredCount { get; init; }
}

public sealed class ComplexityMapExportRow
{
    public required string AlgorithmId { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Views { get; init; }
    public required AlgorithmImplementationStatus Status { get; init; }
    public required bool? Stable { get; init; }
    public required string XMetric { get; init; }
    public required string YMetric { get; init; }
    public required string SizeMetric { get; init; }
    public required string ColorMode { get; init; }
    public required double XValue { get; init; }
    public required double YValue { get; init; }
    public required double SizeValue { get; init; }
    public required int DifficultyStatic { get; init; }
    public required int DifficultyDynamic { get; init; }
    public required int DifficultyEffective { get; init; }
    public required bool Measured { get; init; }
    public double? MeasuredElapsedMs { get; init; }
    public double? MeasuredComparisons { get; init; }
    public double? MeasuredSwaps { get; init; }
    public double? MeasuredWrites { get; init; }
    public int? MeasuredSamples { get; init; }
}
