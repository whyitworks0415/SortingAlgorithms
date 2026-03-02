using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class RadixLsdSortAlgorithm : ISortAlgorithm
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

        var output = new int[arr.Length];
        for (var exp = 1; max / exp > 0; exp *= 10)
        {
            var count = new int[10];
            for (var i = 0; i < arr.Length; i++)
            {
                var digit = (arr[i] / exp) % 10;
                count[digit]++;
                yield return new SortEvent(SortEventType.MarkBucket, i, Aux: digit, Value: arr[i], StepId: step++);
            }

            for (var i = 1; i < count.Length; i++)
            {
                count[i] += count[i - 1];
            }

            for (var i = arr.Length - 1; i >= 0; i--)
            {
                var value = arr[i];
                var digit = (value / exp) % 10;
                var pos = --count[digit];
                output[pos] = value;
                yield return new SortEvent(SortEventType.MarkBucket, pos, Aux: digit, Value: value, StepId: step++);
            }

            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = output[i];
                yield return new SortEvent(SortEventType.Write, i, Value: arr[i], StepId: step++);
            }
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
