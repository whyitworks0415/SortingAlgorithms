using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ExternalMergeSortAlgorithm : ISortAlgorithm
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var working = data.ToArray();
        return ExecuteIterator(working);
    }

    private static IEnumerable<SortEvent> ExecuteIterator(int[] data)
    {
        long step = 0;
        var n = data.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var chunkSize = Math.Clamp((int)Math.Sqrt(n), 128, 4096);
        var runs = new List<RunDescriptor>();
        var runId = 0;

        for (var start = 0; start < n; start += chunkSize)
        {
            var length = Math.Min(chunkSize, n - start);
            var chunk = new int[length];
            Array.Copy(data, start, chunk, 0, length);
            Array.Sort(chunk);

            runs.Add(new RunDescriptor(runId, start, length));
            yield return new SortEvent(SortEventType.RunCreated, I: runId, J: start, Value: length, Aux: 0, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRun, I: runId, Value: 0, Aux: start, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRange, start, start + length - 1, StepId: step++);

            for (var i = 0; i < length; i++)
            {
                data[start + i] = chunk[i];
                yield return new SortEvent(SortEventType.Write, I: start + i, Value: chunk[i], StepId: step++);
            }

            runId++;
        }

        var outputRunId = runId;
        yield return new SortEvent(SortEventType.RunCreated, I: outputRunId, J: 0, Value: n, Aux: 1, StepId: step++);
        yield return new SortEvent(SortEventType.MarkRun, I: outputRunId, Value: 0, Aux: 0, StepId: step++);

        var source = data.ToArray();
        var offsets = new int[runs.Count];
        var queue = new PriorityQueue<(int value, int run), int>();

        const int groupId = 0;
        for (var i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            var firstValue = source[run.Start];
            queue.Enqueue((firstValue, i), firstValue);

            yield return new SortEvent(SortEventType.MergeGroup, I: run.RunId, J: outputRunId, Value: groupId, Aux: i, StepId: step++);
            yield return new SortEvent(SortEventType.RunRead, I: run.RunId, J: 0, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRun, I: run.RunId, Value: 1, Aux: 0, StepId: step++);
        }

        var outIndex = 0;
        while (queue.Count > 0)
        {
            var (value, runIndex) = queue.Dequeue();
            data[outIndex] = value;

            yield return new SortEvent(SortEventType.RunWrite, I: outputRunId, J: outIndex, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRun, I: outputRunId, Value: 2, Aux: outIndex, StepId: step++);
            yield return new SortEvent(SortEventType.Write, I: outIndex, Value: value, StepId: step++);

            offsets[runIndex]++;
            var run = runs[runIndex];
            if (offsets[runIndex] < run.Length)
            {
                var cursor = offsets[runIndex];
                var nextValue = source[run.Start + cursor];
                queue.Enqueue((nextValue, runIndex), nextValue);
                yield return new SortEvent(SortEventType.RunRead, I: run.RunId, J: cursor, StepId: step++);
                yield return new SortEvent(SortEventType.MarkRun, I: run.RunId, Value: 1, Aux: cursor, StepId: step++);
            }

            outIndex++;
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private readonly record struct RunDescriptor(int RunId, int Start, int Length);
}
