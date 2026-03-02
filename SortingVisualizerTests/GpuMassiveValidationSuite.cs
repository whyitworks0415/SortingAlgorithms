using SortingVisualizerApp.Algorithms;
using SortingVisualizerApp.Core;
using SortingVisualizerApp.Gpu;

internal static class GpuMassiveValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: GPU-labeled algorithms with CPU-safe fallback event streams.",
            "Checks: GPU algorithm output equals CPU reference and N=1,000,000 smoke run completes.",
            "Fallback: invalid shader path should report unavailable without crash."
        };

        var runs = 0;

        runs += ValidateGpuLabeledAlgorithms(registry, failures);
        runs += ValidateMassiveSmoke(failures);
        runs += ValidateFallbackPath(failures);

        return new ValidationSuiteResult
        {
            Name = "GPU+Massive",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static int ValidateGpuLabeledAlgorithms(AlgorithmRegistry registry, ICollection<string> failures)
    {
        var runs = 0;
        var candidates = registry.All
            .Where(static meta => meta.Status == AlgorithmImplementationStatus.A
                                  && (meta.SupportedViews & SupportedViews.Bars) != 0
                                  && meta.Factory is not null
                                  && meta.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var meta in candidates)
        {
            foreach (var distribution in new[] { DistributionPreset.Random, DistributionPreset.NearlySorted, DistributionPreset.Reversed })
            {
                runs++;
                const int size = 2048;
                var seed = HashCode.Combine(meta.Id, distribution, 20260301);
                var source = DataGenerator.Generate(size, distribution, seed);
                var expected = source.ToArray();
                Array.Sort(expected);

                var trace = ExecutionHarness.RunAlgorithm(
                    meta.Factory!.Invoke(),
                    source,
                    new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true, Parallelism: 1),
                    new ExecutionLimits
                    {
                        MaxEvents = 8_000_000,
                        Timeout = TimeSpan.FromSeconds(12)
                    },
                    captureEvents: false);

                if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded)
                {
                    failures.Add($"{meta.Name}/{distribution}: execution failed.");
                    continue;
                }

                if (!trace.FinalState.AsSpan().SequenceEqual(expected))
                {
                    failures.Add($"{meta.Name}/{distribution}: output mismatch vs CPU reference.");
                }
            }
        }

        return runs;
    }

    private static int ValidateMassiveSmoke(ICollection<string> failures)
    {
        const int size = 1_000_000;
        var seed = 260301;
        var source = DataGenerator.Generate(size, DistributionPreset.Random, seed);

        var algorithm = new GpuRadixLsdSortAlgorithm();
        var trace = ExecutionHarness.RunAlgorithm(
            algorithm,
            source,
            new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true, Parallelism: 1),
            new ExecutionLimits
            {
                MaxEvents = 1_600_000,
                Timeout = TimeSpan.FromSeconds(45)
            },
            captureEvents: false);

        if (trace.Error is not null)
        {
            failures.Add($"Massive N smoke: error {trace.Error}");
        }
        else if (trace.TimedOut)
        {
            failures.Add("Massive N smoke: timeout at N=1,000,000.");
        }
        else if (trace.EventLimitExceeded)
        {
            failures.Add("Massive N smoke: event limit exceeded at N=1,000,000.");
        }
        else if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
        {
            failures.Add("Massive N smoke: output not sorted at N=1,000,000.");
        }

        return 1;
    }

    private static int ValidateFallbackPath(ICollection<string> failures)
    {
        using var service = new GpuSortService(Path.Combine(Path.GetTempPath(), "missing-shaders-for-sv-app"));
        var available = service.Initialize();
        if (available)
        {
            failures.Add("GPU fallback: unexpected available state for missing shader path.");
        }

        if (string.IsNullOrWhiteSpace(service.LastError))
        {
            failures.Add("GPU fallback: missing shader path should provide error message.");
        }

        return 1;
    }
}
