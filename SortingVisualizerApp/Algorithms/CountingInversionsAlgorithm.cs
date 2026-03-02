namespace SortingVisualizerApp.Algorithms;

public sealed class CountingInversionsAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var buffer = new int[Length];
        var inversions = MergeCount(0, Length, buffer);

        // Surface inversion count for overlays/HUD without changing core stats flow.
        MarkPivotValue(0, inversions > int.MaxValue ? int.MaxValue : (int)inversions);
        MarkBucket(0, 0, inversions > int.MaxValue ? int.MaxValue : (int)inversions);
    }

    public static long CountInversions(int[] source)
    {
        if (source is null || source.Length <= 1)
        {
            return 0;
        }

        var copy = source.ToArray();
        var buffer = new int[copy.Length];
        return MergeCountRaw(copy, 0, copy.Length, buffer);
    }

    private long MergeCount(int left, int rightExclusive, int[] buffer)
    {
        if (rightExclusive - left <= 1)
        {
            return 0;
        }

        var mid = left + ((rightExclusive - left) >> 1);
        var inversions = MergeCount(left, mid, buffer);
        inversions += MergeCount(mid, rightExclusive, buffer);

        MarkRange(left, rightExclusive - 1);

        var i = left;
        var j = mid;
        var k = left;

        while (i < mid && j < rightExclusive)
        {
            if (Compare(i, j) <= 0)
            {
                buffer[k++] = Read(i++);
            }
            else
            {
                buffer[k++] = Read(j++);
                inversions += mid - i;
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

        for (var index = left; index < rightExclusive; index++)
        {
            Write(index, buffer[index]);
        }

        return inversions;
    }

    private static long MergeCountRaw(int[] arr, int left, int rightExclusive, int[] buffer)
    {
        if (rightExclusive - left <= 1)
        {
            return 0;
        }

        var mid = left + ((rightExclusive - left) >> 1);
        var inversions = MergeCountRaw(arr, left, mid, buffer);
        inversions += MergeCountRaw(arr, mid, rightExclusive, buffer);

        var i = left;
        var j = mid;
        var k = left;

        while (i < mid && j < rightExclusive)
        {
            if (arr[i] <= arr[j])
            {
                buffer[k++] = arr[i++];
            }
            else
            {
                buffer[k++] = arr[j++];
                inversions += mid - i;
            }
        }

        while (i < mid)
        {
            buffer[k++] = arr[i++];
        }

        while (j < rightExclusive)
        {
            buffer[k++] = arr[j++];
        }

        for (var index = left; index < rightExclusive; index++)
        {
            arr[index] = buffer[index];
        }

        return inversions;
    }
}
