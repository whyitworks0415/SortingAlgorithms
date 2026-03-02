using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class PolyphaseMergeSortAlgorithm : EventSortAlgorithmBase
{
    private sealed record Run(int Id, int[] Values);

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var data = new int[Length];
        for (var i = 0; i < Length; i++)
        {
            data[i] = Read(i);
        }

        var nextRunId = 0;
        var baseRunSize = Math.Clamp((int)Math.Sqrt(Length), 16, 4096);
        var runQueue = new Queue<Run>();

        // Initial run generation and distribution.
        for (var start = 0; start < data.Length; start += baseRunSize)
        {
            var len = Math.Min(baseRunSize, data.Length - start);
            var segment = new int[len];
            Array.Copy(data, start, segment, 0, len);
            Array.Sort(segment);

            var run = new Run(nextRunId++, segment);
            EmitEvent(SortEventType.RunCreated, run.Id, start, len);
            runQueue.Enqueue(run);
        }

        var phase = 0;
        while (runQueue.Count > 1)
        {
            phase++;
            EmitEvent(SortEventType.MarkStage, value: 6000 + Math.Min(255, phase));

            var phaseOutput = new Queue<Run>();
            while (runQueue.Count > 1)
            {
                var left = runQueue.Dequeue();
                var right = runQueue.Dequeue();
                var merged = MergeRuns(left, right, nextRunId++, phase);
                phaseOutput.Enqueue(merged);
            }

            if (runQueue.Count == 1)
            {
                phaseOutput.Enqueue(runQueue.Dequeue());
            }

            while (phaseOutput.Count > 0)
            {
                runQueue.Enqueue(phaseOutput.Dequeue());
            }
        }

        var finalRun = runQueue.Count == 1 ? runQueue.Dequeue() : null;
        if (finalRun is null)
        {
            return;
        }

        for (var i = 0; i < finalRun.Values.Length; i++)
        {
            Write(i, finalRun.Values[i]);
        }
    }

    private Run MergeRuns(Run left, Run right, int mergedRunId, int phase)
    {
        var mergedLength = left.Values.Length + right.Values.Length;
        var merged = new int[mergedLength];

        EmitEvent(SortEventType.RunCreated, mergedRunId, value: mergedLength);
        EmitEvent(SortEventType.MergeGroup, left.Id, mergedRunId, value: phase, aux: 0);
        EmitEvent(SortEventType.MergeGroup, right.Id, mergedRunId, value: phase, aux: 1);

        EmitEvent(SortEventType.RunRead, left.Id, 0, value: phase);
        EmitEvent(SortEventType.RunRead, right.Id, 0, value: phase);

        var i = 0;
        var j = 0;
        var k = 0;

        while (i < left.Values.Length && j < right.Values.Length)
        {
            if (left.Values[i] <= right.Values[j])
            {
                merged[k++] = left.Values[i++];
            }
            else
            {
                merged[k++] = right.Values[j++];
            }

            if ((k & 127) == 0)
            {
                EmitEvent(SortEventType.RunWrite, mergedRunId, k, value: phase);
            }
        }

        while (i < left.Values.Length)
        {
            merged[k++] = left.Values[i++];
        }

        while (j < right.Values.Length)
        {
            merged[k++] = right.Values[j++];
        }

        EmitEvent(SortEventType.RunRead, left.Id, left.Values.Length, value: phase);
        EmitEvent(SortEventType.RunRead, right.Id, right.Values.Length, value: phase);
        EmitEvent(SortEventType.RunWrite, mergedRunId, mergedLength, value: phase);

        return new Run(mergedRunId, merged);
    }
}
