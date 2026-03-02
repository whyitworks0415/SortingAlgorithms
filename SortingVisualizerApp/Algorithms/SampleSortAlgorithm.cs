using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class SampleSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var values = Snapshot();
        var k = Math.Clamp(Options.Parallelism, 2, 16);

        EmitEvent(SortEventType.ParallelQueueDepth, value: k);
        EmitEvent(SortEventType.ParallelTaskStart, value: k);
        EmitEvent(SortEventType.MarkStage, value: 7401);

        var sample = new List<int>(k * 4);
        var stride = Math.Max(1, values.Length / (k * 4));
        for (var i = 0; i < values.Length && sample.Count < k * 4; i += stride)
        {
            sample.Add(values[i]);
        }

        sample.Sort();
        var pivots = new int[k - 1];
        for (var i = 0; i < pivots.Length; i++)
        {
            pivots[i] = sample[(i + 1) * sample.Count / k];
        }

        var buckets = new List<int>[k];
        for (var i = 0; i < k; i++)
        {
            buckets[i] = new List<int>();
        }

        for (var i = 0; i < values.Length; i++)
        {
            var b = LocateBucket(values[i], pivots);
            buckets[b].Add(values[i]);
            MarkBucket(i, b, values[i]);
        }

        Parallel.For(0, k, new ParallelOptions { MaxDegreeOfParallelism = k }, b => buckets[b].Sort());

        var index = 0;
        for (var b = 0; b < k; b++)
        {
            var bucket = buckets[b];
            EmitEvent(SortEventType.MarkRange, index, index + bucket.Count - 1, value: b);
            for (var i = 0; i < bucket.Count; i++)
            {
                Write(index++, bucket[i]);
            }
        }

        EmitEvent(SortEventType.ParallelTaskEnd, value: k);
    }

    private int[] Snapshot()
    {
        var arr = new int[Length];
        for (var i = 0; i < arr.Length; i++)
        {
            arr[i] = Read(i);
        }

        return arr;
    }

    private static int LocateBucket(int value, ReadOnlySpan<int> pivots)
    {
        var lo = 0;
        var hi = pivots.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (value <= pivots[mid])
            {
                hi = mid;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return lo;
    }
}
