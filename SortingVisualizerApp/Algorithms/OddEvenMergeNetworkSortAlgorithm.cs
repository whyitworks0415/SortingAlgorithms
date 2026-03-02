using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class OddEvenMergeNetworkSortAlgorithm : ISortAlgorithm, INetworkScheduleProvider
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var original = data.ToArray();
        return ExecuteIterator(original);
    }

    public NetworkSchedule BuildSchedule(int length)
    {
        if (length <= 1)
        {
            return new NetworkSchedule(Math.Max(1, length), Array.Empty<NetworkStage>());
        }

        var padded = NextPowerOfTwo(length);
        var stages = new List<NetworkStage>();

        for (var p = 1; p < padded; p <<= 1)
        {
            for (var k = p; k > 0; k >>= 1)
            {
                for (var j = k % p; j + k < padded; j += k << 1)
                {
                    var stageComparators = new List<NetworkComparator>();
                    for (var i = 0; i < k; i++)
                    {
                        var left = i + j;
                        var right = left + k;
                        if (right >= padded)
                        {
                            continue;
                        }

                        if ((left / (p << 1)) != (right / (p << 1)))
                        {
                            continue;
                        }

                        if (left < length && right < length)
                        {
                            stageComparators.Add(new NetworkComparator(left, right, Ascending: true));
                        }
                    }

                    stages.Add(new NetworkStage(stageComparators));
                }
            }
        }

        return new NetworkSchedule(length, stages);
    }

    private static IEnumerable<SortEvent> ExecuteIterator(int[] original)
    {
        long step = 0;
        var n = original.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var padded = NextPowerOfTwo(n);
        var working = new int[padded];
        Array.Copy(original, working, n);
        for (var i = n; i < padded; i++)
        {
            working[i] = int.MaxValue;
        }

        var stageIndex = 0;
        for (var p = 1; p < padded; p <<= 1)
        {
            for (var k = p; k > 0; k >>= 1)
            {
                for (var j = k % p; j + k < padded; j += k << 1)
                {
                    yield return new SortEvent(SortEventType.MarkStage, Value: stageIndex, StepId: step++);
                    yield return new SortEvent(SortEventType.PassStart, Value: stageIndex, StepId: step++);

                    for (var i = 0; i < k; i++)
                    {
                        var left = i + j;
                        var right = left + k;
                        if (right >= padded)
                        {
                            continue;
                        }

                        if ((left / (p << 1)) != (right / (p << 1)))
                        {
                            continue;
                        }

                        if (left < n && right < n)
                        {
                            yield return new SortEvent(SortEventType.Compare, left, right, Aux: stageIndex, StepId: step++);
                        }

                        if (working[left] <= working[right])
                        {
                            continue;
                        }

                        (working[left], working[right]) = (working[right], working[left]);

                        if (left < n && right < n)
                        {
                            yield return new SortEvent(SortEventType.Swap, left, right, Aux: stageIndex, StepId: step++);
                        }
                        else
                        {
                            if (left < n)
                            {
                                yield return new SortEvent(SortEventType.Write, left, Value: working[left], Aux: stageIndex, StepId: step++);
                            }

                            if (right < n)
                            {
                                yield return new SortEvent(SortEventType.Write, right, Value: working[right], Aux: stageIndex, StepId: step++);
                            }
                        }
                    }

                    yield return new SortEvent(SortEventType.PassEnd, Value: stageIndex, StepId: step++);
                    stageIndex++;
                }
            }
        }

        for (var i = 0; i < n; i++)
        {
            if (original[i] == working[i])
            {
                continue;
            }

            original[i] = working[i];
            yield return new SortEvent(SortEventType.Write, i, Value: original[i], StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private static int NextPowerOfTwo(int value)
    {
        var n = 1;
        while (n < value)
        {
            n <<= 1;
        }

        return n;
    }
}
