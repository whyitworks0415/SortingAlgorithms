using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BlockSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        var n = Length;
        if (n <= 1)
        {
            return;
        }

        var blockSize = Math.Clamp((int)Math.Sqrt(n), 16, 256);
        EmitEvent(SortEventType.MarkStage, value: 8401, aux: blockSize); // run/block init

        for (var start = 0; start < n; start += blockSize)
        {
            var end = Math.Min(n - 1, start + blockSize - 1);
            EmitEvent(SortEventType.MarkRange, start, end, value: 84011, aux: blockSize); // block sort range
            InsertionSortRange(start, end);
        }

        var buffer = new int[n];
        for (var width = blockSize; width < n; width <<= 1)
        {
            EmitEvent(SortEventType.MarkStage, value: 8402, aux: width); // block merge pass
            for (var left = 0; left < n; left += width << 1)
            {
                var mid = Math.Min(left + width, n);
                var right = Math.Min(left + (width << 1), n);
                if (mid >= right)
                {
                    continue;
                }

                EmitEvent(SortEventType.MarkRange, left, right - 1, value: 84021, aux: width); // merge range
                Merge(left, mid, right, buffer);
            }
        }
    }

    private void InsertionSortRange(int start, int end)
    {
        for (var i = start + 1; i <= end; i++)
        {
            var j = i;
            while (j > start && Compare(j - 1, j) > 0)
            {
                Swap(j - 1, j);
                j--;
            }
        }
    }

    private void Merge(int left, int mid, int right, int[] buffer)
    {
        var i = left;
        var j = mid;
        var k = left;

        while (i < mid && j < right)
        {
            if (Compare(i, j) <= 0)
            {
                buffer[k++] = Read(i++);
            }
            else
            {
                buffer[k++] = Read(j++);
            }
        }

        while (i < mid)
        {
            buffer[k++] = Read(i++);
        }

        while (j < right)
        {
            buffer[k++] = Read(j++);
        }

        for (var index = left; index < right; index++)
        {
            Write(index, buffer[index]);
        }
    }
}
