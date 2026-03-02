using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class TopologicalSortAlgorithm : ISortAlgorithm, IGraphAlgorithm
{
    private GraphDefinition _graph = new(0, Array.Empty<GraphEdge>(), Array.Empty<int>());
    private int _seed = 1337;
    private float _edgeDensity = 0.15f;
    private bool _lastCycleDetected;

    public GraphDefinition Graph => _graph;
    public bool LastCycleDetected => _lastCycleDetected;

    public void ConfigureGraph(int requestedNodeCount, int seed, float edgeDensity)
    {
        _seed = seed;
        _edgeDensity = Math.Clamp(edgeDensity, 0.01f, 0.9f);
        _graph = BuildRandomDag(Math.Clamp(requestedNodeCount, 10, 200), _seed, _edgeDensity);
    }

    public IEnumerable<SortEvent> Execute(Span<int> data, SortOptions options)
    {
        if (_graph.NodeCount <= 0)
        {
            ConfigureGraph(Math.Clamp(data.Length, 10, 200), _seed, _edgeDensity);
        }

        return ExecuteIterator();
    }

    public void SetGraphForTesting(GraphDefinition graph)
    {
        _graph = graph;
    }

    private IEnumerable<SortEvent> ExecuteIterator()
    {
        long step = 0;
        _lastCycleDetected = false;
        var graph = _graph;
        var adjacency = new List<int>[graph.NodeCount];
        for (var i = 0; i < adjacency.Length; i++)
        {
            adjacency[i] = new List<int>();
        }

        foreach (var edge in graph.Edges)
        {
            if (edge.From < 0 || edge.From >= graph.NodeCount || edge.To < 0 || edge.To >= graph.NodeCount)
            {
                continue;
            }

            adjacency[edge.From].Add(edge.To);
        }

        for (var i = 0; i < adjacency.Length; i++)
        {
            adjacency[i].Sort();
        }

        var indegree = graph.InitialInDegrees.ToArray();
        if (indegree.Length != graph.NodeCount)
        {
            Array.Resize(ref indegree, graph.NodeCount);
        }

        var queue = new PriorityQueue<int, int>();
        var emittedCount = 0;
        for (var node = 0; node < graph.NodeCount; node++)
        {
            if (indegree[node] == 0)
            {
                queue.Enqueue(node, node);
            }
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            yield return new SortEvent(SortEventType.NodeSelected, I: node, StepId: step++);
            yield return new SortEvent(SortEventType.NodeEmitted, I: node, StepId: step++);
            emittedCount++;

            foreach (var to in adjacency[node])
            {
                indegree[to] = Math.Max(0, indegree[to] - 1);
                yield return new SortEvent(SortEventType.InDegreeDecrement, I: node, J: to, Value: indegree[to], StepId: step++);
                if (indegree[to] == 0)
                {
                    queue.Enqueue(to, to);
                }
            }
        }

        _lastCycleDetected = emittedCount < graph.NodeCount;

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private static GraphDefinition BuildRandomDag(int nodeCount, int seed, float edgeDensity)
    {
        var rng = new Random(seed);
        var edges = new List<GraphEdge>(nodeCount * 3);
        var indegrees = new int[nodeCount];

        for (var from = 0; from < nodeCount - 1; from++)
        {
            for (var to = from + 1; to < nodeCount; to++)
            {
                if (rng.NextDouble() >= edgeDensity)
                {
                    continue;
                }

                edges.Add(new GraphEdge(from, to));
                indegrees[to]++;
            }
        }

        for (var node = 0; node < nodeCount - 1; node++)
        {
            if (edges.Any(edge => edge.From == node))
            {
                continue;
            }

            var to = Math.Min(nodeCount - 1, node + 1 + rng.Next(0, Math.Min(4, nodeCount - node - 1)));
            if (to <= node)
            {
                continue;
            }

            if (edges.Contains(new GraphEdge(node, to)))
            {
                continue;
            }

            edges.Add(new GraphEdge(node, to));
            indegrees[to]++;
        }

        return new GraphDefinition(nodeCount, edges.ToArray(), indegrees);
    }
}
