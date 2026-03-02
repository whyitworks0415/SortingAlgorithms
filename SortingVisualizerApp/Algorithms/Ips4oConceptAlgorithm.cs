using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class Ips4oConceptAlgorithm : ISortAlgorithm
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

        yield return new SortEvent(SortEventType.MarkStage, Value: 0, StepId: step++);
        var sampleCount = Math.Clamp((int)Math.Sqrt(n), 8, 64);
        var sample = new int[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var sourceIndex = (int)((long)i * n / sampleCount);
            sample[i] = values[Math.Clamp(sourceIndex, 0, n - 1)];
            yield return new SortEvent(SortEventType.MarkPivot, I: sourceIndex, Value: sample[i], StepId: step++);
        }

        Array.Sort(sample);
        var pivots = sample
            .Where((_, index) => index > 0 && index < sample.Length - 1 && (index % Math.Max(1, sample.Length / 4)) == 0)
            .Distinct()
            .ToArray();
        if (pivots.Length == 0)
        {
            pivots = new[] { sample[sample.Length / 2] };
        }

        yield return new SortEvent(SortEventType.MarkStage, Value: 1, I: pivots.Length, StepId: step++);
        for (var i = 0; i < n; i++)
        {
            var bucket = SelectBucket(values[i], pivots);
            yield return new SortEvent(SortEventType.MarkBucket, I: i, Value: values[i], Aux: bucket, StepId: step++);
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

    private static int SelectBucket(int value, int[] pivots)
    {
        var low = 0;
        var high = pivots.Length;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (value <= pivots[mid])
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }
}
