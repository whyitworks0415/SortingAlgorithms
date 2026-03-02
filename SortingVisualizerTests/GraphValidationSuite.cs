using SortingVisualizerApp.Algorithms;
using SortingVisualizerApp.Core;

internal static class GraphValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: status=A and Graph-supported algorithms.",
            "Checks: emitted order topological validity, exactly-once emission, cycle behavior declaration."
        };

        var metas = registry.All
            .Where(static meta => meta.Status == AlgorithmImplementationStatus.A
                                  && (meta.SupportedViews & SupportedViews.Graph) != 0
                                  && meta.Factory is not null)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runs = 0;
        foreach (var meta in metas)
        {
            var algorithm = meta.Factory!.Invoke();
            if (algorithm is not IGraphAlgorithm graphAlgorithm)
            {
                failures.Add($"{meta.Name}: Graph view supported but IGraphAlgorithm is missing.");
                continue;
            }

            foreach (var nodeCount in new[] { 10, 80, 200 })
            {
                foreach (var density in new[] { 0.08f, 0.22f })
                {
                    runs++;
                    var seed = HashCode.Combine(meta.Id, nodeCount, density, 260226);
                    graphAlgorithm.ConfigureGraph(nodeCount, seed, density);
                    var graph = graphAlgorithm.Graph;

                    var source = DataGenerator.Generate(graph.NodeCount, DistributionPreset.Random, seed);
                    var options = new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true);
                    var trace = ExecutionHarness.RunAlgorithm(
                        algorithm,
                        source,
                        options,
                        new ExecutionLimits
                        {
                            MaxEvents = 3_000_000,
                            Timeout = TimeSpan.FromSeconds(8)
                        },
                        captureEvents: true);

                    if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
                    {
                        failures.Add($"{meta.Name}/nodes={nodeCount}/density={density:0.00}: execution failed.");
                        continue;
                    }

                    ValidateTopologicalOrder(meta.Name, graph, trace.Events, failures);
                }
            }

            runs++;
            ValidateCycleScenario(meta.Name, algorithm, failures, notes);
        }

        return new ValidationSuiteResult
        {
            Name = "Graph",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private static void ValidateTopologicalOrder(
        string algorithmName,
        GraphDefinition graph,
        IReadOnlyList<SortEvent> events,
        ICollection<string> failures)
    {
        var emitted = new List<int>(graph.NodeCount);
        var emittedSet = new HashSet<int>();

        foreach (var ev in events)
        {
            if (ev.Type != SortEventType.NodeEmitted)
            {
                continue;
            }

            if (ev.I < 0 || ev.I >= graph.NodeCount)
            {
                failures.Add($"{algorithmName}: emitted node out of range ({ev.I}).");
                return;
            }

            emitted.Add(ev.I);
            emittedSet.Add(ev.I);
        }

        if (emitted.Count != graph.NodeCount)
        {
            failures.Add($"{algorithmName}: emitted count mismatch ({emitted.Count} vs {graph.NodeCount}).");
            return;
        }

        if (emittedSet.Count != graph.NodeCount)
        {
            failures.Add($"{algorithmName}: some nodes emitted multiple times.");
            return;
        }

        var position = new int[graph.NodeCount];
        for (var i = 0; i < emitted.Count; i++)
        {
            position[emitted[i]] = i;
        }

        for (var i = 0; i < graph.Edges.Length; i++)
        {
            var edge = graph.Edges[i];
            if (position[edge.From] >= position[edge.To])
            {
                failures.Add($"{algorithmName}: topological order violation on edge {edge.From}->{edge.To}.");
                return;
            }
        }
    }

    private static void ValidateCycleScenario(
        string algorithmName,
        ISortAlgorithm algorithm,
        ICollection<string> failures,
        ICollection<string> notes)
    {
        if (algorithm is not TopologicalSortAlgorithm topo)
        {
            notes.Add($"{algorithmName}: cycle scenario test skipped (algorithm-specific hook unavailable).");
            return;
        }

        var cycleGraph = new GraphDefinition(
            nodeCount: 6,
            edges: new[]
            {
                new GraphEdge(0, 1),
                new GraphEdge(1, 2),
                new GraphEdge(2, 3),
                new GraphEdge(3, 4),
                new GraphEdge(4, 5),
                new GraphEdge(5, 2)
            },
            initialInDegrees: new[] { 0, 1, 2, 1, 1, 1 });

        topo.SetGraphForTesting(cycleGraph);

        var source = DataGenerator.Generate(6, DistributionPreset.Random, 1200);
        var options = new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true);
        var trace = ExecutionHarness.RunAlgorithm(
            topo,
            source,
            options,
            new ExecutionLimits
            {
                MaxEvents = 100_000,
                Timeout = TimeSpan.FromSeconds(2)
            },
            captureEvents: true);

        if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
        {
            failures.Add($"{algorithmName}/cycle: execution failed.");
            return;
        }

        var emitted = trace.Events.Count(static ev => ev.Type == SortEventType.NodeEmitted);
        if (emitted >= cycleGraph.NodeCount)
        {
            failures.Add($"{algorithmName}/cycle: expected partial emission under cycle, got {emitted}.");
            return;
        }

        if (!topo.LastCycleDetected)
        {
            failures.Add($"{algorithmName}/cycle: cycle flag not reported.");
        }
    }
}
