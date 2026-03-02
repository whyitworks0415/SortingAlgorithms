using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class IntroSortExpandedAlgorithm : EventSortAlgorithmBase
{
    private const int InsertionThreshold = 16;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var depthLimit = 2 * (int)Math.Floor(Math.Log2(Math.Max(2, Length)));
        IntroSort(0, Length - 1, depthLimit);
    }

    private void IntroSort(int left, int right, int depthLimit)
    {
        while (right - left > InsertionThreshold)
        {
            if (depthLimit == 0)
            {
                HeapSortRange(left, right);
                return;
            }

            depthLimit--;
            var pivot = Partition(left, right);
            var leftSize = pivot - left;
            var rightSize = right - pivot;
            var quality = Math.Min(leftSize, rightSize) / (double)Math.Max(1, Math.Max(leftSize, rightSize));
            EmitEvent(SortEventType.PartitionInfo, left, right, (int)Math.Round(Math.Clamp(quality, 0.0, 1.0) * 1000.0), pivot);
            if (quality < 0.2)
            {
                EmitEvent(SortEventType.BadPartition, pivot, left, leftSize, rightSize);
            }

            IntroSort(pivot + 1, right, depthLimit);
            right = pivot - 1;
        }

        InsertionSortRange(left, right);
    }

    private int Partition(int left, int right)
    {
        return EventSortCommon.LomutoPartition(this, left, right);
    }

    private void InsertionSortRange(int left, int right)
    {
        for (var i = left + 1; i <= right; i++)
        {
            var key = Read(i);
            MarkPivotValue(i, key);

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

    private void HeapSortRange(int left, int right)
    {
        var count = right - left + 1;
        if (count <= 1)
        {
            return;
        }

        for (var i = (count / 2) - 1; i >= 0; i--)
        {
            SiftDown(left, i, count);
        }

        for (var end = count - 1; end > 0; end--)
        {
            Swap(left, left + end);
            SiftDown(left, 0, end);
        }
    }

    private void SiftDown(int offset, int root, int count)
    {
        while (true)
        {
            var child = (root * 2) + 1;
            if (child >= count)
            {
                return;
            }

            var swapIndex = child;
            if (child + 1 < count)
            {
                if (Compare(offset + child, offset + child + 1) < 0)
                {
                    swapIndex = child + 1;
                }
            }

            if (Compare(offset + root, offset + swapIndex) >= 0)
            {
                return;
            }

            Swap(offset + root, offset + swapIndex);
            root = swapIndex;
        }
    }
}

