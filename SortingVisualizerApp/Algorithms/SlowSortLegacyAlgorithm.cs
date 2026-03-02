namespace SortingVisualizerApp.Algorithms;

public sealed class SlowSortLegacyAlgorithm : EventSortAlgorithmBase
{
    private const int NativeLimit = 48;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        if (Length <= NativeLimit)
        {
            SlowSort(0, Length - 1);
            return;
        }

        // Large-N safety fallback for legacy entry.
        MarkBucket(0, 1, Length);
        QuickSortRange(0, Length - 1);
    }

    private void SlowSort(int left, int right)
    {
        if (left >= right)
        {
            return;
        }

        MarkRange(left, right);
        var mid = left + ((right - left) >> 1);
        SlowSort(left, mid);
        SlowSort(mid + 1, right);

        if (Compare(mid, right) > 0)
        {
            Swap(mid, right);
        }

        SlowSort(left, right - 1);
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
