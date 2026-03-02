using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class TeraSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var values = Snapshot();
        var chunkSize = Math.Clamp(Length / 16, 512, 8192);
        var runs = new List<int[]>();

        EmitEvent(SortEventType.MarkStage, value: 7001);
        for (var start = 0; start < values.Length; start += chunkSize)
        {
            var len = Math.Min(chunkSize, values.Length - start);
            var run = new int[len];
            Array.Copy(values, start, run, 0, len);
            Array.Sort(run);
            runs.Add(run);

            EmitEvent(SortEventType.RunCreated, runs.Count - 1, start, len);
            EmitEvent(SortEventType.MarkRun, runs.Count - 1, value: len, aux: start);
        }

        EmitEvent(SortEventType.MarkStage, value: 7002);
        var merged = KWayMerge(runs);
        for (var i = 0; i < merged.Length; i++)
        {
            Write(i, merged[i]);
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

    private int[] KWayMerge(IReadOnlyList<int[]> runs)
    {
        var total = runs.Sum(static run => run.Length);
        var output = new int[total];
        var queue = new PriorityQueue<(int Run, int Index, int Value), (int Value, int Run)>();

        for (var r = 0; r < runs.Count; r++)
        {
            if (runs[r].Length == 0)
            {
                continue;
            }

            queue.Enqueue((r, 0, runs[r][0]), (runs[r][0], r));
        }

        var outIndex = 0;
        while (queue.Count > 0)
        {
            var top = queue.Dequeue();
            output[outIndex++] = top.Value;

            if ((outIndex & 255) == 0)
            {
                EmitEvent(SortEventType.MergeGroup, top.Run, 0, value: outIndex, aux: queue.Count);
            }

            var next = top.Index + 1;
            if (next >= runs[top.Run].Length)
            {
                continue;
            }

            var value = runs[top.Run][next];
            queue.Enqueue((top.Run, next, value), (value, top.Run));
        }

        return output;
    }
}
