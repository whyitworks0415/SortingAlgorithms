namespace SortingVisualizerApp.Core;

public static class ComplexityMapService
{
    public static ComplexityMapBuildResult Build(ComplexityMapBuildRequest request)
    {
        var filtered = request.Algorithms
            .Where(meta => MatchesFilter(meta, request.Filter))
            .ToArray();

        if (filtered.Length == 0)
        {
            return new ComplexityMapBuildResult
            {
                Points = Array.Empty<ComplexityMapPoint>(),
                SourceCount = request.Algorithms.Count,
                FilteredCount = 0,
                MeasuredCount = 0
            };
        }

        var temp = new List<RawPoint>(filtered.Length);
        foreach (var meta in filtered)
        {
            request.MeasuredLookup.TryGetValue(meta.Id, out var measured);

            var complexityNorm = NormalizeAverageComplexity(meta.AverageComplexity);
            var memoryNorm = NormalizeMemoryComplexity(EstimateMemoryComplexity(meta));

            var x = ResolveXRaw(request.XAxis, complexityNorm, measured, out var xMeasured);
            var y = ResolveYRaw(request.YAxis, memoryNorm, complexityNorm, measured, out var yMeasured);
            var size = ResolveSizeRaw(request.SizeMetric, complexityNorm, measured, out var sizeMeasured);

            var staticDifficulty = ResolveStaticDifficulty(meta, request.DifficultyOverrides);
            temp.Add(new RawPoint(meta, measured, x, y, size, xMeasured, yMeasured, sizeMeasured, staticDifficulty, complexityNorm, memoryNorm));
        }

        var elapsedNorm = NormalizeSeries(temp.Select(static item => item.Measured?.ElapsedMs ?? EstimatePseudoElapsed(item.ComplexityNorm)).ToArray(), logScale: true);
        var comparisonsNorm = NormalizeSeries(temp.Select(static item => item.Measured?.Comparisons ?? EstimatePseudoComparisons(item.ComplexityNorm)).ToArray(), logScale: true);
        var writesNorm = NormalizeSeries(temp.Select(static item => item.Measured?.Writes ?? EstimatePseudoWrites(item.ComplexityNorm)).ToArray(), logScale: true);

        var xNorm = NormalizeSeries(temp.Select(static item => item.XRaw).ToArray(), UsesLogScale(request.XAxis));
        var yNorm = NormalizeSeries(temp.Select(static item => item.YRaw).ToArray(), UsesLogScale(request.YAxis));
        var sizeNorm = NormalizeSeries(temp.Select(static item => item.SizeRaw).ToArray(), UsesLogScale(request.SizeMetric));

        var points = new List<ComplexityMapPoint>(temp.Count);
        for (var i = 0; i < temp.Count; i++)
        {
            var raw = temp[i];
            var dynamicRaw = 0.42 * elapsedNorm[i]
                + 0.28 * comparisonsNorm[i]
                + 0.20 * writesNorm[i]
                + 0.10 * raw.MemoryNorm;

            var dynamicDifficulty = Math.Clamp((int)Math.Round(1.0 + dynamicRaw * 4.0, MidpointRounding.AwayFromZero), 1, 5);
            if (raw.Measured is null)
            {
                dynamicDifficulty = raw.StaticDifficulty;
            }

            var effectiveDifficulty = request.DifficultyMode == ComplexityDifficultyMode.Dynamic
                ? dynamicDifficulty
                : raw.StaticDifficulty;

            if (effectiveDifficulty < request.Filter.DifficultyMin || effectiveDifficulty > request.Filter.DifficultyMax)
            {
                continue;
            }

            points.Add(new ComplexityMapPoint
            {
                Metadata = raw.Metadata,
                Measured = raw.Measured,
                XNormalized = xNorm[i],
                YNormalized = yNorm[i],
                SizeNormalized = request.SizeMetric == ComplexityMapSizeMetric.Fixed ? 0.5 : sizeNorm[i],
                XRaw = raw.XRaw,
                YRaw = raw.YRaw,
                SizeRaw = raw.SizeRaw,
                XUsesMeasured = raw.XMeasured,
                YUsesMeasured = raw.YMeasured,
                SizeUsesMeasured = raw.SizeMeasured,
                StaticDifficulty = raw.StaticDifficulty,
                DynamicDifficulty = dynamicDifficulty,
                EffectiveDifficulty = effectiveDifficulty
            });
        }

        return new ComplexityMapBuildResult
        {
            Points = points,
            SourceCount = request.Algorithms.Count,
            FilteredCount = points.Count,
            MeasuredCount = points.Count(static point => point.IsMeasured)
        };
    }

