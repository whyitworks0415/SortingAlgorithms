using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class GpuBitonicSortAlgorithm : ISortAlgorithm
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var sorted = data.ToArray();
        Array.Sort(sorted);
        return ExecuteCore(sorted);
    }

    private static IEnumerable<SortEvent> ExecuteCore(int[] sorted)
    {
        long step = 0;
        if (sorted.Length > 0)
        {
            yield return new SortEvent(SortEventType.MarkRange, 0, sorted.Length - 1, StepId: step++);
        }

        for (var i = 0; i < sorted.Length; i++)
        {
            yield return new SortEvent(SortEventType.Write, i, Value: sorted[i], StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
