using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ZOrderSortAlgorithm : ISpatialSortAlgorithm
{
    public IEnumerable<SortEvent> Execute(SpatialPoint[] data, SpatialSortOptions options)
    {
        return MortonOrderSortAlgorithm.ExecuteIterator(data.ToArray(), useZOrderAlias: true);
    }
}
