using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BubbleSortAlgorithm : ISortAlgorithm
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var working = data.ToArray();
        return ExecuteCore(working);
    }

    private static IEnumerable<SortEvent> ExecuteCore(int[] arr)
    {
        long step = 0;

        for (var i = 0; i < arr.Length - 1; i++)
        {
            var swapped = false;
            for (var j = 0; j < arr.Length - i - 1; j++)
            {
                yield return new SortEvent(SortEventType.Compare, j, j + 1, StepId: step++);
                if (arr[j] <= arr[j + 1])
                {
                    continue;
                }

                (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                swapped = true;
                yield return new SortEvent(SortEventType.Swap, j, j + 1, StepId: step++);
            }

            if (!swapped)
            {
                break;
            }
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
