using SortingVisualizerApp.Core;

internal static class PhaseCCompletionValidationSuite
{
    private static readonly (string Name, AlgorithmImplementationStatus MinStatus)[] Required =
    {
        ("In-Place Merge Sort", AlgorithmImplementationStatus.A),
        ("Smooth Sort", AlgorithmImplementationStatus.A),
        ("Cartesian Tree Sort", AlgorithmImplementationStatus.A),
        ("Polyphase Merge Sort", AlgorithmImplementationStatus.B)
    };

    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: Phase-C required algorithms (3x Divide&Conquer + 1x External Polyphase).",
            "Checks: registry presence/status, Bars correctness for A set, Polyphase event consistency, N=50000 progress smoke."
        };

        var metas = new Dictionary<string, AlgorithmMetadata>(StringComparer.OrdinalIgnoreCase);
        foreach (var requirement in Required)
        {
            var meta = registry.All.FirstOrDefault(item =>
                item.Name.Equals(requirement.Name, StringComparison.OrdinalIgnoreCase));

            if (meta is null)
            {
                failures.Add($"Missing required algorithm: {requirement.Name}");
                continue;
            }

            if (requirement.MinStatus == AlgorithmImplementationStatus.A && meta.Status != AlgorithmImplementationStatus.A)
            {
                failures.Add($"{requirement.Name}: must be status A.");
            }

            if (meta.Factory is null)
            {
                failures.Add($"{requirement.Name}: factory missing.");
                continue;
            }

            metas[requirement.Name] = meta;
        }

        var runs = 0;
        var distributions = new[]
        {
            DistributionPreset.Random,
            DistributionPreset.NearlySorted,
            DistributionPreset.Reversed
        };

        foreach (var name in new[] { "In-Place Merge Sort", "Smooth Sort", "Cartesian Tree Sort" })
        {
            if (!metas.TryGetValue(name, out var meta))
            {
                continue;
            }

            foreach (var n in new[] { 256, 2048 })
            {
                foreach (var distribution in distributions)
                {
                    runs++;
                    var seed = HashCode.Combine(meta.Id, n, distribution, 260401);
                    var source = DataGenerator.Generate(n, distribution, seed);
                    var baseline = ExecutionHarness.BuildMultiset(source);

                    var trace = ExecutionHarness.RunAlgorithm(
                        meta.Factory!.Invoke(),
                        source,
                        new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true),
                        new ExecutionLimits
                        {
                            MaxEvents = n >= 2048 ? 45_000_000 : 10_000_000,
                            Timeout = n >= 2048 ? TimeSpan.FromSeconds(24) : TimeSpan.FromSeconds(10)
                        },
                        captureEvents: false);

                    if (trace.Error is not null)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={n}: {trace.Error}");
                        continue;
                    }

                    if (trace.TimedOut)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={n}: timeout.");
                        continue;
                    }

                    if (trace.EventLimitExceeded)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={n}: event limit exceeded.");
                        continue;
                    }

                    if (!trace.DoneSeen)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={n}: Done event missing.");
                    }

                    if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={n}: output not sorted.");
                    }

                    if (!ExecutionHarness.MultisetEquals(baseline, ExecutionHarness.BuildMultiset(trace.FinalState)))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={n}: multiset changed.");
                    }

                    if (trace.Comparisons + trace.Swaps + trace.Writes == 0)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={n}: no compare/swap/write events.");
                    }
                }
            }
        }

        if (metas.TryGetValue("Polyphase Merge Sort", out var polyphaseMeta))
        {
            runs++;
            var n = 2048;
            var source = DataGenerator.Generate(n, DistributionPreset.Random, 260499);
            var baseline = ExecutionHarness.BuildMultiset(source);

            var polyTrace = ExecutionHarness.RunAlgorithm(
                polyphaseMeta.Factory!.Invoke(),
                source,
                new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true),
                new ExecutionLimits
                {
                    MaxEvents = 30_000_000,
                    Timeout = TimeSpan.FromSeconds(20)
                },
                captureEvents: true);

            if (polyTrace.Error is not null || polyTrace.TimedOut || polyTrace.EventLimitExceeded || !polyTrace.DoneSeen)
            {
                failures.Add("Polyphase Merge Sort: execution failed.");
            }
            else
            {
                ValidateExternalEventConsistency("Polyphase Merge Sort", n, polyTrace.Events, failures);

                if (polyphaseMeta.Status == AlgorithmImplementationStatus.A)
                {
                    if (!ExecutionHarness.IsSortedAscending(polyTrace.FinalState))
                    {
                        failures.Add("Polyphase Merge Sort: status A but output not sorted.");
                    }

                    if (!ExecutionHarness.MultisetEquals(baseline, ExecutionHarness.BuildMultiset(polyTrace.FinalState)))
                    {
                        failures.Add("Polyphase Merge Sort: status A but multiset changed.");
                    }
                }
            }
        }

        // N=50000 progress smoke: done within 10s OR makes clear forward progress.
        foreach (var name in new[] { "In-Place Merge Sort", "Smooth Sort", "Cartesian Tree Sort" })
        {
            if (!metas.TryGetValue(name, out var meta))
            {
                continue;
            }

            runs++;
            var source = DataGenerator.Generate(50_000, DistributionPreset.Random, HashCode.Combine(meta.Id, 50_000, 260450));
            var trace = ExecutionHarness.RunAlgorithm(
                meta.Factory!.Invoke(),
                source,
                new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true),
                new ExecutionLimits
                {
                    MaxEvents = 160_000_000,
                    Timeout = TimeSpan.FromSeconds(10)
                },
                captureEvents: false);

            if (trace.Error is not null)
            {
                failures.Add($"{meta.Name}/smoke N=50000: {trace.Error}");
                continue;
            }

            if (trace.TimedOut)
            {
                if (trace.ProcessedEvents < 100_000)
                {
                    failures.Add($"{meta.Name}/smoke N=50000: timed out with insufficient progress ({trace.ProcessedEvents} events). ");
                }
            }
            else
            {
                if (!trace.DoneSeen)
                {
                    failures.Add($"{meta.Name}/smoke N=50000: completed loop without Done event.");
                }

                if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
                {
                    failures.Add($"{meta.Name}/smoke N=50000: final output not sorted.");
                }
            }
        }

        return new ValidationSuiteResult
        {
            Name = "PhaseC",
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
        var runs = new Dictionary<int, int>();

        for (var index = 0; index < events.Count; index++)
        {
            var ev = events[index];
            switch (ev.Type)
            {
                case SortEventType.RunCreated:
                    if (ev.I < 0)
                    {
                        failures.Add($"{algorithmName}/N={size}: RunCreated invalid id at event {index}.");
                        return;
                    }

                    runs[ev.I] = Math.Max(1, ev.Value);
                    break;

                case SortEventType.RunRead:
                case SortEventType.RunWrite:
                    if (!runs.TryGetValue(ev.I, out var runLength))
                    {
                        failures.Add($"{algorithmName}/N={size}: {ev.Type} references unknown run {ev.I} at event {index}.");
                        return;
                    }

                    if (ev.J < 0 || ev.J > runLength)
                    {
                        failures.Add($"{algorithmName}/N={size}: {ev.Type} cursor out of range at event {index}.");
                        return;
                    }
                    break;

                case SortEventType.MergeGroup:
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
                    break;
            }
        }
    }
}
