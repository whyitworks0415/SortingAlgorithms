using System.Diagnostics;
using SortingVisualizerApp.Algorithms;
using SortingVisualizerApp.Core;

internal static class SpatialValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: status=A and Spatial-supported algorithms.",
            "Checks: key ascending order, point-id multiset preserved, key-computation consistency/determinism, timeout/event-limit guard."
        };

        var metas = registry.All
            .Where(static meta => meta.Status == AlgorithmImplementationStatus.A
                                  && (meta.SupportedViews & SupportedViews.Spatial) != 0
                                  && meta.SpatialFactory is not null)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runs = 0;
        foreach (var meta in metas)
        {
            var keySelector = BuildKeySelector(meta);

            foreach (var size in new[] { 256, 2048 })
            {
                foreach (var distribution in new[]
                         {
                             SpatialDistributionPreset.Uniform,
                             SpatialDistributionPreset.Gaussian,
                             SpatialDistributionPreset.Clusters
                         })
                {
                    runs++;
                    var seed = HashCode.Combine(meta.Id, size, distribution, 260228);
                    var source = SpatialDataGenerator.Generate(size, distribution, seed);
                    var baseline = BuildIdMultiset(source);

                    var trace = RunSpatialAlgorithm(
                        meta.SpatialFactory!.Invoke(),
                        source,
                        new SpatialSortOptions(EmitExtendedEvents: true),
                        keySelector,
                        new ExecutionLimits
                        {
                            MaxEvents = size >= 2048 ? 20_000_000 : 5_000_000,
                            Timeout = size >= 2048 ? TimeSpan.FromSeconds(18) : TimeSpan.FromSeconds(8)
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

                    if (keySelector is not null && trace.KeyMismatch)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: key event mismatch against reference key function.");
                    }

                    if (keySelector is not null && !IsAscendingByKey(trace.FinalState, keySelector))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: final point order is not key-ascending.");
                    }

                    if (!IsAscending(trace.Keys))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: emitted keys are not ascending.");
                    }

                    if (!IdMultisetEquals(baseline, BuildIdMultiset(trace.FinalState)))
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: id multiset changed.");
                    }

                    if (trace.Comparisons + trace.Swaps + trace.Writes == 0)
                    {
                        failures.Add($"{meta.Name}/{distribution}/N={size}: no compare/swap/write events.");
                    }

                    if (keySelector is null)
                    {
                        var trace2 = RunSpatialAlgorithm(
                            meta.SpatialFactory!.Invoke(),
                            source,
                            new SpatialSortOptions(EmitExtendedEvents: true),
                            keySelector,
                            new ExecutionLimits
                            {
                                MaxEvents = size >= 2048 ? 20_000_000 : 5_000_000,
                                Timeout = size >= 2048 ? TimeSpan.FromSeconds(18) : TimeSpan.FromSeconds(8)
                            });

                        if (trace2.Error is not null || trace2.TimedOut || trace2.EventLimitExceeded || !trace2.DoneSeen)
                        {
                            failures.Add($"{meta.Name}/{distribution}/N={size}: deterministic rerun failed.");
                        }
                        else
                        {
                            if (!trace.Keys.AsSpan().SequenceEqual(trace2.Keys))
                            {
                                failures.Add($"{meta.Name}/{distribution}/N={size}: key sequence is not deterministic.");
                            }

                            if (!trace.FinalState.AsSpan().SequenceEqual(trace2.FinalState))
                            {
                                failures.Add($"{meta.Name}/{distribution}/N={size}: final order is not deterministic.");
                            }
                        }
                    }
                }
            }
        }

        return new ValidationSuiteResult
        {
            Name = "Spatial",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static Func<SpatialPoint, uint>? BuildKeySelector(AlgorithmMetadata metadata)
    {
        if (metadata.Name.Contains("Hilbert", StringComparison.OrdinalIgnoreCase))
        {
            return static point => SpatialKeyUtils.HilbertKey16(point.X, point.Y);
        }

        if (metadata.Name.Contains("Spatial Sort", StringComparison.OrdinalIgnoreCase))
        {
            return static point => SpatialSortAlgorithm.SpatialLexicographicKey16(point);
        }

        if (metadata.Name.Contains("KD-Tree", StringComparison.OrdinalIgnoreCase)
            || metadata.Name.Contains("QuadTree", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (metadata.Name.Contains("Z-Order", StringComparison.OrdinalIgnoreCase))
        {
            return static point => SpatialKeyUtils.ZOrderKey16(point.X, point.Y);
        }

        return static point => SpatialKeyUtils.MortonKey16(point.X, point.Y);
    }

    private static SpatialExecutionTrace RunSpatialAlgorithm(
        ISpatialSortAlgorithm algorithm,
        SpatialPoint[] source,
        SpatialSortOptions options,
        Func<SpatialPoint, uint>? keySelector,
        ExecutionLimits limits)
    {
        var state = source.ToArray();
        var keys = new uint[state.Length];

        var stopwatch = Stopwatch.StartNew();
        long processed = 0;
        long comparisons = 0;
        long swaps = 0;
        long writes = 0;
        var doneSeen = false;
        var timedOut = false;
        var eventLimitExceeded = false;
        var keyMismatch = false;
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
                        comparisons++;
                        break;

                    case SortEventType.PointSwap:
                    case SortEventType.Swap:
                        swaps++;
                        if (InRange(ev.I, state.Length) && InRange(ev.J, state.Length))
                        {
                            (state[ev.I], state[ev.J]) = (state[ev.J], state[ev.I]);
                            (keys[ev.I], keys[ev.J]) = (keys[ev.J], keys[ev.I]);
                        }
                        else
                        {
                            error ??= $"Swap index out of range ({ev.I},{ev.J}).";
                        }
                        break;

                    case SortEventType.PointKeyComputed:
                    case SortEventType.OrderUpdate:
                        writes++;
                        if (!InRange(ev.I, state.Length))
                        {
                            error ??= $"Key event index out of range ({ev.I}).";
                            break;
                        }

                        var emittedKey = unchecked((uint)ev.Value);
                        if (keySelector is not null && ev.Type == SortEventType.PointKeyComputed)
                        {
                            var expectedKey = keySelector(state[ev.I]);
                            if (emittedKey != expectedKey)
                            {
                                keyMismatch = true;
                            }
                        }

                        keys[ev.I] = emittedKey;
                        break;

                    case SortEventType.Write:
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

        return new SpatialExecutionTrace
        {
            FinalState = state,
            Keys = keys,
            DoneSeen = doneSeen,
            ProcessedEvents = processed,
            Comparisons = comparisons,
            Swaps = swaps,
            Writes = writes,
            TimedOut = timedOut,
            EventLimitExceeded = eventLimitExceeded,
            KeyMismatch = keyMismatch,
            Error = error
        };
    }

    private static bool IsAscendingByKey(ReadOnlySpan<SpatialPoint> points, Func<SpatialPoint, uint> keySelector)
    {
        if (points.Length <= 1)
        {
            return true;
        }

        var previous = keySelector(points[0]);
        for (var i = 1; i < points.Length; i++)
        {
            var current = keySelector(points[i]);
            if (previous > current)
            {
                return false;
            }

            previous = current;
        }

        return true;
    }

    private static bool IsAscending(ReadOnlySpan<uint> keys)
    {
        for (var i = 1; i < keys.Length; i++)
        {
            if (keys[i - 1] > keys[i])
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<int, int> BuildIdMultiset(ReadOnlySpan<SpatialPoint> points)
    {
        var map = new Dictionary<int, int>(points.Length);
        for (var i = 0; i < points.Length; i++)
        {
            map.TryGetValue(points[i].Id, out var count);
            map[points[i].Id] = count + 1;
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

    private sealed class SpatialExecutionTrace
    {
        public required SpatialPoint[] FinalState { get; init; }
        public required uint[] Keys { get; init; }
        public required bool DoneSeen { get; init; }
        public required long ProcessedEvents { get; init; }
        public required long Comparisons { get; init; }
        public required long Swaps { get; init; }
        public required long Writes { get; init; }
        public required bool TimedOut { get; init; }
        public required bool EventLimitExceeded { get; init; }
        public required bool KeyMismatch { get; init; }
        public required string? Error { get; init; }
    }
}
