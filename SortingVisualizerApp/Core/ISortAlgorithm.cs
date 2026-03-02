namespace SortingVisualizerApp.Core;

public interface ISortAlgorithm
{
    IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options);
}
