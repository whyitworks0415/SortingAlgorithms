using SortingVisualizerApp.Core;
using SortingVisualizerApp.Simulation;

internal static class ParallelValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: Parallel category A algorithms with Bars support.",
            "Checks: sorted output, multiset preservation, event activity, timeout guard.",
            "Extra: Parallelism=1 parity against sequential Quick/Merge and memory counter reset behavior."
        };

        var metas = registry.All
            .Where(static meta => meta.Category.Equals("Parallel", StringComparison.OrdinalIgnoreCase)
                                  && meta.Status == AlgorithmImplementationStatus.A
                                  && (meta.SupportedViews & SupportedViews.Bars) != 0
                                  && meta.Factory is not null)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runs = 0;
        var distributions = new[]
        {
            DistributionPreset.Random,
            DistributionPreset.NearlySorted,
            DistributionPreset.Reversed
        };

        var sizes = new[] { 256, 2048 };
        var requestedParallelism = Math.Clamp(Environment.ProcessorCount, 1, 8);

        foreach (var meta in metas)
        {
            foreach (var size in sizes)
            {
                foreach (var distribution in distributions)
                {
                    runs++;
                    var seed = HashCode.Combine(meta.Id, size, distribution, 280226);
                    var source = DataGenerator.Generate(size, distribution, seed);
                    var baseline = ExecutionHarness.BuildMultiset(source);

                    var limits = new ExecutionLimits
                    {
                        MaxEvents = size >= 2048 ? 45_000_000 : 10_000_000,
                        Timeout = size >= 2048 ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(12)
                    };

                    var options = new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true, Parallelism: requestedParallelism);
                    var trace = ExecutionHarness.RunAlgorithm(meta.Factory!.Invoke(), source, options, limits, captureEvents: false);

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

        runs += ValidateSingleThreadParity(registry, failures);
        runs += ValidateMemoryCounterReset(registry, failures);

        return new ValidationSuiteResult
        {
            Name = "Parallel",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static int ValidateSingleThreadParity(AlgorithmRegistry registry, ICollection<string> failures)
    {
        var runs = 0;

        if (!TryFindByName(registry, "Parallel QuickSort", out var parallelQuick)
            || !TryFindByName(registry, "Quick", out var sequentialQuick)
            || !TryFindByName(registry, "Parallel MergeSort", out var parallelMerge)
            || !TryFindByName(registry, "Merge", out var sequentialMerge))
        {
            failures.Add("Parity setup failed: required algorithms missing in registry.");
            return runs;
        }

        var distributions = new[] { DistributionPreset.Random, DistributionPreset.NearlySorted, DistributionPreset.Reversed };
        var sizes = new[] { 256, 2048 };

        foreach (var distribution in distributions)
        {
            foreach (var size in sizes)
            {
                runs += 2;
                var seed = HashCode.Combine(distribution, size, 90210);
                var source = DataGenerator.Generate(size, distribution, seed);

                ValidateParityCase(parallelQuick.Factory!.Invoke(), sequentialQuick.Factory!.Invoke(), source, distribution, size, "Quick", failures);
                ValidateParityCase(parallelMerge.Factory!.Invoke(), sequentialMerge.Factory!.Invoke(), source, distribution, size, "Merge", failures);
            }
        }

        return runs;
    }

    private static void ValidateParityCase(
        ISortAlgorithm parallelAlgorithm,
        ISortAlgorithm sequentialAlgorithm,
        int[] source,
        DistributionPreset distribution,
        int size,
        string label,
        ICollection<string> failures)
    {
        var limits = new ExecutionLimits
        {
            MaxEvents = size >= 2048 ? 45_000_000 : 10_000_000,
            Timeout = size >= 2048 ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(12)
        };

        var maxValue = Math.Max(1, source.Max());
        var parallelTrace = ExecutionHarness.RunAlgorithm(
            parallelAlgorithm,
            source,
            new SortOptions(MaxValue: maxValue, EmitExtendedEvents: true, Parallelism: 1),
            limits,
            captureEvents: false);

        var sequentialTrace = ExecutionHarness.RunAlgorithm(
            sequentialAlgorithm,
            source,
            new SortOptions(MaxValue: maxValue, EmitExtendedEvents: true, Parallelism: 1),
            limits,
            captureEvents: false);

        if (parallelTrace.Error is not null)
        {
            failures.Add($"Parity/{label}/{distribution}/N={size}: parallel error {parallelTrace.Error}");
            return;
        }

        if (sequentialTrace.Error is not null)
        {
            failures.Add($"Parity/{label}/{distribution}/N={size}: sequential error {sequentialTrace.Error}");
            return;
        }

        if (!parallelTrace.FinalState.AsSpan().SequenceEqual(sequentialTrace.FinalState))
        {
            failures.Add($"Parity/{label}/{distribution}/N={size}: outputs differ at Parallelism=1.");
        }
    }

    private static int ValidateMemoryCounterReset(AlgorithmRegistry registry, ICollection<string> failures)
    {
        if (!TryFindByName(registry, "Bubble", out var bubble) || bubble.Factory is null)
        {
            failures.Add("Memory reset check skipped: Bubble algorithm missing.");
            return 0;
        }

        using var engine = new SimulationEngine();
        var seed = 1234567;
        var source = DataGenerator.Generate(512, DistributionPreset.Random, seed);
        engine.LoadData(source);

        var controls = new RuntimeControls
        {
            EventsPerSecond = 200000,
            SpeedMode = SpeedControlMode.EventsPerSecond,
            CacheLineSize = 32,
            VisualEnabled = false,
            AudioEnabled = false,
            Parallelism = 1
        };
        engine.UpdateRuntimeControls(controls);
        engine.Start(bubble.Factory.Invoke(), bubble.Id, record: false, parallelism: 1);

        var started = DateTime.UtcNow;
        while ((DateTime.UtcNow - started) < TimeSpan.FromSeconds(8))
        {
            var stats = engine.GetStatisticsSnapshot();
            if (stats.IsCompleted)
            {
                break;
            }

            Thread.Sleep(5);
        }

        var beforeStats = engine.GetStatisticsSnapshot();
        if (!beforeStats.IsCompleted)
        {
            failures.Add("Memory reset: engine did not complete Bubble run within timeout.");
            return 1;
        }

        var counts = Array.Empty<int>();
        engine.CopyMemoryAccessTo(ref counts, out var maxBefore);
        if (maxBefore <= 0)
        {
            failures.Add("Memory reset: expected non-zero memory access max before reset.");
        }

        if (beforeStats.CacheHits + beforeStats.CacheMisses <= 0)
        {
            failures.Add("Memory reset: expected cache hit/miss counters before reset.");
        }

        engine.ResetMemoryAccessCounters();
        var afterStats = engine.GetStatisticsSnapshot();
        engine.CopyMemoryAccessTo(ref counts, out var maxAfter);

        if (maxAfter != 0 || counts.Any(static value => value != 0))
        {
            failures.Add("Memory reset: counters not cleared to zero.");
        }

        if (afterStats.CacheHits != 0 || afterStats.CacheMisses != 0)
        {
            failures.Add("Memory reset: cache stats not cleared to zero.");
        }

        return 1;
    }

    private static bool TryFindByName(AlgorithmRegistry registry, string name, out AlgorithmMetadata metadata)
    {
        var found = registry.All.FirstOrDefault(meta => meta.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (found is null)
        {
            metadata = null!;
            return false;
        }

        metadata = found;
        return true;
    }
}
