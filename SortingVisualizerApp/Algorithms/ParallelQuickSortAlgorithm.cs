using System.Threading;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ParallelQuickSortAlgorithm : EventSortAlgorithmBase
{
    private const int InsertionThreshold = 24;
    private const int MinParallelPartition = 2048;

    private SemaphoreSlim? _parallelGate;
    private int _queuedTasks;
    private int _taskIdCounter;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var parallelism = Math.Max(1, Options.Parallelism);
        var maxParallelDepth = parallelism <= 1
            ? 0
            : Math.Clamp((int)Math.Ceiling(Math.Log2(parallelism)) + 2, 1, 12);

        _parallelGate = new SemaphoreSlim(parallelism, parallelism);
        Interlocked.Exchange(ref _queuedTasks, 0);
        EmitEvent(SortEventType.ParallelQueueDepth, value: 0);

        try
        {
            SortRange(0, Length - 1, 0, maxParallelDepth);
        }
        finally
        {
            _parallelGate.Dispose();
            _parallelGate = null;
        }
    }

    private void SortRange(int left, int right, int depth, int maxParallelDepth)
    {
        if (left >= right)
        {
            return;
        }

        if (right - left + 1 <= InsertionThreshold)
        {
            InsertionSortRange(left, right);
            return;
        }

        MarkRange(left, right);
        var pivotIndex = Partition(left, right);
        MarkPivot(pivotIndex);

        var leftSize = pivotIndex - left;
        var rightSize = right - pivotIndex;
        EmitPartitionMetrics(left, right, pivotIndex, leftSize, rightSize);

        var leftRange = (Left: left, Right: pivotIndex - 1);
        var rightRange = (Left: pivotIndex + 1, Right: right);

        var spawnLeft = leftSize >= rightSize;
        var taskRange = spawnLeft ? leftRange : rightRange;
        var localRange = spawnLeft ? rightRange : leftRange;

        Task? task = null;
        if (depth < maxParallelDepth
            && taskRange.Right >= taskRange.Left
            && taskRange.Right - taskRange.Left + 1 >= MinParallelPartition
            && TryStartSubtask(taskRange.Left, taskRange.Right, out var taskToken))
        {
            task = Task.Run(() =>
            {
                try
                {
                    SortRange(taskRange.Left, taskRange.Right, depth + 1, maxParallelDepth);
                }
                finally
                {
                    CompleteSubtask(taskRange.Left, taskRange.Right, taskToken);
                }
            });
        }

        if (localRange.Right >= localRange.Left)
        {
            SortRange(localRange.Left, localRange.Right, depth + 1, maxParallelDepth);
        }

        if (task is not null)
        {
            task.Wait();
        }
        else if (taskRange.Right >= taskRange.Left)
        {
            SortRange(taskRange.Left, taskRange.Right, depth + 1, maxParallelDepth);
        }
    }

    private int Partition(int left, int right)
    {
        var mid = left + ((right - left) >> 1);
        var pivotCandidate = MedianOfThreeIndex(left, mid, right);
        if (pivotCandidate != right)
        {
            Swap(pivotCandidate, right);
        }

        var pivot = Read(right);
        var store = left;
        for (var i = left; i < right; i++)
        {
            Compare(i, right);
            if (Read(i) <= pivot)
            {
                if (store != i)
                {
                    Swap(store, i);
                }

                store++;
            }
        }

        Swap(store, right);
        return store;
    }

    private int MedianOfThreeIndex(int a, int b, int c)
    {
        if (Compare(a, b) > 0)
        {
            (a, b) = (b, a);
        }

        if (Compare(b, c) > 0)
        {
            (b, c) = (c, b);
        }

        if (Compare(a, b) > 0)
        {
            (a, b) = (b, a);
        }

        return b;
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

    private void EmitPartitionMetrics(int left, int right, int pivot, int leftSize, int rightSize)
    {
        var larger = Math.Max(leftSize, rightSize);
        var smaller = Math.Min(leftSize, rightSize);
        var quality = larger <= 0 ? 1.0 : smaller / (double)larger;
        var encoded = (int)Math.Round(Math.Clamp(quality, 0.0, 1.0) * 1000.0);

        EmitEvent(SortEventType.PartitionInfo, left, right, encoded, pivot);

        if (quality < 0.20)
        {
            EmitEvent(SortEventType.BadPartition, pivot, left, leftSize, rightSize);
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
