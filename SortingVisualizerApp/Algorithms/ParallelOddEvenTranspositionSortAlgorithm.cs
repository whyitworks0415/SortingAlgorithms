using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ParallelOddEvenTranspositionSortAlgorithm : EventSortAlgorithmBase
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

        var n = values.Length;
        for (var pass = 0; pass < n; pass++)
        {
            var start = pass & 1;
            EmitEvent(SortEventType.MarkStage, value: 7600 + (pass & 1), aux: pass);

            Parallel.For(start, n - 1, new ParallelOptions { MaxDegreeOfParallelism = degree }, i =>
            {
                if (((i - start) & 1) != 0)
                {
                    return;
                }

                if (values[i] <= values[i + 1])
                {
                    return;
                }

                (values[i], values[i + 1]) = (values[i + 1], values[i]);
            });
        }

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
