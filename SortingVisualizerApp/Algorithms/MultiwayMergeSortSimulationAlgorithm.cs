using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class MultiwayMergeSortSimulationAlgorithm : ISortAlgorithm
{
    private readonly record struct Run(int RunId, int[] Values);

    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var source = data.ToArray();
        return ExecuteIterator(source);
    }

    private static IEnumerable<SortEvent> ExecuteIterator(int[] source)
    {
        long step = 0;
        if (source.Length <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var runs = new List<Run>();
        var chunk = Math.Clamp((int)Math.Sqrt(source.Length), 64, 4096);
        var nextRunId = 0;

        for (var start = 0; start < source.Length; start += chunk)
        {
            var length = Math.Min(chunk, source.Length - start);
            var values = new int[length];
            Array.Copy(source, start, values, 0, length);
            Array.Sort(values);

            var runId = nextRunId++;
            runs.Add(new Run(runId, values));
            yield return new SortEvent(SortEventType.RunCreated, I: runId, J: start, Value: length, Aux: 0, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRun, I: runId, Value: 0, Aux: start, StepId: step++);
            yield return new SortEvent(SortEventType.MarkRange, I: start, J: start + length - 1, Aux: 0, StepId: step++);
        }

        const int fanIn = 4;
        var mergeStage = 0;
        while (runs.Count > 1)
        {
            yield return new SortEvent(SortEventType.MarkStage, Value: mergeStage, I: runs.Count, Aux: fanIn, StepId: step++);
            var next = new List<Run>((runs.Count + fanIn - 1) / fanIn);
            for (var groupStart = 0; groupStart < runs.Count; groupStart += fanIn)
            {
                var group = runs
                    .Skip(groupStart)
                    .Take(fanIn)
                    .ToArray();
                var outLength = group.Sum(static run => run.Values.Length);
                var outRunId = nextRunId++;

                yield return new SortEvent(SortEventType.RunCreated, I: outRunId, J: 0, Value: outLength, Aux: 1, StepId: step++);
                yield return new SortEvent(SortEventType.MarkRun, I: outRunId, Value: 0, Aux: mergeStage, StepId: step++);

                for (var gi = 0; gi < group.Length; gi++)
                {
                    yield return new SortEvent(SortEventType.MergeGroup, I: group[gi].RunId, J: outRunId, Value: mergeStage, Aux: gi, StepId: step++);
                }

                var merged = new int[outLength];
                var cursors = new int[group.Length];
                var queue = new PriorityQueue<(int value, int run), int>();
                for (var i = 0; i < group.Length; i++)
                {
                    if (group[i].Values.Length == 0)
                    {
                        continue;
                    }

                    queue.Enqueue((group[i].Values[0], i), group[i].Values[0]);
                }

                var outCursor = 0;
                while (queue.Count > 0)
                {
                    var (value, runIndex) = queue.Dequeue();
                    var inCursor = cursors[runIndex];
                    var inputRun = group[runIndex];

                    yield return new SortEvent(SortEventType.RunRead, I: inputRun.RunId, J: inCursor, StepId: step++);
                    yield return new SortEvent(SortEventType.MarkRun, I: inputRun.RunId, Value: 1, Aux: inCursor, StepId: step++);

                    merged[outCursor] = value;
                    yield return new SortEvent(SortEventType.RunWrite, I: outRunId, J: outCursor, StepId: step++);
                    yield return new SortEvent(SortEventType.MarkRun, I: outRunId, Value: 2, Aux: outCursor, StepId: step++);
                    outCursor++;

                    cursors[runIndex]++;
                    if (cursors[runIndex] < inputRun.Values.Length)
                    {
                        var nextValue = inputRun.Values[cursors[runIndex]];
                        queue.Enqueue((nextValue, runIndex), nextValue);
                    }
                }

                next.Add(new Run(outRunId, merged));
            }

            runs = next;
            mergeStage++;
        }

        var output = runs[0].Values;
        for (var i = 0; i < output.Length; i++)
        {
            source[i] = output[i];
            yield return new SortEvent(SortEventType.Write, I: i, Value: output[i], StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
