using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class DistributedSampleSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var values = Snapshot();
        var workers = Math.Clamp(Options.Parallelism, 2, 16);

        EmitEvent(SortEventType.MarkStage, value: 7201); // sample
        var sample = new List<int>(workers * 4);
        var stride = Math.Max(1, values.Length / (workers * 4));
        for (var i = 0; i < values.Length && sample.Count < workers * 4; i += stride)
        {
            sample.Add(values[i]);
        }

        sample.Sort();
        var pivots = new int[Math.Max(1, workers - 1)];
        for (var i = 0; i < pivots.Length; i++)
        {
            pivots[i] = sample[(i + 1) * sample.Count / workers];
        }

        EmitEvent(SortEventType.MarkStage, value: 7202); // distribute
        var buckets = new List<int>[workers];
        for (var i = 0; i < workers; i++)
        {
            buckets[i] = new List<int>();
        }

        // Register conceptual source runs so External view/test can track merge provenance.
        var runStarts = new int[workers];
        var runLengths = new int[workers];

        for (var i = 0; i < values.Length; i++)
        {
            var b = LocateBucket(values[i], pivots);
            if (runLengths[b] == 0)
            {
                runStarts[b] = i;
            }

            runLengths[b]++;
            buckets[b].Add(values[i]);
            MarkBucket(i, b, values[i]);
        }

        var outputRunId = workers;
        for (var b = 0; b < workers; b++)
        {
            EmitEvent(SortEventType.RunCreated, b, runStarts[b], Math.Max(1, runLengths[b]), aux: 0);
            EmitEvent(SortEventType.MarkRun, b, value: 0, aux: runStarts[b]);
        }

        EmitEvent(SortEventType.RunCreated, outputRunId, 0, values.Length, aux: 1);
        EmitEvent(SortEventType.MarkRun, outputRunId, value: 0, aux: 0);

        EmitEvent(SortEventType.MarkStage, value: 7203); // local sort/merge
        var outIndex = 0;
        const int groupId = 72;
        for (var b = 0; b < buckets.Length; b++)
        {
            var list = buckets[b];
            list.Sort();
            EmitEvent(SortEventType.MergeGroup, b, outputRunId, value: groupId, aux: b);
            EmitEvent(SortEventType.RunRead, b, 0, value: list.Count, aux: 0);

            for (var i = 0; i < list.Count; i++)
            {
                EmitEvent(SortEventType.RunWrite, outputRunId, outIndex, value: groupId, aux: b);
                Write(outIndex++, list[i]);
            }
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