    public static int ResolveStaticDifficulty(AlgorithmMetadata meta, IReadOnlyDictionary<string, int>? overrides)
    {
        if (overrides is not null && overrides.TryGetValue(meta.Id, out var overrideValue))
        {
            return Math.Clamp(overrideValue, 1, 5);
        }

        if (meta.Difficulty.HasValue)
        {
            return Math.Clamp(meta.Difficulty.Value, 1, 5);
        }

        var category = meta.Category.ToLowerInvariant();
        var name = meta.Name.ToLowerInvariant();
        var complexity = meta.AverageComplexity.ToLowerInvariant();

        if (category.Contains("industrial") || category.Contains("advanced"))
        {
            return 5;
        }

        if (category.Contains("parallel") || category.Contains("external") || category.Contains("networks"))
        {
            return 4;
        }

        if (name.Contains("smooth")
            || name.Contains("in-place merge")
            || name.Contains("ips")
            || name.Contains("grail")
            || name.Contains("wiki")
            || name.Contains("flux")
            || name.Contains("pdq"))
        {
            return 5;
        }

        if (complexity.Contains("n^2") || complexity.Contains("(n+1)!") || complexity.Contains("super-polynomial"))
        {
            return category.Contains("inefficient") ? 1 : 2;
        }

        if (complexity.Contains("n log n") || name.Contains("quick") || name.Contains("merge") || name.Contains("heap"))
        {
            return 3;
        }

        return 2;
    }

    public static double NormalizeAverageComplexity(string complexity)
    {
        var text = complexity.ToLowerInvariant();

        if (text.Contains("(n+1)!") || text.Contains("unbounded"))
        {
            return 1.0;
        }

        if (text.Contains("super-polynomial") || text.Contains("n^2.7"))
        {
            return 0.93;
        }

        if (text.Contains("n^2"))
        {
            return 0.82;
        }

        if (text.Contains("n^1"))
        {
            return 0.70;
        }

        if (text.Contains("n log^2"))
        {
            return 0.58;
        }

        if (text.Contains("n log n"))
        {
            return 0.50;
        }

        if (text.Contains("n +"))
        {
            return 0.42;
        }

        if (text.Contains("o(n)") || text.Contains("o(v"))
        {
            return 0.34;
        }

        if (text.Contains("log n"))
        {
            return 0.22;
        }

        if (text.Contains("conceptual") || text == "-")
        {
            return 0.65;
        }

        return 0.56;
    }

    public static string EstimateMemoryComplexity(AlgorithmMetadata meta)
    {
        var n = meta.Name.ToLowerInvariant();
        if (n.Contains("merge")
            || n.Contains("counting")
            || n.Contains("radix")
            || n.Contains("pigeonhole")
            || n.Contains("bucket")
            || n.Contains("flash")
            || n.Contains("string")
            || n.Contains("suffix")
            || n.Contains("external"))
        {
            return "O(n)";
        }

        if (n.Contains("heap")
            || n.Contains("quick")
            || n.Contains("intro")
            || n.Contains("shell")
            || n.Contains("tree")
            || n.Contains("graph")
            || n.Contains("spatial"))
        {
            return "O(log n)";
        }

        return "O(1)";
    }

    public static double NormalizeMemoryComplexity(string memoryComplexity)
    {
        var text = memoryComplexity.ToLowerInvariant();
        if (text.Contains("o(1)"))
        {
            return 0.20;
        }

        if (text.Contains("log n"))
        {
            return 0.45;
        }

        if (text.Contains("o(n)"))
        {
            return 0.82;
        }

        return 0.58;
    }

