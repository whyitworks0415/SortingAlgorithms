using SortingVisualizerApp.Core;

internal static class ExternalValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: algorithms with External-supported view.",
            "Checks: run/merge event consistency and final sorted output for status=A implementations."
        };

        var metas = registry.All
            .Where(static meta => (meta.SupportedViews & SupportedViews.External) != 0)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runs = 0;
        foreach (var meta in metas)
        {
            if (meta.Factory is null)
            {
                notes.Add($"{meta.Name}: skipped (no implementation, status {meta.Status}).");
                continue;
            }

            foreach (var size in new[] { 256, 2048 })
            {
                runs++;
                var seed = HashCode.Combine(meta.Id, size, 404);
                var source = DataGenerator.Generate(size, DistributionPreset.Random, seed);
                var baseline = ExecutionHarness.BuildMultiset(source);
                var options = new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true);
                var trace = ExecutionHarness.RunAlgorithm(
                    meta.Factory.Invoke(),
                    source,
                    options,
                    new ExecutionLimits
                    {
                        MaxEvents = size >= 2048 ? 30_000_000 : 8_000_000,
                        Timeout = size >= 2048 ? TimeSpan.FromSeconds(18) : TimeSpan.FromSeconds(8)
                    },
                    captureEvents: true);

                if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
                {
                    failures.Add($"{meta.Name}/N={size}: execution failed.");
                    continue;
                }

                ValidateExternalEventConsistency(meta.Name, size, trace.Events, failures);

                if (meta.Status == AlgorithmImplementationStatus.A)
                {
                    if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
                    {
                        failures.Add($"{meta.Name}/N={size}: final output not sorted.");
                    }

                    if (!ExecutionHarness.MultisetEquals(baseline, ExecutionHarness.BuildMultiset(trace.FinalState)))
                    {
                        failures.Add($"{meta.Name}/N={size}: multiset changed.");
                    }
                }
            }
        }

        return new ValidationSuiteResult
        {
            Name = "External",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static void ValidateExternalEventConsistency(
        string algorithmName,
        int size,
        IReadOnlyList<SortEvent> events,
        ICollection<string> failures)
    {
        var runs = new Dictionary<int, (int Start, int Length)>();
        var mergeGroups = new Dictionary<int, (int OutputRun, HashSet<int> Inputs)>();

        for (var index = 0; index < events.Count; index++)
        {
            var ev = events[index];
            switch (ev.Type)
            {
                case SortEventType.RunCreated:
                    if (ev.I < 0)
                    {
                        failures.Add($"{algorithmName}/N={size}: RunCreated invalid run id at event {index}.");
                        return;
                    }

                    runs[ev.I] = (Math.Max(0, ev.J), Math.Max(1, ev.Value));
                    break;

                case SortEventType.RunRead:
                case SortEventType.RunWrite:
                    if (!runs.TryGetValue(ev.I, out var run))
                    {
                        failures.Add($"{algorithmName}/N={size}: {ev.Type} references unknown run {ev.I} at event {index}.");
                        return;
                    }

                    if (ev.J < 0 || ev.J > run.Length)
                    {
                        failures.Add($"{algorithmName}/N={size}: {ev.Type} cursor out of range at event {index}.");
                        return;
                    }
                    break;

                case SortEventType.MergeGroup:
                {
                    if (!runs.ContainsKey(ev.I))
                    {
                        failures.Add($"{algorithmName}/N={size}: MergeGroup input run missing ({ev.I}) at event {index}.");
                        return;
                    }

                    if (!runs.ContainsKey(ev.J))
                    {
                        failures.Add($"{algorithmName}/N={size}: MergeGroup output run missing ({ev.J}) at event {index}.");
                        return;
                    }

                    var groupId = Math.Max(0, ev.Value);
                    if (!mergeGroups.TryGetValue(groupId, out var group))
                    {
                        group = (ev.J, new HashSet<int>());
                    }

                    group.Inputs.Add(ev.I);
                    mergeGroups[groupId] = group;
                    break;
                }
            }
        }
    }
}
