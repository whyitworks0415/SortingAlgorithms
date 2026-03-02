using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class QuickSortAlgorithm : ISortAlgorithm
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var working = data.ToArray();
        return ExecuteCore(working);
    }

    private static IEnumerable<SortEvent> ExecuteCore(int[] arr)
    {
        long step = 0;
        if (arr.Length <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var stack = new Stack<(int Left, int Right)>();
        stack.Push((0, arr.Length - 1));

        while (stack.Count > 0)
        {
            var (left, right) = stack.Pop();
            if (left >= right)
            {
                continue;
            }

            var partitionEvents = new List<SortEvent>(Math.Max(16, right - left + 4));
            var i = RawSortCommon.LomutoPartition(arr, left, right, ref step, partitionEvents);
            for (var e = 0; e < partitionEvents.Count; e++)
            {
                yield return partitionEvents[e];
            }

            // Push larger range first to keep stack depth bounded.
            var leftRange = i - 1 - left;
            var rightRange = right - (i + 1);
            if (leftRange > rightRange)
            {
                stack.Push((left, i - 1));
                stack.Push((i + 1, right));
            }
            else
            {
                stack.Push((i + 1, right));
                stack.Push((left, i - 1));
            }
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
