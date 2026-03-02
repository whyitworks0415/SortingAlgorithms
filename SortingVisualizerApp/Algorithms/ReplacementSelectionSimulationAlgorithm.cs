using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ReplacementSelectionSimulationAlgorithm : ISortAlgorithm
{
    private readonly record struct HeapItem(int Value, int SourceCursor);
    private readonly record struct Run(int RunId, int[] Values);

    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        return ExecuteIterator(data.ToArray());
    }

    private static IEnumerable<SortEvent> ExecuteIterator(int[] source)
    {
        long step = 0;
        var n = source.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        const int sourceRunId = 0;
        var runId = 1;
        var currentOutputRunId = runId++;
        var replacementWindow = Math.Clamp((int)Math.Sqrt(n), 32, 512);

        yield return new SortEvent(SortEventType.RunCreated, I: sourceRunId, J: 0, Value: n, Aux: 0, StepId: step++);
        yield return new SortEvent(SortEventType.RunCreated, I: currentOutputRunId, J: 0, Value: n, Aux: 1, StepId: step++);
        yield return new SortEvent(SortEventType.MergeGroup, I: sourceRunId, J: currentOutputRunId, Value: 0, Aux: 0, StepId: step++);

        var heap = new PriorityQueue<HeapItem, (int value, int cursor)>();
        var frozen = new List<HeapItem>(replacementWindow);
        var nextSource = 0;
        var currentRunValues = new List<int>(replacementWindow * 2);
        var producedRuns = new List<Run>();

        while (nextSource < n && heap.Count < replacementWindow)
        {
            heap.Enqueue(new HeapItem(source[nextSource], nextSource), (source[nextSource], nextSource));
            nextSource++;
        }

        var currentRunCursor = 0;
        var runGroupId = 0;
        var lastOutput = int.MinValue;

        while (heap.Count > 0)
        {
            var item = heap.Dequeue();
            yield return new SortEvent(SortEventType.RunRead, I: sourceRunId, J: item.SourceCursor, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRun, I: sourceRunId, Value: 1, Aux: item.SourceCursor, StepId: step++);

            yield return new SortEvent(SortEventType.RunWrite, I: currentOutputRunId, J: currentRunCursor, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRun, I: currentOutputRunId, Value: 2, Aux: currentRunCursor, StepId: step++);
            currentRunCursor++;
            lastOutput = item.Value;
            currentRunValues.Add(item.Value);

            if (nextSource < n)
            {
                var incoming = new HeapItem(source[nextSource], nextSource);
                nextSource++;
                if (incoming.Value >= lastOutput)
                {
                    heap.Enqueue(incoming, (incoming.Value, incoming.SourceCursor));
                }
                else
                {
                    frozen.Add(incoming);
                }
            }

            if (heap.Count > 0 || frozen.Count == 0)
            {
                continue;
            }

            producedRuns.Add(new Run(currentOutputRunId, currentRunValues.ToArray()));
            currentRunValues.Clear();

            runGroupId++;
            currentOutputRunId = runId++;
            yield return new SortEvent(SortEventType.RunCreated, I: currentOutputRunId, J: 0, Value: n, Aux: 1, StepId: step++);
            yield return new SortEvent(SortEventType.MergeGroup, I: sourceRunId, J: currentOutputRunId, Value: runGroupId, Aux: 0, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRun, I: currentOutputRunId, Value: 0, Aux: runGroupId, StepId: step++);

            for (var i = 0; i < frozen.Count; i++)
            {
                var f = frozen[i];
                heap.Enqueue(f, (f.Value, f.SourceCursor));
            }

            frozen.Clear();
            currentRunCursor = 0;
            lastOutput = int.MinValue;
        }

        if (currentRunValues.Count > 0)
        {
            producedRuns.Add(new Run(currentOutputRunId, currentRunValues.ToArray()));
        }

        var finalRunId = runId++;
        yield return new SortEvent(SortEventType.RunCreated, I: finalRunId, J: 0, Value: n, Aux: 1, StepId: step++);
        for (var i = 0; i < producedRuns.Count; i++)
        {
            yield return new SortEvent(SortEventType.MergeGroup, I: producedRuns[i].RunId, J: finalRunId, Value: runGroupId + 1, Aux: i, StepId: step++);
        }

        var cursors = new int[producedRuns.Count];
        var queue = new PriorityQueue<(int Value, int Run), int>();
        for (var i = 0; i < producedRuns.Count; i++)
        {
            if (producedRuns[i].Values.Length == 0)
            {
                continue;
            }

            queue.Enqueue((producedRuns[i].Values[0], i), producedRuns[i].Values[0]);
        }

        var outputIndex = 0;
        while (queue.Count > 0)
        {
            var (value, runIndex) = queue.Dequeue();
            var run = producedRuns[runIndex];
            var cursor = cursors[runIndex];

            yield return new SortEvent(SortEventType.RunRead, I: run.RunId, J: cursor, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRun, I: run.RunId, Value: 1, Aux: cursor, StepId: step++);

            source[outputIndex] = value;
            yield return new SortEvent(SortEventType.RunWrite, I: finalRunId, J: outputIndex, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRun, I: finalRunId, Value: 2, Aux: outputIndex, StepId: step++);
            yield return new SortEvent(SortEventType.Write, I: outputIndex, Value: value, StepId: step++);
            outputIndex++;

            cursors[runIndex]++;
            if (cursors[runIndex] < run.Values.Length)
            {
                var nextValue = run.Values[cursors[runIndex]];
                queue.Enqueue((nextValue, runIndex), nextValue);
            }
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
