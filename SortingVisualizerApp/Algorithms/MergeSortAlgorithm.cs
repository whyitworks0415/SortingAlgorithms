using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class MergeSortAlgorithm : ISortAlgorithm
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

        var aux = new int[n];
        for (var width = 1; width < n; width *= 2)
        {
            for (var left = 0; left < n; left += width * 2)
            {
                var mid = Math.Min(left + width, n);
                var right = Math.Min(left + width * 2, n);

                if (mid >= right)
                {
                    continue;
                }

                yield return new SortEvent(SortEventType.MarkRange, left, right - 1, Aux: mid, StepId: step++);

                Array.Copy(arr, left, aux, left, right - left);

                var i = left;
                var j = mid;
                for (var k = left; k < right; k++)
                {
                    if (i >= mid)
                    {
                        arr[k] = aux[j++];
                        yield return new SortEvent(SortEventType.Write, k, Value: arr[k], StepId: step++);
                    }
                    else if (j >= right)
                    {
                        arr[k] = aux[i++];
                        yield return new SortEvent(SortEventType.Write, k, Value: arr[k], StepId: step++);
                    }
                    else
                    {
                        yield return new SortEvent(SortEventType.Compare, i, j, StepId: step++);
                        if (aux[i] <= aux[j])
                        {
                            arr[k] = aux[i++];
                            yield return new SortEvent(SortEventType.Write, k, Value: arr[k], StepId: step++);
                        }
                        else
                        {
                            arr[k] = aux[j++];
                            yield return new SortEvent(SortEventType.Write, k, Value: arr[k], StepId: step++);
                        }
                    }
                }
            }
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
