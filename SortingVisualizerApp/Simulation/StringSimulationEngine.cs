using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Simulation;

public sealed class StringSimulationEngine : IDisposable
{
    private readonly object _stateLock = new();

    private StringItem[] _sourceData = Array.Empty<StringItem>();
    private StringItem[] _visualData = Array.Empty<StringItem>();
    private Dictionary<int, StringItem> _itemById = new();
    private readonly MemoryAccessTracker _memoryAccess = new();

    private IEnumerator<SortEvent>? _eventEnumerator;
    private CancellationTokenSource? _workerCts;
    private Task? _workerTask;

    private readonly ConcurrentQueue<SortEvent> _visualEventQueue = new();
    private readonly ConcurrentQueue<AudioTrigger> _audioQueue = new();
    private int _visualEventQueueCount;
    private int _audioQueueCount;
    private long _droppedComparisons;
    private const int MaxAudioQueueDepth = 128;
    private const int MaxVisualQueueDepth = 8192;
    private const int CompareDropStartDepth = 1536;
    private const int CompareDropHardDepth = 4096;
    private const int MaxWorkerBatchEvents = 4096;

    private readonly Stopwatch _elapsed = new();
    private long _comparisons;
    private long _swaps;
    private long _writes;
    private long _processedEvents;
    private long _cacheHits;
    private long _cacheMisses;

    private bool _isRunning;
    private bool _isPaused;
    private bool _isCompleted;

    private int _visualDetailLevel = (int)DetailLevel.L2;
    private int _audioDetailLevel = (int)DetailLevel.L1;
    private bool _visualEnabled = true;
    private bool _audioEnabled = true;
    private double _eventsPerSecond = 5000.0;
    private int _stepRequest;
    private double _effectiveEventsPerSecond;

    public bool HasData
    {
        get
        {
            lock (_stateLock)
            {
                return _visualData.Length > 0;
            }
        }
    }

    public void LoadData(StringItem[] data)
    {
        Stop();

        lock (_stateLock)
        {
            _sourceData = data.ToArray();
            _visualData = data.ToArray();
            _itemById = _sourceData.ToDictionary(static item => item.Id, static item => item);
            _memoryAccess.Resize(_visualData.Length);
        }

        ResetStats();
    }

    public void Start(IStringSortAlgorithm algorithm, StringSortOptions options)
    {
        Stop();

        lock (_stateLock)
        {
            if (_sourceData.Length == 0)
            {
                return;
            }

            _visualData = _sourceData.ToArray();
            _itemById = _sourceData.ToDictionary(static item => item.Id, static item => item);
            _memoryAccess.Resize(_visualData.Length);
            _eventEnumerator = algorithm.Execute(_sourceData.ToArray(), options).GetEnumerator();
        }

        ResetStats();
        _isPaused = false;
        _isCompleted = false;
        _isRunning = true;
        _elapsed.Restart();

        _workerCts = new CancellationTokenSource();
        _workerTask = Task.Run(() => WorkerLoop(_workerCts.Token));
    }

    public void Stop()
    {
        _isRunning = false;
        _isPaused = false;

        var cts = Interlocked.Exchange(ref _workerCts, null);
        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _workerTask?.Wait(250);
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        _workerTask = null;

        lock (_stateLock)
        {
            _eventEnumerator?.Dispose();
            _eventEnumerator = null;
        }

        _elapsed.Stop();
    }

    public void ResetToSource()
    {
        Stop();

        lock (_stateLock)
        {
            _visualData = _sourceData.ToArray();
            _itemById = _sourceData.ToDictionary(static item => item.Id, static item => item);
            _memoryAccess.Resize(_visualData.Length);
        }

        ResetStats();
        _isCompleted = false;
    }

    public void TogglePause()
    {
        if (!_isRunning)
        {
            return;
        }

        _isPaused = !_isPaused;
    }

    public void StepOnce()
    {
        if (!_isRunning)
        {
            return;
        }

        _isPaused = true;
        Interlocked.Exchange(ref _stepRequest, 1);
    }

