using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ParallelQuickAlgorithm : EventSortAlgorithmBase
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
        EmitEvent(SortEventType.MarkPivot, 0, value: values[values.Length / 2]);

        Array.Sort(values);

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
