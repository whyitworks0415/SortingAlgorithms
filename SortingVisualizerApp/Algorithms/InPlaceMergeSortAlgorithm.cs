using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class InPlaceMergeSortAlgorithm : EventSortAlgorithmBase
{
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
        if (left >= right)
        {
            return;
        }

        var mid = left + ((right - left) >> 1);
        SortRange(left, mid);
        SortRange(mid + 1, right);
        MergeInPlace(left, mid, right);
    }

    private void MergeInPlace(int left, int mid, int right)
    {
        EmitEvent(SortEventType.MarkStage, value: 3000 + Math.Min(999, right - left + 1));

        // merge range
        MarkRange(left, right);

        // left and right ranges
        EmitEvent(SortEventType.MarkStage, value: 3101);
        MarkRange(left, mid);
        EmitEvent(SortEventType.MarkStage, value: 3102);
        MarkRange(mid + 1, right);

        if (Compare(mid, mid + 1) <= 0)
        {
            return;
        }

        for (var gap = NextGap(right - left + 1); gap > 0; gap = NextGap(gap))
        {
            EmitEvent(SortEventType.MarkStage, value: 3200 + Math.Min(255, gap));

            var i = left;
            var j = left + gap;
            while (j <= right)
            {
                if (Compare(i, j) > 0)
                {
                    Swap(i, j);
                }

                i++;
                j++;
            }
        }
    }

    private static int NextGap(int gap)
    {
        if (gap <= 1)
        {
            return 0;
        }

        return (gap >> 1) + (gap & 1);
    }
}
