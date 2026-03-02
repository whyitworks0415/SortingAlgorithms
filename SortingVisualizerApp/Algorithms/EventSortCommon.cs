namespace SortingVisualizerApp.Algorithms;

internal static class EventSortCommon
{
    public static int LomutoPartition(EventSortAlgorithmBase algorithm, int left, int right)
    {
        algorithm.MarkRange(left, right);
        algorithm.MarkPivot(right);

        var pivotValue = algorithm.Read(right);
        var i = left;

        for (var j = left; j < right; j++)
        {
            algorithm.Compare(j, right);
            if (algorithm.Read(j) <= pivotValue)
            {
                algorithm.Swap(i, j);
                i++;
            }
        }

        algorithm.Swap(i, right);
        return i;
    }

    public static void MergeRange(EventSortAlgorithmBase algorithm, int[] buffer, int left, int mid, int rightExclusive)
    {
        algorithm.MarkRange(left, rightExclusive - 1);

        var i = left;
        var j = mid;
        var k = left;

        while (i < mid && j < rightExclusive)
        {
            if (algorithm.Compare(i, j) <= 0)
            {
                buffer[k++] = algorithm.Read(i++);
            }
            else
            {
                buffer[k++] = algorithm.Read(j++);
            }
        }

        while (i < mid)
        {
            buffer[k++] = algorithm.Read(i++);
        }

        while (j < rightExclusive)
        {
            buffer[k++] = algorithm.Read(j++);
        }

        for (var idx = left; idx < rightExclusive; idx++)
        {
            algorithm.Write(idx, buffer[idx]);
        }
    }
}
