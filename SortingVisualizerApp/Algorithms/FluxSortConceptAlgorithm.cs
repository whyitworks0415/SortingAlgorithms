using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class FluxSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var n = Length;

        EmitEvent(SortEventType.MarkStage, value: 8601, aux: n); // sampling stage
        var sampleStride = Math.Max(1, n / 16);
        for (var i = 0; i < n; i += sampleStride)
        {
            EmitEvent(SortEventType.MarkPivot, i, value: Read(i), aux: 8601);
        }
        EmitEvent(SortEventType.MarkRange, 0, n - 1, value: 86011, aux: sampleStride);

        EmitEvent(SortEventType.MarkStage, value: 8602, aux: 4); // partition stage
        var partitionCount = 4;
        var chunk = (n + partitionCount - 1) / partitionCount;
        for (var p = 0; p < partitionCount; p++)
        {
            var left = p * chunk;
            if (left >= n)
            {
                break;
            }

            var right = Math.Min(n - 1, left + chunk - 1);
            EmitEvent(SortEventType.MarkRange, left, right, value: 86021, aux: p);
        }

        EmitEvent(SortEventType.MarkStage, value: 8603, aux: partitionCount); // merge/refine stage
        for (var pass = 1; pass < partitionCount; pass++)
        {
            var right = Math.Min(n - 1, ((pass + 1) * chunk) - 1);
            EmitEvent(SortEventType.MarkRange, 0, right, value: 86031, aux: pass);
        }

        // B concept finalization: deterministic sorted writeback.
        var output = Snapshot();
        Array.Sort(output);
        for (var i = 0; i < output.Length; i++)
        {
            Write(i, output[i]);
        }
    }

    private int[] Snapshot()
    {
        var values = new int[Length];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Read(i);
        }

        return values;
    }
}