    public void UpdateRuntimeControls(RuntimeControls controls)
    {
        if (controls is null)
        {
            return;
        }

        var visualLevel = controls.VisualDetail;
        var audioLevel = controls.LinkDetails ? visualLevel : controls.AudioDetail;

        Volatile.Write(ref _visualDetailLevel, (int)visualLevel);
        Volatile.Write(ref _audioDetailLevel, (int)audioLevel);
        Volatile.Write(ref _visualEnabled, controls.VisualEnabled);
        Volatile.Write(ref _audioEnabled, controls.AudioEnabled);
        Volatile.Write(ref _eventsPerSecond, controls.ResolveEventsPerSecond());
        lock (_stateLock)
        {
            _memoryAccess.SetCacheLineSize(Math.Max(1, controls.CacheLineSize));
        }
    }

    public int CopyDataTo(ref StringItem[] destination)
    {
        lock (_stateLock)
        {
            if (destination.Length != _visualData.Length)
            {
                destination = new StringItem[_visualData.Length];
            }

            if (_visualData.Length > 0)
            {
                Array.Copy(_visualData, destination, _visualData.Length);
            }

            return _visualData.Length;
        }
    }

    public int CopyMemoryAccessTo(ref int[] destination, out int maxCount)
    {
        lock (_stateLock)
        {
            maxCount = _memoryAccess.CopyCounts(ref destination);
            return destination.Length;
        }
    }

    public void ResetMemoryAccessCounters()
    {
        lock (_stateLock)
        {
            _memoryAccess.Reset();
            Interlocked.Exchange(ref _cacheHits, 0);
            Interlocked.Exchange(ref _cacheMisses, 0);
        }
    }

    public int DrainVisualEvents(Span<SortEvent> target)
    {
        var count = 0;
        while (count < target.Length && _visualEventQueue.TryDequeue(out var ev))
        {
            Interlocked.Decrement(ref _visualEventQueueCount);
            target[count++] = ev;
        }

        return count;
    }

    public int DrainAudioEvents(Span<AudioTrigger> target)
    {
        var count = 0;
        while (_audioQueue.TryDequeue(out var trigger))
        {
            Interlocked.Decrement(ref _audioQueueCount);
            if (target.Length == 0)
            {
                continue;
            }

            if (count < target.Length)
            {
                target[count++] = trigger;
                continue;
            }

            target.Slice(1).CopyTo(target);
            target[^1] = trigger;
        }

        return Math.Min(count, target.Length);
    }

    public SortStatisticsSnapshot GetStatisticsSnapshot()
    {
        return new SortStatisticsSnapshot(
            Comparisons: Interlocked.Read(ref _comparisons),
            Swaps: Interlocked.Read(ref _swaps),
            Writes: Interlocked.Read(ref _writes),
            ProcessedEvents: Interlocked.Read(ref _processedEvents),
            ElapsedMs: _elapsed.Elapsed.TotalMilliseconds,
            EffectiveEventsPerSecond: _effectiveEventsPerSecond,
            IsRunning: _isRunning,
            IsPaused: _isPaused,
            IsCompleted: _isCompleted,
            DroppedComparisons: Interlocked.Read(ref _droppedComparisons),
            CacheHits: Interlocked.Read(ref _cacheHits),
            CacheMisses: Interlocked.Read(ref _cacheMisses));
    }

    public (int VisualQueueDepth, int AudioQueueDepth) GetQueueDepthSnapshot()
    {
        return (Interlocked.CompareExchange(ref _visualEventQueueCount, 0, 0), Interlocked.CompareExchange(ref _audioQueueCount, 0, 0));
    }

