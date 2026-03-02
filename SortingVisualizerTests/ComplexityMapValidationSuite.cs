using SortingVisualizerApp.Core;

internal static class ComplexityMapValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Checks: metadata-only map build, measured metric projection, filter/search behavior, map CSV/JSON export."
        };

        var runs = 0;

        var all = registry.All.Where(static meta => meta.IsImplemented).ToArray();
        runs++;
        var metaOnly = ComplexityMapService.Build(new ComplexityMapBuildRequest
        {
            Algorithms = all,
            MeasuredLookup = new Dictionary<string, ComplexityMeasuredStats>(StringComparer.OrdinalIgnoreCase),
            Filter = new ComplexityMapFilter(),
            XAxis = ComplexityMapXAxisMetric.AvgTimeComplexity,
            YAxis = ComplexityMapYAxisMetric.ExtraMemoryComplexity,
            SizeMetric = ComplexityMapSizeMetric.Fixed,
            DifficultyMode = ComplexityDifficultyMode.Static
        });

        if (metaOnly.Points.Count == 0)
        {
            failures.Add("Meta-only build returned no points.");
        }

        var target = all.FirstOrDefault(static meta => meta.Status == AlgorithmImplementationStatus.A) ?? all.FirstOrDefault();
        if (target is null)
        {
            failures.Add("No implemented algorithms available for complexity map suite.");
            return new ValidationSuiteResult
            {
                Name = "ComplexityMap",
                Runs = runs,
                Failures = failures,
                Notes = notes
            };
        }

        runs++;
        var measuredLookup = new Dictionary<string, ComplexityMeasuredStats>(StringComparer.OrdinalIgnoreCase)
        {
            [target.Id] = new ComplexityMeasuredStats
            {
                AlgorithmId = target.Id,
                SampleCount = 3,
                ElapsedMs = 123.4,
                Comparisons = 50_000,
                Swaps = 12_345,
                Writes = 23_456
            }
        };

        var measuredBuild = ComplexityMapService.Build(new ComplexityMapBuildRequest
        {
            Algorithms = all,
            MeasuredLookup = measuredLookup,
            Filter = new ComplexityMapFilter(),
            XAxis = ComplexityMapXAxisMetric.MeasuredElapsedMs,
            YAxis = ComplexityMapYAxisMetric.Comparisons,
            SizeMetric = ComplexityMapSizeMetric.SwapsPlusWrites,
            DifficultyMode = ComplexityDifficultyMode.Dynamic
        });

        var measuredPoint = measuredBuild.Points.FirstOrDefault(point => string.Equals(point.Metadata.Id, target.Id, StringComparison.OrdinalIgnoreCase));
        if (measuredPoint is null)
        {
            failures.Add("Measured build missing expected algorithm point.");
        }
        else
        {
            if (!measuredPoint.IsMeasured || !measuredPoint.XUsesMeasured)
            {
                failures.Add("Measured metrics were not applied to the expected point.");
            }
        }

        runs++;
        var filterBuild = ComplexityMapService.Build(new ComplexityMapBuildRequest
        {
            Algorithms = all,
            MeasuredLookup = measuredLookup,
            Filter = new ComplexityMapFilter
            {
                Search = target.Name,
                IncludeStatusA = true,
                IncludeStatusB = false
            },
            DifficultyOverrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [target.Id] = 5
            },
            XAxis = ComplexityMapXAxisMetric.AvgTimeComplexity,
            YAxis = ComplexityMapYAxisMetric.ExtraMemoryComplexity,
            SizeMetric = ComplexityMapSizeMetric.Fixed,
            DifficultyMode = ComplexityDifficultyMode.Static
        });

        if (filterBuild.Points.Count == 0)
        {
            failures.Add("Filtered build returned no points.");
        }
        else if (filterBuild.Points.Any(point => !point.Metadata.Name.Contains(target.Name, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add("Search filter did not constrain result set.");
        }

        runs++;
        var diffFiltered = ComplexityMapService.Build(new ComplexityMapBuildRequest
        {
            Algorithms = all,
            MeasuredLookup = measuredLookup,
            Filter = new ComplexityMapFilter
            {
                Search = target.Name,
                DifficultyMin = 5,
                DifficultyMax = 5
            },
            DifficultyOverrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [target.Id] = 5
            },
            XAxis = ComplexityMapXAxisMetric.AvgTimeComplexity,
            YAxis = ComplexityMapYAxisMetric.ExtraMemoryComplexity,
            SizeMetric = ComplexityMapSizeMetric.Fixed,
            DifficultyMode = ComplexityDifficultyMode.Static
        });

        if (diffFiltered.Points.Count == 0 || diffFiltered.Points.Any(static point => point.EffectiveDifficulty != 5))
        {
            failures.Add("Difficulty filter/override behavior is incorrect.");
        }

        runs++;
        var exportRows = measuredBuild.Points.Take(8).Select(point => new ComplexityMapExportRow
        {
            AlgorithmId = point.Metadata.Id,
            Name = point.Metadata.Name,
            Category = point.Metadata.Category,
            Views = point.Metadata.SupportedViews.ToDisplayString(),
            Status = point.Metadata.Status,
            Stable = point.Metadata.Stable,
            XMetric = "Measured elapsed (ms)",
            YMetric = "Comparisons",
            SizeMetric = "Swaps+Writes",
            ColorMode = "Category",
            XValue = point.XRaw,
            YValue = point.YRaw,
            SizeValue = point.SizeRaw,
            DifficultyStatic = point.StaticDifficulty,
            DifficultyDynamic = point.DynamicDifficulty,
            DifficultyEffective = point.EffectiveDifficulty,
            Measured = point.IsMeasured,
            MeasuredElapsedMs = point.Measured?.ElapsedMs,
            MeasuredComparisons = point.Measured?.Comparisons,
            MeasuredSwaps = point.Measured?.Swaps,
            MeasuredWrites = point.Measured?.Writes,
            MeasuredSamples = point.Measured?.SampleCount
        }).ToArray();

        var tmpRoot = Path.Combine(Path.GetTempPath(), "SortingVisualizer_MapSuite_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpRoot);

        try
        {
            var csv = AnalysisExportService.SaveComplexityMapCsv(tmpRoot, exportRows);
            var json = AnalysisExportService.SaveComplexityMapJson(tmpRoot, exportRows);

            if (!File.Exists(csv))
            {
                failures.Add("Complexity map CSV export file was not created.");
            }

            if (!File.Exists(json))
            {
                failures.Add("Complexity map JSON export file was not created.");
            }

            var expectedPathFragment = Path.Combine("analysis", "maps");
            if (!csv.Contains(expectedPathFragment, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Complexity map CSV export path does not target analysis/maps.");
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tmpRoot, recursive: true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        return new ValidationSuiteResult
        {
            Name = "ComplexityMap",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }
}
