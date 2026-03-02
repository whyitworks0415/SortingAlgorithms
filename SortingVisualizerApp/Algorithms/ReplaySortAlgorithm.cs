using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ReplaySortAlgorithm : ISortAlgorithm
{
    private readonly SortEvent[] _events;
    private readonly int _startIndex;

    public ReplaySortAlgorithm(SortEvent[] events, int startIndex = 0)
    {
        _events = events ?? Array.Empty<SortEvent>();
        _startIndex = Math.Clamp(startIndex, 0, _events.Length);
    }

    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        for (var i = _startIndex; i < _events.Length; i++)
        {
            yield return _events[i];
        }

        if (_events.Length == 0 || _events[^1].Type != SortEventType.Done)
        {
            yield return new SortEvent(SortEventType.Done);
        }
    }
}
