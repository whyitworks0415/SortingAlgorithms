using System.Collections.Concurrent;
using System.Threading;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public abstract class EventSortAlgorithmBase : ISortAlgorithm
{
    private BlockingCollection<SortEvent>? _eventQueue;
    private CancellationToken _emitCancellationToken;
    private long _stepId;

    protected int[] Data { get; private set; } = Array.Empty<int>();
    protected SortOptions Options { get; private set; }

    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        var snapshot = data.ToArray();
        return ExecuteIterator(snapshot, options);
    }

    private IEnumerable<SortEvent> ExecuteIterator(int[] snapshot, SortOptions options)
    {
        Data = snapshot;
        Options = options;
        Interlocked.Exchange(ref _stepId, 0);
        _eventQueue = new BlockingCollection<SortEvent>(boundedCapacity: 4096);

        using var producerCts = new CancellationTokenSource();
        _emitCancellationToken = producerCts.Token;
        Exception? producerException = null;

        var producer = Task.Run(() =>
        {
            try
            {
                SortCore();
                MarkDone();
            }
            catch (Exception ex)
            {
                producerException = ex;
            }
            finally
            {
                _eventQueue.CompleteAdding();
            }
        }, producerCts.Token);

        try
        {
            foreach (var ev in _eventQueue.GetConsumingEnumerable())
            {
                yield return ev;
            }

            producer.GetAwaiter().GetResult();
            if (producerException is not null)
            {
                throw producerException;
            }
        }
        finally
        {
            producerCts.Cancel();
            try
            {
                producer.Wait(50);
            }
            catch
            {
                // Ignore cancellation race when iterator is disposed early.
            }

            _eventQueue?.Dispose();
            _eventQueue = null;
            _emitCancellationToken = default;
        }
    }

    protected int Length => Data.Length;

    protected abstract void SortCore();

    protected internal int Read(int index)
    {
        return Data[index];
    }

    protected internal int Compare(int i, int j)
    {
        Emit(SortEventType.Compare, i, j);
        return Data[i].CompareTo(Data[j]);
    }

    protected internal void Swap(int i, int j)
    {
        if (i == j)
        {
            return;
        }

        (Data[i], Data[j]) = (Data[j], Data[i]);
        Emit(SortEventType.Swap, i, j);
    }

    protected internal void Write(int index, int value)
    {
        Data[index] = value;
        Emit(SortEventType.Write, index, value: value);
    }

    protected internal void MarkPivot(int index)
    {
        Emit(SortEventType.MarkPivot, index, value: Data[index]);
    }

    protected internal void MarkPivotValue(int index, int value)
    {
        Emit(SortEventType.MarkPivot, index, value: value);
    }

    protected internal void MarkRange(int left, int right)
    {
        Emit(SortEventType.MarkRange, left, right);
    }

    protected internal void MarkBucket(int index, int bucket, int value)
    {
        Emit(SortEventType.MarkBucket, index, value: value, aux: bucket);
    }

    protected internal void MarkDone(int index = -1)
    {
        Emit(SortEventType.Done, index);
    }

    protected internal void EmitEvent(SortEventType type, int i = -1, int j = -1, int value = 0, int aux = 0)
    {
        Emit(type, i, j, value, aux);
    }

    protected bool IsLess(int i, int j)
    {
        return Compare(i, j) < 0;
    }

    protected bool IsLessOrEqual(int i, int j)
    {
        return Compare(i, j) <= 0;
    }

    private void Emit(SortEventType type, int i = -1, int j = -1, int value = 0, int aux = 0)
    {
        var queue = _eventQueue;
        if (queue is null || _emitCancellationToken.IsCancellationRequested || queue.IsAddingCompleted)
        {
            return;
        }

        try
        {
            var stepId = Interlocked.Increment(ref _stepId) - 1;
            queue.Add(new SortEvent(type, i, j, value, aux, stepId), _emitCancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Iterator was disposed; stop emitting.
        }
        catch (InvalidOperationException)
        {
            // Queue completed between check and add.
        }
    }
}
