using System.Diagnostics;
using SortingVisualizerApp.Core;

internal static class ExecutionHarness
{
    public static AlgorithmExecutionTrace RunAlgorithm(
        ISortAlgorithm algorithm,
        int[] source,
        SortOptions options,
        ExecutionLimits limits,
        bool captureEvents)
    {
        var state = source.ToArray();
        var events = captureEvents ? new List<SortEvent>(Math.Min(32_768, source.Length * 16)) : null;

        var stopwatch = Stopwatch.StartNew();
        long processed = 0;
        long compares = 0;
        long swaps = 0;
        long writes = 0;
        var doneSeen = false;
        var timedOut = false;
        var eventLimitExceeded = false;
        string? error = null;

        try
        {
            foreach (var ev in algorithm.Execute(source.AsSpan(), options))
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

                if (captureEvents)
                {
                    events!.Add(ev);
                }

                switch (ev.Type)
                {
                    case SortEventType.Compare:
                        compares++;
                        break;
                    case SortEventType.Swap:
                        swaps++;
                        if (IsInRange(ev.I, state.Length) && IsInRange(ev.J, state.Length))
                        {
                            (state[ev.I], state[ev.J]) = (state[ev.J], state[ev.I]);
                        }
                        else
                        {
                            error ??= $"Swap index out of range ({ev.I},{ev.J})";
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
                            error ??= $"Write index out of range ({ev.I})";
                        }
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

        return new AlgorithmExecutionTrace
        {
            FinalState = state,
            DoneSeen = doneSeen,
            ProcessedEvents = processed,
            Comparisons = compares,
            Swaps = swaps,
            Writes = writes,
            TimedOut = timedOut,
            EventLimitExceeded = eventLimitExceeded,
            Error = error,
            Events = events is null ? Array.Empty<SortEvent>() : events
        };
    }

    public static bool IsSortedAscending(ReadOnlySpan<int> data)
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

    public static Dictionary<int, int> BuildMultiset(ReadOnlySpan<int> data)
    {
        var map = new Dictionary<int, int>(data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            map.TryGetValue(data[i], out var count);
            map[data[i]] = count + 1;
        }

        return map;
    }

    public static bool MultisetEquals(Dictionary<int, int> left, Dictionary<int, int> right)
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

    private static bool IsInRange(int index, int length)
    {
        return index >= 0 && index < length;
    }
}
