namespace SortingVisualizerApp.Algorithms;

public sealed class PigeonholeSortAlgorithm : EventSortAlgorithmBase
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

        var minValue = Read(minIndex);
        var maxValue = Read(maxIndex);
        var range = maxValue - minValue + 1;

        if (range <= 0)
        {
            return;
        }

        var holes = new int[range];
        for (var i = 0; i < Length; i++)
        {
            var bucket = Read(i) - minValue;
            holes[bucket]++;
            MarkBucket(i, bucket, Read(i));
        }

        var writeIndex = 0;
        for (var bucket = 0; bucket < holes.Length; bucket++)
        {
            var count = holes[bucket];
            while (count-- > 0)
            {
                Write(writeIndex++, bucket + minValue);
            }
        }
    }
}
