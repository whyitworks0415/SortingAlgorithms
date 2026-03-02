using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class PairwiseNetworkSortAlgorithm : ISortAlgorithm, INetworkScheduleProvider
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        return ExecuteIterator(data.ToArray());
    }

    public NetworkSchedule BuildSchedule(int length)
    {
        if (length <= 1)
        {
            return new NetworkSchedule(Math.Max(1, length), Array.Empty<NetworkStage>());
        }

        var stages = new List<NetworkStage>(length + 16);

        var gap = HighestPowerOfTwoLessOrEqual(length / 2);
        for (; gap >= 1; gap >>= 1)
        {
            var comparators = new List<NetworkComparator>(length / 2);
            for (var start = 0; start < length; start += gap * 2)
            {
                for (var i = 0; i < gap; i++)
                {
                    var left = start + i;
                    var right = left + gap;
                    if (right >= length)
                    {
                        break;
                    }

                    comparators.Add(new NetworkComparator(left, right, Ascending: true));
                }
            }

            if (comparators.Count > 0)
            {
                stages.Add(new NetworkStage(comparators));
            }
        }

        for (var pass = 0; pass < length; pass++)
        {
            var comparators = new List<NetworkComparator>(length / 2);
            var start = pass & 1;
            for (var i = start; i + 1 < length; i += 2)
            {
                comparators.Add(new NetworkComparator(i, i + 1, Ascending: true));
            }

            stages.Add(new NetworkStage(comparators));
        }

        return new NetworkSchedule(length, stages);
    }

    private static IEnumerable<SortEvent> ExecuteIterator(int[] values)
    {
        long step = 0;
        var n = values.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var stageId = 0;
        var gap = HighestPowerOfTwoLessOrEqual(n / 2);
        for (; gap >= 1; gap >>= 1)
        {
            yield return new SortEvent(SortEventType.MarkStage, Value: stageId, StepId: step++);
            for (var start = 0; start < n; start += gap * 2)
            {
                for (var i = 0; i < gap; i++)
                {
                    var left = start + i;
                    var right = left + gap;
                    if (right >= n)
                    {
                        break;
                    }

                    yield return new SortEvent(SortEventType.Compare, I: left, J: right, Aux: stageId, StepId: step++);
                    if (values[left] > values[right])
                    {
                        (values[left], values[right]) = (values[right], values[left]);
                        yield return new SortEvent(SortEventType.Swap, I: left, J: right, Aux: stageId, StepId: step++);
                    }
                }
            }

            stageId++;
        }

        for (var pass = 0; pass < n; pass++)
        {
            var start = pass & 1;
            yield return new SortEvent(SortEventType.MarkStage, Value: stageId, StepId: step++);
            for (var i = start; i + 1 < n; i += 2)
            {
                yield return new SortEvent(SortEventType.Compare, I: i, J: i + 1, Aux: stageId, StepId: step++);
                if (values[i] > values[i + 1])
                {
                    (values[i], values[i + 1]) = (values[i + 1], values[i]);
                    yield return new SortEvent(SortEventType.Swap, I: i, J: i + 1, Aux: stageId, StepId: step++);
                }
            }

            stageId++;
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private static int HighestPowerOfTwoLessOrEqual(int value)
    {
        if (value <= 1)
        {
            return 1;
        }

        var power = 1;
        while ((power << 1) <= value)
        {
            power <<= 1;
        }

        return power;
    }
}
