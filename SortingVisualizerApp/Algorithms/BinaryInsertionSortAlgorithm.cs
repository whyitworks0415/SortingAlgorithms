namespace SortingVisualizerApp.Algorithms;

public sealed class BinaryInsertionSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        for (var i = 1; i < Length; i++)
        {
            var key = Read(i);
            MarkPivotValue(i, key);

            var left = 0;
            var right = i;
            while (left < right)
            {
                var mid = left + ((right - left) / 2);
                Compare(mid, i);

                if (Read(mid) <= key)
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
        }
    }
}
