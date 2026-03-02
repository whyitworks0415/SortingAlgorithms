using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ThreeWayQuickSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        SortRange(0, Length - 1);
    }

    private void SortRange(int left, int right)
    {
        if (left >= right)
        {
            return;
        }

        MarkRange(left, right);
        MarkPivot(left);

        var pivotValue = Read(left);
        var lt = left;
        var i = left + 1;
        var gt = right;

        while (i <= gt)
        {
            Compare(i, left);
            var current = Read(i);

            if (current < pivotValue)
            {
                Swap(lt, i);
                lt++;
                i++;
            }
            else if (current > pivotValue)
            {
                Swap(i, gt);
                gt--;
            }
            else
            {
                i++;
            }
        }

        var leftSize = lt - left;
        var rightSize = right - gt;
        var quality = Math.Min(leftSize, rightSize) / (double)Math.Max(1, Math.Max(leftSize, rightSize));
        EmitEvent(SortEventType.PartitionInfo, left, right, (int)Math.Round(Math.Clamp(quality, 0.0, 1.0) * 1000.0), lt);
        if (quality < 0.2)
        {
            EmitEvent(SortEventType.BadPartition, lt, left, leftSize, rightSize);
        }

        SortRange(left, lt - 1);
        SortRange(gt + 1, right);
    }
}

