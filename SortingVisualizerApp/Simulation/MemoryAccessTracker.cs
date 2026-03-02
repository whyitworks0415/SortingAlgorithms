using System.Diagnostics;

namespace SortingVisualizerApp.Simulation;

internal sealed class MemoryAccessTracker
{
    private const int CacheCapacityLines = 128;

    private int[] _counts = Array.Empty<int>();
    private int _maxCount;
    private int _cacheLineSize = 64;

    private readonly LinkedList<int> _cacheLru = new();
    private readonly Dictionary<int, LinkedListNode<int>> _cacheNodes = new();

    private long _cacheHits;
    private long _cacheMisses;

    public void Resize(int length)
    {
        if (length < 0)
        {
            length = 0;
        }

        if (_counts.Length == length)
        {
            Reset();
            return;
        }

        _counts = new int[length];
        _maxCount = 0;
        _cacheHits = 0;
        _cacheMisses = 0;
        _cacheLru.Clear();
        _cacheNodes.Clear();
    }

    public void Reset()
    {
        if (_counts.Length > 0)
        {
            Array.Clear(_counts, 0, _counts.Length);
        }

        _maxCount = 0;
        _cacheHits = 0;
        _cacheMisses = 0;
        _cacheLru.Clear();
        _cacheNodes.Clear();
    }

    public void SetCacheLineSize(int lineSize)
    {
        _cacheLineSize = Math.Max(1, lineSize);
    }

    public void Access(int index)
    {
        if ((uint)index >= (uint)_counts.Length)
        {
            return;
        }

        var updated = ++_counts[index];
        if (updated > _maxCount)
        {
            _maxCount = updated;
        }

        TrackCache(index);
    }

    public void AccessPair(int i, int j)
    {
        Access(i);
        if (j != i)
        {
            Access(j);
        }
    }

    public int CopyCounts(ref int[] destination)
    {
        if (destination.Length != _counts.Length)
        {
            destination = new int[_counts.Length];
        }

        if (_counts.Length > 0)
        {
            Array.Copy(_counts, destination, _counts.Length);
        }

        return _maxCount;
    }

    public (long Hits, long Misses) CacheStats => (_cacheHits, _cacheMisses);

    [Conditional("DEBUG")]
    public void AssertInvariants()
    {
        Debug.Assert(_cacheLru.Count == _cacheNodes.Count);
    }

    private void TrackCache(int index)
    {
        var line = index / _cacheLineSize;
        if (_cacheNodes.TryGetValue(line, out var node))
        {
            _cacheHits++;
            if (!ReferenceEquals(_cacheLru.First, node))
            {
                _cacheLru.Remove(node);
                _cacheLru.AddFirst(node);
            }
            return;
        }

        _cacheMisses++;
        var newNode = _cacheLru.AddFirst(line);
        _cacheNodes[line] = newNode;

        if (_cacheNodes.Count <= CacheCapacityLines)
        {
            return;
        }

        var tail = _cacheLru.Last;
        if (tail is null)
        {
            return;
        }

        _cacheNodes.Remove(tail.Value);
        _cacheLru.RemoveLast();
    }
}
