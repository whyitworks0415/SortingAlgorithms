using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class PdqSortAlgorithm : EventSortAlgorithmBase
{
    private const int InsertionThreshold = 24;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var badPartitionBudget = 2 * (int)Math.Log2(Math.Max(2, Length));
        SortRange(0, Length - 1, badPartitionBudget);
    }

    private void SortRange(int left, int right, int badPartitionBudget)
    {
        while (right - left + 1 > InsertionThreshold)
        {
            if (badPartitionBudget <= 0)
            {
                HeapSortRange(left, right);
                return;
            }

            MarkRange(left, right);
            var pivotIndex = ChoosePivotMedianOf3(left, right);
            MarkPivot(pivotIndex);
            Swap(left, pivotIndex);

            var pivot = Read(left);
            var i = left + 1;
            var j = right;
            var alreadyPartitioned = true;

            while (true)
            {
                while (i <= right)
                {
                    Compare(i, left);
                    if (Read(i) >= pivot)
                    {
                        break;
                    }

                    i++;
                }

                while (j >= left + 1)
                {
                    Compare(j, left);
                    if (Read(j) <= pivot)
                    {
                        break;
                    }

                    j--;
                }

                if (i > j)
                {
                    break;
                }

                if (i != j)
                {
                    Swap(i, j);
                    alreadyPartitioned = false;
                }

                i++;
                j--;
            }

            Swap(left, j);

            var leftSize = j - left;
            var rightSize = right - j;
            var partitionSize = right - left + 1;
            var quality = Math.Min(leftSize, rightSize) / (double)Math.Max(1, Math.Max(leftSize, rightSize));
            EmitEvent(SortEventType.PartitionInfo, left, right, (int)Math.Round(Math.Clamp(quality, 0.0, 1.0) * 1000.0), j);
            var badPartition = Math.Min(leftSize, rightSize) < partitionSize / 8;
            if (badPartition)
            {
                badPartitionBudget--;
                MarkBucket(j, bucket: 0, value: Math.Max(leftSize, rightSize));
                EmitEvent(SortEventType.BadPartition, j, left, leftSize, rightSize);
            }
            else if (alreadyPartitioned)
            {
                InsertionSortRange(left, right);
                return;
            }

            if (leftSize < rightSize)
            {
                SortRange(left, j - 1, badPartitionBudget);
                left = j + 1;
            }
            else
            {
                SortRange(j + 1, right, badPartitionBudget);
                right = j - 1;
            }
        }

        InsertionSortRange(left, right);
    }

    private int ChoosePivotMedianOf3(int left, int right)
    {
        var mid = left + ((right - left) >> 1);

        if (Compare(left, mid) > 0)
        {
            Swap(left, mid);
        }

        if (Compare(mid, right) > 0)
        {
            Swap(mid, right);
        }

        if (Compare(left, mid) > 0)
        {
            Swap(left, mid);
        }

        return mid;
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
            var child = root * 2 + 1;
            if (child >= count)
            {
                return;
            }

            var best = child;
            if (child + 1 < count && Compare(offset + child, offset + child + 1) < 0)
            {
                best = child + 1;
            }

            if (Compare(offset + root, offset + best) >= 0)
            {
                return;
            }

            Swap(offset + root, offset + best);
            root = best;
        }
    }
}

