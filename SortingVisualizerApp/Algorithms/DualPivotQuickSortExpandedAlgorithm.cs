using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class DualPivotQuickSortExpandedAlgorithm : EventSortAlgorithmBase
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

        if (Compare(left, right) > 0)
        {
            Swap(left, right);
        }

        var p = Read(left);
        var q = Read(right);
        MarkPivot(left);
        MarkPivot(right);

        var l = left + 1;
        var g = right - 1;
        var k = l;

        while (k <= g)
        {
            Compare(k, left);
            if (Read(k) < p)
            {
                Swap(k, l);
                l++;
                k++;
                continue;
            }

            Compare(k, right);
            if (Read(k) >= q)
            {
                while (k < g)
                {
                    Compare(g, right);
                    if (Read(g) <= q)
                    {
                        break;
                    }

                    g--;
                }

                Swap(k, g);
                g--;

                Compare(k, left);
                if (Read(k) < p)
                {
                    Swap(k, l);
                    l++;
                }
            }

            k++;
        }

        l--;
        g++;

        Swap(left, l);
        Swap(right, g);

        var leftSize = l - left;
        var middleSize = g - l - 1;
        var rightSize = right - g;
        var smaller = Math.Min(leftSize, Math.Min(middleSize, rightSize));
        var larger = Math.Max(leftSize, Math.Max(middleSize, rightSize));
        var quality = smaller / (double)Math.Max(1, larger);
        EmitEvent(SortEventType.PartitionInfo, left, right, (int)Math.Round(Math.Clamp(quality, 0.0, 1.0) * 1000.0), l);
        if (quality < 0.15)
        {
            EmitEvent(SortEventType.BadPartition, l, g, leftSize, rightSize);
        }

        SortRange(left, l - 1);
        SortRange(l + 1, g - 1);
        SortRange(g + 1, right);
    }
}

