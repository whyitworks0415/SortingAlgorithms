using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BitonicSortAlgorithm : ISortAlgorithm, INetworkScheduleProvider
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

        for (var k = 2; k <= padded; k <<= 1)
        {
            for (var j = k >> 1; j > 0; j >>= 1)
            {
                var stageComparators = new List<NetworkComparator>();
                for (var i = 0; i < padded; i++)
                {
                    var l = i ^ j;
                    if (l <= i || i >= length || l >= length)
                    {
                        continue;
                    }

                    stageComparators.Add(new NetworkComparator(i, l, Ascending: (i & k) == 0));
                }

                stages.Add(new NetworkStage(stageComparators));
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

        var paddedLength = NextPowerOfTwo(n);
        var working = new int[paddedLength];
        Array.Copy(original, working, n);
        for (var i = n; i < paddedLength; i++)
        {
            working[i] = int.MaxValue;
        }

        var stageIndex = 0;
        for (var k = 2; k <= paddedLength; k <<= 1)
        {
            for (var j = k >> 1; j > 0; j >>= 1)
            {
                yield return new SortEvent(SortEventType.MarkStage, Value: stageIndex, StepId: step++);

                for (var i = 0; i < paddedLength; i++)
                {
                    var l = i ^ j;
                    if (l <= i)
                    {
                        continue;
                    }

                    var ascending = (i & k) == 0;
                    if (i < n && l < n)
                    {
                        yield return new SortEvent(SortEventType.Compare, i, l, Aux: stageIndex, StepId: step++);
                    }

                    var shouldSwap = ascending ? working[i] > working[l] : working[i] < working[l];
                    if (!shouldSwap)
                    {
                        continue;
                    }

                    (working[i], working[l]) = (working[l], working[i]);

                    if (i < n && l < n)
                    {
                        yield return new SortEvent(SortEventType.Swap, i, l, Aux: stageIndex, StepId: step++);
                    }
                    else
                    {
                        if (i < n)
                        {
                            yield return new SortEvent(SortEventType.Write, i, Value: working[i], Aux: stageIndex, StepId: step++);
                        }

                        if (l < n)
                        {
                            yield return new SortEvent(SortEventType.Write, l, Value: working[l], Aux: stageIndex, StepId: step++);
                        }
                    }
                }

                stageIndex++;
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
