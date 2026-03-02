using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class DualPivotQuickSortAlgorithm : ISortAlgorithm
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

        IEnumerable<SortEvent> Sort(int left, int right)
        {
            if (left >= right)
            {
                yield break;
            }

            yield return new SortEvent(SortEventType.MarkRange, left, right, StepId: step++);

            yield return new SortEvent(SortEventType.Compare, left, right, StepId: step++);
            if (arr[left] > arr[right])
            {
                (arr[left], arr[right]) = (arr[right], arr[left]);
                yield return new SortEvent(SortEventType.Swap, left, right, StepId: step++);
            }

            var p = arr[left];
            var q = arr[right];
            yield return new SortEvent(SortEventType.MarkPivot, left, Value: p, StepId: step++);
            yield return new SortEvent(SortEventType.MarkPivot, right, Value: q, StepId: step++);

            var l = left + 1;
            var g = right - 1;
            var k = l;

            while (k <= g)
            {
                yield return new SortEvent(SortEventType.Compare, k, left, StepId: step++);
                if (arr[k] < p)
                {
                    if (k != l)
                    {
                        (arr[k], arr[l]) = (arr[l], arr[k]);
                        yield return new SortEvent(SortEventType.Swap, k, l, StepId: step++);
                    }

                    l++;
                    k++;
                    continue;
                }

                yield return new SortEvent(SortEventType.Compare, k, right, StepId: step++);
                if (arr[k] >= q)
                {
                    while (arr[g] > q && k < g)
                    {
                        yield return new SortEvent(SortEventType.Compare, g, right, StepId: step++);
                        g--;
                    }

                    if (k != g)
                    {
                        (arr[k], arr[g]) = (arr[g], arr[k]);
                        yield return new SortEvent(SortEventType.Swap, k, g, StepId: step++);
                    }

                    g--;
                    yield return new SortEvent(SortEventType.Compare, k, left, StepId: step++);
                    if (arr[k] < p)
                    {
                        if (k != l)
                        {
                            (arr[k], arr[l]) = (arr[l], arr[k]);
                            yield return new SortEvent(SortEventType.Swap, k, l, StepId: step++);
                        }

                        l++;
                    }
                }

                k++;
            }

            l--;
            g++;

            if (left != l)
            {
                (arr[left], arr[l]) = (arr[l], arr[left]);
                yield return new SortEvent(SortEventType.Swap, left, l, StepId: step++);
            }

            if (right != g)
            {
                (arr[right], arr[g]) = (arr[g], arr[right]);
                yield return new SortEvent(SortEventType.Swap, right, g, StepId: step++);
            }

            foreach (var ev in Sort(left, l - 1))
            {
                yield return ev;
            }

            foreach (var ev in Sort(l + 1, g - 1))
            {
                yield return ev;
            }

            foreach (var ev in Sort(g + 1, right))
            {
                yield return ev;
            }
        }

        foreach (var ev in Sort(0, arr.Length - 1))
        {
            yield return ev;
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
