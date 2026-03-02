using SortingVisualizerApp.Core;

internal static class NetworkValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: status=A and Network-supported algorithms.",
            "Checks: stage index collision-free, random sample sorting at N=8/16/32, schedule replay vs event replay equivalence (100 random samples)."
        };

        var metas = registry.All
            .Where(static meta => meta.Status == AlgorithmImplementationStatus.A
                                  && (meta.SupportedViews & SupportedViews.Network) != 0
                                  && meta.Factory is not null)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runs = 0;
        foreach (var meta in metas)
        {
            if (meta.Factory!.Invoke() is not INetworkScheduleProvider provider)
            {
                failures.Add($"{meta.Name}: Network supported but schedule provider missing.");
                continue;
            }

            foreach (var n in new[] { 8, 16, 32 })
            {
                runs++;
                var schedule = provider.BuildSchedule(n);
                ValidateScheduleNoConflicts(meta.Name, n, schedule, failures);

                for (var sample = 0; sample < 100; sample++)
                {
                    var seed = HashCode.Combine(meta.Id, n, sample, 888);
                    var source = DataGenerator.Generate(n, DistributionPreset.Random, seed);
                    var scheduleSorted = ApplySchedule(schedule, source);

                    if (!ExecutionHarness.IsSortedAscending(scheduleSorted))
                    {
                        failures.Add($"{meta.Name}/N={n}/sample={sample}: schedule replay not sorted.");
                        break;
                    }

                    var options = new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true);
                    var trace = ExecutionHarness.RunAlgorithm(
                        meta.Factory!.Invoke(),
                        source,
                        options,
                        new ExecutionLimits
                        {
                            MaxEvents = 4_000_000,
                            Timeout = TimeSpan.FromSeconds(6)
                        },
                        captureEvents: false);

                    if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
                    {
                        failures.Add($"{meta.Name}/N={n}/sample={sample}: execution failed.");
                        break;
                    }

                    if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
                    {
                        failures.Add($"{meta.Name}/N={n}/sample={sample}: event replay not sorted.");
                        break;
                    }

                    if (!trace.FinalState.AsSpan().SequenceEqual(scheduleSorted))
                    {
                        failures.Add($"{meta.Name}/N={n}/sample={sample}: schedule result differs from event replay result.");
                        break;
                    }
                }
            }
        }

        return new ValidationSuiteResult
        {
            Name = "Network",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static void ValidateScheduleNoConflicts(string algorithmName, int n, NetworkSchedule schedule, ICollection<string> failures)
    {
        for (var stageIndex = 0; stageIndex < schedule.Stages.Count; stageIndex++)
        {
            var touched = new HashSet<int>();
            var stage = schedule.Stages[stageIndex];
            foreach (var comparator in stage.Comparators)
            {
                if (comparator.I < 0 || comparator.J < 0 || comparator.I >= n || comparator.J >= n || comparator.I == comparator.J)
                {
                    failures.Add($"{algorithmName}/N={n}: invalid comparator at stage {stageIndex} ({comparator.I},{comparator.J}).");
                    break;
                }

                if (!touched.Add(comparator.I) || !touched.Add(comparator.J))
                {
                    failures.Add($"{algorithmName}/N={n}: stage {stageIndex} has index collision.");
                    break;
                }
            }
        }
    }

    private static int[] ApplySchedule(NetworkSchedule schedule, IReadOnlyList<int> source)
    {
        var values = source.ToArray();
        foreach (var stage in schedule.Stages)
        {
            foreach (var comparator in stage.Comparators)
            {
                var i = comparator.I;
                var j = comparator.J;
                var shouldSwap = comparator.Ascending
                    ? values[i] > values[j]
                    : values[i] < values[j];

                if (shouldSwap)
                {
                    (values[i], values[j]) = (values[j], values[i]);
                }
            }
        }

        return values;
    }
}
