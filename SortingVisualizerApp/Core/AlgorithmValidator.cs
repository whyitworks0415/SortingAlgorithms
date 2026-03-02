namespace SortingVisualizerApp.Core;

public static class AlgorithmValidator
{
    public static IReadOnlyList<string> ValidateImplementedAlgorithms(AlgorithmRegistry registry, int n = 2048)
    {
        var errors = new List<string>();
        var presets = new[]
        {
            DistributionPreset.Random,
            DistributionPreset.NearlySorted,
            DistributionPreset.Reversed
        };

        foreach (var meta in registry.All.Where(static x => x.Status == AlgorithmImplementationStatus.A))
        {
            if ((meta.SupportedViews & SupportedViews.Bars) == 0)
            {
                continue;
            }

            if (!meta.IsImplemented)
            {
                errors.Add($"{meta.Name}: status A but no factory.");
                continue;
            }

            if (meta.Stable is null)
            {
                errors.Add($"{meta.Name}: stable metadata is null.");
            }

            if (!registry.TryCreate(meta.Id, out _, out var algorithm) || algorithm is null)
            {
                errors.Add($"{meta.Name}: failed to instantiate.");
                continue;
            }

            foreach (var preset in presets)
            {
                var data = DataGenerator.Generate(n, preset, seed: 12345);
                var state = data.ToArray();
                var options = new SortOptions(MaxValue: Math.Max(1, state.Max()), EmitExtendedEvents: true);

                try
                {
                    foreach (var ev in algorithm.Execute(state.AsSpan(), options))
                    {
                        switch (ev.Type)
                        {
                            case SortEventType.Swap:
                                if (ev.I >= 0 && ev.I < state.Length && ev.J >= 0 && ev.J < state.Length)
                                {
                                    (state[ev.I], state[ev.J]) = (state[ev.J], state[ev.I]);
                                }
                                break;
                            case SortEventType.Write:
                                if (ev.I >= 0 && ev.I < state.Length)
                                {
                                    state[ev.I] = ev.Value;
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{meta.Name}/{preset}: exception {ex.Message}");
                    continue;
                }

                if (!IsSortedAscending(state))
                {
                    errors.Add($"{meta.Name}/{preset}: output is not sorted.");
                }
            }
        }

        return errors;
    }

    private static bool IsSortedAscending(ReadOnlySpan<int> data)
    {
        for (var i = 1; i < data.Length; i++)
        {
            if (data[i - 1] > data[i])
            {
                return false;
            }
        }

        return true;
    }
}
