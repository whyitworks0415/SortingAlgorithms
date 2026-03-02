using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class WikiSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var n = Length;
        var runSize = Math.Clamp((int)Math.Sqrt(n), 16, 64);

        EmitEvent(SortEventType.MarkStage, value: 8501, aux: runSize); // run discovery
        for (var start = 0; start < n; start += runSize)
        {
            var end = Math.Min(n - 1, start + runSize - 1);
            EmitEvent(SortEventType.MarkRange, start, end, value: 85011, aux: runSize);
        }

        EmitEvent(SortEventType.MarkStage, value: 8502, aux: runSize); // block buffer sizing
        EmitEvent(SortEventType.MarkRange, 0, n - 1, value: 85021, aux: runSize);

        for (var width = runSize; width < n; width <<= 1)
        {
            EmitEvent(SortEventType.MarkStage, value: 8503, aux: width); // block merge phases
            for (var left = 0; left < n; left += width << 1)
            {
                var mid = Math.Min(left + width, n);
                var right = Math.Min(left + (width << 1), n);
                if (mid >= right)
                {
                    continue;
                }

                EmitEvent(SortEventType.MarkRange, left, right - 1, value: 85031, aux: width);
            }
        }

        // B concept finalization: deterministic stable-ish completion path.
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
