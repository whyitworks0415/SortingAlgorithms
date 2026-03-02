using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class InsertionSortAlgorithm : ISortAlgorithm
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var working = data.ToArray();
        return ExecuteCore(working);
    }

    private static IEnumerable<SortEvent> ExecuteCore(int[] arr)
    {
        long step = 0;

        for (var i = 1; i < arr.Length; i++)
        {
            var key = arr[i];
            yield return new SortEvent(SortEventType.MarkPivot, i, Value: key, StepId: step++);

            var j = i - 1;
            while (j >= 0)
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

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
