using System.Diagnostics;
using SortingVisualizerApp.Algorithms;
using SortingVisualizerApp.Core;

internal static class StringValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: status=A and String-supported algorithms.",
            "Checks: lexicographic ascending output, id multiset preservation, event activity, timeout/event-limit guard.",
            "Additional checks: LSD/Trie/Burst stability for equal strings and suffix-array reference parity on small strings."
        };

        var metas = registry.All
            .Where(static meta => meta.Status == AlgorithmImplementationStatus.A
                                  && (meta.SupportedViews & SupportedViews.String) != 0
                                  && meta.StringFactory is not null)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runs = 0;
        foreach (var meta in metas)
        {
            foreach (var size in new[] { 64, 256, 2048 })
            {
                foreach (var distribution in new[]
                         {
                             StringDistributionPreset.Random,
                             StringDistributionPreset.CommonPrefix,
                             StringDistributionPreset.ManyDuplicates
                         })
                {
                    runs++;
                    var seed = HashCode.Combine(meta.Id, size, distribution, 260227);
                    var source = StringDataGenerator.Generate(
                        count: size,
                        length: 12,
                        alphabet: StringAlphabetSet.Mixed,
                        distribution: distribution,
                        seed: seed);

                    var baseline = BuildIdMultiset(source);
                    var trace = RunStringAlgorithm(
                        meta.StringFactory!.Invoke(),
                        source,
                        new StringSortOptions(StringAlphabetSet.Mixed, EmitExtendedEvents: true),
                        new ExecutionLimits
                        {
                            MaxEvents = size >= 2048 ? 18_000_000 : 5_000_000,
                            Timeout = size >= 2048 ? TimeSpan.FromSeconds(16) : TimeSpan.FromSeconds(8)
                        });

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

                    if (!IsLexicographicAscending(trace.FinalState))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: output not lexicographically sorted.");
                    }

                    var resultSet = BuildIdMultiset(trace.FinalState);
                    if (!IdMultisetEquals(baseline, resultSet))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: id multiset changed.");
                    }

                    if (trace.Comparisons + trace.Writes == 0)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: no string compare/write events.");
                    }
                }
            }

            if (meta.Name.Contains("LSD String", StringComparison.OrdinalIgnoreCase))
            {
                runs++;
                ValidateLsdStability(meta, failures);
            }

            if (meta.Name.Contains("Trie-based", StringComparison.OrdinalIgnoreCase))
            {
                runs++;
                ValidateTrieStability(meta, failures);
            }

            if (meta.Name.Contains("Suffix Array", StringComparison.OrdinalIgnoreCase))
            {
                runs++;
                ValidateSuffixArrayReference(meta, failures);
            }

            if (meta.Name.Contains("Burst", StringComparison.OrdinalIgnoreCase))
            {
                runs++;
                ValidateBurstStability(meta, failures);
            }
        }

        return new ValidationSuiteResult
        {
            Name = "String",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static void ValidateLsdStability(AlgorithmMetadata meta, ICollection<string> failures)
    {
        var seed = HashCode.Combine(meta.Id, 2026, 3);
        var source = StringDataGenerator.Generate(
            count: 1024,
            length: 8,
            alphabet: StringAlphabetSet.Lowercase,
            distribution: StringDistributionPreset.ManyDuplicates,
            seed: seed);

        var trace = RunStringAlgorithm(
            meta.StringFactory!.Invoke(),
            source,
            new StringSortOptions(StringAlphabetSet.Lowercase, EmitExtendedEvents: true),
            new ExecutionLimits
            {
                MaxEvents = 8_000_000,
                Timeout = TimeSpan.FromSeconds(10)
            });

        if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
        {
            failures.Add($"{meta.Name}/stable-check: execution failed.");
            return;
        }

        if (!IsStableForEqualStrings(trace.FinalState))
        {
            failures.Add($"{meta.Name}/stable-check: equal-string relative order changed.");
        }
    }

    private static void ValidateTrieStability(AlgorithmMetadata meta, ICollection<string> failures)
    {
        var seed = HashCode.Combine(meta.Id, 2026, 5);
        var source = StringDataGenerator.Generate(
            count: 1024,
            length: 9,
            alphabet: StringAlphabetSet.Lowercase,
            distribution: StringDistributionPreset.ManyDuplicates,
            seed: seed);

        var trace = RunStringAlgorithm(
            meta.StringFactory!.Invoke(),
            source,
            new StringSortOptions(StringAlphabetSet.Lowercase, EmitExtendedEvents: true),
            new ExecutionLimits
            {
                MaxEvents = 10_000_000,
                Timeout = TimeSpan.FromSeconds(10)
            });

        if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
        {
            failures.Add($"{meta.Name}/stable-check: execution failed.");
            return;
        }

        if (!IsStableForEqualStrings(trace.FinalState))
        {
            failures.Add($"{meta.Name}/stable-check: equal-string relative order changed.");
        }
    }

    private static void ValidateSuffixArrayReference(AlgorithmMetadata meta, ICollection<string> failures)
    {
        var samples = new[]
        {
            "banana",
            "abracadabra",
            "mississippi",
            "aaaaab",
            "zxyzzxy"
        };

        for (var i = 0; i < samples.Length; i++)
        {
            var text = samples[i];
            var expected = BuildNaiveSuffixArray(text);
            var actual = SuffixArrayConstructionAlgorithm.BuildSuffixArray(text);
            if (expected.Length != actual.Length || !expected.AsSpan().SequenceEqual(actual))
            {
                failures.Add($"{meta.Name}/suffix-reference/{text}: mismatch against naive suffix-array.");
                return;
            }
        }
    }

    private static void ValidateBurstStability(AlgorithmMetadata meta, ICollection<string> failures)
    {
        var seed = HashCode.Combine(meta.Id, 2026, 7);
        var source = StringDataGenerator.Generate(
            count: 1024,
            length: 9,
            alphabet: StringAlphabetSet.Lowercase,
            distribution: StringDistributionPreset.ManyDuplicates,
            seed: seed);

        var trace = RunStringAlgorithm(
            meta.StringFactory!.Invoke(),
            source,
            new StringSortOptions(StringAlphabetSet.Lowercase, EmitExtendedEvents: true),
            new ExecutionLimits
            {
                MaxEvents = 12_000_000,
                Timeout = TimeSpan.FromSeconds(10)
            });

        if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
        {
            failures.Add($"{meta.Name}/stable-check: execution failed.");
            return;
        }

        if (!IsStableForEqualStrings(trace.FinalState))
        {
            failures.Add($"{meta.Name}/stable-check: equal-string relative order changed.");
        }
    }

    private static StringExecutionTrace RunStringAlgorithm(
        IStringSortAlgorithm algorithm,
        StringItem[] source,
        StringSortOptions options,
        ExecutionLimits limits)
    {
        var state = source.ToArray();
        var byId = source.ToDictionary(static item => item.Id, static item => item);

        var stopwatch = Stopwatch.StartNew();
        long processed = 0;
        long comparisons = 0;
        long writes = 0;
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

                if (processed % 512 == 0 && stopwatch.Elapsed > limits.Timeout)
                {
                    timedOut = true;
                    break;
                }

                switch (ev.Type)
                {
                    case SortEventType.Compare:
                    case SortEventType.CharCompare:
                        comparisons++;
                        break;

                    case SortEventType.Swap:
                        writes++;
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
                        writes++;
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

                    case SortEventType.BucketMove:
                        writes++;
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

        return new StringExecutionTrace
        {
            FinalState = state,
            DoneSeen = doneSeen,
            ProcessedEvents = processed,
            Comparisons = comparisons,
            Writes = writes,
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

    private static Dictionary<int, int> BuildIdMultiset(ReadOnlySpan<StringItem> items)
    {
        var map = new Dictionary<int, int>(items.Length);
        for (var i = 0; i < items.Length; i++)
        {
            map.TryGetValue(items[i].Id, out var count);
            map[items[i].Id] = count + 1;
        }

        return map;
    }

    private static bool IdMultisetEquals(Dictionary<int, int> left, Dictionary<int, int> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var count) || count != pair.Value)
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

    private static int[] BuildNaiveSuffixArray(string text)
    {
        return Enumerable.Range(0, text.Length)
            .OrderBy(index => text[index..], StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class StringExecutionTrace
    {
        public required StringItem[] FinalState { get; init; }
        public required bool DoneSeen { get; init; }
        public required long ProcessedEvents { get; init; }
        public required long Comparisons { get; init; }
        public required long Writes { get; init; }
        public required bool TimedOut { get; init; }
        public required bool EventLimitExceeded { get; init; }
        public required string? Error { get; init; }
    }
}
