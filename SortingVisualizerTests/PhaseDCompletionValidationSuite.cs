using SortingVisualizerApp.Core;

internal static class PhaseDCompletionValidationSuite
{
    private static readonly (string Name, string Category, AlgorithmImplementationStatus Status)[] Required =
    {
        ("Distributed Sample Sort", "Industrial", AlgorithmImplementationStatus.B),
        ("Hadoop Sort", "Industrial", AlgorithmImplementationStatus.B),
        ("TeraSort", "Industrial", AlgorithmImplementationStatus.B),

        ("Bogo Sort", "Inefficient", AlgorithmImplementationStatus.B),
        ("Bozo Sort", "Inefficient", AlgorithmImplementationStatus.B),
        ("Miracle Sort", "Inefficient", AlgorithmImplementationStatus.B),
        ("Quantum Bogo", "Inefficient", AlgorithmImplementationStatus.B),
        ("Sleep Sort", "Inefficient", AlgorithmImplementationStatus.B),
        ("Stalin Sort", "Inefficient", AlgorithmImplementationStatus.B),

        ("Hypercube Sort", "Parallel", AlgorithmImplementationStatus.A),
        ("Odd-Even Transposition (Parallel)", "Parallel", AlgorithmImplementationStatus.A),
        ("Parallel Heap Sort", "Parallel", AlgorithmImplementationStatus.A),
        ("Parallel Merge", "Parallel", AlgorithmImplementationStatus.A),
        ("Parallel Quick", "Parallel", AlgorithmImplementationStatus.A),
        ("Sample Sort", "Parallel", AlgorithmImplementationStatus.A),

        ("Burst Sort", "String", AlgorithmImplementationStatus.A),
        ("Spatial Sort", "Spatial", AlgorithmImplementationStatus.A)
    };

    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: Phase-D Industrial/Inefficient/Parallel/Remaining completion set.",
            "Checks: registry presence/category/status, registry count>=60, A correctness smoke, B concept event consistency."
        };

        if (registry.All.Count < 60)
        {
            failures.Add($"Registry size check failed: expected >= 60 algorithms, actual={registry.All.Count}.");
        }

        var map = new Dictionary<string, AlgorithmMetadata>(StringComparer.OrdinalIgnoreCase);
        foreach (var req in Required)
        {
            var meta = registry.All.FirstOrDefault(item => item.Name.Equals(req.Name, StringComparison.OrdinalIgnoreCase));
            if (meta is null)
            {
                failures.Add($"Missing required algorithm: {req.Name}");
                continue;
            }

            if (!meta.Category.Equals(req.Category, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{req.Name}: category mismatch (expected={req.Category}, actual={meta.Category}).");
            }

            if (meta.Status != req.Status)
            {
                failures.Add($"{req.Name}: status mismatch (expected={req.Status}, actual={meta.Status}).");
            }

            map[req.Name] = meta;
        }

        var runs = 0;

        // A correctness smoke for newly added parallel A entries.
        foreach (var name in new[]
                 {
                     "Hypercube Sort",
                     "Odd-Even Transposition (Parallel)",
                     "Parallel Heap Sort",
                     "Parallel Merge",
                     "Parallel Quick",
                     "Sample Sort"
                 })
        {
            if (!map.TryGetValue(name, out var meta) || meta.Factory is null)
            {
                continue;
            }

            foreach (var n in new[] { 256, 2048 })
            {
                runs++;
                var source = DataGenerator.Generate(n, DistributionPreset.Random, HashCode.Combine(meta.Id, n, 260501));
                var baseline = ExecutionHarness.BuildMultiset(source);
                var trace = ExecutionHarness.RunAlgorithm(
                    meta.Factory.Invoke(),
                    source,
                    new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true, Parallelism: Math.Clamp(Environment.ProcessorCount, 1, 8)),
                    new ExecutionLimits
                    {
                        MaxEvents = n >= 2048 ? 45_000_000 : 10_000_000,
                        Timeout = n >= 2048 ? TimeSpan.FromSeconds(20) : TimeSpan.FromSeconds(8)
                    },
                    captureEvents: false);

                if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
                {
                    failures.Add($"{meta.Name}/N={n}: execution failed.");
                    continue;
                }

                if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
                {
                    failures.Add($"{meta.Name}/N={n}: output not sorted.");
                }

                if (!ExecutionHarness.MultisetEquals(baseline, ExecutionHarness.BuildMultiset(trace.FinalState)))
                {
                    failures.Add($"{meta.Name}/N={n}: multiset changed.");
                }
            }
        }

        // B concept event consistency for industrial/inefficient entries.
        foreach (var name in new[]
                 {
                     "Distributed Sample Sort", "Hadoop Sort", "TeraSort",
                     "Bogo Sort", "Bozo Sort", "Miracle Sort", "Quantum Bogo", "Sleep Sort", "Stalin Sort"
                 })
        {
            if (!map.TryGetValue(name, out var meta) || meta.Factory is null)
            {
                continue;
            }

            runs++;
            var source = DataGenerator.Generate(256, DistributionPreset.Random, HashCode.Combine(meta.Id, 256, 260502));
            var trace = ExecutionHarness.RunAlgorithm(
                meta.Factory.Invoke(),
                source,
                new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true),
                new ExecutionLimits
                {
                    MaxEvents = 4_000_000,
                    Timeout = TimeSpan.FromSeconds(6)
                },
                captureEvents: true);

            if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
            {
                failures.Add($"{meta.Name}: concept execution failed.");
                continue;
            }

            if (!HasMeaningfulEvent(trace.Events))
            {
                failures.Add($"{meta.Name}: no meaningful concept events emitted.");
            }
        }

        // Remaining: Burst Sort(String) and Spatial Sort wiring checks.
        runs++;
        if (!map.TryGetValue("Burst Sort", out var burstMeta)
            || burstMeta.StringFactory is null
            || (burstMeta.SupportedViews & SupportedViews.String) == 0)
        {
            failures.Add("Burst Sort: String wiring invalid.");
        }

        runs++;
        if (!map.TryGetValue("Spatial Sort", out var spatialMeta)
            || spatialMeta.SpatialFactory is null
            || (spatialMeta.SupportedViews & SupportedViews.Spatial) == 0
            || !spatialMeta.Description.Contains("x-then-y", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Spatial Sort: definition/wiring invalid.");
        }

        return new ValidationSuiteResult
        {
            Name = "PhaseD",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static bool HasMeaningfulEvent(IReadOnlyList<SortEvent> events)
    {
        for (var i = 0; i < events.Count; i++)
        {
            if (events[i].Type is not SortEventType.Done)
            {
                return true;
            }
        }

        return false;
    }
}
