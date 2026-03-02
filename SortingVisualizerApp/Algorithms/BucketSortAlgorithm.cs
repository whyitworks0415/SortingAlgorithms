namespace SortingVisualizerApp.Algorithms;

public sealed class BucketSortAlgorithm : EventSortAlgorithmBase
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

        var bucketCount = Math.Clamp((int)Math.Sqrt(Length), 2, Math.Min(Length, 2048));
        var buckets = new List<int>[bucketCount];
        for (var i = 0; i < bucketCount; i++)
        {
            buckets[i] = new List<int>();
        }

        for (var i = 0; i < Length; i++)
        {
            var value = Read(i);
            var bucket = BucketIndex(value, min, max, bucketCount);
            buckets[bucket].Add(value);
            MarkBucket(i, bucket, value);
        }

        var outputIndex = 0;
        for (var bucket = 0; bucket < bucketCount; bucket++)
        {
            var list = buckets[bucket];
            if (list.Count == 0)
            {
                continue;
            }

            list.Sort();
            MarkRange(outputIndex, outputIndex + list.Count - 1);

            for (var i = 0; i < list.Count; i++)
            {
                Write(outputIndex++, list[i]);
            }
        }
    }

    private static int BucketIndex(int value, int min, int max, int bucketCount)
    {
        var range = Math.Max(1, max - min);
        var scaled = (int)(((long)(value - min) * (bucketCount - 1)) / range);
        return Math.Clamp(scaled, 0, bucketCount - 1);
    }
}
