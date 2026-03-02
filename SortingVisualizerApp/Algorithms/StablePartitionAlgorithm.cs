namespace SortingVisualizerApp.Algorithms;

public sealed class StablePartitionAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var snapshot = new int[Length];
        for (var i = 0; i < Length; i++)
        {
            snapshot[i] = Read(i);
        }

        var partitioned = PartitionStable(snapshot, static value => (value & 1) == 0);
        MarkRange(0, Length - 1);
        for (var i = 0; i < partitioned.Length; i++)
        {
            Write(i, partitioned[i]);
        }

        MergeSortRange(0, Length);
    }

    public static int[] PartitionStable(int[] source, Predicate<int> predicate)
    {
        var trueSide = new List<int>(source.Length);
        var falseSide = new List<int>(source.Length);

        for (var i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                trueSide.Add(source[i]);
            }
            else
            {
                falseSide.Add(source[i]);
            }
        }

        var result = new int[source.Length];
        var outIndex = 0;
        for (var i = 0; i < trueSide.Count; i++)
        {
            result[outIndex++] = trueSide[i];
        }

        for (var i = 0; i < falseSide.Count; i++)
        {
            result[outIndex++] = falseSide[i];
        }

        return result;
    }

    private void MergeSortRange(int left, int rightExclusive)
    {
        if (rightExclusive - left <= 1)
        {
            return;
        }

        var mid = left + ((rightExclusive - left) >> 1);
        MergeSortRange(left, mid);
        MergeSortRange(mid, rightExclusive);

        var buffer = new int[rightExclusive - left];
        var i = left;
        var j = mid;
        var k = 0;

        while (i < mid && j < rightExclusive)
        {
            if (Compare(i, j) <= 0)
            {
                buffer[k++] = Read(i++);
            }
            else
            {
                buffer[k++] = Read(j++);
            }
        }

        while (i < mid)
        {
            buffer[k++] = Read(i++);
        }

        while (j < rightExclusive)
        {
            buffer[k++] = Read(j++);
        }

        for (var idx = 0; idx < buffer.Length; idx++)
        {
            Write(left + idx, buffer[idx]);
        }
    }
}
