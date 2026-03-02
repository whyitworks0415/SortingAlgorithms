using System.Diagnostics;

namespace SortingVisualizerApp.Core;

public static class GrowthBenchmarkRunner
{
    public static GrowthBenchmarkSuiteResult Run(
        AlgorithmRegistry registry,
        GrowthBenchmarkRequest request,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var results = new List<GrowthBenchmarkPointResult>();
        var createdAtUtc = DateTime.UtcNow;

        if (!request.HeadlessMode)
        {
            warnings.Add("Non-headless growth mode currently uses the same execution path as headless mode.");
        }

        var sizes = request.Sizes
            .Where(static n => n >= 8)
            .Distinct()
            .OrderBy(static n => n)
            .ToArray();

        if (sizes.Length == 0)
        {
            warnings.Add("Growth run skipped: no valid sizes.");
            return new GrowthBenchmarkSuiteResult
            {
                CreatedAtUtc = createdAtUtc,
                Request = request,
                Results = results,
                Warnings = warnings
            };
        }

        foreach (var algorithmId in request.AlgorithmIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!registry.TryCreate(algorithmId, out var meta, out var algorithm) || algorithm is null)
            {
                warnings.Add($"Skipped '{algorithmId}': factory not available.");
                continue;
            }

            if ((meta.SupportedViews & SupportedViews.Bars) == 0)
            {
                warnings.Add($"Skipped '{meta.Name}': growth benchmark supports Bars algorithms only.");
                continue;
            }

            foreach (var size in sizes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var runSeed = HashCode.Combine(request.Seed, size);
                var state = DataGenerator.Generate(size, request.Distribution, runSeed);
                var baselineCounts = BuildCounts(state);
                var maxValue = state.Length == 0 ? 1 : Math.Max(1, state.Max());
                var options = new SortOptions(MaxValue: maxValue, EmitExtendedEvents: true);

                long comparisons = 0;
                long swaps = 0;
                long writes = 0;
                long processedEvents = 0;
                var completed = false;
                string? error = null;

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    foreach (var ev in algorithm.Execute(state.AsSpan(), options))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        processedEvents++;
                        if (processedEvents > request.MaxEventsPerRun)
                        {
                            error = $"Event limit exceeded ({request.MaxEventsPerRun:N0}).";
                            break;
                        }

                        if (stopwatch.Elapsed > request.TimeoutPerRun)
                        {
                            error = $"Timeout exceeded ({request.TimeoutPerRun.TotalSeconds:0.0}s).";
                            break;
                        }

                        switch (ev.Type)
                        {
                            case SortEventType.Compare:
                                comparisons++;
                                break;

                            case SortEventType.Swap:
                                swaps++;
                                if (IsInRange(ev.I, state.Length) && IsInRange(ev.J, state.Length))
                                {
                                    (state[ev.I], state[ev.J]) = (state[ev.J], state[ev.I]);
                                }
                                else
                                {
                                    error ??= $"Swap index out of range ({ev.I}, {ev.J}).";
                                }
                                break;

                            case SortEventType.Write:
                                writes++;
                                if (IsInRange(ev.I, state.Length))
                                {
                                    state[ev.I] = ev.Value;
                                }
                                else
                                {
                                    error ??= $"Write index out of range ({ev.I}).";
                                }
                                break;

                            case SortEventType.Done:
                                completed = true;
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

                var sorted = IsSortedAscending(state);
                var multisetPreserved = CountsEqual(baselineCounts, BuildCounts(state));

                results.Add(new GrowthBenchmarkPointResult
                {
                    AlgorithmId = meta.Id,
                    AlgorithmName = meta.Name,
                    Size = size,
                    Distribution = request.Distribution,
                    Seed = runSeed,
                    ElapsedMs = stopwatch.Elapsed.TotalMilliseconds,
                    Comparisons = comparisons,
                    Swaps = swaps,
                    Writes = writes,
                    ProcessedEvents = processedEvents,
                    Completed = completed && error is null,
                    Sorted = sorted,
                    MultisetPreserved = multisetPreserved,
                    Error = error
                });
            }
        }

        return new GrowthBenchmarkSuiteResult
        {
            CreatedAtUtc = createdAtUtc,
            Request = request,
            Results = results,
            Warnings = warnings
        };
    }

    private static bool IsInRange(int index, int length)
    {
        return index >= 0 && index < length;
    }

    private static bool IsSortedAscending(ReadOnlySpan<int> data)
    {
        for (var i = 1; i < data.Length; i++)
        {
            if (data[i - 1] > data[i])
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<int, int> BuildCounts(ReadOnlySpan<int> data)
    {
        var counts = new Dictionary<int, int>(data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            counts.TryGetValue(data[i], out var current);
            counts[data[i]] = current + 1;
        }

        return counts;
    }

    private static bool CountsEqual(Dictionary<int, int> left, Dictionary<int, int> right)
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
}
