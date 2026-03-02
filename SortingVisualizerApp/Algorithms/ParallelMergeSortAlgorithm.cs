using System.Threading;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ParallelMergeSortAlgorithm : EventSortAlgorithmBase
{
    private const int InsertionThreshold = 32;
    private const int MinParallelRange = 4096;

    private int[] _buffer = Array.Empty<int>();
    private SemaphoreSlim? _parallelGate;
    private int _queuedTasks;
    private int _taskIdCounter;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        _buffer = new int[Length];
        var parallelism = Math.Max(1, Options.Parallelism);
        var maxParallelDepth = parallelism <= 1
            ? 0
            : Math.Clamp((int)Math.Ceiling(Math.Log2(parallelism)) + 2, 1, 12);

        _parallelGate = new SemaphoreSlim(parallelism, parallelism);
        Interlocked.Exchange(ref _queuedTasks, 0);
        EmitEvent(SortEventType.ParallelQueueDepth, value: 0);

        try
        {
            SortRange(0, Length, 0, maxParallelDepth);
        }
        finally
        {
            _parallelGate.Dispose();
            _parallelGate = null;
            _buffer = Array.Empty<int>();
        }
    }

    private void SortRange(int left, int rightExclusive, int depth, int maxParallelDepth)
    {
        var length = rightExclusive - left;
        if (length <= 1)
        {
            return;
        }

        if (length <= InsertionThreshold)
        {
            InsertionSortRange(left, rightExclusive - 1);
            return;
        }

        var mid = left + (length >> 1);
        Task? task = null;

        if (depth < maxParallelDepth
            && length >= MinParallelRange
            && TryStartSubtask(left, mid - 1, out var taskToken))
        {
            task = Task.Run(() =>
            {
                try
                {
                    SortRange(left, mid, depth + 1, maxParallelDepth);
                }
                finally
                {
                    CompleteSubtask(left, mid - 1, taskToken);
                }
            });

            SortRange(mid, rightExclusive, depth + 1, maxParallelDepth);
            task.Wait();
        }
        else
        {
            SortRange(left, mid, depth + 1, maxParallelDepth);
            SortRange(mid, rightExclusive, depth + 1, maxParallelDepth);
        }

        MergeRange(left, mid, rightExclusive);
    }

    private void MergeRange(int left, int mid, int rightExclusive)
    {
        MarkRange(left, rightExclusive - 1);
        EmitEvent(SortEventType.MergeStart, left, rightExclusive - 1, value: mid);

        Array.Copy(Data, left, _buffer, left, rightExclusive - left);

        var i = left;
        var j = mid;
        for (var k = left; k < rightExclusive; k++)
        {
            if (i >= mid)
            {
                Write(k, _buffer[j++]);
            }
            else if (j >= rightExclusive)
            {
                Write(k, _buffer[i++]);
            }
            else
            {
                EmitEvent(SortEventType.Compare, i, j);
                if (_buffer[i] <= _buffer[j])
                {
                    Write(k, _buffer[i++]);
                }
                else
                {
                    Write(k, _buffer[j++]);
                }
            }
        }

        EmitEvent(SortEventType.MergeComplete, left, rightExclusive - 1, value: mid);
    }

    private void InsertionSortRange(int left, int right)
    {
        for (var i = left + 1; i <= right; i++)
        {
            var key = Read(i);
            var j = i - 1;
            while (j >= left)
            {
                EmitEvent(SortEventType.Compare, j, i);
                if (Read(j) <= key)
                {
                    break;
                }

                Write(j + 1, Read(j));
                j--;
            }

            Write(j + 1, key);
        }
    }

    private bool TryStartSubtask(int left, int right, out int taskToken)
    {
        taskToken = 0;
        var gate = _parallelGate;
        if (gate is null || !gate.Wait(0))
        {
            return false;
        }

        taskToken = Interlocked.Increment(ref _taskIdCounter);
        EmitEvent(SortEventType.ParallelTaskStart, left, right, taskToken, Environment.CurrentManagedThreadId);

        var queueDepth = Interlocked.Increment(ref _queuedTasks);
        EmitEvent(SortEventType.ParallelQueueDepth, value: queueDepth);
        return true;
    }

    private void CompleteSubtask(int left, int right, int taskToken)
    {
        EmitEvent(SortEventType.ParallelTaskEnd, left, right, taskToken, Environment.CurrentManagedThreadId);

        var queueDepth = Interlocked.Decrement(ref _queuedTasks);
        if (queueDepth < 0)
        {
            queueDepth = 0;
            Interlocked.Exchange(ref _queuedTasks, 0);
        }

        EmitEvent(SortEventType.ParallelQueueDepth, value: queueDepth);
        _parallelGate?.Release();
    }
}
