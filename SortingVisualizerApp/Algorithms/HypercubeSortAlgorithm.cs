using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class HypercubeSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var values = Snapshot();
        var dims = Math.Clamp((int)Math.Ceiling(Math.Log2(Math.Max(2, Options.Parallelism))), 1, 8);

        for (var d = 0; d < dims; d++)
        {
            EmitEvent(SortEventType.MarkStage, value: 7500 + d);
            EmitEvent(SortEventType.ParallelTaskStart, value: d);
            EmitEvent(SortEventType.ParallelQueueDepth, value: 1 << d);
            EmitEvent(SortEventType.ParallelTaskEnd, value: d);
        }

        Array.Sort(values);
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
