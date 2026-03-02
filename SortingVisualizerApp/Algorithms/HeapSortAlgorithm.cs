using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class HeapSortAlgorithm : ISortAlgorithm
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var working = data.ToArray();
        return ExecuteCore(working);
    }

    private static IEnumerable<SortEvent> ExecuteCore(int[] arr)
    {
        long step = 0;
        var n = arr.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        IEnumerable<SortEvent> SiftDown(int start, int endExclusive)
        {
            var root = start;
            yield return new SortEvent(SortEventType.MarkRange, 0, endExclusive - 1, StepId: step++);
            while (true)
            {
                var left = root * 2 + 1;
                if (left >= endExclusive)
                {
                    yield break;
                }

                var child = left;
                var right = left + 1;

                if (right < endExclusive)
                {
                    yield return new SortEvent(SortEventType.Compare, left, right, StepId: step++);
                    if (arr[right] > arr[left])
                    {
                        child = right;
                    }
                }

                yield return new SortEvent(SortEventType.Compare, root, child, StepId: step++);
                if (arr[root] >= arr[child])
                {
                    yield break;
                }

                (arr[root], arr[child]) = (arr[child], arr[root]);
                yield return new SortEvent(SortEventType.Swap, root, child, StepId: step++);
                root = child;
            }
        }

        for (var i = n / 2 - 1; i >= 0; i--)
        {
            foreach (var ev in SiftDown(i, n))
            {
                yield return ev;
            }
        }

        for (var end = n - 1; end > 0; end--)
        {
            yield return new SortEvent(SortEventType.MarkRange, 0, end, StepId: step++);
            (arr[0], arr[end]) = (arr[end], arr[0]);
            yield return new SortEvent(SortEventType.Swap, 0, end, StepId: step++);

            foreach (var ev in SiftDown(0, end))
            {
                yield return ev;
            }
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
