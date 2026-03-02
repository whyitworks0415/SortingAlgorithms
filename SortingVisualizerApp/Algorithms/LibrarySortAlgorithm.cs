namespace SortingVisualizerApp.Algorithms;

public sealed class LibrarySortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        for (var i = 1; i < Length; i++)
        {
            var key = Read(i);
            var left = 0;
            var right = i;

            while (left < right)
            {
                var mid = left + ((right - left) >> 1);
                if (Compare(mid, i) <= 0)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid;
                }
            }

            for (var j = i; j > left; j--)
            {
                Write(j, Read(j - 1));
            }

            Write(left, key);

            if ((i & 31) == 0)
            {
                MarkRange(0, i);
                MarkBucket(i, i / 32, key);
            }
        }
    }
}
