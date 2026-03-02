namespace SortingVisualizerApp.Algorithms;

public sealed class FlashSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var minIndex = 0;
        var maxIndex = 0;
        for (var i = 1; i < Length; i++)
        {
            if (Compare(i, minIndex) < 0)
            {
                minIndex = i;
            }

            if (Compare(i, maxIndex) > 0)
            {
                maxIndex = i;
            }
        }

        var min = Read(minIndex);
        var max = Read(maxIndex);
        if (min == max)
        {
            return;
        }

        var classes = Math.Clamp((int)Math.Round(0.43 * Length), 2, Math.Min(Length, 4096));
        var counts = new int[classes];

        for (var i = 0; i < Length; i++)
        {
            var value = Read(i);
            var klass = ClassIndex(value, min, max, classes);
            counts[klass]++;
            MarkBucket(i, klass, value);
        }

        var starts = new int[classes];
        for (var i = 1; i < classes; i++)
        {
            starts[i] = starts[i - 1] + counts[i - 1];
        }

        var cursor = starts.ToArray();
        var output = new int[Length];
        for (var i = 0; i < Length; i++)
        {
            var value = Read(i);
            var klass = ClassIndex(value, min, max, classes);
            output[cursor[klass]++] = value;
        }

        for (var klass = 0; klass < classes; klass++)
        {
            var count = counts[klass];
            if (count <= 1)
            {
                continue;
            }

            var start = starts[klass];
            Array.Sort(output, start, count);
            MarkRange(start, start + count - 1);
        }

        for (var i = 0; i < Length; i++)
        {
            Write(i, output[i]);
        }
    }

    private static int ClassIndex(int value, int min, int max, int classes)
    {
        var range = Math.Max(1, max - min);
        var scaled = (int)(((long)(value - min) * (classes - 1)) / range);
        return Math.Clamp(scaled, 0, classes - 1);
    }
}
