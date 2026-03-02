using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class CountingSortAlgorithm : ISortAlgorithm
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

        var min = arr[0];
        var max = arr[0];
        for (var i = 1; i < arr.Length; i++)
        {
            if (arr[i] < min)
            {
                min = arr[i];
            }

            if (arr[i] > max)
            {
                max = arr[i];
            }
        }

        var range = max - min + 1;
        var counts = new int[range];

        for (var i = 0; i < arr.Length; i++)
        {
            var bucket = arr[i] - min;
            counts[bucket]++;
            yield return new SortEvent(SortEventType.MarkBucket, i, Aux: bucket, Value: counts[bucket], StepId: step++);
        }

        for (var i = 1; i < counts.Length; i++)
        {
            counts[i] += counts[i - 1];
        }

        var output = new int[arr.Length];
        for (var i = arr.Length - 1; i >= 0; i--)
        {
            var value = arr[i];
            var bucket = value - min;
            var pos = --counts[bucket];
            output[pos] = value;
            yield return new SortEvent(SortEventType.MarkBucket, pos, Aux: bucket, Value: value, StepId: step++);
        }

        for (var i = 0; i < arr.Length; i++)
        {
            arr[i] = output[i];
            yield return new SortEvent(SortEventType.Write, i, Value: arr[i], StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
