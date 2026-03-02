namespace SortingVisualizerApp.Core;

public enum SpatialDistributionPreset
{
    Uniform,
    Gaussian,
    Clusters
}

public readonly record struct SpatialPoint(
    int Id,
    float X,
    float Y);

public readonly record struct SpatialSortOptions(
    bool EmitExtendedEvents = true);

public interface ISpatialSortAlgorithm
{
    IEnumerable<SortEvent> Execute(SpatialPoint[] data, SpatialSortOptions options);
}
