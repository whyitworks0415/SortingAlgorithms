using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class RadixMsdSortAlgorithm : ISortAlgorithm
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

        var max = 0;
        for (var i = 0; i < arr.Length; i++)
        {
            if (arr[i] > max)
            {
                max = arr[i];
            }
        }

        var exp = 1;
        while (max / exp >= 10)
        {
            exp *= 10;
        }

        var buffer = new int[arr.Length];

        IEnumerable<SortEvent> InsertionSort(int left, int right)
        {
            for (var i = left + 1; i <= right; i++)
            {
                var key = arr[i];
                yield return new SortEvent(SortEventType.MarkPivot, i, Value: key, StepId: step++);
                var j = i - 1;
                while (j >= left)
                {
                    yield return new SortEvent(SortEventType.Compare, j, i, StepId: step++);
                    if (arr[j] <= key)
                    {
                        break;
                    }

                    arr[j + 1] = arr[j];
                    yield return new SortEvent(SortEventType.Write, j + 1, Value: arr[j], StepId: step++);
                    j--;
                }

                arr[j + 1] = key;
                yield return new SortEvent(SortEventType.Write, j + 1, Value: key, StepId: step++);
            }
        }

        IEnumerable<SortEvent> SortRange(int left, int right, int divisor)
        {
            if (left >= right || divisor <= 0)
            {
                yield break;
            }

            if (right - left <= 16)
            {
                foreach (var ev in InsertionSort(left, right))
                {
                    yield return ev;
                }

                yield break;
            }

            yield return new SortEvent(SortEventType.MarkRange, left, right, Aux: divisor, StepId: step++);

            var counts = new int[10];
            for (var i = left; i <= right; i++)
            {
                var digit = (arr[i] / divisor) % 10;
                counts[digit]++;
                yield return new SortEvent(SortEventType.MarkBucket, i, Aux: digit, Value: arr[i], StepId: step++);
            }

            var offsets = new int[10];
            offsets[0] = left;
            for (var d = 1; d < 10; d++)
            {
                offsets[d] = offsets[d - 1] + counts[d - 1];
            }

            var next = offsets.ToArray();
            for (var i = left; i <= right; i++)
            {
                var value = arr[i];
                var digit = (value / divisor) % 10;
                var pos = next[digit]++;
                buffer[pos] = value;
            }

            for (var i = left; i <= right; i++)
            {
                arr[i] = buffer[i];
                yield return new SortEvent(SortEventType.Write, i, Value: arr[i], StepId: step++);
            }

            if (divisor >= 10)
            {
                for (var d = 0; d < 10; d++)
                {
                    var start = offsets[d];
                    var end = start + counts[d] - 1;
                    if (start < end)
                    {
                        foreach (var ev in SortRange(start, end, divisor / 10))
                        {
                            yield return ev;
                        }
                    }
                }
            }
        }

        foreach (var ev in SortRange(0, arr.Length - 1, exp))
        {
            yield return ev;
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
