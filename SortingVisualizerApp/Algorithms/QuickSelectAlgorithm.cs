using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class QuickSelectAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var k = Length / 2;
        var selected = SelectInPlace(0, Length - 1, k);
        MarkPivotValue(k, selected);
        MarkBucket(k, 1, selected);

        QuickSortRange(0, Length - 1);
    }

    public static int SelectKth(int[] values, int k)
    {
        if (values.Length == 0)
        {
            return 0;
        }

        var copy = values.ToArray();
        k = Math.Clamp(k, 0, copy.Length - 1);
        var left = 0;
        var right = copy.Length - 1;

        while (left <= right)
        {
            var pivot = PartitionRaw(copy, left, right);
            if (pivot == k)
            {
                return copy[pivot];
            }

            if (pivot < k)
            {
                left = pivot + 1;
            }
            else
            {
                right = pivot - 1;
            }
        }

        return copy[Math.Clamp(k, 0, copy.Length - 1)];
    }

    private int SelectInPlace(int left, int right, int k)
    {
        while (left <= right)
        {
            var pivot = EventSortCommon.LomutoPartition(this, left, right);
            if (pivot == k)
            {
                return Read(pivot);
            }

            if (pivot < k)
            {
                left = pivot + 1;
            }
            else
            {
                right = pivot - 1;
            }
        }

        return Read(Math.Clamp(k, 0, Length - 1));
    }

    private void QuickSortRange(int left, int right)
    {
        if (left >= right)
        {
            return;
        }

        var pivot = EventSortCommon.LomutoPartition(this, left, right);
        QuickSortRange(left, pivot - 1);
        QuickSortRange(pivot + 1, right);
    }

    private static int PartitionRaw(int[] data, int left, int right)
    {
        var pivot = data[right];
        var store = left;
        for (var i = left; i < right; i++)
        {
            if (data[i] <= pivot)
            {
                (data[store], data[i]) = (data[i], data[store]);
                store++;
            }
        }

        (data[store], data[right]) = (data[right], data[store]);
        return store;
    }
}
