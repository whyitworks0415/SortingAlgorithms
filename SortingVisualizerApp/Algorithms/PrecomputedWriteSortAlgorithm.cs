using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class PrecomputedWriteSortAlgorithm : ISortAlgorithm
{
    private readonly int[] _sorted;

    public PrecomputedWriteSortAlgorithm(int[] sorted)
    {
        _sorted = sorted.ToArray();
    }

    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var count = Math.Min(data.Length, _sorted.Length);
        return ExecuteCore(_sorted, count);
    }

    private static IEnumerable<SortEvent> ExecuteCore(int[] sorted, int count)
    {
        long step = 0;
        if (count > 0)
        {
            yield return new SortEvent(SortEventType.MarkRange, 0, count - 1, StepId: step++);
        }

        for (var i = 0; i < count; i++)
        {
            yield return new SortEvent(SortEventType.Write, i, Value: sorted[i], StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
