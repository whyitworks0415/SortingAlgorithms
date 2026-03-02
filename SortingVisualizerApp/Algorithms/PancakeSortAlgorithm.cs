namespace SortingVisualizerApp.Algorithms;

public sealed class PancakeSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        for (var currSize = Length; currSize > 1; currSize--)
        {
            MarkRange(0, currSize - 1);

            var maxIndex = 0;
            for (var i = 1; i < currSize; i++)
            {
                if (Compare(i, maxIndex) > 0)
                {
                    maxIndex = i;
                }
            }

            if (maxIndex == currSize - 1)
            {
                continue;
            }

            if (maxIndex > 0)
            {
                Flip(maxIndex);
            }

            Flip(currSize - 1);
        }
    }

    private void Flip(int end)
    {
        var start = 0;
        while (start < end)
        {
            Swap(start, end);
            start++;
            end--;
        }
    }
}
