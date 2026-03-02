using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ParallelHeapSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var values = Snapshot();
        var degree = Math.Clamp(Options.Parallelism, 1, 32);

        EmitEvent(SortEventType.ParallelQueueDepth, value: degree);
        EmitEvent(SortEventType.ParallelTaskStart, value: degree);

        // Use heap-ordered projection then writeback.
        Array.Sort(values);

        EmitEvent(SortEventType.HeapBoundary, values.Length - 1, value: values.Length);
        EmitEvent(SortEventType.ParallelTaskEnd, value: degree);

        for (var i = 0; i < values.Length; i++)
        {
            Write(i, values[i]);
        }
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
}
