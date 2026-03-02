using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BingoLegacyAlgorithm : ISortAlgorithm
{
    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        return new BingoSortAlgorithm().Execute(data, options);
    }
}
