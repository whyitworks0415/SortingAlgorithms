using System.Threading;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ParallelMultiwayMergeSortAlgorithm : EventSortAlgorithmBase
{
    private sealed class ChunkState
    {
        public required int Offset { get; init; }
        public required int[] Values { get; init; }
        public int Cursor;
    }

    private int _queuedTasks;
    private int _taskIdCounter;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var desiredChunks = Math.Clamp(Options.Parallelism, 1, 8);
        var chunkCount = Math.Min(desiredChunks, Length);
        var chunkSize = (int)Math.Ceiling(Length / (double)chunkCount);

        var chunks = new ChunkState[chunkCount];
        var tasks = new Task[chunkCount];

        Interlocked.Exchange(ref _queuedTasks, 0);
        EmitEvent(SortEventType.ParallelQueueDepth, value: 0);

        for (var c = 0; c < chunkCount; c++)
        {
            var chunkIndex = c;
            var offset = chunkIndex * chunkSize;
            var end = Math.Min(Length, offset + chunkSize);
            var count = Math.Max(0, end - offset);

            var values = new int[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = Read(offset + i);
            }

            chunks[chunkIndex] = new ChunkState
            {
                Offset = offset,
                Values = values,
                Cursor = 0
            };

            if (count == 0)
            {
                tasks[chunkIndex] = Task.CompletedTask;
                continue;
            }

            var taskId = Interlocked.Increment(ref _taskIdCounter);
            var queueDepth = Interlocked.Increment(ref _queuedTasks);
            EmitEvent(SortEventType.ParallelTaskStart, offset, end - 1, taskId, Environment.CurrentManagedThreadId);
            EmitEvent(SortEventType.ParallelQueueDepth, value: queueDepth);

            tasks[chunkIndex] = Task.Run(() =>
            {
                try
                {
                    EmitEvent(SortEventType.MarkRange, offset, end - 1);
                    Array.Sort(values);
                }
                finally
                {
                    EmitEvent(SortEventType.ParallelTaskEnd, offset, end - 1, taskId, Environment.CurrentManagedThreadId);
                    var depth = Interlocked.Decrement(ref _queuedTasks);
                    if (depth < 0)
                    {
                        depth = 0;
                        Interlocked.Exchange(ref _queuedTasks, 0);
                    }

                    EmitEvent(SortEventType.ParallelQueueDepth, value: depth);
                }
            });
        }

        Task.WaitAll(tasks);

        EmitEvent(SortEventType.MergeStart, 0, Length - 1, value: chunkCount);

        for (var outIndex = 0; outIndex < Length; outIndex++)
        {
            var bestChunk = -1;
            var bestValue = 0;
            var bestGlobalIndex = -1;

            for (var c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                if (chunk.Cursor >= chunk.Values.Length)
                {
                    continue;
                }

                var candidateValue = chunk.Values[chunk.Cursor];
                var candidateGlobalIndex = chunk.Offset + chunk.Cursor;

                if (bestChunk < 0)
                {
                    bestChunk = c;
                    bestValue = candidateValue;
                    bestGlobalIndex = candidateGlobalIndex;
                    continue;
                }

                EmitEvent(SortEventType.Compare, bestGlobalIndex, candidateGlobalIndex);
                if (candidateValue < bestValue)
                {
                    bestChunk = c;
                    bestValue = candidateValue;
                    bestGlobalIndex = candidateGlobalIndex;
                }
            }

            if (bestChunk < 0)
            {
                break;
            }

            chunks[bestChunk].Cursor++;
            Write(outIndex, bestValue);
        }

        EmitEvent(SortEventType.MergeComplete, 0, Length - 1, value: chunkCount);
    }
}
