using SortingVisualizerApp.Core;

internal static class BarsValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: status=A and Bars-supported algorithms.",
            "Checks: sorted ascending, multiset preserved, compare/swap/write event presence, timeout/event-limit guard."
        };

        var distributions = new[]
        {
            DistributionPreset.Random,
            DistributionPreset.NearlySorted,
            DistributionPreset.Reversed,
            DistributionPreset.ManyDuplicates
        };

        var sizes = new[] { 64, 256, 2048 };
        var metas = registry.All
            .Where(static meta => meta.Status == AlgorithmImplementationStatus.A
                                  && (meta.SupportedViews & SupportedViews.Bars) != 0
                                  && meta.Factory is not null)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runs = 0;
        foreach (var meta in metas)
        {
            foreach (var size in sizes)
            {
                foreach (var distribution in distributions)
                {
                    runs++;
                    var seed = HashCode.Combine(meta.Id, size, distribution, 260226);
                    var source = DataGenerator.Generate(size, distribution, seed);
                    var baseline = ExecutionHarness.BuildMultiset(source);

                    var limits = new ExecutionLimits
                    {
                        MaxEvents = size >= 2048 ? 35_000_000 : 8_000_000,
                        Timeout = size >= 2048 ? TimeSpan.FromSeconds(22) : TimeSpan.FromSeconds(10)
                    };

                    var algorithm = meta.Factory!.Invoke();
                    var options = new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true);
                    var trace = ExecutionHarness.RunAlgorithm(algorithm, source, options, limits, captureEvents: false);

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

                    var resultSet = ExecutionHarness.BuildMultiset(trace.FinalState);
                    if (!ExecutionHarness.MultisetEquals(baseline, resultSet))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: multiset changed.");
                    }

                    if (trace.Comparisons + trace.Swaps + trace.Writes == 0)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: no compare/swap/write events.");
                    }
                }
            }
        }

        return new ValidationSuiteResult
        {
            Name = "Bars",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }
}
