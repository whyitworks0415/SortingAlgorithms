namespace SortingVisualizerApp.Algorithms;

public sealed class KWayMergeAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var runCount = Math.Clamp((int)Math.Sqrt(Length), 2, 16);
        runCount = Math.Min(runCount, Length);

        var runStarts = new int[runCount + 1];
        for (var run = 0; run <= runCount; run++)
        {
            runStarts[run] = run * Length / runCount;
        }

        // Phase 1: locally sort each run.
        for (var run = 0; run < runCount; run++)
        {
            var start = runStarts[run];
            var end = runStarts[run + 1];
            if (start >= end)
            {
                continue;
            }

            MarkRange(start, end - 1);

            var local = new int[end - start];
            for (var i = 0; i < local.Length; i++)
            {
                local[i] = Read(start + i);
                MarkBucket(start + i, run, local[i]);
            }

            Array.Sort(local);
            for (var i = 0; i < local.Length; i++)
            {
                Write(start + i, local[i]);
            }
        }

        // Phase 2: k-way merge all sorted runs.
        var merged = MergeSortedRuns(ReadRuns(runStarts));
        for (var i = 0; i < merged.Length; i++)
        {
            Write(i, merged[i]);
        }
    }

    public static int[] MergeSortedRuns(IReadOnlyList<int[]> runs)
    {
        var nonNullRuns = runs?.Where(static run => run is not null && run.Length > 0).ToArray() ?? Array.Empty<int[]>();
        if (nonNullRuns.Length == 0)
        {
            return Array.Empty<int>();
        }

        var total = 0;
        for (var i = 0; i < nonNullRuns.Length; i++)
        {
            total += nonNullRuns[i].Length;
        }

        var output = new int[total];
        var outIndex = 0;

        var queue = new PriorityQueue<RunCursor, (int Value, int RunId)>();
        for (var runId = 0; runId < nonNullRuns.Length; runId++)
        {
            var run = nonNullRuns[runId];
            queue.Enqueue(new RunCursor(runId, 0, run[0]), (run[0], runId));
        }

        while (queue.Count > 0)
        {
            var cursor = queue.Dequeue();
            output[outIndex++] = cursor.Value;

            var nextIndex = cursor.Index + 1;
            var run = nonNullRuns[cursor.RunId];
            if (nextIndex >= run.Length)
            {
                continue;
            }

            var nextValue = run[nextIndex];
            queue.Enqueue(new RunCursor(cursor.RunId, nextIndex, nextValue), (nextValue, cursor.RunId));
        }

        return output;
    }

    private List<int[]> ReadRuns(IReadOnlyList<int> runStarts)
    {
        var runs = new List<int[]>(runStarts.Count - 1);

        for (var run = 0; run < runStarts.Count - 1; run++)
        {
            var start = runStarts[run];
            var end = runStarts[run + 1];
            var length = Math.Max(0, end - start);
            var segment = new int[length];
            for (var i = 0; i < length; i++)
            {
                segment[i] = Read(start + i);
            }

            runs.Add(segment);
        }

        return runs;
    }

    private readonly record struct RunCursor(int RunId, int Index, int Value);
}
