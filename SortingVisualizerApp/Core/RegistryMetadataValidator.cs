using SortingVisualizerApp.Algorithms;

namespace SortingVisualizerApp.Core;

public static class RegistryMetadataValidator
{
    public static IReadOnlyList<string> Validate(AlgorithmRegistry registry, bool includeStableSmoke)
    {
        var issues = new List<string>();

        var duplicatedIds = registry.All
            .GroupBy(static meta => meta.Id, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        foreach (var duplicatedId in duplicatedIds)
        {
            issues.Add($"Duplicate algorithm id: {duplicatedId}");
        }

        foreach (var meta in registry.All)
        {
            if (meta.SupportedViews == SupportedViews.None)
            {
                issues.Add($"{meta.Name}: supported views are empty.");
            }

            if (meta.Difficulty is < 1 or > 5)
            {
                issues.Add($"{meta.Name}: difficulty must be in range 1..5 when provided.");
            }

            var hasBarsFactory = meta.Factory is not null;
            var hasStringFactory = meta.StringFactory is not null;
            var hasSpatialFactory = meta.SpatialFactory is not null;
            var hasAnyFactory = hasBarsFactory || hasStringFactory || hasSpatialFactory;

            if (meta.Status == AlgorithmImplementationStatus.A && !hasAnyFactory)
            {
                issues.Add($"{meta.Name}: status A but no factory is available.");
                continue;
            }

            // Status B may still provide an executable concept simulation factory.

            if (meta.Status == AlgorithmImplementationStatus.A)
            {
                if (string.IsNullOrWhiteSpace(meta.AverageComplexity) || meta.AverageComplexity == "-")
                {
                    issues.Add($"{meta.Name}: average complexity missing.");
                }

                if (string.IsNullOrWhiteSpace(meta.WorstComplexity) || meta.WorstComplexity == "-")
                {
                    issues.Add($"{meta.Name}: worst complexity missing.");
                }
            }

            if ((meta.SupportedViews & (SupportedViews.Bars | SupportedViews.Network | SupportedViews.External | SupportedViews.Graph)) != 0
                && meta.Status == AlgorithmImplementationStatus.A
                && meta.Factory is null)
            {
                issues.Add($"{meta.Name}: Bars-family view supported but ISortAlgorithm factory is missing.");
            }

            if ((meta.SupportedViews & SupportedViews.String) != 0
                && meta.Status == AlgorithmImplementationStatus.A)
            {
                if (meta.StringFactory is null)
                {
                    issues.Add($"{meta.Name}: String view supported but IStringSortAlgorithm factory is missing.");
                }
                else if (meta.StringFactory.Invoke() is null)
                {
                    issues.Add($"{meta.Name}: StringFactory returned null.");
                }
            }

            if ((meta.SupportedViews & SupportedViews.Spatial) != 0
                && meta.Status == AlgorithmImplementationStatus.A)
            {
                if (meta.SpatialFactory is null)
                {
                    issues.Add($"{meta.Name}: Spatial view supported but ISpatialSortAlgorithm factory is missing.");
                }
                else if (meta.SpatialFactory.Invoke() is null)
                {
                    issues.Add($"{meta.Name}: SpatialFactory returned null.");
                }
            }

            if ((meta.SupportedViews & SupportedViews.Network) != 0
                && meta.Status == AlgorithmImplementationStatus.A
                && meta.Factory is not null)
            {
                var instance = meta.Factory.Invoke();
                if (instance is not INetworkScheduleProvider)
                {
                    issues.Add($"{meta.Name}: Network view supported but INetworkScheduleProvider is missing.");
                }
            }

            if ((meta.SupportedViews & SupportedViews.Graph) != 0
                && meta.Status == AlgorithmImplementationStatus.A
                && meta.Factory is not null)
            {
                var instance = meta.Factory.Invoke();
                if (instance is not IGraphAlgorithm)
                {
                    issues.Add($"{meta.Name}: Graph view supported but IGraphAlgorithm is missing.");
                }
            }
        }

        if (includeStableSmoke)
        {
            var stableIssues = ValidateStableClaims(registry);
            issues.AddRange(stableIssues);
        }

        return issues;
    }

    private static IEnumerable<string> ValidateStableClaims(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var size = 128;
        var seed = 20260226;
        var source = DataGenerator.Generate(size, DistributionPreset.ManyDuplicates, seed);
        var options = new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true);

        foreach (var meta in registry.All.Where(static item =>
                     item.Status == AlgorithmImplementationStatus.A
                     && (item.SupportedViews & SupportedViews.Bars) != 0
                     && item.Stable == true
                     && item.Factory is not null))
        {
            var algorithm = meta.Factory!.Invoke();
            var state = source.ToArray();

            var trace = StableTrace.FromSource(state);
            long events = 0;
            var done = false;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                foreach (var ev in algorithm.Execute(source.AsSpan(), options))
                {
                    events++;
                    if (events > 5_000_000 || stopwatch.Elapsed > TimeSpan.FromSeconds(5))
                    {
                        failures.Add($"{meta.Name}: stable smoke timed out.");
                        break;
                    }

                    trace.Apply(ev);
                    if (ev.Type == SortEventType.Done)
                    {
                        done = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{meta.Name}: stable smoke exception: {ex.Message}");
                continue;
            }

            if (!done)
            {
                failures.Add($"{meta.Name}: stable smoke did not reach Done event.");
                continue;
            }

            if (!trace.IsStableAscending())
            {
                failures.Add($"{meta.Name}: stable=true but stable smoke failed.");
            }
        }

        return failures;
    }

    private sealed class StableTrace
    {
        private readonly StableItem[] _items;

        private StableTrace(StableItem[] items)
        {
            _items = items;
        }

        public static StableTrace FromSource(IReadOnlyList<int> source)
        {
            var items = new StableItem[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                items[i] = new StableItem(source[i], i);
            }

            return new StableTrace(items);
        }

        public void Apply(SortEvent ev)
        {
            switch (ev.Type)
            {
                case SortEventType.Swap:
                    if (InRange(ev.I) && InRange(ev.J))
                    {
                        (_items[ev.I], _items[ev.J]) = (_items[ev.J], _items[ev.I]);
                    }
                    break;
                case SortEventType.Write:
                    if (!InRange(ev.I))
                    {
                        return;
                    }

                    var index = FindFirstByValue(ev.Value);
                    if (index >= 0)
                    {
                        _items[ev.I] = _items[index];
                    }
                    else
                    {
                        _items[ev.I] = new StableItem(ev.Value, int.MaxValue - ev.I);
                    }
                    break;
            }
        }

        public bool IsStableAscending()
        {
            for (var i = 1; i < _items.Length; i++)
            {
                var prev = _items[i - 1];
                var curr = _items[i];
                if (prev.Value > curr.Value)
                {
                    return false;
                }

                if (prev.Value == curr.Value && prev.OriginalIndex > curr.OriginalIndex)
                {
                    return false;
                }
            }

            return true;
        }

        private int FindFirstByValue(int value)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                if (_items[i].Value == value)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool InRange(int index)
        {
            return index >= 0 && index < _items.Length;
        }

        private readonly record struct StableItem(int Value, int OriginalIndex);
    }
}