    private static bool MatchesFilter(AlgorithmMetadata meta, ComplexityMapFilter filter)
    {
        if (!filter.IncludeStatusA && meta.Status == AlgorithmImplementationStatus.A)
        {
            return false;
        }

        if (!filter.IncludeStatusB && meta.Status == AlgorithmImplementationStatus.B)
        {
            return false;
        }

        if ((meta.SupportedViews & filter.ViewMask) == 0)
        {
            return false;
        }

        if (filter.Categories is { Count: > 0 } && !filter.Categories.Contains(meta.Category))
        {
            return false;
        }

        if (filter.StableFilter == ComplexityStableFilter.StableOnly && meta.Stable != true)
        {
            return false;
        }

        if (filter.StableFilter == ComplexityStableFilter.UnstableOnly && meta.Stable != false)
        {
            return false;
        }

        var query = filter.Search?.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            if (!meta.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !meta.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !meta.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static double ResolveXRaw(ComplexityMapXAxisMetric metric, double complexityNorm, ComplexityMeasuredStats? measured, out bool measuredUsed)
    {
        measuredUsed = false;

        return metric switch
        {
            ComplexityMapXAxisMetric.AvgTimeComplexity => complexityNorm,
            ComplexityMapXAxisMetric.MeasuredElapsedMs => ResolveMeasured(measured?.ElapsedMs, EstimatePseudoElapsed(complexityNorm), out measuredUsed),
            ComplexityMapXAxisMetric.Comparisons => ResolveMeasured(measured?.Comparisons, EstimatePseudoComparisons(complexityNorm), out measuredUsed),
            ComplexityMapXAxisMetric.Writes => ResolveMeasured(measured?.Writes, EstimatePseudoWrites(complexityNorm), out measuredUsed),
            _ => complexityNorm
        };
    }

    private static double ResolveYRaw(ComplexityMapYAxisMetric metric, double memoryNorm, double complexityNorm, ComplexityMeasuredStats? measured, out bool measuredUsed)
    {
        measuredUsed = false;

        return metric switch
        {
            ComplexityMapYAxisMetric.ExtraMemoryComplexity => memoryNorm,
            ComplexityMapYAxisMetric.MeasuredElapsedMs => ResolveMeasured(measured?.ElapsedMs, EstimatePseudoElapsed(complexityNorm), out measuredUsed),
            ComplexityMapYAxisMetric.Swaps => ResolveMeasured(measured?.Swaps, EstimatePseudoSwaps(complexityNorm), out measuredUsed),
            ComplexityMapYAxisMetric.Comparisons => ResolveMeasured(measured?.Comparisons, EstimatePseudoComparisons(complexityNorm), out measuredUsed),
            _ => memoryNorm
        };
    }

    private static double ResolveSizeRaw(ComplexityMapSizeMetric metric, double complexityNorm, ComplexityMeasuredStats? measured, out bool measuredUsed)
    {
        measuredUsed = false;
        return metric switch
        {
            ComplexityMapSizeMetric.Fixed => 1.0,
            ComplexityMapSizeMetric.ElapsedMs => ResolveMeasured(measured?.ElapsedMs, EstimatePseudoElapsed(complexityNorm), out measuredUsed),
            ComplexityMapSizeMetric.SwapsPlusWrites => ResolveMeasured(
                measured is null ? null : measured.Swaps + measured.Writes,
                EstimatePseudoSwaps(complexityNorm) + EstimatePseudoWrites(complexityNorm),
                out measuredUsed),
            _ => 1.0
        };
    }

    private static double ResolveMeasured(double? measuredValue, double fallback, out bool measuredUsed)
    {
        if (measuredValue.HasValue && measuredValue.Value > 0.0)
        {
            measuredUsed = true;
            return measuredValue.Value;
        }

        measuredUsed = false;
        return fallback;
    }

    private static bool UsesLogScale(ComplexityMapXAxisMetric metric)
    {
        return metric != ComplexityMapXAxisMetric.AvgTimeComplexity;
    }

    private static bool UsesLogScale(ComplexityMapYAxisMetric metric)
    {
        return metric != ComplexityMapYAxisMetric.ExtraMemoryComplexity;
    }

    private static bool UsesLogScale(ComplexityMapSizeMetric metric)
    {
        return metric != ComplexityMapSizeMetric.Fixed;
    }

    private static double[] NormalizeSeries(double[] values, bool logScale)
    {
        if (values.Length == 0)
        {
            return values;
        }

        var transformed = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            transformed[i] = logScale ? Math.Log10(Math.Max(1.0, values[i]) + 1.0) : values[i];
        }

        var min = transformed.Min();
        var max = transformed.Max();
        var range = Math.Max(1e-9, max - min);

        var normalized = new double[values.Length];
        for (var i = 0; i < transformed.Length; i++)
        {
            normalized[i] = Math.Clamp((transformed[i] - min) / range, 0.0, 1.0);
        }

        return normalized;
    }

    private static double EstimatePseudoElapsed(double complexityNorm)
    {
        return 0.3 + complexityNorm * 1800.0;
    }

    private static double EstimatePseudoComparisons(double complexityNorm)
    {
        return 50.0 + complexityNorm * 8_000_000.0;
    }

    private static double EstimatePseudoSwaps(double complexityNorm)
    {
        return 20.0 + complexityNorm * 2_500_000.0;
    }

    private static double EstimatePseudoWrites(double complexityNorm)
    {
        return 30.0 + complexityNorm * 4_000_000.0;
    }

    private sealed record RawPoint(
        AlgorithmMetadata Metadata,
        ComplexityMeasuredStats? Measured,
        double XRaw,
        double YRaw,
        double SizeRaw,
        bool XMeasured,
        bool YMeasured,
        bool SizeMeasured,
        int StaticDifficulty,
        double ComplexityNorm,
        double MemoryNorm);
}
