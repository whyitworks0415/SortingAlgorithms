using System.Diagnostics;

namespace SortingVisualizerApp.Core;

public static class AlgorithmBenchmarkRunner
{
    public static BenchmarkSuiteResult Run(AlgorithmRegistry registry, BenchmarkRequest request, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var results = new List<BenchmarkResult>();
        var createdAtUtc = DateTime.UtcNow;

        if (!request.HeadlessMode)
        {
            warnings.Add("Non-headless benchmark mode currently falls back to the same execution path as headless mode.");
        }

        if (request.AlgorithmIds.Count == 0)
        {
            warnings.Add("No algorithm selected for benchmark.");
            return new BenchmarkSuiteResult
            {
                CreatedAtUtc = createdAtUtc,
                Request = request,
                Results = results,
                Warnings = warnings
            };
        }

        var baseline = DataGenerator.Generate(
            Math.Clamp(request.Size, 8, 200000),
            request.Distribution,
            request.Seed);

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
                warnings.Add($"Skipped '{meta.Name}': benchmark currently supports Bars-compatible algorithms only.");
                continue;
            }

            var state = baseline.ToArray();
            var beforeCounts = BuildCounts(state);
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
                    if (processedEvents > request.MaxEvents)
                    {
                        error = $"Event limit exceeded ({request.MaxEvents:N0}).";
                        break;
                    }

                    if (stopwatch.Elapsed > request.TimeoutPerAlgorithm)
                    {
                        error = $"Timeout exceeded ({request.TimeoutPerAlgorithm.TotalSeconds:0.0}s).";
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
            var multisetPreserved = CountsEqual(beforeCounts, BuildCounts(state));

            results.Add(new BenchmarkResult
            {
                AlgorithmId = meta.Id,
                AlgorithmName = meta.Name,
                Size = state.Length,
                Distribution = request.Distribution,
                Seed = request.Seed,
                ElapsedMs = stopwatch.Elapsed.TotalMilliseconds,
                Comparisons = comparisons,
                Swaps = swaps,
                Writes = writes,
                ProcessedEvents = processedEvents,
                Sorted = sorted,
                MultisetPreserved = multisetPreserved,
                Completed = completed && error is null,
                Error = error
            });
        }

        return new BenchmarkSuiteResult
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
