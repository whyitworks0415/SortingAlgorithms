namespace SortingVisualizerApp.Core;

public sealed class GraphDefinition
{
    public GraphDefinition(int nodeCount, GraphEdge[] edges, int[] initialInDegrees)
    {
        NodeCount = Math.Max(0, nodeCount);
        Edges = edges ?? Array.Empty<GraphEdge>();
        InitialInDegrees = initialInDegrees ?? Array.Empty<int>();
    }

    public int NodeCount { get; }
    public GraphEdge[] Edges { get; }
    public int[] InitialInDegrees { get; }
}

public readonly record struct GraphEdge(int From, int To);

public interface IGraphAlgorithm
{
    GraphDefinition Graph { get; }
    void ConfigureGraph(int requestedNodeCount, int seed, float edgeDensity);
}
