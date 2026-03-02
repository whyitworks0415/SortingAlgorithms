using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class TimSortAlgorithm : ISortAlgorithm
{
    private const int MinRun = 32;

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

        IEnumerable<SortEvent> Merge(int left, int mid, int right)
        {
            yield return new SortEvent(SortEventType.MarkRange, left, right, Aux: mid, StepId: step++);

            var len1 = mid - left + 1;
            var len2 = right - mid;
            var leftPart = new int[len1];
            var rightPart = new int[len2];

            Array.Copy(arr, left, leftPart, 0, len1);
            Array.Copy(arr, mid + 1, rightPart, 0, len2);

            var i = 0;
            var j = 0;
            var k = left;

            while (i < len1 && j < len2)
            {
                yield return new SortEvent(SortEventType.Compare, left + i, mid + 1 + j, StepId: step++);
                if (leftPart[i] <= rightPart[j])
                {
                    arr[k] = leftPart[i++];
                }
                else
                {
                    arr[k] = rightPart[j++];
                }

                yield return new SortEvent(SortEventType.Write, k, Value: arr[k], StepId: step++);
                k++;
            }

            while (i < len1)
            {
                arr[k] = leftPart[i++];
                yield return new SortEvent(SortEventType.Write, k, Value: arr[k], StepId: step++);
                k++;
            }

            while (j < len2)
            {
                arr[k] = rightPart[j++];
                yield return new SortEvent(SortEventType.Write, k, Value: arr[k], StepId: step++);
                k++;
            }
        }

        for (var start = 0; start < n; start += MinRun)
        {
            var end = Math.Min(start + MinRun - 1, n - 1);
            foreach (var ev in InsertionSort(start, end))
            {
                yield return ev;
            }
        }

        for (var run = MinRun; run < n; run *= 2)
        {
            for (var left = 0; left < n; left += 2 * run)
            {
                var mid = Math.Min(left + run - 1, n - 1);
                var right = Math.Min(left + 2 * run - 1, n - 1);
                if (mid >= right)
                {
                    continue;
                }

                foreach (var ev in Merge(left, mid, right))
                {
                    yield return ev;
                }
            }
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
