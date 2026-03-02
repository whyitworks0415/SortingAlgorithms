using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class HadoopSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var values = Snapshot();
        EmitEvent(SortEventType.MarkStage, value: 7101); // map

        var mapperCount = Math.Clamp(Length / 2048, 2, 16);
        var partitions = new List<int>[mapperCount];
        for (var i = 0; i < mapperCount; i++)
        {
            partitions[i] = new List<int>();
        }

        var min = values.Min();
        var max = values.Max();
        var range = Math.Max(1, max - min);

        for (var i = 0; i < values.Length; i++)
        {
            var bucket = (int)(((long)(values[i] - min) * (mapperCount - 1)) / range);
            partitions[bucket].Add(values[i]);
            MarkBucket(i, bucket, values[i]);
        }

        EmitEvent(SortEventType.MarkStage, value: 7102); // reduce
        var index = 0;
        for (var p = 0; p < mapperCount; p++)
        {
            var part = partitions[p];
            part.Sort();
            EmitEvent(SortEventType.MarkRun, p, value: part.Count, aux: index);

            for (var i = 0; i < part.Count; i++)
            {
                Write(index++, part[i]);
            }
        }
    }

    private int[] Snapshot()
    {
        var arr = new int[Length];
        for (var i = 0; i < arr.Length; i++)
        {
            arr[i] = Read(i);
        }

        return arr;
    }
}
