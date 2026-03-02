namespace SortingVisualizerApp.Algorithms;

public sealed class DoubleSelectionSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        var left = 0;
        var right = Length - 1;

        while (left < right)
        {
            MarkRange(left, right);

            var minIndex = left;
            var maxIndex = left;

            for (var i = left + 1; i <= right; i++)
            {
                if (Compare(i, minIndex) < 0)
                {
                    minIndex = i;
                }

                if (Compare(i, maxIndex) > 0)
                {
                    maxIndex = i;
                }
            }

            Swap(left, minIndex);

            if (maxIndex == left)
            {
                maxIndex = minIndex;
            }

            Swap(right, maxIndex);

            left++;
            right--;
        }
    }
}
