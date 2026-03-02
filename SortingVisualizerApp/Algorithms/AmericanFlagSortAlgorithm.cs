namespace SortingVisualizerApp.Algorithms;

public sealed class AmericanFlagSortAlgorithm : EventSortAlgorithmBase
{
    private const int Radix = 256;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var maxValue = 0;
        for (var i = 0; i < Length; i++)
        {
            maxValue = Math.Max(maxValue, Read(i));
        }

        var shift = 24;
        while (shift > 0 && ((maxValue >> shift) & 0xFF) == 0)
        {
            shift -= 8;
        }

        SortRange(0, Length - 1, shift);
    }

    private void SortRange(int left, int right, int shift)
    {
        if (left >= right || shift < 0)
        {
            return;
        }

        MarkRange(left, right);

        var count = new int[Radix];
        for (var i = left; i <= right; i++)
        {
            var digit = Digit(Read(i), shift);
            count[digit]++;
            MarkBucket(i, digit, Read(i));
        }

        var start = new int[Radix];
        start[0] = left;
        for (var d = 1; d < Radix; d++)
        {
            start[d] = start[d - 1] + count[d - 1];
        }

        var end = new int[Radix];
        for (var d = 0; d < Radix; d++)
        {
            end[d] = start[d] + count[d];
        }

        var next = start.ToArray();
        for (var bucket = 0; bucket < Radix; bucket++)
        {
            while (next[bucket] < end[bucket])
            {
                var index = next[bucket];
                var currentDigit = Digit(Read(index), shift);
                if (currentDigit == bucket)
                {
                    next[bucket]++;
                    continue;
                }

                var destination = next[currentDigit];
                Swap(index, destination);
                next[currentDigit]++;
            }
        }

        if (shift == 0)
        {
            return;
        }

        for (var bucket = 0; bucket < Radix; bucket++)
        {
            var runLeft = start[bucket];
            var runRight = end[bucket] - 1;
            if (runRight > runLeft)
            {
                SortRange(runLeft, runRight, shift - 8);
            }
        }
    }

    private static int Digit(int value, int shift)
    {
        return (value >> shift) & 0xFF;
    }
}
