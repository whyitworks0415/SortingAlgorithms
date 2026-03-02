using System.Numerics;

namespace SortingVisualizerApp.Algorithms;

public sealed class SpreadSortAlgorithm : EventSortAlgorithmBase
{
    private const int InsertionThreshold = 32;
    private const int MaxBucketBits = 8;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        SortRange(0, Length - 1);
    }

    private void SortRange(int left, int right)
    {
        var size = right - left + 1;
        if (size <= 1)
        {
            return;
        }

        if (size <= InsertionThreshold)
        {
            InsertionSort(left, right);
            return;
        }

        MarkRange(left, right);

        var minIndex = left;
        var maxIndex = left;
        for (var i = left + 1; i <= right; i++)
        {
            if (Compare(i, minIndex) < 0)
            {
                minIndex = i;
            }

            if (Compare(maxIndex, i) < 0)
            {
                maxIndex = i;
            }
        }

        var minValue = Read(minIndex);
        var maxValue = Read(maxIndex);
        if (minValue == maxValue)
        {
            return;
        }

        var range = maxValue - minValue;
        var bits = 32 - BitOperations.LeadingZeroCount((uint)range);
        var bucketBits = Math.Clamp(bits, 1, MaxBucketBits);
        var shift = Math.Max(0, bits - bucketBits);
        var bucketCount = 1 << bucketBits;

        var counts = new int[bucketCount];
        for (var i = left; i <= right; i++)
        {
            var bucket = (Read(i) - minValue) >> shift;
            counts[bucket]++;
            MarkBucket(i, bucket, Read(i));
        }

        var offsets = new int[bucketCount];
        offsets[0] = 0;
        for (var b = 1; b < bucketCount; b++)
        {
            offsets[b] = offsets[b - 1] + counts[b - 1];
        }

        var cursor = offsets.ToArray();
        var temp = new int[size];
        for (var i = left; i <= right; i++)
        {
            var value = Read(i);
            var bucket = (value - minValue) >> shift;
            var destination = cursor[bucket]++;
            temp[destination] = value;
        }

        for (var i = 0; i < size; i++)
        {
            Write(left + i, temp[i]);
        }

        if (shift == 0)
        {
            return;
        }

        var runStart = left;
        for (var b = 0; b < bucketCount; b++)
        {
            var runLength = counts[b];
            if (runLength > 1)
            {
                SortRange(runStart, runStart + runLength - 1);
            }

            runStart += runLength;
        }
    }

    private void InsertionSort(int left, int right)
    {
        for (var i = left + 1; i <= right; i++)
        {
            var key = Read(i);
            var j = i - 1;
            while (j >= left)
            {
                Compare(j, i);
                if (Read(j) <= key)
                {
                    break;
                }

                Write(j + 1, Read(j));
                j--;
            }

            Write(j + 1, key);
        }
    }
}
