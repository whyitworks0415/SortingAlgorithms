using SortingVisualizerApp.Core;

internal static class MissingAdvancedAlgorithmsValidationSuite
{
    private static readonly (string Name, AlgorithmImplementationStatus Status)[] Required =
    {
        ("Block Sort", AlgorithmImplementationStatus.A),
        ("WikiSort", AlgorithmImplementationStatus.B),
        ("FluxSort", AlgorithmImplementationStatus.B)
    };

    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: Advanced category completion for Block Sort / WikiSort / FluxSort.",
            "Checks: registry presence/status, A correctness (N=256/2048, 4 distributions), B stage/range consistency + sorted writeback."
        };

        var map = new Dictionary<string, AlgorithmMetadata>(StringComparer.OrdinalIgnoreCase);
        foreach (var req in Required)
        {
            var meta = registry.All.FirstOrDefault(item => item.Name.Equals(req.Name, StringComparison.OrdinalIgnoreCase));
            if (meta is null)
            {
                failures.Add($"Missing required algorithm: {req.Name}");
                continue;
            }

            if (!meta.Category.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{req.Name}: category mismatch (expected Advanced, actual={meta.Category}).");
            }

            if (meta.Status != req.Status)
            {
                failures.Add($"{req.Name}: status mismatch (expected={req.Status}, actual={meta.Status}).");
            }

            if ((meta.SupportedViews & SupportedViews.Bars) == 0)
            {
                failures.Add($"{req.Name}: Bars support is required.");
            }

            if (meta.Factory is null)
            {
                failures.Add($"{req.Name}: missing algorithm factory.");
            }

            map[req.Name] = meta;
        }

        var runs = 0;

        if (map.TryGetValue("Block Sort", out var blockMeta) && blockMeta.Factory is not null)
        {
            foreach (var size in new[] { 256, 2048 })
            {
                foreach (var distribution in new[]
                         {
                             DistributionPreset.Random,
                             DistributionPreset.NearlySorted,
                             DistributionPreset.Reversed,
                             DistributionPreset.ManyDuplicates
                         })
                {
                    runs++;
                    ValidateCorrectness(blockMeta, size, distribution, failures);
                }
            }
        }

        if (map.TryGetValue("WikiSort", out var wikiMeta) && wikiMeta.Factory is not null)
        {
            runs++;
            ValidateConceptConsistency(
                wikiMeta,
                2048,
                DistributionPreset.Random,
                expectedStages: [8501, 8502, 8503],
                failures);
        }

        if (map.TryGetValue("FluxSort", out var fluxMeta) && fluxMeta.Factory is not null)
        {
            runs++;
            ValidateConceptConsistency(
                fluxMeta,
                2048,
                DistributionPreset.NearlySorted,
                expectedStages: [8601, 8602, 8603],
                failures);
        }

        return new ValidationSuiteResult
        {
            Name = "AdvancedFill",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static void ValidateCorrectness(
        AlgorithmMetadata meta,
        int size,
        DistributionPreset distribution,
        ICollection<string> failures)
    {
        var seed = HashCode.Combine(meta.Id, size, distribution, 260301);
        var source = DataGenerator.Generate(size, distribution, seed);
        var baseline = ExecutionHarness.BuildMultiset(source);

        var trace = ExecutionHarness.RunAlgorithm(
            meta.Factory!.Invoke(),
            source,
            new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true),
            new ExecutionLimits
            {
                MaxEvents = size >= 2048 ? 45_000_000 : 10_000_000,
                Timeout = size >= 2048 ? TimeSpan.FromSeconds(24) : TimeSpan.FromSeconds(10)
            },
            captureEvents: false);

        if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
        {
            failures.Add($"{meta.Name}/{distribution}/N={size}: execution failed.");
            return;
        }

        if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
        {
            failures.Add($"{meta.Name}/{distribution}/N={size}: output not sorted.");
        }

        if (!ExecutionHarness.MultisetEquals(baseline, ExecutionHarness.BuildMultiset(trace.FinalState)))
        {
            failures.Add($"{meta.Name}/{distribution}/N={size}: multiset changed.");
        }
    }

    private static void ValidateConceptConsistency(
        AlgorithmMetadata meta,
        int size,
        DistributionPreset distribution,
        int[] expectedStages,
        ICollection<string> failures)
    {
        var seed = HashCode.Combine(meta.Id, size, distribution, 260302);
        var source = DataGenerator.Generate(size, distribution, seed);
        var baseline = ExecutionHarness.BuildMultiset(source);

        var trace = ExecutionHarness.RunAlgorithm(
            meta.Factory!.Invoke(),
            source,
            new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true),
            new ExecutionLimits
            {
                MaxEvents = 8_000_000,
                Timeout = TimeSpan.FromSeconds(10)
            },
            captureEvents: true);

        if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
        {
            failures.Add($"{meta.Name}: concept execution failed.");
            return;
        }

        if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
        {
            failures.Add($"{meta.Name}: final output not sorted.");
        }

        if (!ExecutionHarness.MultisetEquals(baseline, ExecutionHarness.BuildMultiset(trace.FinalState)))
        {
            failures.Add($"{meta.Name}: multiset changed.");
        }

        var hasStage = trace.Events.Any(static ev => ev.Type == SortEventType.MarkStage);
        var hasRange = trace.Events.Any(static ev => ev.Type == SortEventType.MarkRange);
        if (!hasStage || !hasRange)
        {
            failures.Add($"{meta.Name}: missing stage/range events.");
            return;
        }

        var stageIndices = new List<int>(expectedStages.Length);
        foreach (var stage in expectedStages)
        {
            var index = FindFirstEventIndex(trace.Events, SortEventType.MarkStage, stage);
            if (index < 0)
            {
                failures.Add($"{meta.Name}: missing expected stage {stage}.");
                return;
            }

            stageIndices.Add(index);
        }

        for (var i = 1; i < stageIndices.Count; i++)
        {
            if (stageIndices[i] <= stageIndices[i - 1])
            {
                failures.Add($"{meta.Name}: stage ordering is inconsistent.");
                return;
            }
        }

        var firstRangeIndex = FindFirstEventIndex(trace.Events, SortEventType.MarkRange, value: null);
        if (firstRangeIndex < 0)
        {
            failures.Add($"{meta.Name}: no range events found.");
            return;
        }

        if (firstRangeIndex < stageIndices[0])
        {
            failures.Add($"{meta.Name}: first range appeared before first stage.");
        }
    }

    private static int FindFirstEventIndex(IReadOnlyList<SortEvent> events, SortEventType type, int? value)
    {
        for (var i = 0; i < events.Count; i++)
        {
            var ev = events[i];
            if (ev.Type != type)
            {
                continue;
            }

            if (!value.HasValue || ev.Value == value.Value)
            {
                return i;
            }
        }

        return -1;
    }
}
