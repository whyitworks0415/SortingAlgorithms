using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class SleepSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var values = new int[Length];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Read(i);
        }

        var min = values.Min();
        var buckets = new SortedDictionary<int, List<int>>();
        EmitEvent(SortEventType.MarkStage, value: 7341);

        for (var i = 0; i < values.Length; i++)
        {
            var delay = values[i] - min;
            if (!buckets.TryGetValue(delay, out var list))
            {
                list = new List<int>();
                buckets[delay] = list;
            }

            list.Add(values[i]);
            EmitEvent(SortEventType.LevelHighlight, delay, value: i);
        }

        EmitEvent(SortEventType.MarkStage, value: 7342);
        var outIndex = 0;
        foreach (var pair in buckets)
        {
            foreach (var value in pair.Value)
            {
                Write(outIndex++, value);
            }
        }
    }
}
