using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BoseNelsonNetworkSortAlgorithm : ISortAlgorithm, INetworkScheduleProvider
{
    private const int InsertionVariantLimit = 256;

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

        return UseInsertionVariant(length)
            ? BuildInsertionSchedule(length)
            : BuildPairwiseStageSchedule(length);
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
        if (UseInsertionVariant(n))
        {
            for (var i = 1; i < n; i++)
            {
                for (var j = i - 1; j >= 0; j--)
                {
                    yield return new SortEvent(SortEventType.MarkStage, Value: stageId, StepId: step++);
                    yield return new SortEvent(SortEventType.Compare, I: j, J: j + 1, Aux: stageId, StepId: step++);
                    if (values[j] > values[j + 1])
                    {
                        (values[j], values[j + 1]) = (values[j + 1], values[j]);
                        yield return new SortEvent(SortEventType.Swap, I: j, J: j + 1, Aux: stageId, StepId: step++);
                    }

                    stageId++;
                }
            }
        }
        else
        {
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
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private static bool UseInsertionVariant(int length)
    {
        return length <= InsertionVariantLimit;
    }

    private static NetworkSchedule BuildInsertionSchedule(int length)
    {
        var stages = new List<NetworkStage>(Math.Max(1, length * (length - 1) / 4));
        for (var i = 1; i < length; i++)
        {
            for (var j = i - 1; j >= 0; j--)
            {
                stages.Add(new NetworkStage(new[] { new NetworkComparator(j, j + 1, Ascending: true) }));
            }
        }

        return new NetworkSchedule(length, stages);
    }

    private static NetworkSchedule BuildPairwiseStageSchedule(int length)
    {
        var stages = new List<NetworkStage>(length);
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
}
