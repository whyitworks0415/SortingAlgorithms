using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class OddEvenMergeLegacyAlgorithm : ISortAlgorithm, INetworkScheduleProvider
{
    private readonly OddEvenMergeNetworkSortAlgorithm _inner = new();

    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        return _inner.Execute(data, options);
    }

    public NetworkSchedule BuildSchedule(int length)
    {
        return _inner.BuildSchedule(length);
    }
}
