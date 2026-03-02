using System.Diagnostics;
using SortingVisualizerApp.Algorithms;
using SortingVisualizerApp.Core;

internal static class SpecialAlgorithmValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Checks: QuickSelect kth correctness, PartialSort top-k correctness, StablePartition stability, K-way merge consistency, inversion count parity.",
            "Burst stability rule: equal strings preserve original relative order."
        };

        var runs = 0;

        runs++;
        ValidateQuickSelect(failures);

        runs++;
        ValidatePartialSort(failures);

        runs++;
        ValidateStablePartition(failures);

        runs++;
        ValidateKWayMerge(failures);

        runs++;
        ValidateCountingInversions(failures);

        runs++;
        ValidateBurstStability(registry, failures);

        return new ValidationSuiteResult
        {
            Name = "Special",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static void ValidateQuickSelect(ICollection<string> failures)
    {
        foreach (var n in new[] { 17, 64, 257, 1024 })
        {
            foreach (var distribution in new[]
                     {
                         DistributionPreset.Random,
                         DistributionPreset.NearlySorted,
                         DistributionPreset.Reversed,
                         DistributionPreset.ManyDuplicates
                     })
            {
                var seed = HashCode.Combine("quickselect", n, distribution, 20260301);
                var source = DataGenerator.Generate(n, distribution, seed);
                var sorted = source.ToArray();
                Array.Sort(sorted);

                foreach (var k in new[] { 0, n / 4, n / 2, n - 1 })
                {
                    var actual = QuickSelectAlgorithm.SelectKth(source, k);
                    var expected = sorted[Math.Clamp(k, 0, n - 1)];
                    if (actual != expected)
                    {
                        failures.Add($"QuickSelect static mismatch: N={n}, distribution={distribution}, k={k}, expected={expected}, actual={actual}");
                        return;
                    }
                }
            }
        }
    }

    private static void ValidatePartialSort(ICollection<string> failures)
    {
        foreach (var n in new[] { 32, 128, 1024 })
        {
            foreach (var distribution in new[]
                     {
                         DistributionPreset.Random,
                         DistributionPreset.NearlySorted,
                         DistributionPreset.Reversed,
                         DistributionPreset.ManyDuplicates
                     })
            {
                var seed = HashCode.Combine("partialsort", n, distribution, 20260301);
                var source = DataGenerator.Generate(n, distribution, seed);

                foreach (var k in new[] { 1, Math.Max(2, n / 8), Math.Max(4, n / 3), n })
                {
                    var expected = source.ToArray();
                    Array.Sort(expected);
                    expected = expected.Take(Math.Clamp(k, 0, expected.Length)).ToArray();

                    var actual = PartialSortAlgorithm.PartialTopK(source, k);
                    if (!expected.AsSpan().SequenceEqual(actual))
                    {
                        failures.Add($"PartialSort top-k mismatch: N={n}, distribution={distribution}, k={k}");
                        return;
                    }
                }
            }
        }
    }

    private static void ValidateStablePartition(ICollection<string> failures)
    {
        var random = new Random(20260301);
        for (var caseIndex = 0; caseIndex < 16; caseIndex++)
        {
            var source = new int[256];
            for (var i = 0; i < source.Length; i++)
            {
                source[i] = random.Next(0, 64);
            }

            var result = StablePartitionAlgorithm.PartitionStable(source, static value => (value & 1) == 0);

            var evens = source.Where(static value => (value & 1) == 0).ToArray();
            var odds = source.Where(static value => (value & 1) != 0).ToArray();
            var expected = evens.Concat(odds).ToArray();

            if (!expected.AsSpan().SequenceEqual(result))
            {
                failures.Add("StablePartition mismatch: stable predicate order was not preserved.");
                return;
            }
        }
    }

    private static void ValidateKWayMerge(ICollection<string> failures)
    {
        var random = new Random(20260301);
        for (var trial = 0; trial < 20; trial++)
        {
            var runCount = random.Next(2, 8);
            var runs = new List<int[]>(runCount);

            for (var run = 0; run < runCount; run++)
            {
                var length = random.Next(1, 80);
                var values = new int[length];
                for (var i = 0; i < length; i++)
                {
                    values[i] = random.Next(0, 2000);
                }

                Array.Sort(values);
                runs.Add(values);
            }

            var expected = runs.SelectMany(static run => run).OrderBy(static v => v).ToArray();
            var actual = KWayMergeAlgorithm.MergeSortedRuns(runs);
            if (!expected.AsSpan().SequenceEqual(actual))
            {
                failures.Add("K-way Merge mismatch: merged output differs from reference ordering.");
                return;
            }
        }
    }

    private static void ValidateCountingInversions(ICollection<string> failures)
    {
        var random = new Random(20260301);
        for (var n = 4; n <= 64; n += 4)
        {
            for (var trial = 0; trial < 15; trial++)
            {
                var values = new int[n];
                for (var i = 0; i < n; i++)
                {
                    values[i] = random.Next(0, 50);
                }

                var expected = CountInversionsNaive(values);
                var actual = CountingInversionsAlgorithm.CountInversions(values);
                if (expected != actual)
                {
                    failures.Add($"CountingInversions mismatch: N={n}, expected={expected}, actual={actual}");
                    return;
                }
            }
        }
    }

    private static void ValidateBurstStability(AlgorithmRegistry registry, ICollection<string> failures)
    {
        var meta = registry.All.FirstOrDefault(static item =>
            item.Status == AlgorithmImplementationStatus.A
            && (item.SupportedViews & SupportedViews.String) != 0
            && item.StringFactory is not null
            && item.Name.Contains("Burst", StringComparison.OrdinalIgnoreCase));

        if (meta is null || meta.StringFactory is null)
        {
            failures.Add("Burst(String) algorithm missing from registry.");
            return;
        }

        var algorithm = meta.StringFactory.Invoke();

        var source = StringDataGenerator.Generate(
            count: 512,
            length: 10,
            alphabet: StringAlphabetSet.Lowercase,
            distribution: StringDistributionPreset.ManyDuplicates,
            seed: 20260301);

        var trace = RunStringAlgorithm(
            algorithm,
            source,
            new StringSortOptions(StringAlphabetSet.Lowercase, EmitExtendedEvents: true),
            new ExecutionLimits
            {
                MaxEvents = 8_000_000,
                Timeout = TimeSpan.FromSeconds(8)
            });

        if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
        {
            failures.Add($"{meta.Name}: execution failed in burst-specific validation.");
            return;
        }

        if (!IsLexicographicAscending(trace.FinalState))
        {
            failures.Add($"{meta.Name}: output is not lexicographically sorted.");
            return;
        }

        if (!IsStableForEqualStrings(trace.FinalState))
        {
            failures.Add($"{meta.Name}: equal-string stability rule violated.");
        }
    }

    private static long CountInversionsNaive(IReadOnlyList<int> values)
    {
        long count = 0;
        for (var i = 0; i < values.Count; i++)
        {
            for (var j = i + 1; j < values.Count; j++)
            {
                if (values[i] > values[j])
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static StringTrace RunStringAlgorithm(
        IStringSortAlgorithm algorithm,
        StringItem[] source,
        StringSortOptions options,
        ExecutionLimits limits)
    {
        var state = source.ToArray();
        var byId = source.ToDictionary(static item => item.Id, static item => item);

        var stopwatch = Stopwatch.StartNew();
        var processed = 0L;
        var doneSeen = false;
        var timedOut = false;
        var eventLimitExceeded = false;
        string? error = null;

        try
        {
            foreach (var ev in algorithm.Execute(source.ToArray(), options))
            {
                processed++;
                if (processed > limits.MaxEvents)
                {
                    eventLimitExceeded = true;
                    break;
                }

                if ((processed & 511) == 0 && stopwatch.Elapsed > limits.Timeout)
                {
                    timedOut = true;
                    break;
                }

                switch (ev.Type)
                {
                    case SortEventType.Swap:
                        if (InRange(ev.I, state.Length) && InRange(ev.J, state.Length))
                        {
                            (state[ev.I], state[ev.J]) = (state[ev.J], state[ev.I]);
                        }
                        else
                        {
                            error ??= $"Swap index out of range ({ev.I},{ev.J}).";
                        }
                        break;

                    case SortEventType.Write:
                        if (!InRange(ev.I, state.Length))
                        {
                            error ??= $"Write index out of range ({ev.I}).";
                            break;
                        }

                        if (!byId.TryGetValue(ev.Value, out var item))
                        {
                            error ??= $"Unknown row id for Write event ({ev.Value}).";
                            break;
                        }

                        state[ev.I] = item;
                        break;

                    case SortEventType.Done:
                        doneSeen = true;
                        break;
                }

                if (error is not null)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
        }

        return new StringTrace
        {
            FinalState = state,
            DoneSeen = doneSeen,
            TimedOut = timedOut,
            EventLimitExceeded = eventLimitExceeded,
            Error = error
        };
    }

    private static bool IsLexicographicAscending(ReadOnlySpan<StringItem> items)
    {
        for (var i = 1; i < items.Length; i++)
        {
            if (string.CompareOrdinal(items[i - 1].Text, items[i].Text) > 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsStableForEqualStrings(ReadOnlySpan<StringItem> items)
    {
        for (var i = 1; i < items.Length; i++)
        {
            if (!string.Equals(items[i - 1].Text, items[i].Text, StringComparison.Ordinal))
            {
                continue;
            }

            if (items[i - 1].OriginalIndex > items[i].OriginalIndex)
            {
                return false;
            }
        }

        return true;
    }

    private static bool InRange(int index, int length)
    {
        return index >= 0 && index < length;
    }

    private sealed class StringTrace
    {
        public required StringItem[] FinalState { get; init; }
        public required bool DoneSeen { get; init; }
        public required bool TimedOut { get; init; }
        public required bool EventLimitExceeded { get; init; }
        public required string? Error { get; init; }
    }
}
