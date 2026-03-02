using SortingVisualizerApp.Core;

internal static class StableValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = new List<string>();
        var notes = new List<string>
        {
            "Targets: stable=true and Bars-supported A algorithms.",
            "Validation uses event replay with range-context write tracing (best-effort for write-heavy implementations)."
        };

        var metas = registry.All
            .Where(static meta => meta.Status == AlgorithmImplementationStatus.A
                                  && (meta.SupportedViews & SupportedViews.Bars) != 0
                                  && meta.Stable == true
                                  && meta.Factory is not null)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runs = 0;
        foreach (var meta in metas)
        {
            foreach (var distribution in new[] { DistributionPreset.ManyDuplicates, DistributionPreset.Random })
            {
                runs++;
                var size = 512;
                var seed = HashCode.Combine(meta.Id, distribution, 2602);
                var source = DataGenerator.Generate(size, distribution, seed);
                var options = new SortOptions(MaxValue: Math.Max(1, source.Max()), EmitExtendedEvents: true);
                var trace = ExecutionHarness.RunAlgorithm(
                    meta.Factory!.Invoke(),
                    source,
                    options,
                    new ExecutionLimits
                    {
                        MaxEvents = 12_000_000,
                        Timeout = TimeSpan.FromSeconds(12)
                    },
                    captureEvents: true);

                if (trace.Error is not null || trace.TimedOut || trace.EventLimitExceeded || !trace.DoneSeen)
                {
                    failures.Add($"{meta.Name}/{distribution}: execution failed before stability check.");
                    continue;
                }

                if (!ExecutionHarness.IsSortedAscending(trace.FinalState))
                {
                    failures.Add($"{meta.Name}/{distribution}: output not sorted.");
                    continue;
                }

                var stableTrace = StabilityTrace.FromSource(source);
                for (var i = 0; i < trace.Events.Count; i++)
                {
                    stableTrace.Apply(trace.Events[i]);
                }

                if (!stableTrace.IsStableAscending())
                {
                    failures.Add($"{meta.Name}/{distribution}: stable order broken (value ties reordered).");
                }
            }
        }

        return new ValidationSuiteResult
        {
            Name = "Stable",
            Runs = runs,
            Failures = failures,
            Notes = notes
        };
    }

    private sealed class StabilityTrace
    {
        private readonly StableItem[] _items;
        private readonly List<WriteRangeContext> _contexts = new();

        private StabilityTrace(StableItem[] items)
        {
            _items = items;
        }

        public static StabilityTrace FromSource(IReadOnlyList<int> source)
        {
            var items = new StableItem[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                items[i] = new StableItem(source[i], i);
            }

            return new StabilityTrace(items);
        }

        public void Apply(SortEvent ev)
        {
            switch (ev.Type)
            {
                case SortEventType.MarkRange:
                    AddContext(ev.I, ev.J);
                    break;
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

                    var chosen = TryTakeContextItem(ev.I, ev.Value)
                                 ?? TryTakeCurrentItem(ev.Value)
                                 ?? new StableItem(ev.Value, int.MaxValue - ev.I);
                    _items[ev.I] = chosen;
                    MarkContextWrite(ev.I);
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

        private void AddContext(int left, int right)
        {
            if (!InRange(left) || !InRange(right) || left > right)
            {
                return;
            }

            var map = new Dictionary<int, Queue<StableItem>>();
            for (var i = left; i <= right; i++)
            {
                var item = _items[i];
                if (!map.TryGetValue(item.Value, out var queue))
                {
                    queue = new Queue<StableItem>();
                    map[item.Value] = queue;
                }

                queue.Enqueue(item);
            }

            _contexts.Add(new WriteRangeContext(left, right, map));
            if (_contexts.Count > 16)
            {
                _contexts.RemoveAt(0);
            }
        }

        private StableItem? TryTakeContextItem(int index, int value)
        {
            for (var i = _contexts.Count - 1; i >= 0; i--)
            {
                var context = _contexts[i];
                if (!context.Contains(index))
                {
                    continue;
                }

                if (!context.ValueQueues.TryGetValue(value, out var queue) || queue.Count == 0)
                {
                    continue;
                }

                return queue.Dequeue();
            }

            return null;
        }

        private StableItem? TryTakeCurrentItem(int value)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                if (_items[i].Value == value)
                {
                    return _items[i];
                }
            }

            return null;
        }

        private void MarkContextWrite(int index)
        {
            for (var i = _contexts.Count - 1; i >= 0; i--)
            {
                var context = _contexts[i];
                if (!context.Contains(index))
                {
                    continue;
                }

                context.WrittenIndices.Add(index);
                if (context.WrittenIndices.Count >= context.Length)
                {
                    _contexts.RemoveAt(i);
                }
            }
        }

        private bool InRange(int index)
        {
            return index >= 0 && index < _items.Length;
        }

        private sealed class WriteRangeContext
        {
            public WriteRangeContext(int left, int right, Dictionary<int, Queue<StableItem>> valueQueues)
            {
                Left = left;
                Right = right;
                ValueQueues = valueQueues;
            }

            public int Left { get; }
            public int Right { get; }
            public int Length => Right - Left + 1;
            public Dictionary<int, Queue<StableItem>> ValueQueues { get; }
            public HashSet<int> WrittenIndices { get; } = new();

            public bool Contains(int index)
            {
                return index >= Left && index <= Right;
            }
        }

        private readonly record struct StableItem(int Value, int OriginalIndex);
    }
}
