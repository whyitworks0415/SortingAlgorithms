namespace SortingVisualizerApp.Core;

public static class ReplayUtilsV1
{
    public static ReplayFileV1 BuildFromRecorded(
        string algorithmId,
        int seed,
        DistributionPreset distribution,
        int maxValue,
        int[] initialData,
        SortEvent[] events,
        int keyframeIntervalEvents = 50_000)
    {
        keyframeIntervalEvents = Math.Max(1, keyframeIntervalEvents);

        var keyframes = new List<ReplayKeyframe>(16)
        {
            new ReplayKeyframe(0, initialData.ToArray())
        };

        var state = initialData.ToArray();
        for (var i = 0; i < events.Length; i++)
        {
            ApplyEvent(state, events[i]);

            var eventIndex = i + 1;
            if (eventIndex % keyframeIntervalEvents == 0)
            {
                keyframes.Add(new ReplayKeyframe(eventIndex, state.ToArray()));
            }
        }

        if (keyframes[^1].EventIndex != events.Length)
        {
            keyframes.Add(new ReplayKeyframe(events.Length, state.ToArray()));
        }

        return new ReplayFileV1(
            AlgorithmId: algorithmId,
            N: initialData.Length,
            Seed: seed,
            Distribution: distribution,
            MaxValue: maxValue,
            CreatedUtc: DateTime.UtcNow,
            Events: events.ToArray(),
            Keyframes: keyframes.ToArray());
    }

    public static int[] ReconstructStateAtEvent(ReplayFileV1 replay, int targetEventIndex)
    {
        targetEventIndex = Math.Clamp(targetEventIndex, 0, replay.Events.Length);

        var keyframes = replay.Keyframes;
        if (keyframes.Length == 0)
        {
            return new int[replay.N];
        }

        var lo = 0;
        var hi = keyframes.Length - 1;
        var selected = 0;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            var idx = keyframes[mid].EventIndex;
            if (idx <= targetEventIndex)
            {
                selected = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        var keyframe = keyframes[selected];
        var state = keyframe.Snapshot.ToArray();
        for (var i = keyframe.EventIndex; i < targetEventIndex; i++)
        {
            ApplyEvent(state, replay.Events[i]);
        }

        return state;
    }

    private static void ApplyEvent(int[] state, SortEvent ev)
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