    private async Task WorkerLoop(CancellationToken token)
    {
        var frameTimer = Stopwatch.StartNew();
        var rateTimer = Stopwatch.StartNew();
        var previousTicks = frameTimer.ElapsedTicks;
        var lastProcessed = 0L;
        var budget = 0.0;

        while (!token.IsCancellationRequested && _isRunning)
        {
            if (_isPaused && Interlocked.CompareExchange(ref _stepRequest, 0, 0) == 0)
            {
                await Task.Delay(1, token).ConfigureAwait(false);
                continue;
            }

            var nowTicks = frameTimer.ElapsedTicks;
            var deltaSeconds = (nowTicks - previousTicks) / (double)Stopwatch.Frequency;
            previousTicks = nowTicks;

            var processCount = 0;
            if (_isPaused)
            {
                if (Interlocked.Exchange(ref _stepRequest, 0) > 0)
                {
                    processCount = 1;
                }
            }
            else
            {
                var eps = Math.Max(1.0, Volatile.Read(ref _eventsPerSecond));
                budget += eps * deltaSeconds;
                processCount = (int)Math.Floor(budget);
                if (processCount > 0)
                {
                    budget -= processCount;
                }
            }

            processCount = Math.Min(processCount, MaxWorkerBatchEvents);

            if (processCount <= 0)
            {
                await Task.Delay(1, token).ConfigureAwait(false);
                continue;
            }

            for (var i = 0; i < processCount; i++)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (!TryProcessNextEvent())
                {
                    _isRunning = false;
                    _isCompleted = true;
                    _isPaused = false;
                    _elapsed.Stop();
                    break;
                }

                if (_isPaused)
                {
                    break;
                }
            }

            if (rateTimer.ElapsedMilliseconds >= 250)
            {
                var processedNow = Interlocked.Read(ref _processedEvents);
                var delta = processedNow - lastProcessed;
                lastProcessed = processedNow;

                var seconds = Math.Max(0.001, rateTimer.Elapsed.TotalSeconds);
                _effectiveEventsPerSecond = delta / seconds;
                rateTimer.Restart();
            }
        }
    }

    private bool TryProcessNextEvent()
    {
        SortEvent ev;
        lock (_stateLock)
        {
            if (_eventEnumerator is null)
            {
                return false;
            }

            if (!_eventEnumerator.MoveNext())
            {
                return false;
            }

            ev = _eventEnumerator.Current;
            ApplyEventUnsafe(ev);
            TrackMemoryUnsafe(ev);
            var cacheSnapshot = _memoryAccess.CacheStats;
            Interlocked.Exchange(ref _cacheHits, cacheSnapshot.Hits);
            Interlocked.Exchange(ref _cacheMisses, cacheSnapshot.Misses);
        }

        if (ev.Type == SortEventType.Done)
        {
            return false;
        }

        UpdateCounters(ev);
        MaybeEmitVisualEvent(ev);
        MaybeEmitAudioEvent(ev);
        return true;
    }

    private void ApplyEventUnsafe(SortEvent ev)
    {
        switch (ev.Type)
        {
            case SortEventType.Swap:
                if (IsValidIndex(ev.I) && IsValidIndex(ev.J))
                {
                    (_visualData[ev.I], _visualData[ev.J]) = (_visualData[ev.J], _visualData[ev.I]);
                }
                break;
            case SortEventType.Write:
                if (IsValidIndex(ev.I) && _itemById.TryGetValue(ev.Value, out var item))
                {
                    _visualData[ev.I] = item;
                }
                break;
        }
    }

    private void TrackMemoryUnsafe(SortEvent ev)
    {
        switch (ev.Type)
        {
            case SortEventType.Compare:
            case SortEventType.CharCompare:
            case SortEventType.Swap:
                _memoryAccess.AccessPair(ev.I, ev.J);
                break;
            case SortEventType.Write:
            case SortEventType.BucketMove:
            case SortEventType.CharIndex:
                _memoryAccess.Access(ev.I);
                break;
        }
    }

    private void UpdateCounters(SortEvent ev)
    {
        Interlocked.Increment(ref _processedEvents);
        switch (ev.Type)
        {
            case SortEventType.Compare:
            case SortEventType.CharCompare:
                Interlocked.Increment(ref _comparisons);
                break;
            case SortEventType.Swap:
                Interlocked.Increment(ref _swaps);
                break;
            case SortEventType.Write:
            case SortEventType.BucketMove:
                Interlocked.Increment(ref _writes);
                break;
        }
    }

    private void MaybeEmitVisualEvent(SortEvent ev)
    {
        if (!Volatile.Read(ref _visualEnabled))
        {
            return;
        }

        var detail = (DetailLevel)Volatile.Read(ref _visualDetailLevel);
        if (!PassesDetail(ev.Type, detail))
        {
            return;
        }

        if (IsCompareLike(ev.Type))
        {
            var depth = Interlocked.CompareExchange(ref _visualEventQueueCount, 0, 0);
            if (ShouldDropCompareForPressure(depth, ev.StepId))
            {
                Interlocked.Increment(ref _droppedComparisons);
                return;
            }
        }

        _visualEventQueue.Enqueue(ev);
        var count = Interlocked.Increment(ref _visualEventQueueCount);
        while (count > MaxVisualQueueDepth && _visualEventQueue.TryDequeue(out var dropped))
        {
            count = Interlocked.Decrement(ref _visualEventQueueCount);
            if (IsCompareLike(dropped.Type))
            {
                Interlocked.Increment(ref _droppedComparisons);
            }
        }
    }

    private void MaybeEmitAudioEvent(SortEvent ev)
    {
        if (!Volatile.Read(ref _audioEnabled))
        {
            return;
        }

        if (ev.Type is not
            (SortEventType.Compare
            or SortEventType.Write
            or SortEventType.Swap
            or SortEventType.CharCompare
            or SortEventType.CharIndex
            or SortEventType.BucketMove
            or SortEventType.PassStart))
        {
            return;
        }

        var detail = (DetailLevel)Volatile.Read(ref _audioDetailLevel);
        if (!PassesDetail(ev.Type, detail))
        {
            return;
        }

        if (IsCompareLike(ev.Type))
        {
            var depth = Interlocked.CompareExchange(ref _audioQueueCount, 0, 0);
            if (depth > 48)
            {
                return;
            }
        }

        var value = ResolveEventValue(ev);
        var pan = ResolveEventPan(ev);
        _audioQueue.Enqueue(new AudioTrigger(ev.Type, value, 256, pan));
        var count = Interlocked.Increment(ref _audioQueueCount);
        while (count > MaxAudioQueueDepth && _audioQueue.TryDequeue(out _))
        {
            count = Interlocked.Decrement(ref _audioQueueCount);
        }
    }

    private int ResolveEventValue(SortEvent ev)
    {
        return ev.Type switch
        {
            SortEventType.CharCompare => Math.Clamp(ev.Value, 0, 255),
            SortEventType.CharIndex => Math.Clamp(ev.Aux != 0 ? ev.Aux : ev.Value, 0, 255),
            SortEventType.BucketMove => Math.Clamp(ev.Aux, 0, 255),
            SortEventType.PassStart => Math.Clamp(ev.Value, 0, 255),
            SortEventType.Write => Math.Clamp(ev.Aux, 0, 255),
            _ => Math.Clamp(ev.Value, 0, 255)
        };
    }

    private float ResolveEventPan(SortEvent ev)
    {
        lock (_stateLock)
        {
            if (_visualData.Length <= 1)
            {
                return 0.0f;
            }

            var index = ev.Type switch
            {
                SortEventType.CharCompare => ev.I >= 0 && ev.J >= 0 ? (ev.I + ev.J) / 2 : Math.Max(ev.I, ev.J),
                SortEventType.BucketMove => ev.I,
                SortEventType.Write => ev.I,
                _ => ev.I >= 0 ? ev.I : ev.J
            };

            index = Math.Clamp(index, 0, _visualData.Length - 1);
            var t = index / (float)Math.Max(1, _visualData.Length - 1);
            return t * 2.0f - 1.0f;
        }
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _visualData.Length;
    }

    private static bool PassesDetail(SortEventType type, DetailLevel level)
    {
        return level switch
        {
            DetailLevel.L1 => type is SortEventType.Swap or SortEventType.Write or SortEventType.BucketMove,
            DetailLevel.L2 => type is SortEventType.Compare or SortEventType.Swap or SortEventType.Write or SortEventType.CharCompare or SortEventType.CharIndex or SortEventType.BucketMove,
            _ => type != SortEventType.Done
        };
    }

    private static bool IsCompareLike(SortEventType type)
    {
        return type is SortEventType.Compare or SortEventType.CharCompare;
    }

    private static bool ShouldDropCompareForPressure(int queueDepth, long stepId)
    {
        if (queueDepth >= CompareDropHardDepth)
        {
            return true;
        }

        if (queueDepth < CompareDropStartDepth)
        {
            return false;
        }

        return (stepId & 0x3) != 0;
    }

    private void ResetStats()
    {
        Interlocked.Exchange(ref _comparisons, 0);
        Interlocked.Exchange(ref _swaps, 0);
        Interlocked.Exchange(ref _writes, 0);
        Interlocked.Exchange(ref _processedEvents, 0);
        Interlocked.Exchange(ref _droppedComparisons, 0);
        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);

        _effectiveEventsPerSecond = 0.0;

        while (_visualEventQueue.TryDequeue(out _))
        {
        }

        while (_audioQueue.TryDequeue(out _))
        {
        }

        Interlocked.Exchange(ref _visualEventQueueCount, 0);
        Interlocked.Exchange(ref _audioQueueCount, 0);

        lock (_stateLock)
        {
            _memoryAccess.Reset();
        }
        _elapsed.Reset();
    }

    public void Dispose()
    {
        Stop();
    }
}
