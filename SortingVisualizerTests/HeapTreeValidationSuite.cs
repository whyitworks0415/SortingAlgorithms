using SortingVisualizerApp.Core;

internal static class HeapTreeValidationSuite
{
    private static readonly (string Name, AlgorithmImplementationStatus RequiredStatus)[] ExpectedAlgorithms =
    {
        ("Binary Heap Sort", AlgorithmImplementationStatus.A),
        ("Binomial Heap Sort", AlgorithmImplementationStatus.A),
        ("Fibonacci Heap Sort", AlgorithmImplementationStatus.A),
        ("AVL Tree Sort", AlgorithmImplementationStatus.A),
        ("Red-Black Tree Sort", AlgorithmImplementationStatus.A),
        ("Treap Sort", AlgorithmImplementationStatus.A),
        ("Splay Tree Sort", AlgorithmImplementationStatus.A),
        ("Skip List Sort", AlgorithmImplementationStatus.A),
        ("B-Tree Sort", AlgorithmImplementationStatus.B)
    };

    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: Heap/Tree category completion entries.",
            "Checks: registry presence/status, stable metadata set, sorted output, multiset preservation, timeout/event-limit guard.",
            "Structure events: MarkStructure/Rotation/MergeTree/LevelHighlight/HeapBoundary must appear (captured N=256 runs)."
        };

        var metas = new List<AlgorithmMetadata>(ExpectedAlgorithms.Length);
        foreach (var expected in ExpectedAlgorithms)
        {
            var meta = registry.All.FirstOrDefault(item =>
                item.Category.Equals("Heap/Tree", StringComparison.OrdinalIgnoreCase)
                && item.Name.Equals(expected.Name, StringComparison.OrdinalIgnoreCase));

            if (meta is null)
            {
                failures.Add($"Registry missing Heap/Tree entry: {expected.Name}");
                continue;
            }

            if (meta.Status != expected.RequiredStatus)
            {
                failures.Add($"{expected.Name}: expected status={expected.RequiredStatus}, actual={meta.Status}.");
            }

            if (meta.Stable is null)
            {
                failures.Add($"{expected.Name}: stable metadata must be explicit (true/false). ");
            }

            if (meta.Factory is null)
            {
                failures.Add($"{expected.Name}: factory missing.");
                continue;
            }

            metas.Add(meta);
        }

        var runs = 0;
        var distributions = new[]
        {
            DistributionPreset.Random,
            DistributionPreset.NearlySorted,
            DistributionPreset.Reversed
        };

        foreach (var meta in metas)
        {
            foreach (var size in new[] { 256, 2048 })
            {
                foreach (var distribution in distributions)
                {
                    runs++;
                    var seed = HashCode.Combine(meta.Id, size, distribution, 260301);
                    var source = DataGenerator.Generate(size, distribution, seed);
                    var baseline = ExecutionHarness.BuildMultiset(source);

                    var trace = ExecutionHarness.RunAlgorithm(
                        meta.Factory!.Invoke(),
                        source,
                        new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true),
                        new ExecutionLimits
                        {
                            MaxEvents = size >= 2048 ? 40_000_000 : 10_000_000,
                            Timeout = size >= 2048 ? TimeSpan.FromSeconds(24) : TimeSpan.FromSeconds(10)
                        },
                        captureEvents: size <= 256);

                    if (trace.Error is not null)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: {trace.Error}");
                        continue;
                    }

                    if (trace.TimedOut)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: timeout.");
                        continue;
                    }

                    if (trace.EventLimitExceeded)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: event limit exceeded.");
                        continue;
                    }

                    if (!trace.DoneSeen)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: Done event missing.");
                    }

                    if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: output not sorted.");
                    }

                    if (!ExecutionHarness.MultisetEquals(baseline, ExecutionHarness.BuildMultiset(trace.FinalState)))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: multiset changed.");
                    }

                    if (meta.Status == AlgorithmImplementationStatus.A
                        && trace.Comparisons + trace.Swaps + trace.Writes == 0)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: no compare/swap/write events.");
                    }

                    if (size <= 256 && !HasStructureEvent(trace.Events))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: missing required structure events.");
                    }
                }
            }
        }

        return new ValidationSuiteResult
        {
            Name = "HeapTree",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static bool HasStructureEvent(IReadOnlyList<SortEvent> events)
    {
        for (var i = 0; i < events.Count; i++)
        {
            if (events[i].Type is SortEventType.MarkStructure
                or SortEventType.Rotation
                or SortEventType.MergeTree
                or SortEventType.LevelHighlight
                or SortEventType.HeapBoundary)
            {
                return true;
            }
        }

        return false;
    }
}
