using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class IntroSortAlgorithm : ISortAlgorithm
{
    private const int InsertionThreshold = 16;

    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var working = data.ToArray();
        return ExecuteCore(working);
    }

    private static IEnumerable<SortEvent> ExecuteCore(int[] arr)
    {
        long step = 0;
        var depthLimit = 2 * (int)Math.Floor(Math.Log2(Math.Max(2, arr.Length)));

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

        IEnumerable<SortEvent> HeapSortRange(int left, int right)
        {
            var length = right - left + 1;
            if (length <= 1)
            {
                yield break;
            }

            IEnumerable<SortEvent> SiftDown(int start, int endExclusive)
            {
                var root = start;
                while (true)
                {
                    var child = root * 2 + 1;
                    if (child >= endExclusive)
                    {
                        yield break;
                    }

                    if (child + 1 < endExclusive)
                    {
                        yield return new SortEvent(SortEventType.Compare, left + child, left + child + 1, StepId: step++);
                        if (arr[left + child] < arr[left + child + 1])
                        {
                            child++;
                        }
                    }

                    yield return new SortEvent(SortEventType.Compare, left + root, left + child, StepId: step++);
                    if (arr[left + root] >= arr[left + child])
                    {
                        yield break;
                    }

                    (arr[left + root], arr[left + child]) = (arr[left + child], arr[left + root]);
                    yield return new SortEvent(SortEventType.Swap, left + root, left + child, StepId: step++);
                    root = child;
                }
            }

            for (var i = length / 2 - 1; i >= 0; i--)
            {
                foreach (var ev in SiftDown(i, length))
                {
                    yield return ev;
                }
            }

            for (var end = length - 1; end > 0; end--)
            {
                (arr[left], arr[left + end]) = (arr[left + end], arr[left]);
                yield return new SortEvent(SortEventType.Swap, left, left + end, StepId: step++);

                foreach (var ev in SiftDown(0, end))
                {
                    yield return ev;
                }
            }
        }

        IEnumerable<SortEvent> Intro(int left, int right, int depth)
        {
            if (right <= left)
            {
                yield break;
            }

            var length = right - left + 1;
            if (length <= InsertionThreshold)
            {
                foreach (var ev in InsertionSort(left, right))
                {
                    yield return ev;
                }

                yield break;
            }

            if (depth <= 0)
            {
                foreach (var ev in HeapSortRange(left, right))
                {
                    yield return ev;
                }

                yield break;
            }

            var partitionEvents = new List<SortEvent>(Math.Max(16, right - left + 4));
            var i = RawSortCommon.LomutoPartition(arr, left, right, ref step, partitionEvents);
            foreach (var ev in partitionEvents)
            {
                yield return ev;
            }

            foreach (var ev in Intro(left, i - 1, depth - 1))
            {
                yield return ev;
            }

            foreach (var ev in Intro(i + 1, right, depth - 1))
            {
                yield return ev;
            }
        }

        foreach (var ev in Intro(0, arr.Length - 1, depthLimit))
        {
            yield return ev;
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
