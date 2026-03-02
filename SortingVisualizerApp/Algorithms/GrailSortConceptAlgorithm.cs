using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class GrailSortConceptAlgorithm : ISortAlgorithm
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        return ExecuteIterator(data.ToArray());
    }

    private static IEnumerable<SortEvent> ExecuteIterator(int[] values)
    {
        long step = 0;
        var n = values.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var blockSize = Math.Clamp((int)Math.Sqrt(n), 8, 1024);
        yield return new SortEvent(SortEventType.MarkStage, Value: 0, I: blockSize, StepId: step++);

        for (var start = 0; start < n; start += blockSize)
        {
            var end = Math.Min(n - 1, start + blockSize - 1);
            yield return new SortEvent(SortEventType.MarkRange, I: start, J: end, Aux: blockSize, StepId: step++);
        }

        yield return new SortEvent(SortEventType.MarkStage, Value: 1, I: blockSize, StepId: step++);
        for (var width = blockSize; width < n; width <<= 1)
        {
            for (var left = 0; left < n; left += width << 1)
            {
                var right = Math.Min(n - 1, left + (width << 1) - 1);
                yield return new SortEvent(SortEventType.MarkRange, I: left, J: right, Aux: width, StepId: step++);
            }
        }

        yield return new SortEvent(SortEventType.MarkStage, Value: 2, StepId: step++);
        var sorted = values.ToArray();
        Array.Sort(sorted);
        for (var i = 0; i < n; i++)
        {
            values[i] = sorted[i];
            yield return new SortEvent(SortEventType.Write, I: i, Value: values[i], StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
