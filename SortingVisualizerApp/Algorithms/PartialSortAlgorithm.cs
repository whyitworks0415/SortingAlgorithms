using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class PartialSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var k = Math.Clamp(Length / 4, 1, Length);
        PartitionTopK(k);
        MarkRange(0, k - 1);
        InsertionSortRange(0, k - 1);

        // Keep bars-suite compatibility by returning fully sorted state.
        QuickSortRange(0, Length - 1);
    }

    public static int[] PartialTopK(int[] values, int k)
    {
        if (values.Length == 0)
        {
            return Array.Empty<int>();
        }

        var copy = values.ToArray();
        k = Math.Clamp(k, 0, copy.Length);
        if (k == 0)
        {
            return Array.Empty<int>();
        }

        Array.Sort(copy);
        var result = new int[k];
        Array.Copy(copy, result, k);
        return result;
    }

    private void PartitionTopK(int k)
    {
        var left = 0;
        var right = Length - 1;
        var target = k - 1;

        while (left <= right)
        {
            var pivot = EventSortCommon.LomutoPartition(this, left, right);
            if (pivot == target)
            {
                return;
            }

            if (pivot < target)
            {
                left = pivot + 1;
            }
            else
            {
                right = pivot - 1;
            }
        }
    }

    private void InsertionSortRange(int left, int right)
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
}
