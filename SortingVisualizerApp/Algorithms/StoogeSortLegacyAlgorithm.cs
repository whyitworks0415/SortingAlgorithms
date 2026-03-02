namespace SortingVisualizerApp.Algorithms;

public sealed class StoogeSortLegacyAlgorithm : EventSortAlgorithmBase
{
    private const int NativeLimit = 24;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        if (Length <= NativeLimit)
        {
            Stooge(0, Length - 1);
            return;
        }

        // Large-N safety fallback for legacy entry.
        MarkBucket(0, 0, Length);
        QuickSortRange(0, Length - 1);
    }

    private void Stooge(int left, int right)
    {
        MarkRange(left, right);
        if (Compare(left, right) > 0)
        {
            Swap(left, right);
        }

        if (right - left + 1 <= 2)
        {
            return;
        }

        var third = (right - left + 1) / 3;
        Stooge(left, right - third);
        Stooge(left + third, right);
        Stooge(left, right - third);
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
